using System;
using System.Linq;
using System.Net;
using System.Speech.Synthesis.TtsEngine;
using EVE.ISXEVE;
using Metatron.Core;
using Metatron.Core.Interfaces;
using IShip = Metatron.Core.IShip;

namespace Metatron.ActionModules
{
    internal sealed class Defense : ModuleBase
    {
        internal FleeReasons FleeReason;
        internal bool IsFleeing
        {
            get { return FleeReason != FleeReasons.None; }
        }

		public Destination FleeDestination;

		private bool _isAmmoAvailable;
        private DateTime _fleeUntilTimer = DateTime.Now;

		private string _officerName = string.Empty;

        private readonly IIsxeveProvider _isxeveProvider;
        private readonly IEntityProvider _entityProvider;
        private readonly IShip _ship;
        private readonly IMeCache _meCache;
        private readonly IDefensiveConfiguration _defensiveConfiguration;
        private readonly ISocial _social;
        private readonly IDrones _drones;
        private readonly IAlerts _alerts;
        private readonly ISafespots _safespots;
        private readonly IMovement _movement;
        private readonly ITargetQueue _targetQueue;

        internal Defense(IIsxeveProvider isxeveProvider, IEntityProvider entityProvider, IShip ship, IMeCache meCache, IDefensiveConfiguration defensiveConfiguration, ISocial social, IDrones drones, IAlerts alerts, ISafespots safespots, IMovement movement, ITargetQueue targetQueue)
        {
            _isxeveProvider = isxeveProvider;
            _entityProvider = entityProvider;
            _ship = ship;
            _meCache = meCache;
            _defensiveConfiguration = defensiveConfiguration;
            _social = social;
            _drones = drones;
            _alerts = alerts;
            _safespots = safespots;
            _movement = movement;
            _targetQueue = targetQueue;

            ModuleManager.ModulesToPulse.Add(this);
			ModuleName = "Defense";
			PulseFrequency = 1;
        }

        public override void Pulse()
        {
            var methodName = "Pulse";
			LogTrace(methodName);

            if (!ShouldPulse()) return;
        	StartPulseProfiling();

        	//Gotta be in space for these and not trying to dock for these...
        	if (!_meCache.InStation && _meCache.InSpace)
        	{
                _isAmmoAvailable = !_ship.IsInventoryReady || _ship.IsAmmoAvailable;

        		if (!_ship.CloakingDeviceModules.Any() || !_ship.CloakingDeviceModules.First().IsActive)
        		{
        			ManageTank();
        		}
        	}

        	if (_meCache.ToEntity.Mode != (int)Modes.Warp)
        	{
        		SetState();
        		ProcessState();
        	}
        	EndPulseProfiling();
        }

        private void ManageTank()
        {
            var methodName = "ManageTank";
			LogTrace(methodName);

            if (ShouldUseShieldBoosters())
            {
	            if (!_ship.ActivateModuleList(_ship.ShieldBoosterModules, false))
                    return;
            }
            else
            {
                if (!_ship.DeactivateModuleList(_ship.ShieldBoosterModules, true))
                    return;
            }

            if (_meCache.Ship.ArmorPct < 95 || _defensiveConfiguration.AlwaysRunTank)
            {
                if (!_ship.ActivateModuleList(_ship.ArmorRepairerModules, false))
                {
                    return;
                }
            }
            else if (_meCache.Ship.ArmorPct >= 95 || !_defensiveConfiguration.AlwaysRunTank)
            {
                if (!_ship.DeactivateModuleList(_ship.ArmorRepairerModules, true))
                    return;
            }
            
            //Only kick on hardeners if we have a reason to
            if (_meCache.TargetedBy.Any() || _defensiveConfiguration.AlwaysRunTank)
            {
                _ship.ActivateModuleList(_ship.ActiveHardenerModules, true);
                _ship.ActivateModuleList(_ship.EccmModules, true);
            }
            else
            {
                if (!_ship.DeactivateModuleList(_ship.ActiveHardenerModules, false))
                    return;
                if (!_ship.DeactivateModuleList(_ship.DamageControlModules, false))
                    return;
                if (!_ship.DeactivateModuleList(_ship.EccmModules, false))
                    return;
            }
        }

