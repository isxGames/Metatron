using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Linq;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    // ReSharper disable ConvertToConstant.Local
    // ReSharper disable InconsistentNaming
    internal class Attackers : ModuleBase, IAttackers
    {
		private readonly string WarpScrambleEffect = "effects.WarpScramble",
			WarpScrambleAttack = "warpScrambler",
			WebifyEffect = "effects.ModifyTargetSpeed",
			WebifyAttack = "webify",
			RemoteSensorDampAttack = "ewRemoteSensorDamp", // bad data from ISXEVE; partial match
			GenericEcmEffect = "effects.ElectronicAttributeModifyTarget", //bad data from ISXEVE; partial match
			EnergyNeutralizeAttack = "ewEnergyNeut",
			TrackingDisruptAttack = "ewTrackingDisrupt",
			LaserEffect = "effects.Laser",
			MissileEffect = "effects.MissileDeployment";

		//For checking for an increase in number of NPCs targeting me
		private readonly int _secondsToWait = 5;

		private DateTime _aggroCheckTimer = DateTime.Now.Subtract(new TimeSpan(0, 0, 5));
        private DateTime _lastPirateNpcCountResetTimer = DateTime.Now.Subtract(new TimeSpan(0, 0, 5));
		private bool _hasFullAggro;
		private int _lastPirateNpcCount;

		private bool _isUnderDangerousEwarAttack;

        public bool HaveHostilesRecentlySpawned { get; private set; }

        private readonly List<IEntityWrapper> _threatEntities = new List<IEntityWrapper>(); 
        public ReadOnlyCollection<IEntityWrapper> ThreatEntities
        {
            get { return _threatEntities.AsReadOnly(); }
        }

        private readonly Dictionary<Int64, QueueTarget> _queueTargetsByEntityId = new Dictionary<long, QueueTarget>(); 
        public Dictionary<Int64, QueueTarget> QueueTargetsByEntityId
        {
            get { return _queueTargetsByEntityId; }
        }

        private readonly IMeCache _meCache;
        private readonly IConfiguration _configuration;
        private readonly IShip _ship;
        private readonly IDrones _drones;
        private readonly IEntityProvider _entityProvider;
        private readonly IAlerts _alerts;
        private readonly IAsteroidBelts _asteroidBelts;
        private readonly IPossibleEwarNpcs _possibleEwarNpcs;
        private readonly ITargetQueue _targetQueue;
        private readonly IModuleManager _moduleManager;

		public Attackers(IMeCache meCache, IConfiguration configuration, IShip ship, IDrones drones,
            IEntityProvider entityProvider, IAlerts alerts, IAsteroidBelts asteroidBelts, IPossibleEwarNpcs possibleEwarNpcs, 
            ITargetQueue targetQueue, IModuleManager moduleManager)
		{
		    _meCache = meCache;
		    _configuration = configuration;
		    _ship = ship;
		    _drones = drones;
		    _entityProvider = entityProvider;
		    _alerts = alerts;
		    _asteroidBelts = asteroidBelts;
		    _possibleEwarNpcs = possibleEwarNpcs;
		    _targetQueue = targetQueue;
		    _moduleManager = moduleManager;

		    ModuleManager.ModulesToPulse.Add(this);
			ModuleName = "Attackers";
			IsEnabled = true;
			PulseFrequency = 1;
		}

		public override void Pulse()
		{
			if (!ShouldPulse())
				return;

			//I should really always be determinine what NPCs or players are threatening my ship.
			//I just shouldn't ACT on it unless I have to.
			if (!_meCache.InSpace || _meCache.InStation)
				return;

			//If in warp, just return
			if (_meCache.ToEntity.Mode == (int)Modes.Warp)
				return;

			//Firstly, determine which NPCs are threatening me.
			DetermineThreats();

			//Next, determine the attacks from these threats.
			DetermineThreatAttacks();

			//Determien if I've got full aggro
			CheckAggro();

			//If I'm in a non-combat mode and I have some form of weaponry, queue targets.
		    var canAddNewQueueTargets = _moduleManager.IsNonCombatMode && CalculateTotalWeapons() > 0;
		    QueueThreats(canAddNewQueueTargets);
		}

		public override bool Initialize()
		{
			IsInitialized = true;
			return IsInitialized;
		}

		public override bool OutOfFrameCleanup()
		{
			IsCleanedUpOutOfFrame = true;
			return IsCleanedUpOutOfFrame;
		}

		public override void InFrameCleanup()
		{
			_isUnderDangerousEwarAttack = false;
		}

		public bool HasFullAggro
		{
			get
			{
				return _hasFullAggro;
			}
		}

		public bool IsUnderDangerousEwarAttack
		{
			get { return _isUnderDangerousEwarAttack; }
		}

        private int CalculateTotalWeapons()
        {
            var methodName = "CalculateTotalWeapons";
            LogTrace(methodName);

            var weaponGroups = 0;
            if (!_configuration.MiningConfig.UseMiningDrones &&
                _drones.TotalDrones > 0)
                weaponGroups++;
            weaponGroups += _ship.TurretModules.Count;
            weaponGroups += _ship.LauncherModules.Count;
            return weaponGroups;
        }

		private void CheckAggro()
		{
			var methodName = "CheckAggro";
			LogTrace(methodName);

			//get the number of targets targeting me
			var numAggressingMe = _meCache.Attackers.Count;
			var droneAggroDistance = _meCache.DroneControlDistance + 10000;
			var countTargetingMeWithinAggroRange = _meCache.TargetedBy.Count(x => IsNpc(x) && x.Distance <= droneAggroDistance);
			var pirateNpcCount = _entityProvider.EntityWrappers.Count(x => IsNpc(x) && IsRatTarget(x));

			//if the # of npcs went up, we do not have full aggro
			//If I've got more entities targeting me than I do redboxing me I don't have full aggro.
			//LogMessage(methodName, LogSeverityTypes.Debug, "Previously targeting me: {0}, now targeting me: {1}",
			//    _numEntitiesLastCheck, numNpcs);

            if (pirateNpcCount > _lastPirateNpcCount)
            {
                HaveHostilesRecentlySpawned = true;
                _lastPirateNpcCountResetTimer = DateTime.Now.AddSeconds(_secondsToWait);
            }
            else
            {
                if (DateTime.Now >= _lastPirateNpcCountResetTimer)
                {
                    HaveHostilesRecentlySpawned = false;
                }
            }

			if (countTargetingMeWithinAggroRange > numAggressingMe || pirateNpcCount > _lastPirateNpcCount)
			{
				//reset the timer
				_aggroCheckTimer = DateTime.Now.AddSeconds(_secondsToWait);
				//reset haveFullAggro
				_hasFullAggro = false;
			}
			else
			{
				//If we've maintained the same aggro and timer is expired...
				if (DateTime.Now.CompareTo(_aggroCheckTimer) >= 0)
				{
					//Set haveFullAggro.
					_hasFullAggro = true;
				}
			}
			_lastPirateNpcCount = pirateNpcCount;
		}

		private void DetermineThreats()
		{
			var methodName = "DetermineThreats";
			LogTrace(methodName);

			//Start out by clearing old results.
			_threatEntities.Clear();

			//Iterate all results of TargetingMe, check for matches in the Attacker dictionary, and add them.
			foreach (var entity in
				_meCache.TargetedBy.Where(entity => IsNpc(entity) && _meCache.AttackersById.ContainsKey(entity.ID)))
			{
                _threatEntities.Add(entity);
			}

			//Firstly, iterate the queueTargets and look for with a matching Entity
			foreach (var id in _queueTargetsByEntityId.Keys.ToList())
			{
				//If there's no matching Threat_Entity, remove the queuetarget.
				var matchFound = ThreatEntities.Any(entity => entity.ID == id);

				if (!matchFound)
				{
					_queueTargetsByEntityId.Remove(id);
				}
			}

			//Core.Metatron.Logging.LogMessage(ObjectName, new Core.LogEventArgs(Core.LogSeverityTypes.Debug,
			//	"Pulse", String.Format("We have {0} NPCs targeting us.", ThreatEntities.Count)));
		}

		private void DetermineThreatAttacks()
		{
			var methodName = "DetermineThreatAttacks";
			LogTrace(methodName);

			foreach (var entity in ThreatEntities)
			{
				var attacks = _meCache.AttackersById[entity.ID].GetAttacks();

				var attackAndJamNames = attacks
					.Where(attack => !LavishScriptObject.IsNullOrInvalid(attack) && !string.IsNullOrEmpty(attack.Name))
					.Select(attack => attack.Name)
					.ToList();

				var jammer = _meCache.AttackersById[entity.ID].ToJammer;
				if (!LavishScriptObject.IsNullOrInvalid(jammer))
				{
					var jams = jammer.GetJams();
					if (jams != null)
					{
						var jamNames = jams.Where(jam => !string.IsNullOrEmpty(jam));

						attackAndJamNames.AddRange(jamNames);
					}
				}

				bool isWarpScrambling = false, isEcm = false, isWebbing = false, isNeuting = false, isRemoteSensorDampening = false, isJamming = false;

				foreach (var attack in attackAndJamNames)
				{
					//Ignore MissileEffect and LaserEffect
					if (attack == MissileEffect || attack == LaserEffect) continue;

					LogMessage(methodName, LogSeverityTypes.Debug, "Threat {0} ({1}) has attack or jam \'{2}\'",
						entity.Name, entity.ID, attack);

					isWarpScrambling |= attack == WarpScrambleEffect || attack == WarpScrambleAttack;
					isWebbing |= attack == WebifyEffect || attack == WebifyAttack;
					isNeuting |= attack == EnergyNeutralizeAttack;
					isJamming |= entity.ToEntity.IsTargetJammingMe;
					isEcm |= attack == GenericEcmEffect || attack == TrackingDisruptAttack;
					isRemoteSensorDampening |= attack == RemoteSensorDampAttack;
				}

				if (isWarpScrambling)
					_alerts.WarpDisrupted();

				if (isWarpScrambling || isWebbing)
					_isUnderDangerousEwarAttack = true;

				//Check the list of possible ewar NPCs and prioritize them as such
				if (_possibleEwarNpcs.IsInDatabase(entity.Name))
					isEcm = true;

				int priority;

				//These are ordered else-ifs to set priority in order.
				if (isWarpScrambling)
				{
					priority = (int)TargetPriorities.Kill_WarpScrambler;
				}
                else if (isJamming)
                {
                    priority = (int)TargetPriorities.Kill_TargetJammer;
                }
                else if (isWebbing)
				{
					priority = (int)TargetPriorities.Kill_Webifier;
				}
				else if (isNeuting)
				{
					priority = (int)TargetPriorities.Kill_EnergyNeutralizer;
				}
				else if (isRemoteSensorDampening)
				{
					priority = (int) TargetPriorities.Kill_RemoteSensorDampener;
				}
				else if (isEcm)
				{
					priority = (int)TargetPriorities.Kill_OtherElectronicWarfare;
				}
				else
				{
					//Try to get ther priority of the target
					priority = GetTargetPriority(entity);
				}

				var queueTarget = new QueueTarget(
					entity.ID, priority, _targetQueue.GetTargetSubpriority(entity.ID), TargetTypes.Kill, _targetQueue.GetTrackingScore(entity.ID));

				if (_queueTargetsByEntityId.ContainsKey(queueTarget.Id))
				{
                    //make sure we don't accidentally "de-prioritize" a target.
					if (_queueTargetsByEntityId[queueTarget.Id].Priority > queueTarget.Priority)
					{
						_queueTargetsByEntityId[queueTarget.Id] = queueTarget;
					}
				}
				else
				{
					_queueTargetsByEntityId.Add(queueTarget.Id, queueTarget);
				}
			}
		}

		public bool IsDreadnoughtSpawn(int groupId)
		{
			switch (groupId)
			{
				case (int)GroupIDs.AsteroidAngelCartelDreadnought:
				case (int)GroupIDs.AsteroidBloodRaiderDreadnought:
				case (int)GroupIDs.AsteroidGuristasDreadnought:
				case (int)GroupIDs.AsteroidSanshasDreadnought:
				case (int)GroupIDs.AsteroidSerpentisDreadnought:
				case (int)GroupIDs.AsteroidRogueDroneCarrier:
				case (int)GroupIDs.AsteroidRogueDroneSuperCarrier:
				case (int)GroupIDs.AsteroidSanshasSuperCarrier:
				case (int)GroupIDs.AsteroidAngelCartelTitan:
				case (int)GroupIDs.AsteroidBloodRaiderTitan:
				case (int)GroupIDs.AsteroidGuristasTitan:
				case (int)GroupIDs.AsteroidSerpentisTitan:
					return true;
			}
			return false;
		}

		public bool IsNpc(IEntityWrapper entity)
		{
			if (entity.IsNPC)
				return true;

			switch (entity.GroupID)
			{
				case (int)GroupIDs.AsteroidRogueDroneBattleCruiser:
				case (int)GroupIDs.AsteroidRogueDroneBattleship:
				case (int)GroupIDs.AsteroidRogueDroneCruiser:
				case (int)GroupIDs.AsteroidRogueDroneDestroyer:
				case (int)GroupIDs.AsteroidRogueDroneFrigate:
					return true;
			}

			return false;
		}

		public int GetTargetPriority(IEntityWrapper entity)
		{
		    if (_possibleEwarNpcs.IsInDatabase(entity.Name))
		        return (int)TargetPriorities.Kill_OtherElectronicWarfare;

			if (_configuration.MainConfig.ActiveBehavior == BotModes.Ratting && _configuration.RattingConfig.IsAnomalyMode)
			{
				return GetRattingTargetPriority(entity.GroupID);
			}
			return GetMissionTargetPriority(entity.GroupID);
		}

		private int GetRattingTargetPriority(int groupId)
		{
			switch (groupId)
			{
				case (int)GroupIDs.MissionAmarrEmpireBattleship:
				case (int)GroupIDs.MissionCaldariStateBattleship:
				case (int)GroupIDs.MissionCONCORDBattleship:
				case (int)GroupIDs.MissionGallenteFederationBattleship:
				case (int)GroupIDs.MissionGenericBattleships:
				case (int)GroupIDs.MissionKhanidBattleship:
				case (int)GroupIDs.MissionMinmatarRepublicBattleship:
				case (int)GroupIDs.MissionMorduBattleship:
				case (int)GroupIDs.MissionThukkerBattleship:
				case (int)GroupIDs.DeadspaceAngelCartelBattleship:
				case (int)GroupIDs.DeadspaceBloodRaidersBattleship:
				case (int)GroupIDs.DeadspaceRogueDroneBattleship:
				case (int)GroupIDs.DeadspaceSerpentisBattleship:
				case (int)GroupIDs.DeadspaceGuristasBattleship:
				case (int)GroupIDs.AsteroidAngelCartelBattleship:
				case (int)GroupIDs.AsteroidAngelCartelCommanderBattleship:
				case (int)GroupIDs.AsteroidBloodRaidersBattleship:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderBattleship:
				case (int)GroupIDs.AsteroidGuristasBattleship:
				case (int)GroupIDs.AsteroidGuristasCommanderBattleship:
				case (int)GroupIDs.AsteroidRogueDroneBattleship:
				case (int)GroupIDs.AsteroidRogueDroneCommanderBattleship:
				case (int)GroupIDs.AsteroidSanshasNationBattleship:
				case (int)GroupIDs.AsteroidSanshasNationCommanderBattleship:
				case (int)GroupIDs.AsteroidSerpentisBattleship:
				case (int)GroupIDs.AsteroidSerpentisCommanderBattleship:
					return (int)RattingTargetPriorities.Kill_BattleShip;
				case (int)GroupIDs.DestructibleSentryGun:
					return (int)RattingTargetPriorities.Kill_DestructibleSentryGun;
				case (int)GroupIDs.MissionAmarrEmpireBattleCruiser:
				case (int)GroupIDs.MissionCaldariStateBattleCruiser:
				case (int)GroupIDs.MissionCONCORDBattleCruiser:
				case (int)GroupIDs.MissionGallenteFederationBattleCruiser:
				case (int)GroupIDs.MissionGenericBattleCruisers:
				case (int)GroupIDs.MissionKhanidBattleCruiser:
				case (int)GroupIDs.MissionMinmatarRepublicBattleCruiser:
				case (int)GroupIDs.MissionMorduBattleCruiser:
				case (int)GroupIDs.MissionThukkerBattleCruiser:
				case (int)GroupIDs.DeadspaceAngelCartelBattleCruiser:
				case (int)GroupIDs.DeadspaceBloodRaidersBattleCruiser:
				case (int)GroupIDs.DeadspaceRogueDroneBattleCruiser:
				case (int)GroupIDs.DeadspaceSerpentisBattleCruiser:
				case (int)GroupIDs.DeadspaceGuristasBattleCruiser:
				case (int)GroupIDs.AsteroidAngelCartelBattleCruiser:
				case (int)GroupIDs.AsteroidAngelCartelCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidBloodRaidersBattleCruiser:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidGuristasBattleCruiser:
				case (int)GroupIDs.AsteroidGuristasCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidRogueDroneBattleCruiser:
				case (int)GroupIDs.AsteroidRogueDroneCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidSanshasNationBattleCruiser:
				case (int)GroupIDs.AsteroidSanshasNationCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidSerpentisBattleCruiser:
				case (int)GroupIDs.AsteroidSerpentisCommanderBattleCruiser:
					return (int)RattingTargetPriorities.Kill_BattleCruiser;
				case (int)GroupIDs.MissionAmarrEmpireDestroyer:
				case (int)GroupIDs.MissionCaldariStateDestroyer:
				case (int)GroupIDs.MissionCONCORDDestroyer:
				case (int)GroupIDs.MissionGallenteFederationDestroyer:
				case (int)GroupIDs.MissionGenericDestroyers:
				case (int)GroupIDs.MissionKhanidDestroyer:
				case (int)GroupIDs.MissionMinmatarRepublicDestroyer:
				case (int)GroupIDs.MissionMorduDestroyer:
				case (int)GroupIDs.MissionThukkerDestroyer:
				case (int)GroupIDs.DeadspaceAngelCartelDestroyer:
				case (int)GroupIDs.DeadspaceBloodRaidersDestroyer:
				case (int)GroupIDs.DeadspaceRogueDroneDestroyer:
				case (int)GroupIDs.DeadspaceSerpentisDestroyer:
				case (int)GroupIDs.DeadspaceGuristasDestroyer:
				case (int)GroupIDs.AsteroidAngelCartelDestroyer:
				case (int)GroupIDs.AsteroidAngelCartelCommanderDestroyer:
				case (int)GroupIDs.AsteroidBloodRaidersDestroyer:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderDestroyer:
				case (int)GroupIDs.AsteroidGuristasDestroyer:
				case (int)GroupIDs.AsteroidGuristasCommanderDestroyer:
				case (int)GroupIDs.AsteroidRogueDroneDestroyer:
				case (int)GroupIDs.AsteroidRogueDroneCommanderDestroyer:
				case (int)GroupIDs.AsteroidSanshasNationDestroyer:
				case (int)GroupIDs.AsteroidSanshasNationCommanderDestroyer:
				case (int)GroupIDs.AsteroidSerpentisDestroyer:
				case (int)GroupIDs.AsteroidSerpentisCommanderDestroyer:
					return (int)RattingTargetPriorities.Kill_Destroyer;
				case (int)GroupIDs.MissionAmarrEmpireCruiser:
				case (int)GroupIDs.MissionCaldariStateCruiser:
				case (int)GroupIDs.MissionCONCORDCruiser:
				case (int)GroupIDs.MissionGallenteFederationCruiser:
				case (int)GroupIDs.MissionGenericCruisers:
				case (int)GroupIDs.MissionKhanidCruiser:
				case (int)GroupIDs.MissionMinmatarRepublicCruiser:
				case (int)GroupIDs.MissionMorduCruiser:
				case (int)GroupIDs.MissionThukkerCruiser:
				case (int)GroupIDs.DeadspaceAngelCartelCruiser:
				case (int)GroupIDs.DeadspaceBloodRaidersCruiser:
				case (int)GroupIDs.DeadspaceRogueDroneCruiser:
				case (int)GroupIDs.DeadspaceSerpentisCruiser:
				case (int)GroupIDs.DeadspaceGuristasCruiser:
				case (int)GroupIDs.AsteroidAngelCartelCruiser:
				case (int)GroupIDs.AsteroidAngelCartelCommanderCruiser:
				case (int)GroupIDs.AsteroidBloodRaidersCruiser:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderCruiser:
				case (int)GroupIDs.AsteroidGuristasCruiser:
				case (int)GroupIDs.AsteroidGuristasCommanderCruiser:
				case (int)GroupIDs.AsteroidRogueDroneCruiser:
				case (int)GroupIDs.AsteroidRogueDroneCommanderCruiser:
				case (int)GroupIDs.AsteroidSanshasNationCruiser:
				case (int)GroupIDs.AsteroidSanshasNationCommanderCruiser:
				case (int)GroupIDs.AsteroidSerpentisCruiser:
				case (int)GroupIDs.AsteroidSerpentisCommanderCruiser:
					return (int)RattingTargetPriorities.Kill_Cruiser;
				//Mission frigates
				case (int)GroupIDs.MissionAmarrEmpireFrigate:
				case (int)GroupIDs.MissionCaldariStateFrigate:
				case (int)GroupIDs.MissionCONCORDFrigate:
				case (int)GroupIDs.MissionGallenteFederationFrigate:
				case (int)GroupIDs.MissionGenericFrigates:
				case (int)GroupIDs.MissionKhanidFrigate:
				case (int)GroupIDs.MissionMinmatarRepublicFrigate:
				case (int)GroupIDs.MissionMorduFrigate:
				case (int)GroupIDs.MissionThukkerFrigate:
				case (int)GroupIDs.DeadspaceAngelCartelFrigate:
				case (int)GroupIDs.DeadspaceBloodRaidersFrigate:
				case (int)GroupIDs.DeadspaceRogueDroneFrigate:
				case (int)GroupIDs.DeadspaceSerpentisFrigate:
				case (int)GroupIDs.DeadspaceGuristasFrigate:
				case (int)GroupIDs.AsteroidAngelCartelFrigate:
				case (int)GroupIDs.AsteroidAngelCartelCommanderFrigate:
				case (int)GroupIDs.AsteroidBloodRaidersFrigate:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderFrigate:
				case (int)GroupIDs.AsteroidGuristasFrigate:
				case (int)GroupIDs.AsteroidGuristasCommanderFrigate:
				case (int)GroupIDs.AsteroidRogueDroneFrigate:
				case (int)GroupIDs.AsteroidRogueDroneCommanderFrigate:
				case (int)GroupIDs.AsteroidSanshasNationFrigate:
				case (int)GroupIDs.AsteroidSanshasNationCommanderFrigate:
				case (int)GroupIDs.AsteroidSerpentisFrigate:
				case (int)GroupIDs.AsteroidSerpentisCommanderFrigate:
					return (int)RattingTargetPriorities.Kill_Frigate;
				default:
					return (int)RattingTargetPriorities.Kill_Other;
			}
		}

		private int GetMissionTargetPriority(int groupId)
		{
			switch (groupId)
			{
				case (int)GroupIDs.MissionAmarrEmpireBattleship:
				case (int)GroupIDs.MissionCaldariStateBattleship:
				case (int)GroupIDs.MissionCONCORDBattleship:
				case (int)GroupIDs.MissionGallenteFederationBattleship:
				case (int)GroupIDs.MissionGenericBattleships:
				case (int)GroupIDs.MissionKhanidBattleship:
				case (int)GroupIDs.MissionMinmatarRepublicBattleship:
				case (int)GroupIDs.MissionMorduBattleship:
				case (int)GroupIDs.MissionThukkerBattleship:
				case (int)GroupIDs.DeadspaceAngelCartelBattleship:
				case (int)GroupIDs.DeadspaceBloodRaidersBattleship:
				case (int)GroupIDs.DeadspaceRogueDroneBattleship:
				case (int)GroupIDs.DeadspaceSerpentisBattleship:
				case (int)GroupIDs.DeadspaceGuristasBattleship:
				case (int)GroupIDs.AsteroidAngelCartelBattleship:
				case (int)GroupIDs.AsteroidAngelCartelCommanderBattleship:
				case (int)GroupIDs.AsteroidBloodRaidersBattleship:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderBattleship:
				case (int)GroupIDs.AsteroidGuristasBattleship:
				case (int)GroupIDs.AsteroidGuristasCommanderBattleship:
				case (int)GroupIDs.AsteroidRogueDroneBattleship:
				case (int)GroupIDs.AsteroidRogueDroneCommanderBattleship:
				case (int)GroupIDs.AsteroidSanshasNationBattleship:
				case (int)GroupIDs.AsteroidSanshasNationCommanderBattleship:
				case (int)GroupIDs.AsteroidSerpentisBattleship:
				case (int)GroupIDs.AsteroidSerpentisCommanderBattleship:
					return (int)TargetPriorities.Kill_BattleShip;
				case (int)GroupIDs.DestructibleSentryGun:
					return (int)TargetPriorities.Kill_DestructibleSentryGun;
				case (int)GroupIDs.MissionAmarrEmpireBattleCruiser:
				case (int)GroupIDs.MissionCaldariStateBattleCruiser:
				case (int)GroupIDs.MissionCONCORDBattleCruiser:
				case (int)GroupIDs.MissionGallenteFederationBattleCruiser:
				case (int)GroupIDs.MissionGenericBattleCruisers:
				case (int)GroupIDs.MissionKhanidBattleCruiser:
				case (int)GroupIDs.MissionMinmatarRepublicBattleCruiser:
				case (int)GroupIDs.MissionMorduBattleCruiser:
				case (int)GroupIDs.MissionThukkerBattleCruiser:
				case (int)GroupIDs.DeadspaceAngelCartelBattleCruiser:
				case (int)GroupIDs.DeadspaceBloodRaidersBattleCruiser:
				case (int)GroupIDs.DeadspaceRogueDroneBattleCruiser:
				case (int)GroupIDs.DeadspaceSerpentisBattleCruiser:
				case (int)GroupIDs.DeadspaceGuristasBattleCruiser:
				case (int)GroupIDs.AsteroidAngelCartelBattleCruiser:
				case (int)GroupIDs.AsteroidAngelCartelCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidBloodRaidersBattleCruiser:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidGuristasBattleCruiser:
				case (int)GroupIDs.AsteroidGuristasCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidRogueDroneBattleCruiser:
				case (int)GroupIDs.AsteroidRogueDroneCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidSanshasNationBattleCruiser:
				case (int)GroupIDs.AsteroidSanshasNationCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidSerpentisBattleCruiser:
				case (int)GroupIDs.AsteroidSerpentisCommanderBattleCruiser:
					return (int)TargetPriorities.Kill_BattleCruiser;
				case (int)GroupIDs.MissionAmarrEmpireDestroyer:
				case (int)GroupIDs.MissionCaldariStateDestroyer:
				case (int)GroupIDs.MissionCONCORDDestroyer:
				case (int)GroupIDs.MissionGallenteFederationDestroyer:
				case (int)GroupIDs.MissionGenericDestroyers:
				case (int)GroupIDs.MissionKhanidDestroyer:
				case (int)GroupIDs.MissionMinmatarRepublicDestroyer:
				case (int)GroupIDs.MissionMorduDestroyer:
				case (int)GroupIDs.MissionThukkerDestroyer:
				case (int)GroupIDs.DeadspaceAngelCartelDestroyer:
				case (int)GroupIDs.DeadspaceBloodRaidersDestroyer:
				case (int)GroupIDs.DeadspaceRogueDroneDestroyer:
				case (int)GroupIDs.DeadspaceSerpentisDestroyer:
				case (int)GroupIDs.DeadspaceGuristasDestroyer:
				case (int)GroupIDs.AsteroidAngelCartelDestroyer:
				case (int)GroupIDs.AsteroidAngelCartelCommanderDestroyer:
				case (int)GroupIDs.AsteroidBloodRaidersDestroyer:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderDestroyer:
				case (int)GroupIDs.AsteroidGuristasDestroyer:
				case (int)GroupIDs.AsteroidGuristasCommanderDestroyer:
				case (int)GroupIDs.AsteroidRogueDroneDestroyer:
				case (int)GroupIDs.AsteroidRogueDroneCommanderDestroyer:
				case (int)GroupIDs.AsteroidSanshasNationDestroyer:
				case (int)GroupIDs.AsteroidSanshasNationCommanderDestroyer:
				case (int)GroupIDs.AsteroidSerpentisDestroyer:
				case (int)GroupIDs.AsteroidSerpentisCommanderDestroyer:
					return (int)TargetPriorities.Kill_Destroyer;
				case (int)GroupIDs.MissionAmarrEmpireCruiser:
				case (int)GroupIDs.MissionCaldariStateCruiser:
				case (int)GroupIDs.MissionCONCORDCruiser:
				case (int)GroupIDs.MissionGallenteFederationCruiser:
				case (int)GroupIDs.MissionGenericCruisers:
				case (int)GroupIDs.MissionKhanidCruiser:
				case (int)GroupIDs.MissionMinmatarRepublicCruiser:
				case (int)GroupIDs.MissionMorduCruiser:
				case (int)GroupIDs.MissionThukkerCruiser:
				case (int)GroupIDs.DeadspaceAngelCartelCruiser:
				case (int)GroupIDs.DeadspaceBloodRaidersCruiser:
				case (int)GroupIDs.DeadspaceRogueDroneCruiser:
				case (int)GroupIDs.DeadspaceSerpentisCruiser:
				case (int)GroupIDs.DeadspaceGuristasCruiser:
				case (int)GroupIDs.AsteroidAngelCartelCruiser:
				case (int)GroupIDs.AsteroidAngelCartelCommanderCruiser:
				case (int)GroupIDs.AsteroidBloodRaidersCruiser:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderCruiser:
				case (int)GroupIDs.AsteroidGuristasCruiser:
				case (int)GroupIDs.AsteroidGuristasCommanderCruiser:
				case (int)GroupIDs.AsteroidRogueDroneCruiser:
				case (int)GroupIDs.AsteroidRogueDroneCommanderCruiser:
				case (int)GroupIDs.AsteroidSanshasNationCruiser:
				case (int)GroupIDs.AsteroidSanshasNationCommanderCruiser:
				case (int)GroupIDs.AsteroidSerpentisCruiser:
				case (int)GroupIDs.AsteroidSerpentisCommanderCruiser:
					return (int)TargetPriorities.Kill_Cruiser;
				//Mission frigates
				case (int)GroupIDs.MissionAmarrEmpireFrigate:
				case (int)GroupIDs.MissionCaldariStateFrigate:
				case (int)GroupIDs.MissionCONCORDFrigate:
				case (int)GroupIDs.MissionGallenteFederationFrigate:
				case (int)GroupIDs.MissionGenericFrigates:
				case (int)GroupIDs.MissionKhanidFrigate:
				case (int)GroupIDs.MissionMinmatarRepublicFrigate:
				case (int)GroupIDs.MissionMorduFrigate:
				case (int)GroupIDs.MissionThukkerFrigate:
				case (int)GroupIDs.DeadspaceAngelCartelFrigate:
				case (int)GroupIDs.DeadspaceBloodRaidersFrigate:
				case (int)GroupIDs.DeadspaceRogueDroneFrigate:
				case (int)GroupIDs.DeadspaceSerpentisFrigate:
				case (int)GroupIDs.DeadspaceGuristasFrigate:
				case (int)GroupIDs.AsteroidAngelCartelFrigate:
				case (int)GroupIDs.AsteroidAngelCartelCommanderFrigate:
				case (int)GroupIDs.AsteroidBloodRaidersFrigate:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderFrigate:
				case (int)GroupIDs.AsteroidGuristasFrigate:
				case (int)GroupIDs.AsteroidGuristasCommanderFrigate:
				case (int)GroupIDs.AsteroidRogueDroneFrigate:
				case (int)GroupIDs.AsteroidRogueDroneCommanderFrigate:
				case (int)GroupIDs.AsteroidSanshasNationFrigate:
				case (int)GroupIDs.AsteroidSanshasNationCommanderFrigate:
				case (int)GroupIDs.AsteroidSerpentisFrigate:
				case (int)GroupIDs.AsteroidSerpentisCommanderFrigate:
					return (int)TargetPriorities.Kill_Frigate;
				default:
					return (int)TargetPriorities.Kill_Other;
			}
		}

		private void QueueThreats(bool canAddQueueTargets)
		{
			var methodName = "QueueThreats";
			LogTrace(methodName);

			foreach (var queueTarget in _queueTargetsByEntityId.Values.ToList())
			{
				if (!_targetQueue.IsQueued(queueTarget.Id))
				{
					if (canAddQueueTargets)
					{
						//Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Standard,
						//methodName, string.Format("Queueing attacking NPC with ID {0}, priority {1}, sub-priority {2}, type {3}.",
						//queueTarget.ID, queueTarget.Priority, queueTarget.SubPriority, queueTarget.Type.ToString())));
						_targetQueue.EnqueueTarget(
							queueTarget.Id, queueTarget.Priority, queueTarget.Type);
					}
				}
				else
				{
					_targetQueue.EnqueueTarget(
						queueTarget.Id, queueTarget.Priority, queueTarget.Type);
				}
			}
		}

		public bool IsRatTarget(IEntityWrapper entity)
		{
			//if it's not an npc, return false.
			if (entity.IsPC || !IsNpc(entity))
			{
				return false;
			}

			//If it's a celestial, station, or asteroid return false
			switch (entity.CategoryID)
			{
				case (int)CategoryIDs.Station:
				case (int)CategoryIDs.Celestial:
				case (int)CategoryIDs.Asteroid:
				case (int)CategoryIDs.Charge:
					return false;
			}

			//If it's a collidable, jetcan, or spawn container, return false.
			switch (entity.GroupID)
			{
				case (int)GroupIDs.LargeCollidableObject:
				case (int)GroupIDs.LargeCollidableShip:
				case (int)GroupIDs.LargeCollidableStructure:
				case (int)GroupIDs.SpawnContainer:
				case (int)GroupIDs.CargoContainer:
				case (int)GroupIDs.DeadspaceOverseersStructure:
				case (int)GroupIDs.CombatDrone:
				case (int)GroupIDs.Orbital_Infrastructure:
					return false;
			}

			return true;
		}

		public bool IsConcordTarget(int groupId)
		{
			//If it's an asteroid, it's a concord target.
			if (_asteroidBelts.AsteroidGroupsByGroupId.ContainsKey(groupId))
			{
				return true;
			}

			switch (groupId)
			{
				case (int)GroupIDs.LargeCollidableObject:
				case (int)GroupIDs.LargeCollidableShip:
				case (int)GroupIDs.SentryGun:
				case (int)GroupIDs.ConcordDrone:
				case (int)GroupIDs.CustomsOfficial:
				case (int)GroupIDs.PoliceDrone:
				case (int)GroupIDs.ConvoyDrone:
				case (int)GroupIDs.FactionDrone:
				case (int)GroupIDs.Billboard:
				case (int)GroupIDs.SpawnContainer:
				case (int)GroupIDs.Orbital_Infrastructure:
					return true;
				default:
					return false;
			}
		}

		public DamageProfile GetDamageProfileFromNpc(int groupId)
		{
			switch (groupId)
			{
				case (int)GroupIDs.AsteroidGuristasBattleCruiser:
				case (int)GroupIDs.AsteroidGuristasBattleship:
				case (int)GroupIDs.AsteroidGuristasCommanderBattleship:
				case (int)GroupIDs.AsteroidGuristasCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidGuristasCommanderCruiser:
				case (int)GroupIDs.AsteroidGuristasCommanderDestroyer:
				case (int)GroupIDs.AsteroidGuristasCommanderFrigate:
				case (int)GroupIDs.AsteroidGuristasCruiser:
				case (int)GroupIDs.AsteroidGuristasDestroyer:
				case (int)GroupIDs.AsteroidGuristasFrigate:
				case (int)GroupIDs.AsteroidGuristasHauler:
				case (int)GroupIDs.AsteroidGuristasOfficer:
				case (int)GroupIDs.DeadspaceGuristasBattleCruiser:
				case (int)GroupIDs.DeadspaceGuristasBattleship:
				case (int)GroupIDs.DeadspaceGuristasCruiser:
				case (int)GroupIDs.DeadspaceGuristasDestroyer:
				case (int)GroupIDs.DeadspaceGuristasFrigate:
				case (int)GroupIDs.MissionCaldariStateBattleCruiser:
				case (int)GroupIDs.MissionCaldariStateBattleship:
				case (int)GroupIDs.MissionCaldariStateCarrier:
				case (int)GroupIDs.MissionCaldariStateCruiser:
				case (int)GroupIDs.MissionCaldariStateDestroyer:
				case (int)GroupIDs.MissionCaldariStateFrigate:
				case (int)GroupIDs.MissionCaldariStateOther:
				case (int)GroupIDs.MissionMorduBattleCruiser:
				case (int)GroupIDs.MissionMorduBattleship:
				case (int)GroupIDs.MissionMorduCruiser:
				case (int)GroupIDs.MissionMorduDestroyer:
				case (int)GroupIDs.MissionMorduFrigate:
				case (int)GroupIDs.MissionMorduOther:
					return DamageProfile.GuristasResistances;
				case (int)GroupIDs.AsteroidSerpentisBattleCruiser:
				case (int)GroupIDs.AsteroidSerpentisBattleship:
				case (int)GroupIDs.AsteroidSerpentisCommanderBattleship:
				case (int)GroupIDs.AsteroidSerpentisCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidSerpentisCommanderCruiser:
				case (int)GroupIDs.AsteroidSerpentisCommanderDestroyer:
				case (int)GroupIDs.AsteroidSerpentisCommanderFrigate:
				case (int)GroupIDs.AsteroidSerpentisCruiser:
				case (int)GroupIDs.AsteroidSerpentisDestroyer:
				case (int)GroupIDs.AsteroidSerpentisFrigate:
				case (int)GroupIDs.AsteroidSerpentisHauler:
				case (int)GroupIDs.AsteroidSerpentisOfficer:
				case (int)GroupIDs.DeadspaceSerpentisBattleCruiser:
				case (int)GroupIDs.DeadspaceSerpentisBattleship:
				case (int)GroupIDs.DeadspaceSerpentisCruiser:
				case (int)GroupIDs.DeadspaceSerpentisDestroyer:
				case (int)GroupIDs.DeadspaceSerpentisFrigate:
				case (int)GroupIDs.MissionGallenteFederationBattleCruiser:
				case (int)GroupIDs.MissionGallenteFederationBattleship:
				case (int)GroupIDs.MissionGallenteFederationCarrier:
				case (int)GroupIDs.MissionGallenteFederationCruiser:
				case (int)GroupIDs.MissionGallenteFederationDestroyer:
				case (int)GroupIDs.MissionGallenteFederationFrigate:
				case (int)GroupIDs.MissionGallenteFederationOther:
					return DamageProfile.SerpentisResistances;
				case (int)GroupIDs.AsteroidAngelCartelBattleCruiser:
				case (int)GroupIDs.AsteroidAngelCartelBattleship:
				case (int)GroupIDs.AsteroidAngelCartelCommanderBattleship:
				case (int)GroupIDs.AsteroidAngelCartelCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidAngelCartelCommanderCruiser:
				case (int)GroupIDs.AsteroidAngelCartelCommanderDestroyer:
				case (int)GroupIDs.AsteroidAngelCartelCommanderFrigate:
				case (int)GroupIDs.AsteroidAngelCartelCruiser:
				case (int)GroupIDs.AsteroidAngelCartelDestroyer:
				case (int)GroupIDs.AsteroidAngelCartelFrigate:
				case (int)GroupIDs.AsteroidAngelCartelHauler:
				case (int)GroupIDs.AsteroidAngelCartelOfficer:
				case (int)GroupIDs.DeadspaceAngelCartelBattleCruiser:
				case (int)GroupIDs.DeadspaceAngelCartelBattleship:
				case (int)GroupIDs.DeadspaceAngelCartelCruiser:
				case (int)GroupIDs.DeadspaceAngelCartelDestroyer:
				case (int)GroupIDs.DeadspaceAngelCartelFrigate:
				case (int)GroupIDs.MissionMinmatarRepublicBattleCruiser:
				case (int)GroupIDs.MissionMinmatarRepublicBattleship:
				case (int)GroupIDs.MissionMinmatarRepublicCarrier:
				case (int)GroupIDs.MissionMinmatarRepublicCruiser:
				case (int)GroupIDs.MissionMinmatarRepublicDestroyer:
				case (int)GroupIDs.MissionMinmatarRepublicFrigate:
				case (int)GroupIDs.MissionMinmatarRepublicOther:
				case (int)GroupIDs.MissionThukkerBattleCruiser:
				case (int)GroupIDs.MissionThukkerBattleship:
				case (int)GroupIDs.MissionThukkerCruiser:
				case (int)GroupIDs.MissionThukkerDestroyer:
				case (int)GroupIDs.MissionThukkerFrigate:
				case (int)GroupIDs.MissionThukkerOther:
					return DamageProfile.AngelResistances;
				case (int)GroupIDs.AsteroidSanshasNationBattleCruiser:
				case (int)GroupIDs.AsteroidSanshasNationBattleship:
				case (int)GroupIDs.AsteroidSanshasNationCommanderBattleship:
				case (int)GroupIDs.AsteroidSanshasNationCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidSanshasNationCommanderCruiser:
				case (int)GroupIDs.AsteroidSanshasNationCommanderDestroyer:
				case (int)GroupIDs.AsteroidSanshasNationCommanderFrigate:
				case (int)GroupIDs.AsteroidSanshasNationCruiser:
				case (int)GroupIDs.AsteroidSanshasNationDestroyer:
				case (int)GroupIDs.AsteroidSanshasNationFrigate:
				case (int)GroupIDs.AsteroidSanshasNationHauler:
				case (int)GroupIDs.AsteroidSanshasNationOfficer:
				case (int)GroupIDs.DeadspaceSanshasNationBattleCruiser:
				case (int)GroupIDs.DeadspaceSanshasNationBattleship:
				case (int)GroupIDs.DeadspaceSanshasNationCruiser:
				case (int)GroupIDs.DeadspaceSanshasNationDestroyer:
				case (int)GroupIDs.DeadspaceSanshasNationFrigate:
				case (int)GroupIDs.MissionAmarrEmpireBattleCruiser:
				case (int)GroupIDs.MissionAmarrEmpireBattleship:
				case (int)GroupIDs.MissionAmarrEmpireCarrier:
				case (int)GroupIDs.MissionAmarrEmpireCruiser:
				case (int)GroupIDs.MissionAmarrEmpireDestroyer:
				case (int)GroupIDs.MissionAmarrEmpireFrigate:
				case (int)GroupIDs.MissionAmarrEmpireOther:
					return DamageProfile.SanshaResistances;
				case (int)GroupIDs.AsteroidBloodRaidersBattleCruiser:
				case (int)GroupIDs.AsteroidBloodRaidersBattleship:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderBattleship:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderCruiser:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderDestroyer:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderFrigate:
				case (int)GroupIDs.AsteroidBloodRaidersCruiser:
				case (int)GroupIDs.AsteroidBloodRaidersDestroyer:
				case (int)GroupIDs.AsteroidBloodRaidersFrigate:
				case (int)GroupIDs.AsteroidBloodRaidersHauler:
				case (int)GroupIDs.AsteroidBloodRaidersOfficer:
				case (int)GroupIDs.DeadspaceBloodRaidersBattleCruiser:
				case (int)GroupIDs.DeadspaceBloodRaidersBattleship:
				case (int)GroupIDs.DeadspaceBloodRaidersCruiser:
				case (int)GroupIDs.DeadspaceBloodRaidersDestroyer:
				case (int)GroupIDs.DeadspaceBloodRaidersFrigate:
				case (int)GroupIDs.MissionKhanidBattleCruiser:
				case (int)GroupIDs.MissionKhanidBattleship:
				case (int)GroupIDs.MissionKhanidCruiser:
				case (int)GroupIDs.MissionKhanidDestroyer:
				case (int)GroupIDs.MissionKhanidFrigate:
				case (int)GroupIDs.MissionKhanidOther:
					return DamageProfile.BloodResistances;
				case (int)GroupIDs.MissionGenericBattleCruisers:
				case (int)GroupIDs.MissionGenericBattleships:
				case (int)GroupIDs.MissionGenericCruisers:
				case (int)GroupIDs.MissionGenericDestroyers:
				case (int)GroupIDs.MissionGenericFrigates:
				case (int)GroupIDs.MissionCONCORDBattleCruiser:
				case (int)GroupIDs.MissionCONCORDBattleship:
				case (int)GroupIDs.MissionCONCORDCruiser:
				case (int)GroupIDs.MissionCONCORDDestroyer:
				case (int)GroupIDs.MissionCONCORDFrigate:
					return DamageProfile.Default;
				case (int)GroupIDs.AsteroidRogueDroneBattleCruiser:
				case (int)GroupIDs.AsteroidRogueDroneBattleship:
				case (int)GroupIDs.AsteroidRogueDroneCommanderBattleship:
				case (int)GroupIDs.AsteroidRogueDroneCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidRogueDroneCommanderCruiser:
				case (int)GroupIDs.AsteroidRogueDroneCommanderDestroyer:
				case (int)GroupIDs.AsteroidRogueDroneCommanderFrigate:
				case (int)GroupIDs.AsteroidRogueDroneCruiser:
				case (int)GroupIDs.AsteroidRogueDroneDestroyer:
				case (int)GroupIDs.AsteroidRogueDroneFrigate:
				case (int)GroupIDs.AsteroidRogueDroneHauler:
				case (int)GroupIDs.AsteroidRogueDroneSwarm:
				case (int)GroupIDs.DeadspaceRogueDroneBattleCruiser:
				case (int)GroupIDs.DeadspaceRogueDroneBattleship:
				case (int)GroupIDs.DeadspaceRogueDroneCruiser:
				case (int)GroupIDs.DeadspaceRogueDroneDestroyer:
				case (int)GroupIDs.DeadspaceRogueDroneFrigate:
					return DamageProfile.DroneResistances;
			}
			return DamageProfile.Default;
		}

		public bool IsOfficer(int groupId)
		{
			switch (groupId)
			{
				case (int)GroupIDs.AsteroidAngelCartelOfficer:
				case (int)GroupIDs.AsteroidBloodRaidersOfficer:
				case (int)GroupIDs.AsteroidGuristasOfficer:
				case (int)GroupIDs.AsteroidSanshasNationOfficer:
				case (int)GroupIDs.AsteroidSerpentisOfficer:
				case (int)GroupIDs.AsteroidRogueDroneSwarm:
					return true;
			}
			return false;
		}

		public bool IsHauler(int groupId)
		{
			switch (groupId)
			{
				case (int)GroupIDs.AsteroidAngelCartelHauler:
				case (int)GroupIDs.AsteroidBloodRaidersHauler:
				case (int)GroupIDs.AsteroidGuristasHauler:
				case (int)GroupIDs.AsteroidRogueDroneHauler:
				case (int)GroupIDs.AsteroidSanshasNationHauler:
				case (int)GroupIDs.AsteroidSerpentisHauler:
					return true;
			}
			return false;
		}

		public bool IsCommander(int groupId)
		{
			switch (groupId)
			{
				case (int)GroupIDs.AsteroidAngelCartelCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidGuristasCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidRogueDroneCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidSanshasNationCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidSerpentisCommanderBattleCruiser:
				case (int)GroupIDs.AsteroidAngelCartelCommanderBattleship:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderBattleship:
				case (int)GroupIDs.AsteroidGuristasCommanderBattleship:
				case (int)GroupIDs.AsteroidRogueDroneCommanderBattleship:
				case (int)GroupIDs.AsteroidSanshasNationCommanderBattleship:
				case (int)GroupIDs.AsteroidSerpentisCommanderBattleship:
				case (int)GroupIDs.AsteroidAngelCartelCommanderCruiser:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderCruiser:
				case (int)GroupIDs.AsteroidGuristasCommanderCruiser:
				case (int)GroupIDs.AsteroidRogueDroneCommanderCruiser:
				case (int)GroupIDs.AsteroidSanshasNationCommanderCruiser:
				case (int)GroupIDs.AsteroidSerpentisCommanderCruiser:
				case (int)GroupIDs.AsteroidAngelCartelCommanderDestroyer:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderDestroyer:
				case (int)GroupIDs.AsteroidGuristasCommanderDestroyer:
				case (int)GroupIDs.AsteroidRogueDroneCommanderDestroyer:
				case (int)GroupIDs.AsteroidSanshasNationCommanderDestroyer:
				case (int)GroupIDs.AsteroidSerpentisCommanderDestroyer:
				case (int)GroupIDs.AsteroidAngelCartelCommanderFrigate:
				case (int)GroupIDs.AsteroidBloodRaidersCommanderFrigate:
				case (int)GroupIDs.AsteroidGuristasCommanderFrigate:
				case (int)GroupIDs.AsteroidRogueDroneCommanderFrigate:
				case (int)GroupIDs.AsteroidSanshasNationCommanderFrigate:
				case (int)GroupIDs.AsteroidSerpentisCommanderFrigate:
					return true;
			}
			return false;
		}

        public bool IsExhumerOrIndustrial(int groupId)
        {
            switch (groupId)
            {
                case (int)GroupIDs.Industrial:
                case (int)GroupIDs.CapitalIndustrialShip:
                case (int)GroupIDs.MiningBarge:
                case (int)GroupIDs.Exhumer:
                case (int)GroupIDs.IndustrialCommandShip:
                case (int)GroupIDs.TransportShip:
                    return true;
            }

            return false;
        }
	}
    // ReSharper restore ConvertToConstant.Local
    // ReSharper restore InconsistentNaming
}
