using System;
using System.Linq;
using EVE.ISXEVE.Interfaces;
using Metatron.Core;
using EVE.ISXEVE;
using LavishScriptAPI;
using Metatron.ActionModules;
using Metatron.BehaviorModules.PartialBehaviors;
using Metatron.Core.Config;
using Metatron.Core.EventCommunication;
using Metatron.Core.Interfaces;
using IShip = Metatron.Core.IShip;

namespace Metatron.BehaviorModules
{
    internal sealed class Freighter : BehaviorBase
    {
        private FreighterStates _freighterState = FreighterStates.Idle;
		private CorporateHangarArrayDropOffStates _corporateHangarArrayPickupState = CorporateHangarArrayDropOffStates.Idle;
        private IBookMark _pickupBookmark, _dropoffBookmark;

        //TODO: Need to create MoveToPickUpLocationPartialBehavior to finish fixing Shield-Aware pickup
		private readonly MoveToDropOffLocationPartialBehavior _moveToDropOffLocationPartialBehavior;
    	private readonly DropOffCargoPartialBehavior _dropOffCargoPartialBehavior;
        private readonly IShip _ship;
        private readonly IMeCache _meCache;
        private readonly IMovement _movement;
        private readonly IMainConfiguration _mainConfiguration;

        private readonly IEveWindowProvider _eveWindowProvider;

    	//Initialize the class
        public Freighter(IEveWindowProvider eveWindowProvider, ICargoConfiguration cargoConfiguration, IMainConfiguration mainConfiguration,
            IMiningConfiguration miningConfiguration, IMeCache meCache, IShip ship, IStation station, IJettisonContainer jettisonContainer, 
            IEntityProvider entityProvider, IEventCommunications eventCommunications, MoveToDropOffLocationPartialBehavior moveToDropOffLocationPartialBehavior, 
            DropOffCargoPartialBehavior dropOffCargoPartialBehavior, IMovement movement)
        {
            _eveWindowProvider = eveWindowProvider;
            _moveToDropOffLocationPartialBehavior = moveToDropOffLocationPartialBehavior;
            _dropOffCargoPartialBehavior = dropOffCargoPartialBehavior;
            _movement = movement;
            _ship = ship;
            _meCache = meCache;
            _mainConfiguration = mainConfiguration;

            ModuleName = "Freighter";
            PulseFrequency = 2;
            BehaviorManager.BehaviorsToPulse.Add(BotModes.Freighting, this);
            IsEnabled = true;
        }

        public override void Pulse()
        {
            var methodName = "Pulse";
			LogTrace(methodName);

            if (!ShouldPulse() || _mainConfiguration.ActiveBehavior != BotModes.Freighting || _movement.IsMoving ||
                (!_meCache.InStation && _meCache.ToEntity.Mode == (int) Modes.Warp))
                return;

            if (!_ship.IsInventoryReady) return;

            if (GetBookmarks())
            {
                SetPulseState();
                ProcessPulseState();
            }
            else
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Failed to get bookmarks.");
            }
        }

        protected override void SetPulseState()
        {
            var methodName = "SetPulseState";
			LogTrace(methodName);

            //If we're fleeing, set state to DefenceHasControl.
            if (Core.Metatron.Defense.IsFleeing)
            {
                _freighterState = FreighterStates.DefenseHasControl;
            }

            switch (_freighterState)
            {
                case FreighterStates.Idle:
                    //If I'm idle, do something.
                    if (_freighterState == FreighterStates.Idle)
                    {
                        //go to pickup if I'm not there
                        //Will have to be handled differently for the different dropoff types. The one constant is
                        //that I need to be in the right system regardless.

                        //If I'm not in the right system, the bookmark is a station I'm not in, or I'm in space and not at the bookmark, move to it.
                        if (!IsAtPickup())
                        {
                        	LogMessage(methodName, LogSeverityTypes.Debug, "Idle and not at pickup; going to pickup.");
                            _freighterState = FreighterStates.MoveToPickup;
                        }
                        else
                        {
                        	LogMessage(methodName, LogSeverityTypes.Debug, "Idle and at pickup; doing pickup.");
                            _freighterState = FreighterStates.Pickup;
                        }
                    }
                    break;
                case FreighterStates.MoveToPickup:
                    //If I should be going to pickup and I'm there, change state.
                    if (IsAtPickup())
                    {
						LogMessage(methodName, LogSeverityTypes.Debug, "MoveToPickup state and at pickup; going to Pickup state.");
                        _freighterState = FreighterStates.Pickup;
                    }
                    break;
                case FreighterStates.MoveToDropoff:
                    //If I should be going to dropoff and I'm there, change state.
                    if (IsAtDropoff())
                    {
						LogMessage(methodName, LogSeverityTypes.Debug, "MoveToDropoff state and at dropoff; going to TrackDropoff state.");
                        _freighterState = FreighterStates.Dropoff;
                    }
                    break;
                case FreighterStates.DefenseHasControl:
                    //If defence has control and nolonger fleeing, reset to idle.
                    if (!Core.Metatron.Defense.IsFleeing)
                    {
						LogMessage(methodName, LogSeverityTypes.Debug, "DefenseHasControl state and no longer fleeing; going to Idle state.");
                        _freighterState = FreighterStates.Idle;
                        goto case FreighterStates.Idle;
                    }
                    break;
                default:
                    break;
            }
        }