    	private bool ShouldUseShieldBoosters()
    	{
			if (!_ship.ShieldBoosterModules.Any()) return false;

			if (_defensiveConfiguration.AlwaysShieldBoost) return true;

			var maxShieldToBoostTo = _meCache.Ship.MaxShield * 0.95;
			var boosterActivationPoint = maxShieldToBoostTo - _ship.ShieldBoosterModules.First().ShieldBonus.GetValueOrDefault(0);

    		return _meCache.Ship.Shield <= boosterActivationPoint;
    	}

    	private void SetState()
        {
            var methodName = "SetPulseState";
			LogTrace(methodName);

            if (DateTime.Now.CompareTo(_fleeUntilTimer) >= 0)
            {
                switch (FleeReason)
                {
                    case FleeReasons.LocalUnsafe:
                        //If we're fleeing because local is unsafe and it is safe, reset.
                        if (_social.IsLocalSafe)
                        {
                            ResetFleeStatus();
                        }
                        break;
                    case FleeReasons.LowArmorTank:
                        //See if we have enough armor to resume
                        if (!_meCache.InStation && _meCache.Ship.ArmorPct == 100)
                        {
                            ResetFleeStatus();
                        }
                        break;
                    case FleeReasons.LowShieldTank:
                        //See if we havae enough shield to resume
                        if (_meCache.InStation || _meCache.Ship.ShieldPct >= _defensiveConfiguration.ResumeShieldPct)
                        {
                            ResetFleeStatus();
                        }
                        break;
                    case FleeReasons.LowCapacitor:
                        //See if we have enough cap or are in station
                        if (_meCache.InStation || _meCache.Ship.CapacitorPct >= _defensiveConfiguration.ResumeCapPct)
                        {
                            ResetFleeStatus();
                        }
                        break;
                    case FleeReasons.TargetJammed:
                        //If we can target, we're good
                        if (_meCache.InStation || _meCache.MaxLockedTargets > 0)
                        {
                            ResetFleeStatus();
                        }
                        break;
					case FleeReasons.OfficerPresent:
						//If it's gone, we're set.
						if (_meCache.InStation || !IsOfficerSpawnPresent())
						{
							ResetFleeStatus();
						}
						break;
                }
            }

            //Handle downtime approaching
			if (_meCache.IsDowntimeImminent)
            {
                SetFleeStatus(FleeReasons.DowntimeNear);
                return;
            }

            //If I'm in a pod...
            if (_meCache.ToEntity.TypeId == (int)TypeIDs.Capsule)
            {
                SetFleeStatus(FleeReasons.InCapsule);
            }

            //Handle local being unsafe
            if (!_social.IsLocalSafe)
            {
                SetFleeStatus(FleeReasons.LocalUnsafe);
                return;
            }

        	if (_meCache.InStation) 
				return;

        	//Handle running on low armor/shield tank
        	if (_defensiveConfiguration.RunOnLowTank)
        	{
        		if (_meCache.Ship.ArmorPct < _defensiveConfiguration.MinimumArmorPct)
        		{
        			SetFleeStatus(FleeReasons.LowArmorTank);
        			return;
        		}
        		if (_meCache.Ship.ShieldPct < _defensiveConfiguration.MinimumShieldPct)
        		{
        			SetFleeStatus(FleeReasons.LowShieldTank);
        			return;
        		}
        	}

        	//Handle running on low drones
        	if (_defensiveConfiguration.RunOnLowDrones && _drones.TotalDrones < _defensiveConfiguration.MinimumNumDrones)
        	{
                LogMessage(methodName, LogSeverityTypes.Debug, "Fleeing because of low drones. Drones in space: {0}, Drones on ship: {1}",
                    _drones.DronesInSpace, _drones.DronesInBay);
        		SetFleeStatus(FleeReasons.LowDrones);
        		return;
        	}

        	//Handle running on targetjammed
        	if (_defensiveConfiguration.RunIfTargetJammed && _ship.MaxLockedTargets == 0)
        	{
        		SetFleeStatus(FleeReasons.TargetJammed);
        		return;
        	}

        	//Handle running on low cap
        	if (_defensiveConfiguration.RunOnLowCap && _meCache.Ship.CapacitorPct < _defensiveConfiguration.MinimumCapPct)
        	{
        		SetFleeStatus(FleeReasons.LowCapacitor);
        		return;
        	}

        	//Handle fleeing on low ammo
        	if (_defensiveConfiguration.RunOnLowAmmo && !_isAmmoAvailable)
        	{
        		SetFleeStatus(FleeReasons.LowAmmunition);
        		return;
        	}

            if (IsDreadSpawnPresent() && Core.Metatron.Config.MainConfig.ActiveBehavior != BotModes.CombatAssist)
            {
                SetFleeStatus(FleeReasons.OfficerPresent);
            }

        	//Handle an officer being present
        	if (!IsOfficerSpawnPresent())
        	{
				return;	
        	}

        	SetFleeStatus(FleeReasons.OfficerPresent);
        }

