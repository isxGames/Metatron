using System.Collections.Generic;
using System.Linq;
using Metatron.Core;
using Metatron.Core.Config;
using Metatron.Core.EventCommunication;
using Metatron.Core.Interfaces;

namespace Metatron.BehaviorModules.PartialBehaviors
{
	public class DropOffCargoPartialBehavior : PartialBehaviorBase
	{
		private readonly RandomWaitObject _doDropoffRandomWait;

		private StationDropOffStates _stationDropOffState = StationDropOffStates.Idle;
		private JetcanDropOffStates _jetcanDropOffState = JetcanDropOffStates.Idle;
		private CorporateHangarArrayDropOffStates _corporateHangarArrayDropOffState = CorporateHangarArrayDropOffStates.Idle;

		private bool _isComplete = true;

	    private readonly IEveWindowProvider _eveWindowProvider;
	    private readonly ICargoConfiguration _cargoConfiguration;
	    private readonly IMainConfiguration _mainConfiguration;
	    private readonly IMiningConfiguration _miningConfiguration;
	    private readonly IMeCache _meCache;
	    private readonly IShip _ship;
	    private readonly IStation _station;
	    private readonly IJettisonContainer _jettisonContainer;
	    private readonly IEntityProvider _entityProvider;
	    private readonly IEventCommunications _eventCommunications; 

		public DropOffCargoPartialBehavior(IEveWindowProvider eveWindowProvider, ICargoConfiguration cargoConfiguration, IMainConfiguration mainConfiguration,
            IMiningConfiguration miningConfiguration, IMeCache meCache, IShip ship, IStation station, IJettisonContainer jettisonContainer, 
            IEntityProvider entityProvider, IEventCommunications eventCommunications)
		{
		    _eveWindowProvider = eveWindowProvider;
		    _cargoConfiguration = cargoConfiguration;
		    _mainConfiguration = mainConfiguration;
            _miningConfiguration = miningConfiguration;
		    _meCache = meCache;
            _ship = ship;
		    _station = station;
		    _jettisonContainer = jettisonContainer;
		    _entityProvider = entityProvider;
		    _eventCommunications = eventCommunications;

		    ModuleName = "DropOffCargoPartialBehavior";

			_doDropoffRandomWait = new RandomWaitObject(ModuleName);
			_doDropoffRandomWait.AddWait(new KeyValuePair<int, int>(601, 1800), 1.5);
			_doDropoffRandomWait.AddWait(new KeyValuePair<int, int>(121, 600), 6.67);
			_doDropoffRandomWait.AddWait(new KeyValuePair<int, int>(11, 120), 15);
			_doDropoffRandomWait.AddWait(new KeyValuePair<int, int>(6, 10), 30);
			_doDropoffRandomWait.AddWait(new KeyValuePair<int, int>(1, 5), 66.67);
		}

		public override BehaviorExecutionResults Execute()
		{
			var methodName = "Execute";
			LogTrace(methodName);

			var result = DropOffCargo();

			_isComplete = result != BehaviorExecutionResults.Incomplete;

			return result;
		}

		public override void Reset()
		{
			_stationDropOffState = StationDropOffStates.Idle;
			_jetcanDropOffState = JetcanDropOffStates.Idle;
			_corporateHangarArrayDropOffState = CorporateHangarArrayDropOffStates.Idle;
		}

		public bool IsComplete
		{
			get { return _isComplete; }
		}

		private BehaviorExecutionResults DropOffCargo()
		{
			var methodName = "DropOffCargo";
			LogTrace(methodName);
			
			switch (_cargoConfiguration.DropoffLocation.LocationType)
			{
				case LocationTypes.Station:
					return DropOffCargoToStation();
				case LocationTypes.StationCorpHangar:
					return DropOffCargoToStationCorporateHangars();
				case LocationTypes.Jetcan:
					return DropOffCargoToJetCan();
				case LocationTypes.CorpHangarArray:
					return DropOffCargoToCorporateHangarArray();
				case LocationTypes.ShipBay:
					goto case LocationTypes.CorpHangarArray;
			}

			LogMessage(methodName, LogSeverityTypes.Critical, "Error: TrackDropoff location type {0} is not supported.",
				_cargoConfiguration.DropoffLocation.LocationType);
			return BehaviorExecutionResults.Error;
		}

