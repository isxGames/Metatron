using System;
using System.Collections.Generic;
using System.Linq;
using Metatron.BehaviorModules.PartialBehaviors;
using Metatron.Core;
using EVE.ISXEVE;
using LavishScriptAPI;
using System.Diagnostics;
using Metatron.ActionModules;
using Metatron.Core.Config;
using Metatron.Core.EventCommunication;
using Metatron.Core.Extensions;
using Metatron.Core.Interfaces;
using IShip = Metatron.Core.IShip;

namespace Metatron.BehaviorModules
{
	/// <summary>
	/// Handle the hauling operations, primarily picking up jetcans and hauling to station or CHA.
	/// Have an EventHandler for listening for pickup requests.
	/// </summary>
	internal sealed class Hauler : BehaviorBase
	{
		//States we'll need
		private HaulerStates _haulerState = HaulerStates.Idle;
		private HaulerPickupTypes _haulerPickupType = HaulerPickupTypes.Jetcan;
		private BounceWarpStates _bounceWarpState = BounceWarpStates.Idle;

		//Timer for statistics
		private readonly Stopwatch _timeForTrip = new Stopwatch();

		//Required lists for WaitforRequest
        private readonly List<FleetNeedPickupEventArgs> _queuedPickupRequests = new List<FleetNeedPickupEventArgs>();
        private volatile List<FleetNeedPickupEventArgs> _requestsToValidate = new List<FleetNeedPickupEventArgs>();

		//Keep track of cans grabbed in a pulse to prevent inf. loops
		private readonly List<Int64> _cansGrabbedThisPulse = new List<Int64>();

		//Variables used for cyclefleet or grabbing cans
		private Int64 _pickupCanId = -1, _currentFleetMemberId;
		private int _lastFleetMemberIndex;
		private double _freeCapacity;

		//"Cleaned" list of fleetmembers we'll haul for
		private List<FleetMember> _fleetMembers = new List<FleetMember>();

        private readonly EventHandler<FleetNeedPickupEventArgs> _fleetNeedPickup;

		//Used for getting fleetmember names
		private readonly List<IEntityWrapper> _nearFleetMembers = new List<IEntityWrapper>();
		private readonly List<Int64> _nearFleetMemberOrcas = new List<Int64>();
		private uint _lastPulseUpdated;

		//See if we've "reset" the temp hauler bookmark
		private bool _resetTempHaulerBookmark;

		//Track whether or not we've requested all pickup requests
		bool _requestedPickupRequestResends;
		private readonly DropOffCargoPartialBehavior _dropOffCargoPartialBehavior;
		private readonly MoveToDropOffLocationPartialBehavior _moveToDropOffLocationPartialBehavior;

        //Timer for the service fleet wait system.
	    private DateTime _serviceFleetWait;

	    private readonly IEveWindowProvider _eveWindowProvider;
	    private readonly ISafespots _safespots;
	    private readonly IMainConfiguration _mainConfiguration;
	    private readonly IMovement _movement;
	    private readonly IShip _ship;
	    private readonly IMeCache _meCache;
	    private readonly ICargoConfiguration _cargoConfiguration;
	    private readonly IBookMarkCache _bookMarkCache;
	    private readonly IEntityProvider _entityProvider;

        public Hauler(IEveWindowProvider eveWindowProvider, ICargoConfiguration cargoConfiguration, IMainConfiguration mainConfiguration,
            IMiningConfiguration miningConfiguration, IMeCache meCache, IShip ship, IStation station, IJettisonContainer jettisonContainer,
            IEntityProvider entityProvider, IEventCommunications eventCommunications, ISafespots safespots, IMovement movement, IBookMarkCache bookMarkCache, MoveToDropOffLocationPartialBehavior moveToDropOffLocationPartialBehavior)
		{
		    _eveWindowProvider = eveWindowProvider;
            _safespots = safespots;
            _movement = movement;
            _bookMarkCache = bookMarkCache;
            _moveToDropOffLocationPartialBehavior = moveToDropOffLocationPartialBehavior;
            _mainConfiguration = mainConfiguration;
            _ship = ship;
            _meCache = meCache;
            _cargoConfiguration = cargoConfiguration;
            _entityProvider = entityProvider;

            _dropOffCargoPartialBehavior = new DropOffCargoPartialBehavior(eveWindowProvider, cargoConfiguration, mainConfiguration,
                miningConfiguration, meCache, ship, station, jettisonContainer, entityProvider, eventCommunications);

		    ModuleName = "Hauler";
		    CanSendCombatAssistanceRequests = true;
			PulseFrequency = 2;
			BehaviorManager.BehaviorsToPulse.Add(BotModes.Hauling, this);
            _fleetNeedPickup = new EventHandler<FleetNeedPickupEventArgs>(QueuePickupRequest);
            Core.Metatron.EventCommunications.FleetNeedPickupEvent.EventRaised += _fleetNeedPickup;
			IsEnabled = true;
		}

		public override void Pulse()
		{
			var methodName = "Pulse";
			LogTrace(methodName);

			if (!ShouldPulse() || _mainConfiguration.ActiveBehavior != BotModes.Hauling ||
			    _movement.IsMoving || _meCache.ToEntity.Mode == (int) Modes.Warp) 
				return;

		    if (!_ship.IsInventoryReady) return;

			StartPulseProfiling();

			//Build the FleetMember list we'll be using
			BuildFleetMemberList();

			//check for and acknowledge any pickup requests if in WaitForRequest mode
			if (Core.Metatron.Config.HaulingConfig.HaulerMode == HaulerModes.WaitForRequestEvent)
			{
				ValidatePickupRequests();
			}

			//update free cargo capacity
			_freeCapacity = _meCache.Ship.CargoCapacity -
			                _meCache.Ship.UsedCargoCapacity;

			//Do setstate/processstate
			SetPulseState();
			ProcessPulseState();

			EndPulseProfiling();
		}

		protected override void SetPulseState()
		{
			var methodName = "SetPulseState";
			LogTrace(methodName);

			//Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
			//	"SetPulseState", string.Format("Hauler state: {0}", HaulerState.ToString())));
			if (Core.Metatron.Defense.IsFleeing)
			{
				_haulerState = HaulerStates.DefenseHasControl;
			}
			else
			{
				switch (_haulerState)
				{
					case HaulerStates.Idle:
						//If we're not supposedto be running, don't change state.
				        if (HasMaxRuntimeExpired())
				            return;

				        //If we're idling, see if we need to dropoff
						if (_meCache.Ship.UsedCargoCapacity >= _cargoConfiguration.CargoFullThreshold)
						{
							_haulerState = HaulerStates.GoToDropoff;
						}
						else
						{
							if (Core.Metatron.Config.HaulingConfig.HaulerMode == HaulerModes.WaitForRequestEvent)
							{
								_haulerState = HaulerStates.WaitingForRequest;
								goto case HaulerStates.WaitingForRequest;
							}
							_haulerState = HaulerStates.GoToPickup;
						}
						break;
					case HaulerStates.WaitingForRequest:
						//We can have a timing issue and end up here even if in cycle mode, so switch back if we need to.
                        if (Core.Metatron.Config.HaulingConfig.HaulerMode == HaulerModes.CycleFleetMembers)
						{
							_haulerState = HaulerStates.Idle;
							goto case HaulerStates.Idle;
						}

						//IF we have requests, handle them. Otherwise, keep waiting.
						if (_queuedPickupRequests.Count > 0)
						{
							_haulerState = HaulerStates.GoToPickup;
						}
						break;
					case HaulerStates.Error:
						break;
					//If defense had control, reset to idle.
					case HaulerStates.DefenseHasControl:
						_haulerState = HaulerStates.Idle;
						goto case HaulerStates.Idle;
				}
			}
		}