        private void ResetFleeStatus()
        {
            var methodName = "ResetFleeStatus";
        	LogTrace(methodName);

        	LogMessage(methodName, LogSeverityTypes.Debug, "Resetting flee status: {0}",
        	           FleeReason);

            //This pulses before movement so we need to remove the flee desti from the desti queue
            if (_movement.DestinationQueue.Count > 0 &&
                _movement.DestinationQueue[0] == FleeDestination)
            {
				LogMessage(methodName, LogSeverityTypes.Debug, "No longer need to flee to FleeDestination, clearing the movement queue.");
                _movement.ClearDestinations(true);
            }

            FleeReason = FleeReasons.None;
            FleeDestination = null;
        }

        private void SetFleeStatus(FleeReasons fleeReason)
        {
            var methodName = "SetFleeStatus";
        	LogTrace(methodName, "FleeReason: {0}", fleeReason);

            FleeReason = fleeReason;
            //update the fleeing timer
            UpdateFleeTimer();

            //Play an alert if necessary
            switch (fleeReason)
            {
                case FleeReasons.InCapsule:
                    LogMessage(methodName, LogSeverityTypes.Critical, "Fleeing because we're in a pod.");
                    _alerts.Fleeing("In Capsule");
                    break;
                case FleeReasons.DowntimeNear:
                    LogMessage(methodName, LogSeverityTypes.Critical, "Fleeing because downtime is imminent.");
                    _alerts.Fleeing("Downtime Near");
                    break;
                case FleeReasons.LocalUnsafe:
                    LogMessage(methodName, LogSeverityTypes.Critical, "Fleeing because local is not safe.");
                    //_alerts.Alert_Flee("Local Unsafe");
                    break;
                case FleeReasons.LowAmmunition:
                    LogMessage(methodName, LogSeverityTypes.Critical, "Fleeing because we're low on ammunition.");
                    _alerts.Fleeing("Low Ammunition");
                    break;
                case FleeReasons.LowArmorTank:
                    LogMessage(methodName, LogSeverityTypes.Critical, "Fleeing because our armor tank is low.");
                    _alerts.Fleeing("Low armor tank");
                    break;
                case FleeReasons.LowShieldTank:
                    LogMessage(methodName, LogSeverityTypes.Critical, "Fleeing because our shield tank is low.");
                    _alerts.Fleeing("Low shield tank");
                    break;
                case FleeReasons.LowCapacitor:
                    LogMessage(methodName, LogSeverityTypes.Critical, "Fleeing because we're low on capacitor.");
                    _alerts.Fleeing("Low Capacitor");
                    break;
                case FleeReasons.LowDrones:
                    LogMessage(methodName, LogSeverityTypes.Critical, "Fleeing because we're low on drones.");
                    _alerts.Fleeing("Low Drones");
                    break;
                case FleeReasons.TargetJammed:
                    LogMessage(methodName, LogSeverityTypes.Critical, "Fleeing because we're target jammed.");
                    //_alerts.Alert_Flee("Target Jammed");
                    break;
				case FleeReasons.OfficerPresent:
                    LogMessage(methodName, LogSeverityTypes.Critical, "Fleeing from officer spawn \"{0}\".", _officerName);
					_alerts.FactionSpawn(_officerName);
					break;
            }
        }