		private BehaviorExecutionResults DropOffCargoToStation()
		{
			var methodName = "DropOffCargoToStation";
			LogTrace(methodName);
			LogMessage(methodName, LogSeverityTypes.Debug, $"StationDropOffState is {_stationDropOffState}");

            switch (_stationDropOffState)
			{
				
				case StationDropOffStates.Idle:
					//Just go to the Unload Cargo state.
					_stationDropOffState = StationDropOffStates.UnloadCargoHold;
					goto case StationDropOffStates.UnloadCargoHold;
				case StationDropOffStates.UnloadCargoHold:
					if (_doDropoffRandomWait.ShouldWait())
						return BehaviorExecutionResults.Incomplete;

					if (!_ship.IsCargoHoldActive)
					{
						LogMessage(methodName, LogSeverityTypes.Debug, "Making my ship's cargo hold active.");
						_ship.MakeCargoHoldActive();
						return BehaviorExecutionResults.Incomplete;
					}

					//If I'm mining, only unload ore. Otherwise, unload everything.
					if (_mainConfiguration.ActiveBehavior == BotModes.Mining)
						_ship.TransferOreInCargoHoldToStationHangar();
					else
						_ship.TransferCargoHoldToStationHangar();

					_stationDropOffState = StationDropOffStates.UnloadOreHold;
					break;
				case StationDropOffStates.UnloadOreHold:
					if (!_ship.HasOreHold())
					{
						_stationDropOffState = StationDropOffStates.StackHangar;
						goto case StationDropOffStates.StackHangar;
					}

					if (!_ship.IsOreHoldActive)
					{
						LogMessage(methodName, LogSeverityTypes.Debug, "Making my ship's ore hold active.");
						_ship.MakeOreHoldActive();
						return BehaviorExecutionResults.Incomplete;
					}

					//I need to add methods for specifically unloading the ore hold.
					_ship.TransferOreHoldToStationHangar();

					_stationDropOffState = StationDropOffStates.StackHangar;
					break;
				case StationDropOffStates.StackHangar:
					if (!_station.IsStationHangarActive)
					{
						LogMessage(methodName, LogSeverityTypes.Debug, "Making the station hangar active.");
						_station.MakeStationHangarActive();
						return BehaviorExecutionResults.Incomplete;
					}

					_ship.StackInventory();

					_stationDropOffState = StationDropOffStates.RearmCharges;
					break;
				case StationDropOffStates.RearmCharges:
					//See if we need to rearm crystals.
					var isRearmComplete = _ship.RearmMiningCrystals(null);
					
					if (!isRearmComplete)
						break;

					//Set state to idle, but don't use a goto case or it'll break things.
					_stationDropOffState = StationDropOffStates.Idle;
					return BehaviorExecutionResults.Complete;
			}
			return BehaviorExecutionResults.Incomplete;
		}