        protected override void ProcessPulseState()
        {
            var methodName = "ProcessPulseState";
			LogTrace(methodName);

			LogMessage(methodName, LogSeverityTypes.Debug, "Processing state: {0}",
				_freighterState);
            switch (_freighterState)
            {
                case FreighterStates.Idle:
                case FreighterStates.DefenseHasControl:
                case FreighterStates.Error:
                    //do nothing
                    break;
                case FreighterStates.MoveToPickup:
                    //Go to the pickup point
                    MoveToPickup();
                    break;
                case FreighterStates.Pickup:
                    //Do the pickup process
                    Pickup();
                    break;
                case FreighterStates.MoveToDropoff:
                    //Go to the dropoff point
                    MoveToDropoff();
                    break;
                case FreighterStates.Dropoff:
                    //Do the dropoff process
                    Dropoff();
                    break;
                default:
                    break;
            }
        }

        protected override void _processCleanupState()
        {
            throw new NotImplementedException();
        }

        protected override void _setCleanupState()
        {
            throw new NotImplementedException();
        }

        private void MoveToPickup()
        {
            var methodName = "MoveToPickup";
			LogTrace(methodName);

            switch (Core.Metatron.Config.CargoConfig.PickupLocation.LocationType)
            {
                case LocationTypes.Station:
                    //Only need to queue the bookmark 
					LogMessage(methodName, LogSeverityTypes.Debug, "Moving to bookmark {0}.",
                        Core.Metatron.Config.CargoConfig.PickupLocation.BookmarkLabel);
                    _movement.QueueDestination(new Destination(DestinationTypes.BookMark,
                        _pickupBookmark.ID) { Dock = true });
                    break;
                case LocationTypes.StationCorpHangar:
                    goto case LocationTypes.Station;
                case LocationTypes.CorpHangarArray:
                    //First queue the bookmark and then the CHA entity
					LogMessage(methodName, LogSeverityTypes.Debug, "Moving to bookmark {0}.",
                        Core.Metatron.Config.CargoConfig.PickupLocation.BookmarkLabel);
                    _movement.QueueDestination(new Destination(DestinationTypes.BookMark,
                        _pickupBookmark.ID));

					LogMessage(methodName, LogSeverityTypes.Debug, "Queueing pickup entity {0}.",
                        Core.Metatron.Config.CargoConfig.PickupLocation.EntityID);
                    _movement.QueueDestination(new Destination(DestinationTypes.Entity,
                        Core.Metatron.Config.CargoConfig.PickupLocation.EntityID) { Distance = (int)Ranges.LootActivate });
                    break;
            }
        }

        private void MoveToDropoff()
        {
            var methodName = "MoveToDropoff";
			LogTrace(methodName);

			var result = _moveToDropOffLocationPartialBehavior.Execute();

			if (result == BehaviorExecutionResults.Incomplete)
				return;

			if (result == BehaviorExecutionResults.Error)
				_freighterState = FreighterStates.Error;
        }