		protected override void ProcessPulseState()
		{
			var methodName = "ProcessPulseState";
			LogTrace(methodName);

			LogMessage(methodName, LogSeverityTypes.Debug, "Processing hauler state: {0}",
				_haulerState);

			//Clear teh cans grabbed this pulse
			_cansGrabbedThisPulse.Clear();

			//reset the hauler bookmark if necessary
			if (!_resetTempHaulerBookmark)
			{
				LogMessage(methodName, LogSeverityTypes.Debug, "Resetting temporary hauling bookmark if it exists.");
				Core.Metatron.Bookmarks.RemoveTemporaryHaulingBookmarks();
				_resetTempHaulerBookmark = true;
			}

            if (_meCache.InSpace && (!_movement.IsMoving || _movement.MovementType == MovementTypes.Approach))
            {
                var gangLinkModules = _ship.GangLinkModules.Where(m => !m.IsActive);
                if (gangLinkModules.Any())
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Activating ganglink modules.");
                    _ship.ActivateModuleList(gangLinkModules, true);
                }
            }

			switch (_haulerState)
			{
				//Do nothing if idle or waiting for request
				case HaulerStates.Idle:
					//See if we're supposed to be running before continuing
                    if (HasMaxRuntimeExpired())
                        return;

                    if (Core.Metatron.Config.HaulingConfig.HaulerMode == HaulerModes.WaitForRequestEvent)
					{
						_haulerState = HaulerStates.WaitingForRequest;
						goto case HaulerStates.WaitingForRequest;
					}

					_haulerState = HaulerStates.GoToPickup;
					goto case HaulerStates.GoToPickup;
                //CycleDelay for the 
                case HaulerStates.CycleDelay:
                    CycleDelay();
			        break;
                //Do nothing if defence has control.
				case HaulerStates.DefenseHasControl:
					break;
				case HaulerStates.WaitingForRequest:
					//If we're not supposed to be running go back to idle
					if (HasMaxRuntimeExpired())
					{
						_haulerState = HaulerStates.Idle;
						goto case HaulerStates.Idle;
					}

					//If we've got cargo and are idling in our dropoff station, unload
					if (_meCache.Ship.UsedCargoCapacity > 0 ||
						!_dropOffCargoPartialBehavior.IsComplete)
					{
						//Make sure we're at a dropoff
                        switch (_cargoConfiguration.DropoffLocation.LocationType)
						{
							case LocationTypes.Station:
								//If we're idling in a station there's a very good chance we're idling in our dropoff
								//Check ID against our dropoff bookmark Entity ID
								var bookMark = _bookMarkCache.FirstBookMarkStartingWith(
                                    _cargoConfiguration.DropoffLocation.BookmarkLabel, true);

								if (bookMark != null)
								{
									//If the StationID is teh same as the ItemID (entity ID)
									if (_meCache.StationId == bookMark.ItemId)
									{
										//Do dropoff 
										LogMessage(methodName, LogSeverityTypes.Standard, "Doing dropoff while waiting for requests.");
										DropoffCargo();
										return;
									}
								}
								break;
							case LocationTypes.CorpHangarArray:
								//Make sure I'm near the CHA
								if (_entityProvider.EntityWrappersById.ContainsKey(
									_cargoConfiguration.DropoffLocation.EntityID))
								{
                                    var corporateHangarArray = _entityProvider.EntityWrappersById[_cargoConfiguration.DropoffLocation.EntityID];

									if (corporateHangarArray.Distance <= (int)Ranges.LootActivate)
									{
										LogMessage(methodName, LogSeverityTypes.Standard, "Doing dropoff while waiting for requests.");
										DropoffCargo();
									}
								}
								break;
						}
					}

					//If we have a pickup system bookmark and we're not in that system, move to it.
					if (_cargoConfiguration.PickupSystemBookmark != string.Empty)
					{
						var pickupSystemBookmark = _bookMarkCache.FirstBookMarkMatching(
								_cargoConfiguration.PickupSystemBookmark, false);

						//If we got a valid bookmark...
						if (pickupSystemBookmark != null)
						{
							//Determine if we need to move to it. If I'm in the wrong system, or I'm instation and either
							//it's not a station bookmark or it's another station...
							if (_meCache.SolarSystemId != pickupSystemBookmark.SolarSystemId ||
								(_meCache.InStation && (pickupSystemBookmark.ItemId < 0 || pickupSystemBookmark.ItemId != _meCache.StationId)))
							{
								//Undock if I'm already in station
								if (_meCache.InStation)
								{
									LogMessage(methodName, LogSeverityTypes.Standard, "Undocking to move to pickup system bookmark {0}.",
										pickupSystemBookmark.Label);
									_movement.QueueDestination(new Destination(DestinationTypes.Undock));
								}
								else
								{
									//Move to the actual bookmark
									_movement.QueueDestination(new Destination(DestinationTypes.SolarSystem,
										pickupSystemBookmark.SolarSystemId));
									LogMessage(methodName, LogSeverityTypes.Standard, "Moving to pickup system bookmark {0}.",
										pickupSystemBookmark.SolarSystemId);
								}
								return;
							}
						}
					}

					//Go to a safe spot if we're not at one, and not at a pickup system bookmark
					if (!_safespots.IsSafe())
					{
					    var safespot = _safespots.GetSafeSpot();
					    _movement.QueueDestination(safespot);

					    _logging.LogMessage(ModuleName, methodName, LogSeverityTypes.Standard, "Moving to a safespot while waiting.");
					}
					//If we are at a safespot in space, try to activate a cloaking device
					else if (_meCache.InSpace)
					{
					    var cloakingDevice = _ship.CloakingDeviceModules.FirstOrDefault();
						if (cloakingDevice != null)
                        {
                            if (!cloakingDevice.IsActive)
						    {
							    LogMessage(methodName, LogSeverityTypes.Standard, "Activating cloaking device while waiting at safespot.");
                                cloakingDevice.Click();
						    }
                        }
						else
						{
						    var gangLinks = _ship.GangLinkModules.Where(g => !g.IsActive);
						    if (gangLinks.Any())
						    {
                                _ship.ActivateModuleList(gangLinks, true);
						        LogMessage(methodName, LogSeverityTypes.Standard, "Activating ganglinks while waiting at safespot.");
						    }
						}
					}
					return;
				case HaulerStates.Error:
					HandleError();
					break;
				//Go to the fleetmember that requested pickup
				case HaulerStates.GoToPickup:
					var shouldGoToCase = MoveToPickup();

					if (shouldGoToCase)
					{
						if (_haulerState == HaulerStates.Idle)
							goto case HaulerStates.Idle;
						if (_haulerState == HaulerStates.DoPickup)
							goto case HaulerStates.DoPickup;
					}
					break;
				case HaulerStates.DoPickup:
					shouldGoToCase = Pickup();

					//If we need to goto the new case, do so
                    if (shouldGoToCase)
                    {
                        if (_haulerState == HaulerStates.Idle)
                            goto case HaulerStates.Idle;
                        if (_haulerState == HaulerStates.GoToPickup)
                            goto case HaulerStates.GoToPickup;
                        if (_haulerState == HaulerStates.GoToDropoff)
                            goto case HaulerStates.GoToDropoff;
                    }
			        break;
				case HaulerStates.GoToDropoff:
					MoveToDropoff();
					break;
				case HaulerStates.DoDropoff:
					DropoffCargo();
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

		private void HandleError()
		{
			var methodName = "HandleError";
			LogTrace(methodName);

			//Go to a safe spot if we're not at one
			if (_safespots.IsSafe()) return;

		    var safespot = _safespots.GetSafeSpot();
		    _movement.QueueDestination(safespot);
		    LogMessage(methodName, LogSeverityTypes.Standard, "Moving to a safe spot due to being in an error state.");
		}

		private bool MoveToPickup()
		{
			var methodName = "MoveToPickup";
			LogTrace(methodName);

			//Start the timer if it's not running
			if (!_timeForTrip.IsRunning)
			{
				_timeForTrip.Start();
			}

			//Make sure we're not instation
			if (_meCache.InStation && !_meCache.InSpace)
			{
				//undock if we were
				_movement.QueueDestination(new Destination(DestinationTypes.Undock));
				return false;
			}

			//If I have an active cloaking device, deactivate it.
		    var activeCloakingDevice = _ship.CloakingDeviceModules.FirstOrDefault(m => m.IsActive);
			if (activeCloakingDevice != null)
			{
				LogMessage(methodName, LogSeverityTypes.Debug, "Deactivating cloaking device before warping.");
                activeCloakingDevice.Click();
				return false;
			}

			//IF we're cycling, we're going to need to know the name or ID of the solar system to go to and
			//travel to that before attempting to travel to fleetmember, because waitForRequests uses the
			//solarSystemID passed by the event.
			if (Core.Metatron.Config.HaulingConfig.HaulerMode == HaulerModes.WaitForRequestEvent)
			{
				//Better handle the user changing modes on us - ifwe have no requests, idle.
				if (_queuedPickupRequests.Count == 0)
				{
					LogMessage(methodName, LogSeverityTypes.Debug, "No queued requests; idling.");
					_haulerState = HaulerStates.Idle;
					return true;
				}

				//See if we have a valid pickup can
				Core.Metatron.Bookmarks.TempCanBookmark = Core.Metatron.Bookmarks.GetTempCanBookmark();
				if (Core.Metatron.Bookmarks.TempCanBookmark != null)
				{
					//Get the bookmark
					var bookMark = Core.Metatron.Bookmarks.TempCanBookmark;

					if (DistanceTo(bookMark.X, bookMark.Y, bookMark.Z) > (int)Ranges.Warp)
					{
						//Cool, have a valid can to move to.
						_movement.QueueDestination(new Destination(DestinationTypes.BookMark,
							Core.Metatron.Bookmarks.TempCanBookmark.Id));
						LogMessage(methodName, LogSeverityTypes.Standard, "Moving to temp can bookmark \"{0}\".",
							Core.Metatron.Bookmarks.TempCanBookmark.Label);
						//return; don't want it to do anything else this pusle.
						return false;
					}
				}

				//Make sure we're in the same solarsystem as the requested pickup, move if necessary
			    var queuedPickupRequest = _queuedPickupRequests[0];
			    if (queuedPickupRequest.SolarSystemId != _meCache.SolarSystemId)
				{
					LogMessage(methodName, LogSeverityTypes.Standard, "We are in solar system {0} and pickup is in solar system {1}; travelling",
						_meCache.SolarSystemId, queuedPickupRequest.SolarSystemId);
					_movement.QueueDestination(new Destination(DestinationTypes.SolarSystem, queuedPickupRequest.SolarSystemId));
				    return false;
				}

			    //If the entity isn't valid, warp to fleetmember
			    IEntityWrapper requestedCan = null;
			    if (_entityProvider.EntityWrappersById.ContainsKey(queuedPickupRequest.TargetCanEntityId))
			    {
			        LogMessage(methodName, LogSeverityTypes.Debug, "Found entity for requested can {0} from member {1}",
			                   queuedPickupRequest.TargetCanEntityId, queuedPickupRequest.SendingFleetMemberCharId);
			        requestedCan = _entityProvider.EntityWrappersById[queuedPickupRequest.TargetCanEntityId];
			    }

			    //If the can isn't valid...
			    if (requestedCan == null)
			    {
                    if (Core.Metatron.Bookmarks.TempCanBookmark != null)
                    {
                        LogMessage(methodName, LogSeverityTypes.Standard, "At temp bookmark \"{0}\" and no can is present; removing bookmark.",
                            Core.Metatron.Bookmarks.TempCanBookmark.Label);
                        Core.Metatron.Bookmarks.RemoveTemporaryHaulingBookmarks();
                    }

			        //Iterate fleet members
			        if (!_fleetMembers.Any(fleetMember => fleetMember.CharID == queuedPickupRequest.SendingFleetMemberCharId))
			        {
                        LogMessage(methodName, LogSeverityTypes.Standard, "We have a request from fleet member {0} who is no longer in our fleet; dequeueing.",
                            queuedPickupRequest.SendingFleetMemberCharId);
			            _queuedPickupRequests.Remove(queuedPickupRequest);
			            return false;
			        }

			        var fleetMemberBuddy = _meCache.Buddies.FirstOrDefault(buddy => buddy.CharID == queuedPickupRequest.SendingFleetMemberCharId);

			        if (fleetMemberBuddy != null && !fleetMemberBuddy.IsOnline)
			        {
			            LogMessage(methodName, LogSeverityTypes.Standard,
			                       "Ignoring pickup request from offline fleet member {0}.",
			                       queuedPickupRequest.SendingFleetMemberCharId);
			            _queuedPickupRequests.RemoveAt(0);
			            return false;
			        }

			        if (_entityProvider.EntityWrappersById.ContainsKey(queuedPickupRequest.SendingFleetMemberEntityId))
			        {
			            LogMessage(methodName, LogSeverityTypes.Standard,
			                       "Fleet member's ToEntity valid and can not found; it was probably popped or stolen. Dequeueing. Stop AFK jetcanning, you noob.");

			            //RemoveBookmarkAndCacheEntry this can and any other invalid cans
			            var fleetMembers = GetNearbyFleetMembers();
			            foreach (var entity in fleetMembers)
			            {
			                for (var index = 0; index < _queuedPickupRequests.Count; index++)
			                {
			                    var q = _queuedPickupRequests[index];
			                    //If we have a valid entity for the requesting fleetmember but
			                    //have no valid entity for the requested can, remove it
			                    if (entity.ID != q.SendingFleetMemberEntityId ||
			                        _entityProvider.EntityWrappersById.ContainsKey(q.TargetCanEntityId))
			                        continue;

			                    LogMessage(methodName, LogSeverityTypes.Debug,
			                               "Purging invalid requested can {0} near member {1} ({2}).",
			                               q.TargetCanEntityId, entity.Name, entity.ID);
			                    _queuedPickupRequests.RemoveAt(index);
			                    index--;
			                }
			            }

			            //QueuedPickupRequests.RemoveAt(0);
			            _haulerState = HaulerStates.Idle;
			            return true;
			        }

			        LogMessage(methodName, LogSeverityTypes.Standard, "Moving to fleet member {0}",
			                   queuedPickupRequest.SendingFleetMemberCharId);
			        _movement.QueueDestination(new Destination(DestinationTypes.FleetMember)
			                                                    {
			                                                        FleetMemberId = queuedPickupRequest.SendingFleetMemberCharId,
			                                                        FleetMemberEntityId =
			                                                            queuedPickupRequest.SendingFleetMemberEntityId
			                                                    });
			        return false;
			    }

			    //Make sure we request refresh on the requested can to keep its data valid
			    requestedCan.RequestObjectRefresh();

			    //If we're above lootActivate range, approach
			    if (requestedCan.Distance > (int)Ranges.LootActivate)
			    {
			        //if we're bouncewarping and the can is worth bouncewarping to and is under warp range...
			        if (CanBounceWarpToJetcan(requestedCan))
			        {
			            //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Standard,
			            //    "MoveToPickup", String.Format("Bounce warping to can {0} ({1}).", requestedCan.Name, requestedCan.ID)));
			            BounceWarp(requestedCan);
			        }
			            //if we can't bouncewarp or far enough to warp to, do so
			        else
			        {
			            var requestedCans = new List<IEntityWrapper>();
			            foreach (var request in _queuedPickupRequests)
			            {
			                if (!_entityProvider.EntityWrappersById.ContainsKey(request.TargetCanEntityId)) 
			                    continue;

			                var entity = _entityProvider.EntityWrappersById[request.TargetCanEntityId];
			                if (!requestedCans.Contains(entity))
			                {
			                    requestedCans.Add(entity);
			                }
			            }

			            //If it's within warp distance queue it as a tractor target
			            TryQueueJetcansForTractoring(requestedCans);

			            LogMessage(methodName, LogSeverityTypes.Standard, "Moving to can {0} ({1}).",
			                       requestedCan.Name, requestedCan.ID);
			            _movement.QueueDestination(
			                new Destination(DestinationTypes.Entity, requestedCan.ID) { Distance = (int)Ranges.LootActivate });
			        }
			        return false;
			    }

			    _haulerPickupType = requestedCan.TypeID == (int)TypeIDs.Orca ? HaulerPickupTypes.Orca : HaulerPickupTypes.Jetcan;

			    //We're at the can, do pickup
			    _pickupCanId = requestedCan.ID;
			    _currentFleetMemberId = queuedPickupRequest.SendingFleetMemberEntityId;

			    LogMessage(methodName, LogSeverityTypes.Debug, "Going to DoPickup state for can {0} ({1}).",
			               requestedCan.Name, requestedCan.ID);
			    _haulerState = HaulerStates.DoPickup;
			    return true;
			}
            if (Core.Metatron.Config.HaulingConfig.HaulerMode == HaulerModes.CycleFleetMembers)
			{
				var pickupSysBookMark = Core.Metatron.Bookmarks.GetHaulerPickupSystemBookMark();
				if (pickupSysBookMark != null && pickupSysBookMark.SolarSystemId != _meCache.SolarSystemId)
				{
					LogMessage(methodName, LogSeverityTypes.Standard, "Moving to pickup system bookmark {0}",
					           Core.Metatron.Bookmarks.GetHaulerPickupSystemBookMark().SolarSystemId);
					_movement.QueueDestination(
						new Destination(DestinationTypes.BookMark, Core.Metatron.Bookmarks.GetHaulerPickupSystemBookMark().Id));

					return false;
				}

				//Make sure we're undocked
				LogMessage(methodName, LogSeverityTypes.Standard, "Fleet Members: {0}",
				           _fleetMembers.Count);

				if (_fleetMembers.Count > 0)
				{
					//Undock if we're in station
					if (_meCache.InStation && !_meCache.InSpace)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Undocking from station.");
						_movement.QueueDestination(
							new Destination(DestinationTypes.Undock));
						return false;
					}

					//If we've reached the cap, wrap around
					if (_fleetMembers.Count <= _lastFleetMemberIndex)
					{
						_lastFleetMemberIndex = 0;
					}

					//Iterate the fleet and try to find a member to work on.
					var fleetMember = _fleetMembers[_lastFleetMemberIndex];

					var fleetMemberBuddy = _meCache.Buddies.FirstOrDefault(buddy => buddy.CharID == fleetMember.CharID);

					if (fleetMemberBuddy != null && !fleetMemberBuddy.IsOnline)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Cannot service fleet member {0} because they are offline.",
						           fleetMember.CharID);
						_lastFleetMemberIndex++;
						return false;
					}

					LogMessage(methodName, LogSeverityTypes.Debug, "Working on fleetMember {0}",
					           fleetMember.CharID);

					string fleetMemberName;
					using (var fleetMemberPilot = fleetMember)
					{
						fleetMemberName = fleetMemberPilot.Name;
					}

					var tempFleetMember = _entityProvider.EntityWrappers.FirstOrDefault(
						entity => entity.Name == fleetMemberName);

					//If we have a valid entity for the fleetmember, we need to get near the can.
					if (tempFleetMember == null)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Moving to fleet member {0}.",
						           fleetMemberName);

