using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EVE.ISXEVE;
using EVE.ISXEVE.Interfaces;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    public sealed class MeCache : ModuleBase, IMeCache
    {
		//Me reference for accessing EVE information
        public Me Me { get; private set; }

		//Cached data from the Me object
        public string Name { get; private set; }
        public string CorporationTicker { get; private set; }
        public string Corporation { get; private set; }
        public string AllianceTicker { get; private set; }
        public DateTime ActiveTargetActivatedAt { get; private set; }
        public string Alliance { get; private set; }
        public long StationId { get; private set; }
        public int SolarSystemId { get; private set; }
        public int AllianceId { get; private set; }
        public Int64 CharId { get; private set; }
        public Int64 ShipId { get; private set; }
        public Int64 ActiveTargetId { get; private set; }
        public Int64 CorporationId { get; private set; }
        public double DroneControlDistance { get; private set; }
        public double MaxLockedTargets { get; private set; }
        public double MaxActiveDrones { get; private set; }
        public double WalletBalance { get; private set; } //Double.MinValue
        public bool InSpace { get; private set; }
        public bool InStation { get; private set; }
        public bool AutoPilotEnabled { get; private set; }
        public bool IsFleetInvited { get; private set; }

        //Track/cache my active target
        public IEntityWrapper ActiveTarget { get; private set; }

        //Un-cached (ISXEVE-live) object lists
        public EVETime EveTime { get; private set; }

		private readonly List<FleetMember> _fleetMembers = new List<FleetMember>();
		private readonly Dictionary<long, FleetMember> _fleetMembersById = new Dictionary<long, FleetMember>();
		private readonly List<Attacker> _attackers = new List<Attacker>();
		private readonly Dictionary<long, Attacker> _attackersById = new Dictionary<long, Attacker>();
		private readonly List<AgentMission> _agentMissions = new List<AgentMission>();
		private readonly List<Being> _buddies = new List<Being>();
		private readonly List<ActiveDrone> _activeDrones = new List<ActiveDrone>();
        private readonly List<IItem> _hangarItems = new List<IItem>();
        private readonly List<IItem> _hangarShips = new List<IItem>();

		//Cached (Non-ISXEVE) object lists
        private readonly List<IEntityWrapper> _targets = new List<IEntityWrapper>(); 
        public ReadOnlyCollection<IEntityWrapper> Targets { get { return _targets.AsReadOnly(); } }

        private readonly List<IEntityWrapper> _targetedBy = new List<IEntityWrapper>();
        public ReadOnlyCollection<IEntityWrapper> TargetedBy { get { return _targetedBy.AsReadOnly(); } }

		//cache game hour and minute
        public int GameHour { get; private set; }
        public int GameMinute { get; private set; }
        public bool IsDowntimeNear { get; private set; }
        public bool IsDowntimeImminent { get; private set; }

        public IMeToEntityCache ToEntity { get; private set; }
        public IShipCache Ship { get; private set; }

		private bool _buddyListOpened, _buddyListClosed;

        private readonly IIsxeveProvider _isxeveProvider;
        private readonly IEveWindowProvider _eveWindowProvider;

        internal MeCache(IIsxeveProvider isxeveProvider, IEveWindowProvider eveWindowProvider)
        {
            _isxeveProvider = isxeveProvider;
            _eveWindowProvider = eveWindowProvider;

            ActiveTargetActivatedAt = DateTime.MinValue;

            PulseFrequency = 1;
            ModuleManager.CachesToPulse.Add(this);
            ToEntity = new MeToEntityCache();
            Ship = new ShipCache();
			base.ModuleName = "MeCache";
        }

		public bool CheckValidity()
		{
			var methodName = "CheckValidity";
			LogTrace(methodName);

			if (LavishScriptObject.IsNullOrInvalid(Me))
			{
				Me = new Me();

				if (LavishScriptObject.IsNullOrInvalid(Me))
				{
                    LogMessage(methodName, LogSeverityTypes.Standard, "\"Me\" instance null or invalid.");
					return false;
				}
			}

			InSpace = Me.InSpace;
			InStation = Me.InStation;

			//If neither are valid we have a problem.
			return (InSpace || InStation) && (!InSpace || !InStation);
		}

		public override void CriticalPulse()
		{
			var methodName = "CriticalPulse";
			LogTrace(methodName);

			//StartMethodProfiling("InSpace");
		    Me = new Me();

            if (LavishScriptObject.IsNullOrInvalid(Me))
                return;

			StationId = Me.StationID;
			SolarSystemId = Me.SolarSystemID;
            AutoPilotEnabled = Me.AutoPilotOn;
            //EndMethodProfiling();
        }

        public override void Pulse()
        {
            var methodName = "Pulse";
			LogTrace(methodName);

        	if (!ShouldPulse()) 
				return;

            if (LavishScriptObject.IsNullOrInvalid(Me))
                return;

        	StartPulseProfiling();

            EveTime = new EVETime();

        	GameHour = GetGameHour();
        	GameMinute = GetGameMinute();
        	IsDowntimeNear = GetIsDowntimeNear();
        	IsDowntimeImminent = GetIsDowntimeImminent();

        	WalletBalance = Me.Wallet.Balance;

        	//StartMethodProfiling("Name");
        	Name = Me.Name;
        	IsFleetInvited = Me.Fleet.Invited;
        	//EndMethodProfiling();

        	//StartMethodProfiling("Fleet");
        	if (Me.Fleet.ID > -1)
        	{
        		var fleetMembers = Me.Fleet.GetMembers();
        		if (fleetMembers != null)
        		{
        			foreach (var fleetMember in
        				fleetMembers.Where(fleetMember => !_fleetMembersById.ContainsKey(fleetMember.CharID)))
        			{
        				_fleetMembersById.Add(fleetMember.CharID, fleetMember);
        				_fleetMembers.Add(fleetMember);
        			}
        			fleetMembers.Clear();
        		}
        		else
        		{
					LogMessage(methodName, LogSeverityTypes.Debug, "Me.Fleet.GetMembers returned a null list.");
        		}
        	}
        	//EndMethodProfiling();

        	//StartMethodProfiling("Corp");
            var corp = Me.Corp;
        	if (corp.ID >= 0)
        	{
        		if (corp.ID != CorporationId)
        		{
        			if (Metatron.CorporationCache.IsInitialized)
        			{
                        if (Metatron.CorporationCache.CachedCorporationsById.ContainsKey(corp.ID))
        				{
                            var corporation = Metatron.CorporationCache.CachedCorporationsById[corp.ID];
        					Corporation = corporation.Name;
        					CorporationTicker = corporation.Ticker;
                            CorporationId = corp.ID;
        				}
        				else
        				{
                            Metatron.CorporationCache.GetCorporationInfo(corp.ID);
        				}
        			}
        		}
        	}
        	else
        	{
        		CorporationId = -1;
        		Corporation = String.Empty;
        		CorporationTicker = String.Empty;
        	}
        	//EndMethodProfiling();

        	//StartMethodProfiling("Alliance");
        	if (Me.AllianceID >= 0)
        	{
        		if (Me.AllianceID != AllianceId)
        		{
        			if (Metatron.AllianceCache.IsInitialized)
        			{
        				if (Metatron.AllianceCache.CachedAlliancesById.ContainsKey(Me.AllianceID))
        				{
        					var alliance = Metatron.AllianceCache.CachedAlliancesById[Me.AllianceID];
        					Alliance = alliance.Name;
        					AllianceTicker = alliance.Ticker;
        					AllianceId = Me.AllianceID;
        				}
        				else
        				{
        					Metatron.AllianceCache.GetAllianceInfo(Me.AllianceID);
        				}
        			}
        		}
        	}
        	else
        	{
        		AllianceId = -1;
        		Alliance = String.Empty;
        		AllianceTicker = String.Empty;
        	}
        	//EndMethodProfiling();

        	//StartMethodProfiling("Drones");
        	ShipId = Me.ShipID;
        	CharId = Me.CharID;
        	MaxLockedTargets = Me.MaxLockedTargets;
        	MaxActiveDrones = Me.MaxActiveDrones;
        	DroneControlDistance = Me.DroneControlDistance;
        	//EndMethodProfiling();

        	//StartMethodProfiling("Buddies");
            using (var addressBookWindow = _eveWindowProvider.GetWindowByName("addressbook"))
			{
				if (_buddyListOpened && !_buddyListClosed && addressBookWindow.IsValid)
				{
					addressBookWindow.Close();
					_buddyListClosed = true;
				}
			}
        	if (!_buddyListOpened)
        	{
        		_buddyListOpened = true;
        		_isxeveProvider.Eve.Execute(ExecuteCommand.OpenPeopleAndPlaces);
        	    _isxeveProvider.Eve.RefreshBookmarks();
        	}

        	if (_buddyListOpened && _buddyListClosed)
        	{
                var buddies = _isxeveProvider.Eve.GetContacts();

        		if (buddies == null)
        			LogMessage(methodName, LogSeverityTypes.Debug, "EVE.GetBuddies returned a null list.");
				else
        			_buddies.AddRange(buddies);
        	}

            var agentMissions = _isxeveProvider.Eve.GetAgentMissions();

        	if (agentMissions == null)
        		LogMessage(methodName, LogSeverityTypes.Debug, "EVE.GetAgentMissions returned a null list.");
        	else
        		_agentMissions.AddRange(agentMissions);

        	//EndMethodProfiling();
        	if (!InStation && InSpace)
        	{
        		var activeDrones = Me.GetActiveDrones();

        		if (activeDrones == null)
        			LogMessage(methodName, LogSeverityTypes.Debug, "Me.GetActiveDrones returned a null list.");
				else
        			_activeDrones.AddRange(activeDrones);

        		//Pulse the entitypopulator
        		StartMethodProfiling("EntityPopulator.Pulse");
        		try
        		{
        			Metatron.EntityProvider.Pulse();
        		}
        		catch (Exception e)
        		{
        			LogException(e, "EntityPopulator Pulse", "Caught exception while pulsing EntityPopulator:");
        			return;
        		}
        		EndMethodProfiling();

        		//StartMethodProfiling("BuildTargets");
        		foreach (var entity in Metatron.EntityProvider.EntityWrappers)
        		{
        			if (entity.IsLockedTarget)
        			{
        				_targets.Add(entity);
        			}
        			if (entity.IsTargetingMe)
        			{
                        _targetedBy.Add(entity);
        			}
        		}
        		//EndMethodProfiling();

        		//StartMethodProfiling("GetActiveTarget");
        		if (!LavishScriptObject.IsNullOrInvalid(Me.ActiveTarget))
        		{
        			//ActiveTargetId = Me.ActiveTarget.ID;
                    if (ActiveTargetId != Me.ActiveTarget.ID)
                    {
                        ActiveTargetId = Me.ActiveTarget.ID;
                        ActiveTargetActivatedAt = DateTime.Now;
                    }
        		    ActiveTarget = Targets.FirstOrDefault(entity => entity.ID == ActiveTargetId);
        		}
        		else
        		{
        			ActiveTargetId = -1;
        			ActiveTarget = null;
        		}
        		//EndMethodProfiling();

        		//Get Attackers
        		var attackers = Me.GetAttackers();
        		if (attackers != null)
        		{
        			foreach (var attacker in attackers)
        			{
        				//Fucking ISXEVE unable to determine if something's valid or not.
        				//Keeps returning invalid attackers with invalid entities.
        				//Fuck. Can't ever just have something in that library *work right*
        				if (LavishScriptObject.IsNullOrInvalid(attacker))
        				{
        					continue;
        				}

        				var entityId = attacker.ID;

        				//Check for invalidity again, just to be fucking sure.
        				//Fucking ISXEVE.
        				if (entityId == -1)
        				{
        					continue;
        				}

        				//Here, another validity check. Make sure we recognize the entity it's
        				//saying is the originator.
        				//if (!Metatron.EntityPopulator.EntityWrappersByID.ContainsKey(entityID))
        				//{
        				//continue;
        				//}

        				if (_attackersById.ContainsKey(entityId))
							continue;

        				_attackersById.Add(entityId, attacker);
        				_attackers.Add(attacker);
        			}
        			attackers.Clear();
        		}
        	}

        	if (InStation && !InSpace && Me.StationID > 0)
        	{
        		if (Metatron.Ship.IsInventoryOpen && Metatron.Ship.IsInventoryReady)
        		{
        			var hangarItems = Me.GetHangarItems();
        			if (hangarItems == null)
        				LogMessage(methodName, LogSeverityTypes.Debug, "Me.GetHangarItems() returned a null list.");
					else
                        _hangarItems.AddRange(hangarItems);

                    var hangarShips = Me.GetHangarShips();
                    if (hangarShips == null)
                        LogMessage(methodName, LogSeverityTypes.Debug, "Me.GetHangarShips() returned a null list.");
                    else
                        _hangarShips.AddRange(hangarShips);
        		}
        	}

        	EndPulseProfiling();
        }

		public override void InFrameCleanup()
		{
			#region LSO cleanup

		    _agentMissions.Clear();

		    _activeDrones.Clear();

			_hangarItems.Clear();

			_fleetMembers.Clear();
			_fleetMembersById.Clear();

			_buddies.Clear();

			_attackers.Clear();
			_attackersById.Clear();

			_hangarShips.Clear();
			#endregion

			#region Other Cleanup
			_targets.Clear();
            _targetedBy.Clear();
			#endregion
		}

		private int GetGameHour()
		{
			int hour;
			Int32.TryParse(EveTime.Time.Split(':')[0], out hour);

			return hour;
		}

		private int GetGameMinute()
		{
			int minute;
			Int32.TryParse(EveTime.Time.Split(':')[1], out minute);

			return minute;
		}

		private bool GetIsDowntimeNear()
		{
			return (GameHour == 10 && GameMinute >= 30);
		}

		private bool GetIsDowntimeImminent()
		{
			return (GameHour == 10 && GameMinute >= 50);
		}

        public bool IsValid
        {
            get
            {
            	return !LavishScriptObject.IsNullOrInvalid(Me);
            }
        }

		public IEveInvWindow GetInventoryWindow()
		{
            var inventoryWindow = _eveWindowProvider.GetInventoryWindow();

			return LavishScriptObject.IsNullOrInvalid(inventoryWindow) ? null : inventoryWindow;
		}

		public bool IsInventoryOpen
		{
			get { return GetInventoryWindow() != null; }
		}

        public ReadOnlyCollection<FleetMember> FleetMembers
        {
            get { return _fleetMembers.AsReadOnly(); }
        }

        public Dictionary<Int64, FleetMember> FleetMembersById
        {
            get { return _fleetMembersById; }
        }

        public ReadOnlyCollection<Attacker> Attackers
        {
            get { return _attackers.AsReadOnly(); }
        }

        public Dictionary<Int64, Attacker> AttackersById
        {
            get { return _attackersById; }
        }

        public ReadOnlyCollection<AgentMission> AgentMissions
        {
            get { return _agentMissions.AsReadOnly(); }
        }

        public ReadOnlyCollection<Being> Buddies
        {
            get { return _buddies.AsReadOnly(); }
        }

        public ReadOnlyCollection<ActiveDrone> ActiveDrones
        {
            get { return _activeDrones.AsReadOnly(); }
        }

        public ReadOnlyCollection<IItem> HangarItems
        {
            get { return _hangarItems.AsReadOnly(); }
        }

        public ReadOnlyCollection<IItem> HangarShips
        {
            get { return _hangarShips.AsReadOnly(); }
        }

        public bool IsAtBookMark(CachedBookMark bookMark)
        {
            if (SolarSystemId != bookMark.SolarSystemId) return false;

            if (bookMark.ItemId >= 0)
            {
                return InStation && Metatron.MeCache.StationId == bookMark.ItemId;
            }

            return InSpace && DistanceTo(bookMark.X, bookMark.Y, bookMark.Z) < (int) Ranges.Warp;
        }
    }
}