		private BehaviorExecutionResults DropOffCargoToStationCorporateHangars()
		{
			var methodName = "DropoffAtStationCorpHangarArray";
			LogTrace(methodName);

			switch (_stationDropOffState)
			{
				case StationDropOffStates.Idle:
					_stationDropOffState = StationDropOffStates.InitializeStationCorporateHangars;
					goto case StationDropOffStates.InitializeStationCorporateHangars;
				case StationDropOffStates.InitializeStationCorporateHangars:
					LogMessage(methodName, LogSeverityTypes.Debug, "Initializing station corporate hangars for item transfer.");
					_station.InitializeStationCorpHangars();

					_stationDropOffState = StationDropOffStates.UnloadCargoHold;
					break;
				case StationDropOffStates.UnloadCargoHold:
					if (_doDropoffRandomWait.ShouldWait())
						return BehaviorExecutionResults.Incomplete;

					if (!_ship.IsCargoHoldActive)
					{
						LogMessage(methodName, LogSeverityTypes.Debug, "Making my ship's cargo hold active.");
						_ship.MakeCargoHoldActive();
						return BehaviorExecutionResults.Incomplete;
					}

					//If I'm mining, only unload ore. Otherwise, unload everything.
					if (_mainConfiguration.ActiveBehavior == BotModes.Mining)
						_ship.TransferOreInCargoHoldToStationCorporateHangars();
					else
						_ship.TransferCargoHoldToStationCorporateHangars();

					_stationDropOffState = StationDropOffStates.UnloadOreHold;
					break;
				case StationDropOffStates.UnloadOreHold:
					if (!true)
					{
						_stationDropOffState = StationDropOffStates.StackHangar;
						goto case StationDropOffStates.StackHangar;
					}

					if (!_ship.IsOreHoldActive)
					{
						LogMessage(methodName, LogSeverityTypes.Debug, "Making my ship's ore hold active.");
						_ship.MakeOreHoldActive();
						return BehaviorExecutionResults.Incomplete;
					}

					//I need to add methods for specifically unloading the ore hold.
					_ship.TransferOreHoldToStationCorporateHangars();

					_stationDropOffState = StationDropOffStates.StackHangar;
					break;
				case StationDropOffStates.StackHangar:
					if (!_station.IsStationHangarActive)
					{
						LogMessage(methodName, LogSeverityTypes.Debug, "Making the station hangar active.");
						_station.MakeStationHangarActive();
						return BehaviorExecutionResults.Incomplete;
					}

					_ship.StackInventory();

					_stationDropOffState = StationDropOffStates.RearmCharges;
					break;
				case StationDropOffStates.RearmCharges:
					var rearmComplete = _ship.RearmMiningCrystals(null);

					if (!rearmComplete)
						break;

					_stationDropOffState = StationDropOffStates.Idle;
					return BehaviorExecutionResults.Complete;
			}

			return BehaviorExecutionResults.Incomplete;
		}

		private BehaviorExecutionResults DropOffCargoToJetCan()
		{
            var methodName = "DropOffCargoToJetCan";
			LogTrace(methodName);

			if (_jetcanDropOffState == JetcanDropOffStates.Idle)
				SetJetcanDropoffState();

			//If the jetcan doesn't exist, override the state to 'create can'.
			if (_jettisonContainer.CurrentContainer == null)
				_jetcanDropOffState = JetcanDropOffStates.CreateCan;

			switch (_jetcanDropOffState)
			{
				case JetcanDropOffStates.MarkCanFull:
					_jetcanDropOffState = JetcanDropOffStates.CreateCan;

					if (_jettisonContainer.CurrentContainer != null)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Marking active can full.");
						MarkJetcanFull();
					}
					else
						goto case JetcanDropOffStates.CreateCan;

					break;
				case JetcanDropOffStates.CreateCan:
					_jettisonContainer.SetActiveCan();

					if (_jettisonContainer.CurrentContainer != null)
					{
						_jettisonContainer.RenameActiveCan(false);

						_jetcanDropOffState = JetcanDropOffStates.OpenCan;
						goto case JetcanDropOffStates.OpenCan;
					}

					if (true)
					{
						if (!_ship.IsOreHoldActive)
						{
							LogMessage(methodName, LogSeverityTypes.Standard, "Making ship ore hold active before creating a jetcan.");
							_ship.MakeOreHoldActive();
							return BehaviorExecutionResults.Incomplete;
						}
					}
					else if (!_ship.IsCargoHoldActive)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Making ship cargo hold active before creating a jetcan.");
						_ship.MakeCargoHoldActive();
						return BehaviorExecutionResults.Incomplete;
					}