						_movement.QueueDestination(
							new Destination(DestinationTypes.FleetMember)
								{
									FleetMemberId = fleetMember.CharID,
									FleetMemberName = fleetMemberName
								});
						return false;
					}

					//Try to find a can owned by this fleetmember
					var cans = GetJetcansNearFleetMember(tempFleetMember);
					IEntityWrapper tempCan = null;
					if (cans != null && cans.Count > 0)
					{
						tempCan = cans[0];
					}

					//If the can's null, cycle members.
					if (cans == null || tempCan == null)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Could not find a valid can near fleetmember {0}, moving on.",
						           tempFleetMember.Name);

						//If we have enough fleet members, go to the next one, otherwise go to a safespot
						if (_fleetMembers.Count > 1)
						{
							_lastFleetMemberIndex++;
						}
						else
						{
						    var safespot = _safespots.GetSafeSpot();
						    _movement.QueueDestination(safespot);
						}

						return false;
					}

					//make sure the can stays up to date
					tempCan.RequestObjectRefresh();

					//If we're too far from the can
					if (tempCan.Distance > (int)Ranges.LootActivate)
					{
						//If we can bounce warp and are within bounce range...
						if (CanBounceWarpToJetcan(tempCan))
						{
							//Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Standard,
							//    "MoveToPickup", String.Format("Bounce warping to can {0} ({1})", tempCan.Name, tempCan.ID)));
							BounceWarp(tempCan);
						}
							//Else we need to move to it, either approach or warp
						else
						{
							//If it's within warp distance queue it as a tractor target
							TryQueueJetcansForTractoring(cans);

							LogMessage(methodName, LogSeverityTypes.Standard, "Moving to can {0} ({1}).",
							           tempCan.Name, tempCan.ID);
							_movement.QueueDestination(
								new Destination(DestinationTypes.Entity, tempCan.ID) { Distance = (int)Ranges.LootActivate });
						}
						return false;
					}

					//Set the type of pickup we're doing
					if (tempCan.TypeID == (int)TypeIDs.Orca)
					{
						_haulerPickupType = HaulerPickupTypes.Orca;
					}
					else
					{
						_haulerPickupType = HaulerPickupTypes.Jetcan;
					}

					//Increment the fleet member
					//but only if there are no more cans within, say, 5km of the current can
					var cansNearCurrentFleetMember = GetJetcansNearFleetMember(tempFleetMember);
					for (var index = 0; index > cansNearCurrentFleetMember.Count; index++)
					{
						var can = cansNearCurrentFleetMember[index];

						if (Distance(can.X, can.Y, can.Z, tempFleetMember.X, tempFleetMember.Y, tempFleetMember.Z) <= 5000) 
							continue;

						cansNearCurrentFleetMember.RemoveAt(index);
						index--;
					}

					//If there are no more nearby cans...
					if (cansNearCurrentFleetMember.Count == 0 || _haulerPickupType == HaulerPickupTypes.Orca)
					{
						_lastFleetMemberIndex++;
					}

					//Set the current member and can ID
					_pickupCanId = tempCan.ID;
					_currentFleetMemberId = tempFleetMember.ID;

					//Go to pickup state
					LogMessage(methodName, LogSeverityTypes.Debug, "Going to DoPickup state");
					_haulerState = HaulerStates.DoPickup;
					//return true to signify we can go to the pickup state
					return true;
				}
			}
			return false;
		}

		private bool Pickup()
		{
			var methodName = "Pickup";
			LogTrace(methodName);

			if (!_meCache.InSpace)
			{
				LogMessage(methodName, LogSeverityTypes.Debug, "In station for DoPickup; idling.");
				_haulerState = HaulerStates.Idle;
				return true;
			}

			IEntityWrapper targetCan = null;
			if (_entityProvider.EntityWrappersById.ContainsKey(_pickupCanId))
			{
				targetCan = _entityProvider.EntityWrappersById[_pickupCanId];
				LogMessage(methodName, LogSeverityTypes.Debug, "Found entity for target can {0} ({1}).",
					targetCan.Name, targetCan.ID);
			}

			//Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
			//	"DoPickup", "LSO.IsNullOrInvalid(targetCan)"));
			if (targetCan == null)
			{
				LogMessage(methodName, LogSeverityTypes.Standard, "Can with Entity ID of {0} was not found. Possibly stolen? Dequeueing pickup request.",
					_pickupCanId);

				if (_queuedPickupRequests.Count > 0)
				{
					_queuedPickupRequests.RemoveAt(0);
				}

				//Reset the ID
				_pickupCanId = -1;
				_currentFleetMemberId = -1;
				_haulerState = HaulerStates.Idle;
				return true;
			}

			//Make sure we can safely access this can
			if (!targetCan.ToEntity.HaveLootRights)
			{
				LogMessage(methodName, LogSeverityTypes.Standard, "No loot rights for can {0} ({1}), probably flipped.",
					targetCan.Name, targetCan.ID);

				if (_queuedPickupRequests.Count > 0)
				{
					_queuedPickupRequests.RemoveAt(0);
				}

				//reset the ID
				_pickupCanId = -1;
				_currentFleetMemberId = -1;
				_haulerState = HaulerStates.Idle;
				return true;
			}

			//Make sure we keep the target can up to date
			targetCan.RequestObjectRefresh();

			//Get in range of the can if we have to
			if (targetCan.Distance > (int)Ranges.LootActivate)
			{
				//if it's too far, bounce warp
				if (CanBounceWarpToJetcan(targetCan))
				{
					BounceWarp(targetCan);
				}
				else
				{
					//If it's within warp distance queue it as a tractor target
					var nearbyCans = new List<IEntityWrapper> { targetCan };

					//IF my fleetmember is on-grid get cans near him and queue them for tractor
					if (_entityProvider.EntityWrappersById.ContainsKey(_currentFleetMemberId))
					{
						nearbyCans = GetJetcansNearFleetMember(_entityProvider.EntityWrappersById[_currentFleetMemberId]);
					}
					TryQueueJetcansForTractoring(nearbyCans);

					LogMessage(methodName, LogSeverityTypes.Standard, "Moving closer to can {0} ({1}).",
						targetCan.Name, targetCan.ID);
					_movement.QueueDestination(
						new Destination(DestinationTypes.Entity, targetCan.ID) { Distance = (int)Ranges.LootActivate });
				}
			    return false;
			}

		    //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
		    //	"DoPickup", "LSO.IsNullOrInvalid(targetCan.ToEntity.LootWindow)"));
            var cargoWindow = _eveWindowProvider.GetWindowByItemId(targetCan.ID);
		    if (LavishScriptObject.IsNullOrInvalid(cargoWindow))
		    {
		        LogMessage(methodName, LogSeverityTypes.Standard, "Opening cargo of target can {0} ({1})",
		                   targetCan.Name, targetCan.ID);
		        targetCan.Open();
		        return false;
		    }
		    //Move some cargo over. Leave at least one unit to prevent the can from popping.
		    //If the can is older than 1h, go ahead and eat it.
		    //Try to parse the time the can was created
		    //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
		    //	"DoPickup", "targetCan.Name"));
		    var name = targetCan.Name;
		    var canExpired = true;

		    //If it's an orca don't worry about popping it
		    switch (_haulerPickupType)
		    {
		        case HaulerPickupTypes.Jetcan:
		            var timeCanCreated = GetTimeCreatedFromJetcanName(name);

		            //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
		            //	"DoPickup", "hour set"));
		            if (timeCanCreated != DateTime.MinValue)
		            {
		                DateTime gameTime = _meCache.GameHour <= 11 ? 
                            new DateTime(1, 1, 2, _meCache.GameHour, _meCache.GameMinute, 0) : 
                            new DateTime(1, 1, 1, _meCache.GameHour, _meCache.GameMinute, 0);
		                //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
		                //	"DoPickup", String.Format("Parsed can creation time of {0}:{1}", canTime.Hour, canTime.Minute)));

		                if (gameTime.Subtract(timeCanCreated).Minutes <= 90)
		                {
		                    canExpired = false;
		                }
		            }
		            break;
		        case HaulerPickupTypes.Orca:
		            break;
		    }

		    //Get the list of items in the can for working with
		    var canItems = targetCan.ToEntity.GetCargo();

		    //If the can doesn't have any items yet it hasn't loaded. Just return.
		    if (canItems.Count == 0)
		    {
		        LogMessage(methodName, LogSeverityTypes.Standard, "Can has no items, probably still loading. Waiting...");
		        return false;
		    }

		    var canItemCount = canItems.Count;

		    var canIsFull = name.Contains("full", StringComparison.InvariantCultureIgnoreCase);

		    //bool for whether or not the owner is in range
		    var ownerInRange = false;
		    //Also check if the can is still within range of the miner.
		    var ownerName = string.Empty;
		    //Get the refernce first for efficiency with Entity access. Blech for inefficiency.
		    var can = targetCan.ToEntity;

		    if (can.Owner.IsValid)
		    {
		        ownerName = can.Owner.Name;
		    }

		    //Iterate all nearby entities and see if we can find the can's owner
		    foreach (var entity in _entityProvider.EntityWrappers.Where(entity => entity.Name == ownerName))
		    {
		        //If the owner is in loot range, consider it in range. Break regardless.
		        if (Distance(entity.X, entity.Y, entity.Z, targetCan.X, targetCan.Y, targetCan.Z) <= (int)Ranges.LootActivate)
		        {
		            ownerInRange = true;
		        }
		        break;
		    }

		    //Iterate all items we got out of the can
		    var emptiedCan = true;
		    foreach (var item in canItems)
		    {
		        //Decrement the canItemCount to signify we've gone through another item
		        canItemCount--;

		        //Make sure we can move at least one of the item before doing any other logic
		        if (_freeCapacity <= item.Volume) 
		            continue;

		        var requiredVolume = item.Quantity * item.Volume;

		        //If I require more cargo than I have, move what I can
		        if (requiredVolume > _freeCapacity)
		        {
		            //Get the quantity of items to move and update the free capacity
		            var quantityToMove = (int)Math.Floor(_freeCapacity / item.Volume);
		            _freeCapacity -= quantityToMove * item.Volume;

		            //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
		            //	"DoPickup", String.Format("Moving quantity {0} of item {1}", quantityToMove, i.Name)));

		            //Move the quantity of item to my ship
                    item.MoveTo(_meCache.Ship.Id, ToDestinationNames.CargoHold.ToString(), quantityToMove);
		            emptiedCan = false;

		            //Break the loop since we can't move anything else
		            break;
		        }

		        //See if I -need- to preserve the can, and if so if I even can
		        if (canItemCount <= 0 && !canExpired && !canIsFull && ownerInRange &&
		            !_cargoConfiguration.AlwaysPopCans)
		        {
		            //Signify we're preserving the can in logs...
		            LogMessage(methodName, LogSeverityTypes.Debug, "Preserving the can.");

		            //If I'm preserving, move all but one item
		            var quantityToMove = item.Quantity - 1;
		            _freeCapacity -= quantityToMove * item.Volume;
                    item.MoveTo(_meCache.Ship.Id, ToDestinationNames.CargoHold.ToString(), quantityToMove);

		            //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
		            //	"DoPickup", String.Format("Moving all but one ({0}) of item {1}", i.Quantity - 1, i.Name)));
		        }
		        else
		        {
		            //Signify we're not preserving the can in logs...
		            LogMessage(methodName, LogSeverityTypes.Debug, "Not preserving the can.");

		            //Not preserving the can, move it all.
		            _freeCapacity -= item.Quantity * item.Volume;
                    item.MoveTo(_meCache.Ship.Id, ToDestinationNames.CargoHold.ToString(), item.Quantity);

		            //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
		            //	"DoPickup", String.Format("Moving all {0} of item {1}", i.Quantity, i.Name)));
		        }
		    }

		    //If we picked up from an orca, remove other requests for the same orca
		    if (_haulerPickupType == HaulerPickupTypes.Orca)
		    {
		        for (var index = 1; index < _queuedPickupRequests.Count; index++)
		        {
		            if (_queuedPickupRequests[index].TargetCanEntityId != _queuedPickupRequests[0].TargetCanEntityId) 
		                continue;

		            _queuedPickupRequests.RemoveAt(index);
		            index--;
		        }
		    }

		    //If we're cycling, we can go ahead and shift to the next member.
		    //Cycle doesn't use QPR.
		    //If we don't need to make another trip to pop that can, dequeue it.
		    if (emptiedCan && _queuedPickupRequests.Count > 0)
		    {
		        var index = -1;

		        //Iterate queued pickup requests looking for our current request
		        for (var subIndex = 0; subIndex < _queuedPickupRequests.Count; subIndex++)
		        {
		            var e = _queuedPickupRequests[subIndex];

		            //Make sure we have a matching request for the can
		            //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
		            //    "DoPickup", String.Format("Checking request {0} against can {1}...", e.TargetCanEntityID, _pickupCanId)));
		            if (e.TargetCanEntityId != _pickupCanId) 
		                continue;

		            //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
		            //    "DoPickup", String.Format("Found matching request {0} for can {1}.", e.TargetCanEntityID, _pickupCanId)));
		            index = subIndex;
		            break;
		        }

		        //If we found a matching request for the can...
		        if (index >= 0)
		        {
		            //RemoveBookmarkAndCacheEntry it.
		            LogMessage(methodName, LogSeverityTypes.Debug, "Don't need another trip; dequeueing.");

		            var e = _queuedPickupRequests[index];
		            Core.Metatron.EventCommunications.FleetPickupCompletedEvent.SendEvent(e);
		            _queuedPickupRequests.RemoveAt(index);
		        }
		        else
		        {
		            //Otherwise don't dequeue it!
		            LogMessage(methodName, LogSeverityTypes.Debug, "Don't need another trip; no request to dequeue.");
		        }
		    }

		    //If I have free space see if I need to grab more cans
		    if (_meCache.Ship.CargoCapacity - _freeCapacity < _cargoConfiguration.CargoFullThreshold)
		    {
		        //See if we have any other nearby cans we need to grab.
		        var gotCan = false;
		        if (_currentFleetMemberId > -1)
		        {
		            IEntityWrapper currentFleetMember = null;
		            if (_entityProvider.EntityWrappersById.ContainsKey(_currentFleetMemberId))
		            {
		                currentFleetMember = _entityProvider.EntityWrappersById[_currentFleetMemberId];
		            }

		            if (currentFleetMember != null)
		            {
		                if (!_cansGrabbedThisPulse.Contains(_pickupCanId))
		                {
		                    _cansGrabbedThisPulse.Add(_pickupCanId);
		                }

		                var nearbyCans = GetJetcansNearFleetMember(currentFleetMember);

		                if (nearbyCans != null)
		                {
		                    foreach (var entity in nearbyCans.Where(entity => !_cansGrabbedThisPulse.Contains(entity.ID)))
		                    {
		                        _pickupCanId = entity.ID;
		                        gotCan = true;
		                        LogMessage(methodName, LogSeverityTypes.Debug, "Found unchecked can {0} ({1}) near fleet member {2} ({3}).",
		                                   entity.Name, entity.ID, currentFleetMember.Name, currentFleetMember.ID);
		                        break;
		                    }
		                }
		            }
		        }

		        if (!gotCan)
		        {
		            _pickupCanId = -1;
		        }

		        if ((Core.Metatron.Config.HaulingConfig.HaulerMode == HaulerModes.WaitForRequestEvent &&
		             (_queuedPickupRequests.Count > 0 || _pickupCanId > -1)) ||
		            (Core.Metatron.Config.HaulingConfig.HaulerMode == HaulerModes.CycleFleetMembers &&
		             (_fleetMembers.Count > 1 || _pickupCanId > -1)))
		        {
		            //If I have another can to pick up, do so
		            if (_pickupCanId > -1)
		            {
		                //pre-update the can
		                if (_entityProvider.EntityWrappersById.ContainsKey(_pickupCanId))
		                {
		                    _entityProvider.EntityWrappersById[_pickupCanId].RequestObjectRefresh();
		                }

		                LogMessage(methodName, LogSeverityTypes.Standard, "Done with this pickup and more nearby cans, doing pickup again.");
		                _haulerState = HaulerStates.DoPickup;
		                return true;
		            }

		            //Otherwise move
		            LogMessage(methodName, LogSeverityTypes.Standard, "Done with this pickup and have more to grab, moving to pickup.");
		            _haulerState = HaulerStates.GoToPickup;
		            return true;
		        }

		        //If I'm in waitforrequest I should idle. otherwise I should go to dropoff.
		        if (Core.Metatron.Config.HaulingConfig.HaulerMode == HaulerModes.WaitForRequestEvent)
		        {
		            LogMessage(methodName, LogSeverityTypes.Standard, "Done with this pickup and no more to grab, idling.");
		            _haulerState = HaulerStates.Idle;
		            return true;
		        }

		        LogMessage(methodName, LogSeverityTypes.Standard, "Done with this pickup and no more to grab, going to dropoff.");
		        _haulerState = HaulerStates.GoToDropoff;

                //Service the next member in fleet
                if (_lastFleetMemberIndex + 1 >= _fleetMembers.Count)
                    _lastFleetMemberIndex = 0;
                else
                    _lastFleetMemberIndex++;

		        return true;
		    }

		    //If I'm using WaitForRequest, see if I can bookmark something to warp to next goToPickup
		    if (Core.Metatron.Config.HaulingConfig.HaulerMode == HaulerModes.WaitForRequestEvent)
		    {
		        //Clear any old bookmarks
		        Core.Metatron.Bookmarks.RemoveTemporaryHaulingBookmarks();

		        //Check all requests, see if we have a nearby request other than the one just looted
		        if (_queuedPickupRequests.Count > 0)
		        {
		            var e = _queuedPickupRequests[0];
		            //If we've got a valid entity for the requested can... 
		            if (_entityProvider.EntityWrappersById.ContainsKey(e.TargetCanEntityId))
		            {
		                var entity = _entityProvider.EntityWrappersById[e.TargetCanEntityId];

		                //Temp bookmark it
		                Core.Metatron.Bookmarks.CreateTemporaryHaulingBookmark(entity);
		                LogMessage(methodName, LogSeverityTypes.Standard, "Creating temp hauling bookmark for requested can {0} ({1}).",
		                           entity.Name, entity.ID);
		            }
		        }
		    }

		    LogMessage(methodName, LogSeverityTypes.Standard, "No free cargo; going to dropoff.");
		    _haulerState = HaulerStates.GoToDropoff;
		    return true;
		}

		private void MoveToDropoff()
		{
			var methodName = "MoveToDropoff";
			LogTrace(methodName);

			var result = _moveToDropOffLocationPartialBehavior.Execute();

			if (result == BehaviorExecutionResults.Incomplete)
				return;

            if (result == BehaviorExecutionResults.Complete)
                _haulerState = HaulerStates.DoDropoff;

			if (result == BehaviorExecutionResults.Error)
				_haulerState = HaulerStates.Error;
		}

		private void DropoffCargo()
		{
			var methodName = "DropoffCargo";
			LogTrace(methodName);

			var result = _dropOffCargoPartialBehavior.Execute();

			if (result == BehaviorExecutionResults.Incomplete)
				return;

			if (result == BehaviorExecutionResults.Error)
				_haulerState = HaulerStates.Error;

			RecordDropoff();
            if (Core.Metatron.Config.HaulingConfig.HaulerMode == HaulerModes.CycleFleetMembers && Core.Metatron.Config.HaulingConfig.HaulerCycleFleetDelay > 0)
            {
                _serviceFleetWait = DateTime.Now.AddSeconds(Core.Metatron.Config.HaulingConfig.HaulerCycleFleetDelay);
                _haulerState = HaulerStates.CycleDelay;
            }
		    _haulerState = HaulerStates.Idle;
		}

		private void RecordDropoff()
		{
			_timeForTrip.Stop();
			Core.Metatron.Statistics.TrackDropoff(_timeForTrip.Elapsed);
			_timeForTrip.Reset();
			_timeForTrip.Start();
		}

        private void CycleDelay()
        {
            if (DateTime.Now.CompareTo(_serviceFleetWait) >= 0)
            {
                _haulerState = HaulerStates.Idle;
            }
        }

		private bool CanBounceWarpToJetcan(IEntityWrapper can)
		{
			var methodName = "CanBounceWarpToJetcan";
			LogTrace(methodName, "Entity: {0}", can.ID);

            return (Core.Metatron.Config.MovementConfig.UseBounceWarp &&
					can.Distance > Core.Metatron.Config.MovementConfig.MaxSlowboatTime *
					_meCache.ToEntity.MaxVelocity) &&
					can.Distance < (int)Ranges.Warp &&
					(_ship.TractorBeamModules.Count == 0 ||
                    can.Distance >= _ship.TractorBeamModules[0].OptimalRange.GetValueOrDefault(0));
		}

		private void TryQueueJetcansForTractoring(List<IEntityWrapper> cans)
		{
			var methodName = "TryQueueJetcansForTractoring";
			LogTrace(methodName);

			//prioritize cans marked "full", sub-order by the time a can was created
			cans = cans.OrderByDescending(entity => BoolToInt(entity.Name.Contains("full", StringComparison.InvariantCultureIgnoreCase))).ThenBy(
				entity => GetTimeCreatedFromJetcanName(entity.Name).Ticks).ToList();

			foreach (var entity in cans)
			{
				//also make sure I'm not a tard and queueing orcas.
				if (entity.Distance < (int)Ranges.Warp && entity.Distance > (int)Ranges.LootActivate &&
					!Core.Metatron.TargetQueue.IsQueued(entity.ID) &&
					entity.TypeID != (int)TypeIDs.Orca)
				{
					LogMessage(methodName, LogSeverityTypes.Debug, "Enqueueing can \'{0}\' ({1}) as a tractor beam target.",
						entity.Name, entity.ID);
					Core.Metatron.TargetQueue.EnqueueTarget(
						entity.ID, (int)TargetPriorities.Wreck_TractorSalvage, TargetTypes.LootSalvage);
				}
			}
		}

		private DateTime GetTimeCreatedFromJetcanName(string name)
		{
			var methodName = "GetTimeCreatedFromJetcanName";
			LogTrace(methodName, "Name: {0}", name);

			if (name.Contains(':') && name.Length >= name.IndexOf(':') + 1)
			{
				int hour, minute;
				//Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
				//	"DoPickup", "hour substring"));
				string hourString = string.Empty, minuteString = string.Empty;
				LogMessage(methodName, LogSeverityTypes.Debug, "Jetcan debug: Name: {0}, length: {1}", name, name.Length);

				//Get the index
				var index = name.IndexOf(':');
				//Need to have index +/- 2 characters to parse time
				if (name.Length - index >= 2 &&
					name.Length - 2 >= index)
				{
					minuteString = name.Substring(index + 1, 2);
					hourString = name.Substring(index - 2, 2);
				}

				if (int.TryParse(hourString, out hour) && int.TryParse(minuteString, out minute))
				{
					DateTime canTime;
					//Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
					//	"DoPickup", "hour set"));
					canTime = _meCache.GameHour <= 11 ?
						new DateTime(1, 1, 2, hour, minute, 0) : new DateTime(1, 1, 1, hour, minute, 0);
					return canTime;
					//Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
					//	"DoPickup", String.Format("Parsed can creation time of {0}:{1}", canTime.Hour, canTime.Minute)));
				}
			}
			return DateTime.MinValue;
		}

		private void BuildFleetMemberList()
		{
			var methodName = "BuildFleetMemberList";
			LogTrace(methodName);

			//List of fleet members to start with, copy instead of reference
			_fleetMembers = new List<FleetMember>(_meCache.FleetMembers);

			FleetMember member;
			for (var index = 0; index < _fleetMembers.Count; index++)
			{
				member = _fleetMembers[index];

				//If the member is us, we're not only hauling for skip list and it's on the skip list,
				// or we're only hauling for skip list and it's not on the skip list, remove it.
				if (member.CharID == _meCache.CharId ||
					(!Core.Metatron.Config.FleetConfig.OnlyHaulForSkipList &&
					Core.Metatron.Config.FleetConfig.FleetCharIDsToSkip.Contains(member.CharID)) ||
					(Core.Metatron.Config.FleetConfig.OnlyHaulForSkipList &&
					!Core.Metatron.Config.FleetConfig.FleetCharIDsToSkip.Contains(member.CharID)))
				{
					_fleetMembers.RemoveAt(index);
					index--;
				}
			}
		}

		private List<IEntityWrapper> GetJetcansNearFleetMember(IEntityWrapper fleetMember)
		{
			var methodName = "GetJetcansNearFleetMember";
			LogTrace(methodName, "FleetMember: {0}", fleetMember.Name);

			var fleetMembers = GetNearbyFleetMembers();
			var fleetMemberNames = fleetMembers.Select(x => x.Name).ToList();
			var fleetMemberOrcas = GetNearbyFleetMemberOrcas();

			var cans = (from ce in _entityProvider.EntityWrappers
					where !_cansGrabbedThisPulse.Contains(ce.ID) &&
						(ce.GroupID == (int)GroupIDs.CargoContainer &&
						fleetMemberNames.Contains(ce.ToEntity.Owner.Name) &&
						ce.ToEntity.HaveLootRights) ||
						(fleetMemberOrcas.Contains(ce.ID) &&
						ce.ToEntity.HaveLootRights)
					orderby BoolToInt(ce.Name.Contains("full", StringComparison.InvariantCultureIgnoreCase)) descending,
						Distance(fleetMember.X, fleetMember.Y, fleetMember.Z, ce.X, ce.Y, ce.Z) ascending
					select ce).ToList();

			LogMessage(methodName, LogSeverityTypes.Debug, "Got {0} cans near member {1} ({2}).",
				cans.Count, fleetMember.Name, fleetMember.ID);

			return cans;
		}

		private List<IEntityWrapper> GetNearbyFleetMembers()
		{
			var methodName = "GetNearbyFleetMembers";
			LogTrace(methodName);

			//If we need to update it...
			if (Core.Metatron.Pulses > _lastPulseUpdated)
			{
				//do so
				_lastPulseUpdated = Core.Metatron.Pulses;

				RebuildFleetMemberLists();
			}

			return _nearFleetMembers;
		}

		private List<Int64> GetNearbyFleetMemberOrcas()
		{
			var methodName = "GetNearbyFleetMemberOrcas";
			LogTrace(methodName);

			//If we need to update it...
			if (Core.Metatron.Pulses > _lastPulseUpdated)
			{
				//do so
				_lastPulseUpdated = Core.Metatron.Pulses;

				RebuildFleetMemberLists();
			}

			return _nearFleetMemberOrcas;
		}

		private void RebuildFleetMemberLists()
		{
			var methodName = "RebuildFleetMemberLists";
			LogTrace(methodName);

			//clear both existing lists
			_nearFleetMembers.Clear();
			_nearFleetMemberOrcas.Clear();

			//Gotta check the _fleetMembers list as well as EntityWrappers and find any collisions
			//Those are the orcas we can grab from
			foreach (var fleetMember in _fleetMembers)
			{
				foreach (var entity in _entityProvider.EntityWrappers)
				{
					string fleetMemberName;
					using (var fleetMemberPilot = fleetMember)
					{
						fleetMemberName = fleetMemberPilot.Name;
					}

					if (entity.CategoryID != (int)CategoryIDs.Ship || entity.ID == _meCache.ToEntity.Id || fleetMemberName != entity.Name) 
						continue;

					//Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
					//methodName, String.Format("Checking member {0} against entity {1}...", fm.ToPilot.Name, entity.Name)));
					//Any entities matching fleetMembers in orcas are valid.

					//Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
					//methodName, "Matched, adding..."));
					if (entity.TypeID == (int)TypeIDs.Orca)
					{
						//Add the entity's ID
						_nearFleetMemberOrcas.Add(entity.ID);
					}

					_nearFleetMembers.Add(entity);

					//foudn the only match for this fleet member
					break;
				}
			}
		}

		private void QueuePickupRequest(object sender, FleetNeedPickupEventArgs e)
		{
			var methodName = "QueuePickupRequest";
			LogTrace(methodName, "FleetMemberID: {0}", e.SendingFleetMemberCharId);

			if (_mainConfiguration.ActiveBehavior != BotModes.Hauling) 
				return;

			lock (this)
			{
				_requestsToValidate.Add(e);
			}

			LogMessage(methodName, LogSeverityTypes.Debug, "Received pickup request for can {0} from fleetmember {1} in solar system {2}.",
				e.TargetCanEntityId, e.SendingFleetMemberCharId, e.SolarSystemId);
		}

		private void ValidatePickupRequests()
		{
			var methodName = "ValidatePickupRequests";
			LogTrace(methodName);

			//If I haven't requested resend, request resends
			if (!_requestedPickupRequestResends)
			{
				Core.Metatron.EventCommunications.FleetSendAllPickupRequestsEvent.SendEventFromArgs(
					_meCache.CharId, _meCache.SolarSystemId);
				_requestedPickupRequestResends = true;
			}

			lock (this)
			{
				//Iterate all pickup requests and validate them if possible
				foreach (var e in _requestsToValidate)
				{
					//Don't need to check against the skip list as that's done when building _fleetMemberss
					var fmFound = _fleetMembers.Any(fleetMember => fleetMember.CharID == e.SendingFleetMemberCharId);

					//Iterate each of teh members of fleet, if we find one matching then it's a valid request
					if (fmFound)
					{
						LogMessage(methodName, LogSeverityTypes.Debug, "Validated pickup request for can {0} from fleetmember {1} in solar system {2}.",
							e.TargetCanEntityId, e.SendingFleetMemberCharId, e.SolarSystemId);

						_queuedPickupRequests.Add(e);
						Core.Metatron.EventCommunications.FleetNeedPickupConfirmedEvent.SendEventFromArgs(
							_meCache.CharId, _meCache.SolarSystemId,
							e.TargetCanEntityId, _meCache.ToEntity.Id, _meCache.ToEntity.Name);
					}
					else
					{
						LogMessage(methodName, LogSeverityTypes.Debug, "Could not find player with charID of {0} in our fleet, marking pickup request as invalid.",
							e.SendingFleetMemberCharId);
					}
				}
				_requestsToValidate.Clear();
			}
		}

		private void BounceWarp(IEntityWrapper asteroidToBookmark)
		{
			var methodName = "UseBounceWarp";
			LogTrace(methodName, "Entity: {0}", asteroidToBookmark.ID);

			switch (_bounceWarpState)
			{
				case BounceWarpStates.Idle:
					_bounceWarpState = BounceWarpStates.RemoveTemporaryBookmarks;
					goto case BounceWarpStates.RemoveTemporaryBookmarks;
				case BounceWarpStates.RemoveTemporaryBookmarks:
					//RemoveBookmarkAndCacheEntry any old bookmarks
					//Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
					//    "UseBounceWarp", "Removing temp hauling bookmarks."));
					Core.Metatron.Bookmarks.RemoveTemporaryHaulingBookmarks();
					_bounceWarpState = BounceWarpStates.CreateTemporaryBookmark;
					break;
				case BounceWarpStates.CreateTemporaryBookmark:
					var tempAsteroid = asteroidToBookmark;
					//If we have a valid asteroid within warp range, bookmark it
					if (tempAsteroid.Distance < (int)Ranges.Warp)
					{
						LogMessage(methodName, LogSeverityTypes.Debug, "Bookmarking can {0} ({1}).",
							tempAsteroid.Name, tempAsteroid.ID);
						Core.Metatron.Bookmarks.CreateTemporaryHaulingBookmark(tempAsteroid);
						_bounceWarpState = BounceWarpStates.SetTemporaryBookmark;
					}
					else
					{
						LogMessage(methodName, LogSeverityTypes.Debug, "Can was invalid.");
					}
					break;
				case BounceWarpStates.SetTemporaryBookmark:
					//Get the bookmark we created
					var tempCanBookmark = _bookMarkCache.FirstBookMarkStartingWith(
						Core.Metatron.Config.MovementConfig.TemporaryCanBookMarkPrefix, true);
					if (tempCanBookmark != null)
					{
						Core.Metatron.Bookmarks.TempCanBookmark = tempCanBookmark;
						LogMessage(methodName, LogSeverityTypes.Standard, "Using temp. can bookmark \'{0}\' for bounce warping.",
							tempCanBookmark.Label);

						//These two can be done in one step
						_bounceWarpState = BounceWarpStates.QueueDestinations;
						goto case BounceWarpStates.QueueDestinations;
					}
					break;
				case BounceWarpStates.QueueDestinations:
					IEntityWrapper tempPlanet = null;
					try
					{
						int groupID = (int)GroupIDs.Planet;
						tempPlanet = (from IEntityWrapper ce in _entityProvider.EntityWrappers
									  where ce.GroupID == groupID &&
									  ce.Distance > (Int64)Ranges.PlanetWarpIn
									  select ce).First();
					}
					catch (Exception) { }
					//If we got a planet, do something
					if (tempPlanet != null)
					{
						//If we're within 15,000,000 m of the planet, we're fucking close enough
						_movement.QueueDestination(
							new Destination(DestinationTypes.Entity, tempPlanet.ID) { Distance = (Int64)Ranges.PlanetWarpIn });
						//Warp to within our warpin distance
						_movement.QueueDestination(
							new Destination(DestinationTypes.BookMark, Core.Metatron.Bookmarks.TempCanBookmark.Id, 0));
					}
					else
					{
						//Couldn't find -anything- to bounce warp to; turn off bounce warp
						Core.Metatron.Config.MovementConfig.UseBounceWarp = false;
					}
					//}
					_bounceWarpState = BounceWarpStates.Idle;
					break;
			}
		}
	}

	internal enum HaulerStates
	{
		Idle,
		WaitingForRequest,
		GoToPickup,
		DoPickup,
		GoToDropoff,
		DoDropoff,
		Error,
		DefenseHasControl,
        CycleDelay
	}

	internal enum HaulerPickupTypes
	{
		Jetcan,
		Orca
	}
}