        private void Pickup()
        {
            var methodName = "Pickup";
			LogTrace(methodName);

            if (!_ship.IsInventoryReady)
                return;

            switch (Core.Metatron.Config.CargoConfig.PickupLocation.LocationType)
            {
                case LocationTypes.Station:
					if (!Core.Metatron.Station.IsStationHangarActive)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Making station hangar active.");
						Core.Metatron.Station.MakeStationHangarActive();
						return;
					}

                    //Move what I can from station hangar to my hangar
                    var hangarItems = _meCache.Me.GetHangarItems();
                    if (hangarItems.Count > 0)
                    {
                        _ship.TransferStationHangarToShipHangar();
                        _freighterState = FreighterStates.MoveToDropoff;
                    }
                    else
                    {
						LogMessage(methodName, LogSeverityTypes.Standard, "No items to pick up.");
                        Core.Metatron.Alerts.NothingForFreighterToPickup();
                    }
                    break;
                case LocationTypes.StationCorpHangar:
                    //If the station corp hangar isn't open; open it.
                    if (!Core.Metatron.Station.IsStationCorpHangarActive)
                    {
                        LogMessage(methodName, LogSeverityTypes.Standard, "Making station corporate hangar active.");
                        Core.Metatron.Station.MakeStationCorpHangarActive();
                        return;
                    }

                    //Move what I can from corporate hangar to station hangar
                    var corpHangarItems = _meCache.Me.GetCorpHangarItems(Core.Metatron.Config.CargoConfig.PickupLocation.HangarDivision);
                    if (corpHangarItems.Count > 0)
                    {
                        _ship.TransferStationCorpHangarToShipHangar();
                        _freighterState = FreighterStates.MoveToDropoff;
                    }
                    else
                    {
						LogMessage(methodName, LogSeverityTypes.Standard, "No items to pick up.");
                        Core.Metatron.Alerts.NothingForFreighterToPickup();
                    }
                    break;
                case LocationTypes.CorpHangarArray:
                    PickupFromCorporateHangarArray();
            		break;
            }
        }

    	private void PickupFromCorporateHangarArray()
    	{
    		var methodName = "PickupFromCorporateHangarArray";
    		LogTrace(methodName);

    		if (!IsAtPickup())
    		{
    			_freighterState = FreighterStates.MoveToPickup;
				_corporateHangarArrayPickupState = CorporateHangarArrayDropOffStates.Idle;

    			LogMessage(methodName, LogSeverityTypes.Standard, "No longer at pickup location, moving back to it.");
    		}

			LogMessage(methodName, LogSeverityTypes.Debug, "Processing state: {0}", _corporateHangarArrayPickupState);

    		var corporateHangarArrayEntity = Core.Metatron.EntityProvider.EntityWrappers.FirstOrDefault(
    			entity => entity.ID == Core.Metatron.Config.CargoConfig.PickupLocation.EntityID);

			if (corporateHangarArrayEntity == null)
			{
				LogMessage(methodName, LogSeverityTypes.Debug, "Error: Could not find entity with ID {0} for corporate hangar array pickup.",
					Core.Metatron.Config.CargoConfig.PickupLocation.EntityID);
				_freighterState = FreighterStates.Error;
				return;
			}

    	    var pickupCorporateHangarArray = new CorporateHangarArray(corporateHangarArrayEntity, _eveWindowProvider);

			switch (_corporateHangarArrayPickupState)
			{
				case CorporateHangarArrayDropOffStates.Idle:
					_corporateHangarArrayPickupState = CorporateHangarArrayDropOffStates.OpenCan;
					goto case CorporateHangarArrayDropOffStates.OpenCan;
				case CorporateHangarArrayDropOffStates.OpenCan:
					if (!pickupCorporateHangarArray.IsCurrentContainerWindowOpen)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Opening cargo of CHA {0} ({1}).",
								   pickupCorporateHangarArray.CurrentContainer.Name, pickupCorporateHangarArray.CurrentContainer.ID);
						pickupCorporateHangarArray.CurrentContainer.Open();
						return;
					}

					LogMessage(methodName, LogSeverityTypes.Standard, "Expanding the array's corporate hangars tab.");
					pickupCorporateHangarArray.InitializeCorporateHangars();

					_corporateHangarArrayPickupState = CorporateHangarArrayDropOffStates.MakeChaChildWindowActive;
					break;
				case CorporateHangarArrayDropOffStates.MakeChaChildWindowActive:
					if (!pickupCorporateHangarArray.IsCurrentContainerWindowActive)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Making CHA {0} cargo window active.",
								   pickupCorporateHangarArray.CurrentContainer.Name);
						pickupCorporateHangarArray.MakeCurrentContainerWindowActive();
						return;
					}

					_corporateHangarArrayPickupState = CorporateHangarArrayDropOffStates.UnloadCargoHold;
					break;
				case CorporateHangarArrayDropOffStates.UnloadCargoHold:
					LogMessage(methodName, LogSeverityTypes.Standard, "Transferring cargo from the pickup corp hangar array to ship cargo.");

					_ship.TransferCorporateHangarArrayToShipHangar(pickupCorporateHangarArray);

					_corporateHangarArrayPickupState = CorporateHangarArrayDropOffStates.StackCargo;
					break;
				case CorporateHangarArrayDropOffStates.StackCargo:
					//Stack my cargo and the CHA's.
					if (!_ship.IsCargoHoldActive)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Making my ship's cargo window active to stack cargo.");
						_ship.MakeCargoHoldActive();
						return;
					}

					_ship.StackInventory();

					_corporateHangarArrayPickupState = CorporateHangarArrayDropOffStates.Idle;
					_freighterState = FreighterStates.MoveToDropoff;
					break;
			}
    	}

    	private void Dropoff()
        {
            var methodName = "TrackDropoff";
			LogTrace(methodName);

            if (!_ship.IsInventoryReady)
                return;

			var result = _dropOffCargoPartialBehavior.Execute();

			if (result == BehaviorExecutionResults.Incomplete)
				return;

			if (result == BehaviorExecutionResults.Error)
				_freighterState = FreighterStates.Error;

			//RecordDropoff();

			_freighterState = FreighterStates.Idle;
        }

        private bool IsAtPickup()
        {
            var methodName = "IsAtPickup";
			LogTrace(methodName);

            //Will vary from pickup type to pickup type.
            //Determine what conditions are needed to be true and return false if not met
            switch (Core.Metatron.Config.CargoConfig.PickupLocation.LocationType)
            {
                case LocationTypes.Station:
                    if (_meCache.InStation &&
                        _meCache.StationId == _pickupBookmark.ItemID)
                    {
                        return true;
                    }
                    break;
                case LocationTypes.CorpHangarArray:
					if (!_meCache.InSpace) return false;

                    if (_meCache.InSpace &&
                        Core.Metatron.EntityProvider.EntityWrappersById.ContainsKey(Core.Metatron.Config.CargoConfig.PickupLocation.EntityID) &&
                        Core.Metatron.EntityProvider.EntityWrappersById[Core.Metatron.Config.CargoConfig.PickupLocation.EntityID].Distance <= (int)Ranges.LootActivate)
                    {
                        return true;
                    }
                    break;
                case LocationTypes.StationCorpHangar:
                    goto case LocationTypes.Station;
            }
            return false;
        }

        private bool IsAtDropoff()
        {
            var methodName = "IsAtDropoff";
			LogTrace(methodName);

            //Determine what conditions are needed to be true and return false if not met
            switch (Core.Metatron.Config.CargoConfig.DropoffLocation.LocationType)
            {
                case LocationTypes.Station:
                    if (_meCache.InStation &&
                        _meCache.StationId == _dropoffBookmark.ItemID)
                    {
                        return true;
                    }
                    break;
                case LocationTypes.StationCorpHangar:
                    goto case LocationTypes.Station;
                case LocationTypes.CorpHangarArray:
                    if (_meCache.InSpace &&
                        Core.Metatron.EntityProvider.EntityWrappersById.ContainsKey(Core.Metatron.Config.CargoConfig.DropoffLocation.EntityID) &&
                        Core.Metatron.EntityProvider.EntityWrappersById[Core.Metatron.Config.CargoConfig.DropoffLocation.EntityID].Distance <= (int)Ranges.LootActivate)
                    {
                        return true;
                    }
                    break;
            }
            return false;
        }

        private bool GetBookmarks()
        {
            var methodName = "GetBookmarks";
			LogTrace(methodName);

            //First do pickup bookmark
            if (Core.Metatron.Config.CargoConfig.PickupLocation.BookmarkLabel != String.Empty)
            {
                _pickupBookmark = null;

                var cachedBookMark = Core.Metatron.BookMarkCache.FirstBookMarkStartingWith(
                    Core.Metatron.Config.CargoConfig.PickupLocation.BookmarkLabel, false);

                if (cachedBookMark != null)
                    _pickupBookmark = Core.Metatron.BookMarkCache.GetBookMarkFor(cachedBookMark);
            }

            //Next, dropoff bookmark
            if (Core.Metatron.Config.CargoConfig.DropoffLocation.BookmarkLabel != String.Empty)
            {
                _dropoffBookmark = null;

                var cachedBookMark = Core.Metatron.BookMarkCache.FirstBookMarkStartingWith(
                    Core.Metatron.Config.CargoConfig.DropoffLocation.BookmarkLabel, false);

                _dropoffBookmark = Core.Metatron.BookMarkCache.GetBookMarkFor(cachedBookMark);
            }

            return !LavishScriptObject.IsNullOrInvalid(_pickupBookmark) && !LavishScriptObject.IsNullOrInvalid(_dropoffBookmark);
        }

        public enum FreighterStates
        {
            Idle,
            Error,
            DefenseHasControl,
            MoveToPickup,
            Pickup,
            MoveToDropoff,
            Dropoff
        }
    }
}