					var ore = CargoContainer.GetOreFromList(true ? _meCache.Ship.Ship.GetOreHoldCargo() : _meCache.Ship.Cargo.ToList());

					if (ore.Count == 0)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Error: We're above the cargo full threshold but have no ore to jetisson.");
						return BehaviorExecutionResults.Error;
					}

					var item = ore.First();

					LogMessage(methodName, LogSeverityTypes.Standard, "Creating new jetcan from item \"{0}\" ({1}).",
							   item.Name, item.ID);

					_jettisonContainer.CreateJetCan(item);
					break;
				case JetcanDropOffStates.OpenCan:
					if (!_jettisonContainer.IsCurrentContainerWindowOpen)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Opening cargo of active jetcan \"{0}\" ({1}).",
								   _jettisonContainer.CurrentContainer.Name, _jettisonContainer.CurrentContainerId);
						_jettisonContainer.CurrentContainer.Open();
					}
					else
					{
						_jetcanDropOffState = JetcanDropOffStates.TransferCargo;
						goto case JetcanDropOffStates.TransferCargo;
					}
					break;
				case JetcanDropOffStates.TransferCargo:
					if (true)
					{
						if (!_ship.IsOreHoldActive)
						{
							LogMessage(methodName, LogSeverityTypes.Standard, "Making ship ore hold active before transferring cargo to a jetcan.");
							_ship.MakeOreHoldActive();
							return BehaviorExecutionResults.Incomplete;
						}
					}
					else if (!_ship.IsCargoHoldActive)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Making ship cargo hold active before transferring cargo to a jetcan.");
						_ship.MakeCargoHoldActive();
						return BehaviorExecutionResults.Incomplete;
					}

					LogMessage(methodName, LogSeverityTypes.Standard, "Transferring ore to the active jetcan.");

					if (true)
						_ship.TransferOreHoldToJetCan(_jettisonContainer.CurrentContainerId);
					else
						_ship.TransferCargoHoldToJetCan(_jettisonContainer.CurrentContainerId);

					_jetcanDropOffState = JetcanDropOffStates.Idle;
					return BehaviorExecutionResults.Complete;
			}

			return BehaviorExecutionResults.Incomplete;
		}

		private BehaviorExecutionResults DropOffCargoToCorporateHangarArray()
		{
			var methodName = "DropoffAtCorpHangarArray";
			LogTrace(methodName);

			LogMessage(methodName, LogSeverityTypes.Debug, "Processing state: {0}", _corporateHangarArrayDropOffState);

			var dropoffCorporateHangarArrayEntity = _entityProvider.EntityWrappers.FirstOrDefault(
				entity => entity.ID == _cargoConfiguration.DropoffLocation.EntityID);

			if (dropoffCorporateHangarArrayEntity == null)
			{
				LogMessage(methodName, LogSeverityTypes.Debug, "Error: Could not find entity with ID {0} for corporate hangar array dropoff.",
					_cargoConfiguration.DropoffLocation.EntityID);
				return BehaviorExecutionResults.Error;
			}

			var dropoffCorporateHangarArray = new CorporateHangarArray(dropoffCorporateHangarArrayEntity, _eveWindowProvider);

			switch (_corporateHangarArrayDropOffState)
			{
				case CorporateHangarArrayDropOffStates.Idle:
					_corporateHangarArrayDropOffState = CorporateHangarArrayDropOffStates.OpenCan;
					goto case CorporateHangarArrayDropOffStates.OpenCan;
				case CorporateHangarArrayDropOffStates.OpenCan:
					if (!dropoffCorporateHangarArray.IsCurrentContainerWindowOpen)
					{
						LogMessage(methodName, LogSeverityTypes.Debug, "Opening cargo of corporate hangar array \"{0}\" ({1}).",
								   dropoffCorporateHangarArray.CurrentContainer.Name, dropoffCorporateHangarArray.CurrentContainer.ID);
						dropoffCorporateHangarArray.CurrentContainer.Open();
						return BehaviorExecutionResults.Incomplete;
					}

					LogMessage(methodName, LogSeverityTypes.Debug, "Expanding the corporate hangar array's inventory tab.");
					dropoffCorporateHangarArray.InitializeCorporateHangars();

					_corporateHangarArrayDropOffState = CorporateHangarArrayDropOffStates.MakeChaChildWindowActive;
					break;
				case CorporateHangarArrayDropOffStates.MakeChaChildWindowActive:
					if (!dropoffCorporateHangarArray.IsCurrentContainerWindowActive)
					{
						LogMessage(methodName, LogSeverityTypes.Debug, "Making the cargo window for corporate hangar array \"{0}\" active.",
								   dropoffCorporateHangarArray.CurrentContainer.Name);
						dropoffCorporateHangarArray.MakeCurrentContainerWindowActive();
						return BehaviorExecutionResults.Incomplete;
					}

					_corporateHangarArrayDropOffState = CorporateHangarArrayDropOffStates.UnloadCargoHold;
					break;
				case CorporateHangarArrayDropOffStates.UnloadCargoHold:
					if (dropoffCorporateHangarArray.IsActiveContainerFull)
					{
						LogMessage(methodName, LogSeverityTypes.Critical, "Error: Corporate hangar array \"{0}\" is full.",
								   _cargoConfiguration.DropoffLocation.EntityID);
						return BehaviorExecutionResults.Error;
					}

					if (!_ship.IsCargoHoldActive)
					{
						LogMessage(methodName, LogSeverityTypes.Debug, "Making ship cargo hold active before attempting to unload it.");
						_ship.MakeCargoHoldActive();
						return BehaviorExecutionResults.Incomplete;
					}

					if (_mainConfiguration.ActiveBehavior == BotModes.Mining)
						_ship.TransferOreInCargoHoldToCorporateHangarArray(dropoffCorporateHangarArray);
					else
						_ship.TransferCargoHoldToCorporateHangarArray(dropoffCorporateHangarArray);

					_corporateHangarArrayDropOffState = CorporateHangarArrayDropOffStates.UnloadOreHold;
					return BehaviorExecutionResults.Incomplete;
				case CorporateHangarArrayDropOffStates.UnloadOreHold:
					if (!true)
					{
						_corporateHangarArrayDropOffState = CorporateHangarArrayDropOffStates.RearmCrystals;
						goto case CorporateHangarArrayDropOffStates.RearmCrystals;
					}

					if (dropoffCorporateHangarArray.IsActiveContainerFull)
					{
						LogMessage(methodName, LogSeverityTypes.Critical, "Error: Corporate hangar array \"{0}\" is full.",
								   _cargoConfiguration.DropoffLocation.EntityID);
						return BehaviorExecutionResults.Error;
					}

					if (!_ship.IsOreHoldActive)
					{
						LogMessage(methodName, LogSeverityTypes.Debug, "Making ship ore hold active before attempting to unload it.");
						_ship.MakeOreHoldActive();
						return BehaviorExecutionResults.Incomplete;
					}

					_ship.TransferOreHoldToCorporateHangarArray(dropoffCorporateHangarArray);

					_corporateHangarArrayDropOffState = CorporateHangarArrayDropOffStates.RearmCrystals;
					break;
				case CorporateHangarArrayDropOffStates.RearmCrystals:
					if (_mainConfiguration.ActiveBehavior != BotModes.Mining || _miningConfiguration.IsIceMining)
					{
						_corporateHangarArrayDropOffState = CorporateHangarArrayDropOffStates.StackCargo;
						goto case CorporateHangarArrayDropOffStates.StackCargo;
					}

					//If we're dropping off to an orca ship bay and it's at least half full, call for pickup.
					if (_cargoConfiguration.DropoffLocation.LocationType == LocationTypes.ShipBay)
					{
						if (dropoffCorporateHangarArray.IsActiveContainerHalfFull)
						{
							LogMessage(methodName, LogSeverityTypes.Debug, "Sending pickup request for the ship bay.");
							_eventCommunications.FleetNeedPickupEvent.SendEventFromArgs(
								_meCache.CharId,
								_meCache.SolarSystemId,
								_cargoConfiguration.DropoffLocation.EntityID,
								_entityProvider.EntityWrappersById[
									_cargoConfiguration.DropoffLocation.EntityID].ID,
								_meCache.Name);
						}

						_corporateHangarArrayDropOffState = CorporateHangarArrayDropOffStates.StackCargo;
					}
					else
					{
						//If we're not dropping off to orca, see if we're good on crystals. If so, stack cargo.
						var rearmComplete = _ship.RearmMiningCrystals(dropoffCorporateHangarArray);
						
						if (!rearmComplete)
							break;

						_corporateHangarArrayDropOffState = CorporateHangarArrayDropOffStates.StackCargo;
					}
					break;
				case CorporateHangarArrayDropOffStates.StackCargo:
					//Stack my cargo and the CHA's.
					if (!dropoffCorporateHangarArray.IsCurrentContainerWindowActive)
					{
						LogMessage(methodName, LogSeverityTypes.Debug, "Making the cargo window of corporate hangar array \"{0}\" active in order to stack cargo.",
									dropoffCorporateHangarArray.CurrentContainer.Name);
						dropoffCorporateHangarArray.MakeCurrentContainerWindowActive();
						return BehaviorExecutionResults.Incomplete;
					}

					_ship.StackInventory();

					_corporateHangarArrayDropOffState = CorporateHangarArrayDropOffStates.Idle;
					return BehaviorExecutionResults.Complete;
			}

			return BehaviorExecutionResults.Incomplete;
		}

		private void SetJetcanDropoffState()
		{
			if (_jettisonContainer.CurrentContainer == null)
				_jetcanDropOffState = JetcanDropOffStates.MarkCanFull;
			else if (_jettisonContainer.CurrentContainer.Distance > (int)Ranges.LootActivate)
				_jetcanDropOffState = JetcanDropOffStates.MarkCanFull;
			else if (!_jettisonContainer.IsCurrentContainerWindowOpen)
				_jetcanDropOffState = JetcanDropOffStates.OpenCan;
			else if (_jettisonContainer.IsActiveCanFull())
				_jetcanDropOffState = JetcanDropOffStates.MarkCanFull;
			else _jetcanDropOffState = JetcanDropOffStates.TransferCargo;
		}

		private void MarkJetcanFull()
		{
			var methodName = "MarkJetcanFull";
			LogTrace(methodName);

			//Send a request for pickup
			_eventCommunications.FleetNeedPickupEvent.SendEventFromArgs(
				_meCache.CharId, _meCache.SolarSystemId,
				_jettisonContainer.CurrentContainerId,
				_meCache.ToEntity.Id,
				_meCache.Name);

			_jettisonContainer.MarkActiveCanFull();
		}
	}

	public enum StationDropOffStates
	{
		Idle,
		InitializeStationCorporateHangars,
		UnloadCargoHold,
		UnloadOreHold,
		StackHangar,
		RearmCharges
	}

	public enum JetcanDropOffStates
	{
		Idle,
		CreateCan,
		OpenCan,
		MarkCanFull,
		TransferCargo,
		StackCargo,
		WaitForPickup
	}

	public enum CorporateHangarArrayDropOffStates
	{
		Idle,
		MakeChaChildWindowActive,
		OpenCan,
		UnloadCargoHold,
		UnloadOreHold,
		StackCargo,
		RearmCrystals
	}
}