        private void ProcessState()
        {
            var methodName = "ProcessPulseState";
			LogTrace(methodName);

            //If I'm at a safespot and flee reason was DowntimeNear, save and exit
            if (FleeReason == FleeReasons.DowntimeNear && _safespots.IsSafe())
            {
            	LogMessage(methodName, LogSeverityTypes.Standard, methodName, "Downtime near and we're at a safespot. Exiting...");
                DowntimeExitAndRelaunch();
                return;
            }

            //If I'm in station just return, nothing to process
            if (_meCache.InStation) return;

            //If I need to flee, do so.
			if (!IsFleeing) return;

			if (!_meCache.ToEntity.IsWarpScrambled)
			{
				Flee();
			}
        }

        private void Flee()
        {
            var methodName = "Flee";
            LogTrace(methodName);

            //If I'm in a station, no further action required
            if (_meCache.InStation)
            {
                return;
            }
            //_ship.DeactivateModuleList(_ship.GangLinkModules, true); //Get that bastion off so we can RUN!

            //Activate any DCMs we might have onboard for extra safety:
            _ship.ActivateModuleList(_ship.DamageControlModules, true);

            //If we're moving, it's not obstacle avoidance movement, and we haven't done flee move or are moving to the wrong destination... clear movement
            var destination = _movement.DestinationQueue.FirstOrDefault();
            if (destination != null && !destination.IsObstacleAvoidanceMovement & (FleeDestination == null || destination != FleeDestination))
            {
                LogMessage(methodName, LogSeverityTypes.Standard,
                           "We have queued destinations and either no set flee destination or are moving to the wrong destination. Clearing movemenet queue.");
                _movement.ClearDestinations(true);

                if (_meCache.ToEntity.Mode != (int) Modes.Warp)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Stopping ship in an attempt to abort/prevent warp.");
                    _isxeveProvider.Eve.Execute(ExecuteCommand.CmdStopShip);
                }
            }

            //If we've already got the flee destination queued, we don't need to do a damn thing.
            if (FleeDestination != null && _movement.DestinationQueue.Contains(FleeDestination))
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Already have a flee destination...");
                return;
            }

            if (_safespots.IsSafe())
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Already at a safe location...");

                //If we're fleeing because local is unsafe and I'm at a safespot and have a cloak, activate it.
                if (FleeReason == FleeReasons.DowntimeNear || FleeReason == FleeReasons.LocalUnsafe)
                {
                    var cloakingDevice = _ship.CloakingDeviceModules.FirstOrDefault();
                    if (cloakingDevice != null && !cloakingDevice.IsActive)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "At safe location, we have a cloak, and downtime is near or local is unsafe. We should cloak.");
                        cloakingDevice.Click();
                    }
                }
            }
            else
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Not at a safe location - moving to one.");
                
                var safeDestination = _safespots.GetSafeSpot();
                FleeDestination = safeDestination;
                _movement.QueueDestination(FleeDestination);
            }
        }

        private void UpdateFleeTimer()
        {
            if (_defensiveConfiguration.WaitAfterFleeing)
            {
                _fleeUntilTimer = DateTime.Now.AddMinutes(_defensiveConfiguration.MinutesToWait);
            }
        }

        private bool IsDreadSpawnPresent()
        {
            StartMethodProfiling("Dreadnought Query");
            var matchingEntity = _entityProvider.EntityWrappers
                .FirstOrDefault(entity => entity.IsNPC && Core.Metatron.Attackers.IsDreadnoughtSpawn(entity.GroupID));
            EndMethodProfiling();

            _officerName = matchingEntity == null ? string.Empty : matchingEntity.Name;
            return matchingEntity != null;
        }

        private bool IsOfficerSpawnPresent()
		{
            StartMethodProfiling("Officer Query");
			var matchingEntity = _entityProvider.EntityWrappers
                .FirstOrDefault(entity => entity.IsNPC && Core.Metatron.Attackers.IsOfficer(entity.GroupID));
			EndMethodProfiling();

			_officerName = matchingEntity == null ? string.Empty : matchingEntity.Name;
			return matchingEntity != null;
		}

        public enum FleeReasons
        {
            None,
            LocalUnsafe,
            LowShieldTank,
            LowArmorTank,
            LowCapacitor,
            LowAmmunition,
            TargetJammed,
            DowntimeNear,
			LowDrones,
            InCapsule,
			OfficerPresent
        }

        public enum HideReasons
        {
            None,
            LocalUnsafe,
            NoAmmoAvailable,
            LowArmorTank,
            LowShieldTank,
            DowntimeNear,
			LowDrones
        }
    }
}
