using System;
using System.Collections.Generic;
using System.Linq;
using Metatron.Core;
using Metatron.ActionModules;
using LavishScriptAPI;
using Metatron.Core.Interfaces;
using Metatron.Core.EventCommunication;

namespace Metatron.BehaviorModules
{
    internal class MissionRunner : BehaviorBase
    {
        private MissionRunnerStates _missionRunnerState = MissionRunnerStates.Idle;
        private MissionTurnInStates _missionTurnInState = MissionTurnInStates.Idle;
        private GetMissionStates _getMissionState = GetMissionStates.Idle;
        public CachedMission ActiveMission;

		private readonly RandomWaitObject _betweenMissionsRandomWaitObject;
    	private readonly RandomWaitObject _duringMissionRandomWaitObject;

        private bool _startupJournalShut, _missionCompleted, _hasMaxRuntimeExpired;

		bool _anotherTripRequired;
		public bool MultipleCourierTripsRequired
		{
			get
			{
				return (ActiveMission.CargoVolume > Core.Metatron.MeCache.Ship.CargoCapacity);
			}
		}

		bool _finalCargoUnloadComplete;
        bool _surveyScanCompleted;

        public CachedAgent SelectedAgent { get; private set; }

		readonly string TAG_COURIER_START = "objective.source",
            TAG_COURIER_END = "objective.destination",
            TYPE_COURIER = "Courier",
			TYPE_MINING = "Mining",
			TYPE_ENCOUNTER = "Encounter",
            WINDOWNAME_JOURNAL = "journal",
			WINDOWNAME_SURVEYSCANNER = "SurveyScanView";

    	private CourierMissionStates _courierState = CourierMissionStates.Idle;

        private readonly IConfiguration _configuration;
        private readonly IMissionCache _missionCache;
        private readonly IAgentCache _agentCache;
        private readonly IEveWindowProvider _eveWindowProvider;

        internal MissionRunner(IConfiguration configuration, IMissionCache missionCache, IAgentCache agentCache, IEveWindowProvider eveWindowProvider)
        {
            _configuration = configuration;
            _missionCache = missionCache;
            _agentCache = agentCache;
            _eveWindowProvider = eveWindowProvider;

            BehaviorManager.BehaviorsToPulse.Add(BotModes.Missioning, this);
            ModuleName = "MissionRunner";
            CanSendCombatAssistanceRequests = true;
            PulseFrequency = 2;
            IsEnabled = true;

			_betweenMissionsRandomWaitObject = new RandomWaitObject(ModuleName);
			_betweenMissionsRandomWaitObject.AddWait(new KeyValuePair<int, int>(601, 1800), 0.75);
			_betweenMissionsRandomWaitObject.AddWait(new KeyValuePair<int, int>(121, 600), 3.33);
			_betweenMissionsRandomWaitObject.AddWait(new KeyValuePair<int, int>(11, 120), 7.5);
			_betweenMissionsRandomWaitObject.AddWait(new KeyValuePair<int, int>(6, 10), 15);
            _betweenMissionsRandomWaitObject.AddWait(new KeyValuePair<int, int>(3, 5), 33);
			_betweenMissionsRandomWaitObject.AddWait(new KeyValuePair<int, int>(1, 2), 66.67);

			_duringMissionRandomWaitObject = new RandomWaitObject(ModuleName);
			_duringMissionRandomWaitObject.AddWait(new KeyValuePair<int, int>(11, 22), 5);
			_duringMissionRandomWaitObject.AddWait(new KeyValuePair<int, int>(1, 4), 25);
        }

        public override void Pulse()
        {
            //Make sure I should be pulsing
        	if (!ShouldPulse()) 
				return;

        	StartPulseProfiling();

        	//Make sure I'm running this behavior
        	if (Core.Metatron.Config.MainConfig.ActiveBehavior != BotModes.Missioning)
        		return;
        	//Make sure we're not moving
        	if (Core.Metatron.Movement.IsMoving)
        		return;
        	//IF I'm fleeing, just return.
        	if (Core.Metatron.Defense.IsFleeing)
        		return;
            //Make sure the cargo is ready
            if (!Core.Metatron.Ship.IsInventoryReady)
                return;

            _hasMaxRuntimeExpired = HasMaxRuntimeExpired();

        	//Set and process pulse state
        	SetPulseState();
        	ProcessPulseState();

        	EndPulseProfiling();
        }

        protected override void SetPulseState()
        {
            var methodName = "SetPulseState";
			LogTrace(methodName);

			switch (_missionRunnerState)
			{
				//Start out by updating the mission cache
				case MissionRunnerStates.Idle:
					_missionRunnerState = MissionRunnerStates.UpdateMissionCache;
					break;
			}
        }

        protected override void ProcessPulseState()
        {
            var methodName = "ProcessPulseState";
			LogTrace(methodName);

            switch (_missionRunnerState)
            {
                //Update the mission cache:
                case MissionRunnerStates.UpdateMissionCache:
                    //Initially shut the journal. Needs to be opened by ISXEVE or funky shit happens.
                    if (!_startupJournalShut)
                    {
                        _startupJournalShut = true;
                        using (var journalWindow = _eveWindowProvider.GetWindowByName(WINDOWNAME_JOURNAL))
						{
							if (!LavishScriptObject.IsNullOrInvalid(journalWindow))
							{
								journalWindow.Close();
								return;
							}
						}
                    }

                    if (_missionCache.UpdateCache(0, true))
                    {
                        _missionRunnerState = MissionRunnerStates.SelectAgent;
                    }
                    break;
                //Pick an agent to use
                case MissionRunnerStates.SelectAgent:
                    if (_hasMaxRuntimeExpired)
                    {
                        return;
                    }

                    //If downtime is near, don't select an agent. Instead, pause.
					if (Core.Metatron.MeCache.IsDowntimeNear)
                    {
						LogMessage(methodName, LogSeverityTypes.Standard, "We're getting close to standard EVE downtime and are pausing missioning.");
                        IsEnabled = false;
                        return;
                    }

                    //Set the active agent
                    SelectAgent();

                    //If we didn't get an agent, return.
                    if (SelectedAgent == null)
                    {
                        LogMessage(methodName, LogSeverityTypes.Standard, "There are currently no agents available. We're waiting 5 minutes before checking again.");
                        DelayNextPulseBySeconds(300);
                        return;
                    }

                    //See if we have a matching, accepted mission
                    var cachedMission = _missionCache.GetCachedMissionForAgentId(SelectedAgent.Id);
                    if (cachedMission != null && cachedMission.State == (int)MissionStates.Accepted)
                    {
                        //Set it active.
                        ActiveMission = cachedMission;

                        //Go to the MoveToStart state.
                        _missionRunnerState = MissionRunnerStates.RunMission;
                        goto case MissionRunnerStates.RunMission;
                    }

                    //No cached mission returned or it wasn't accepted, means we need to go get a mission.
                    _missionRunnerState = MissionRunnerStates.MoveToAgent;
                    goto case MissionRunnerStates.MoveToAgent;
                case MissionRunnerStates.MoveToAgent:
                    //If I'm at the agent, go to MissionStart.
                    if (Core.Metatron.MeCache.SolarSystemId == SelectedAgent.SolarSystemId &&
                        Core.Metatron.MeCache.InStation && Core.Metatron.MeCache.StationId == SelectedAgent.StationId)
                    {
                        _missionRunnerState = MissionRunnerStates.GetMissionFromAgent;
                        goto case MissionRunnerStates.GetMissionFromAgent;
                    }
                    
            		if (Core.Metatron.MeCache.InStation && Core.Metatron.MeCache.StationId != SelectedAgent.StationId)
            		{
            			Core.Metatron.Movement.QueueDestination(new Destination(DestinationTypes.Undock));
            		}

            		//If I'm not in the same system, first move there
            		if (Core.Metatron.MeCache.SolarSystemId != SelectedAgent.SolarSystemId)
            		{
            			Core.Metatron.Movement.QueueDestination(new Destination(DestinationTypes.SolarSystem, SelectedAgent.SolarSystemId));
            		}
            		else if (!Core.Metatron.MeCache.InStation)
            		{
            			//Try to find a matching entitywrapper
            			var stationEntity = Core.Metatron.EntityProvider.EntityWrappers.FirstOrDefault(
							entityWrapper => entityWrapper.Name == SelectedAgent.Station);

            			//This will satisfy the conditions of the outer If check, thus it'll hit after dock.
            			Core.Metatron.Movement.QueueDestination(new Destination(DestinationTypes.Entity, stationEntity.ID) { Distance = 200, Dock = true });
            		}
            		break;
                case MissionRunnerStates.GetMissionFromAgent:
                    switch (_getMissionState)
                    {
                        case GetMissionStates.Idle:
                            _getMissionState = GetMissionStates.GetMission;
                            goto case GetMissionStates.GetMission;
                        case GetMissionStates.GetMission:
                            GetMissionFromAgent();
                            break;
                        case GetMissionStates.CheckLoadout:
                            var npcResistanceProfiles = Core.Metatron.MissionDatabase.GetNpcResistanceProfiles(ActiveMission);
                            var doneRearming = Core.Metatron.Ship.RearmShip(npcResistanceProfiles);

                            if (doneRearming)
                            {
                                _getMissionState = GetMissionStates.Idle;
                                _missionRunnerState = MissionRunnerStates.RunMission;
                            }
                            break;
                    }
                    break;
                case MissionRunnerStates.RunMission:
					if (_duringMissionRandomWaitObject.ShouldWait()) { return; }

                    RunMission();
                    break;
                case MissionRunnerStates.TurnInMission:

                    if (_betweenMissionsRandomWaitObject.ShouldWait()) { return; }
                    TurnInMission();

                    break;
            }
        }

        private void CallSalvager()
        {
            var methodName = "CallSalvager";
            LogTrace(methodName);

            if (!_configuration.SalvageConfig.CreateSalvageBookmarks)
            {
                return;
            }
            else
            {
                var salvageBookmarks = Core.Metatron.BookMarkCache.GetBookMarksStartingWith(_configuration.MovementConfig.SalvagingPrefix, false);
                LogTrace(methodName, $"Total: {salvageBookmarks.Count}");
                foreach (var bookMark in salvageBookmarks)
                {
                    if (bookMark.CreatorId == Core.Metatron.MeCache.Me.ID)
                    {
                        LogTrace(methodName, $"IsMine: {bookMark.Id}");
                        var bookmarkSafetyEvent = new SalvageBookmarkReadyEventArgs(_logging, bookMark.Id, Core.Metatron.MeCache.Name, Core.Metatron.MeCache.Me.ID, bookMark.SolarSystemId);
                        Core.Metatron.EventCommunications.SalvageBookmarkReadyEvent.SendEvent(bookmarkSafetyEvent);
                    }
                }
            }
        }

        private void GetMissionFromAgent()
        {
            var methodName = "GetMissionFromAgent";
            LogTrace(methodName);

            var stateResult = _missionCache.GetMissionFromAgent(SelectedAgent.Id);

            if (stateResult != ConversationStateResults.DeclineFailed)
            {
                if (stateResult == ConversationStateResults.MissionNotAccepted)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Did not accept mission from selected agent; selecting agent.");
                    _missionRunnerState = MissionRunnerStates.SelectAgent;
                    _getMissionState = GetMissionStates.Idle;
                }

                if (stateResult == ConversationStateResults.MissionAccepted)
                {
                    ActiveMission = _missionCache.GetCachedMissionForAgentId(SelectedAgent.Id);
                    LogMessage(methodName, LogSeverityTypes.Debug, "Successfully got mission from selected agent.");

                    _getMissionState = GetMissionStates.CheckLoadout;

                    //if (Core.Metatron.Ship.AbleToSwapAmmoLoadout)
                    //    _getMissionState = GetMissionStates.CheckLoadout;
                    //else
                    //{
                    //    _getMissionState = GetMissionStates.Idle;
                    //    _missionRunnerState = MissionRunnerStates.RunMission;
                    //}
                }
            }
            else
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Failed to decline mission from selected agent; selecting agent.");
                _missionRunnerState = MissionRunnerStates.SelectAgent;
                _getMissionState = GetMissionStates.Idle;
            }
        }

        protected override void _setCleanupState()
        {
            throw new NotImplementedException();
        }

        protected override void _processCleanupState()
        {
            throw new NotImplementedException();
        }

        private void RunMission()
        {
            var methodName = "RunMission";
			LogTrace(methodName);

            //Different "run" methods for different mission types
			if (ActiveMission.Type.Contains(TYPE_COURIER))
			{
				RunCourierMission();
			}
			else if (ActiveMission.Type.Contains(TYPE_MINING))
			{
				RunMiningMission();
			}
			else if (ActiveMission.Type.Contains(TYPE_ENCOUNTER))
			{
				RunEncounterMission();
			}
        }

        private void RunCourierMission()
        {
            var methodName = "RunCourierMission";
			LogTrace(methodName);

            if (_courierState == CourierMissionStates.Idle)
            	SetCourierState();

        	ProcessCourierState();
        }

    	private void SetCourierState()
		{
			var methodName = "SetCourierState";
			LogTrace(methodName);

			if (!Core.Metatron.Ship.IsCargoHoldActive)
			{
				LogMessage(methodName, LogSeverityTypes.Standard, "Making the ship cargo active.");
				Core.Metatron.Ship.MakeCargoHoldActive();
				return;
			}

			if (!Core.Metatron.Ship.CargoContainsItem(ActiveMission.TypeId))
				_courierState = CourierMissionStates.MoveToStart;
			else
				_courierState = CourierMissionStates.MoveToEnd;
		}

		private void ProcessCourierState()
		{
			var methodName = "ProcessCourierState";
			LogTrace(methodName);

			switch (_courierState)
			{
				case CourierMissionStates.MoveToStart:
			        var isAtMissionStartBookmark = _missionCache.IsAtMissionStartBookmark(ActiveMission);
                    if (isAtMissionStartBookmark)
					{
						_courierState = CourierMissionStates.LoadCourierCargo;
						goto case CourierMissionStates.LoadCourierCargo;
					}

					MoveToMissionStart();
					break;
				case CourierMissionStates.LoadCourierCargo:
					if (!Core.Metatron.Station.IsStationHangarActive)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Making station hangar active.");
						Core.Metatron.Station.MakeStationHangarActive();
						return;
					}

					var missionItem = Core.Metatron.MeCache.HangarItems.FirstOrDefault(
						item => item.TypeID == ActiveMission.TypeId);

					if (missionItem == null)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Mission item not found in station hangar; moving to mission end.");
						_courierState = CourierMissionStates.MoveToEnd;
						goto case CourierMissionStates.MoveToEnd;
					}

					//If I have space for cargo
					var freeCargoCapacity = Core.Metatron.MeCache.Ship.CargoCapacity - Core.Metatron.MeCache.Ship.UsedCargoCapacity;
					if (freeCargoCapacity >= missionItem.Volume)
					{
						//If another trip is required...
						var requiredCapacity = Core.Metatron.MeCache.HangarItems.Where(
							item => item.TypeID == ActiveMission.TypeId).Sum(
							item => item.Volume*item.Quantity);

						if (requiredCapacity > freeCargoCapacity)
						{
							LogMessage(methodName, LogSeverityTypes.Standard, "Cargohold has {0} m^3 free and {1} m^3 is needed for the (remaining) mission items - another trip is required.",
								freeCargoCapacity, requiredCapacity);
							_anotherTripRequired = true;
						}
						else
						{
							_anotherTripRequired = false;
						}

						var fitAllItems = false;
						LogMessage(methodName, LogSeverityTypes.Standard, "Moving mission items from hangar to cargo.");
						Core.Metatron.Ship.TransferItemFromHangarToCargo(ActiveMission.TypeId, out fitAllItems);
						_courierState = CourierMissionStates.MoveToEnd;
					}
					break;
				case CourierMissionStates.MoveToEnd:
			        var isAtMissionEndBookmark = _missionCache.IsAtMissionEndBookmark(ActiveMission);
                    if (isAtMissionEndBookmark)
					{
						_courierState = CourierMissionStates.UnloadCourierCargo;
						goto case CourierMissionStates.UnloadCourierCargo;
					}

					MoveToMissionEnd();
					break;
				case CourierMissionStates.UnloadCourierCargo:
					if (!Core.Metatron.Ship.IsCargoHoldActive)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Making ship cargohold active.");
						Core.Metatron.Ship.MakeCargoHoldActive();
						return;
					}

					LogMessage(methodName, LogSeverityTypes.Standard, "Transfering non-charge items to station hangar.");
					Core.Metatron.Ship.TransferCargoHoldToStationExcludingCategoryIds((int)CategoryIDs.Charge);
					_courierState = CourierMissionStates.StackCargo;
					break;
				case CourierMissionStates.StackCargo:
					if (!Core.Metatron.Station.IsStationHangarActive)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Making station hangar active.");
						Core.Metatron.Station.MakeStationHangarActive();
						return;
					}

					LogMessage(methodName, LogSeverityTypes.Standard, "Stacking station hangar.");
                    var eveWindow = _eveWindowProvider.GetInventoryWindow();
					eveWindow.StackAll();

					if (_anotherTripRequired)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Another trip was required, mission not yet complete.");
						_courierState = CourierMissionStates.MoveToStart;
						goto case CourierMissionStates.MoveToStart;
					}

					_courierState = CourierMissionStates.Idle;
					_missionRunnerState = MissionRunnerStates.TurnInMission;
					break;
			}
		}

		private void RunMiningMission()
		{
			var methodName = "RunMiningMission";
			LogTrace(methodName);

			//If the mission isn't complete
            var isAtMissionEndBookmark = _missionCache.IsAtMissionEndBookmark(ActiveMission);
			if (!_missionCompleted)
			{
                var isAtMissionStartBookmark = _missionCache.IsAtMissionStartBookmark(ActiveMission);
				//If my cargo's full...
				if (Core.Metatron.MeCache.Ship.UsedCargoCapacity >= Core.Metatron.Config.CargoConfig.CargoFullThreshold)
				{
					//if I'm not at the dropoff...
                    if (!isAtMissionEndBookmark)
					{
						//if I'm at the mission bookmark and the rock isn't there, set complete
                        if (isAtMissionStartBookmark)
						{
							if (Core.Metatron.EntityProvider.EntityWrappers.Count(
								x => x.TypeID == ActiveMission.TypeId) == 0)
							{
								_missionCompleted = true;
							}
						}
						//Move to the mission end bookmark
						MoveToMissionEnd();
					}
					else
					{
						if (Core.Metatron.MeCache.Ship.Ship.HasOreHold)
						{
							if (!Core.Metatron.Ship.IsOreHoldActive)
							{
								LogMessage(methodName, LogSeverityTypes.Standard, "Making the active ship's ore hold active.");
								Core.Metatron.Ship.MakeOreHoldActive();
							}
							else
							{
								Core.Metatron.Ship.TransferOreHoldToStationHangar();
							}
						}
						else
						{
							if (!Core.Metatron.Ship.IsCargoHoldActive)
							{
								LogMessage(methodName, LogSeverityTypes.Standard, "Making the active ship's cargo hold active.");
								Core.Metatron.Ship.MakeCargoHoldActive();
							}
							else
							{
								Core.Metatron.Ship.TransferOreInCargoHoldToStationHangar();								
							}
						}
					}
				}
				else
				{
					//if I'm not at the mission bookmark
					if (!isAtMissionStartBookmark)
					{
						//Move to the mission start bookmark
						MoveToMissionStart();
					}
					else
					{
						//Try to get an entity matching the target asteroid type
						var entities = Core.Metatron.EntityProvider.EntityWrappers.Where(
							entity => entity.TypeID == ActiveMission.TypeId).OrderBy(
							entity => entity.Distance).ToList();

						//If it was found
						if (entities.Count > 0)
						{
							//if I'm too far from the rock...
							//Only do this for the closest entity
							if (entities[0].Distance > Core.Metatron.Ship.MaximumMiningRange)
							{
								//I need to move to the rock.
								Core.Metatron.Movement.QueueDestination(new Destination(DestinationTypes.Entity,
									entities[0].ID) { Distance = Core.Metatron.Ship.MaximumMiningRange * 0.95 });
							}

							
							//If the target isn't queued...
							//Do this for all entities
							foreach (var entity in
								entities.Where(entity => !Core.Metatron.TargetQueue.IsQueued(entity.ID) && entity.Distance <= Core.Metatron.Ship.MaximumMiningRange))
							{
								Core.Metatron.TargetQueue.EnqueueTarget(
									entity.ID, (int)TargetPriorities.Mining, (int)TargetTypes.Mine);
							}
						}
						else
						{
							//If survey scan not completed...
                            /*if (!_surveyScanCompleted)
                            {
                                //If the survey window isn't visible...
                                var surveyWindow = _eveWindowProvider.GetWindowByName(WINDOWNAME_SURVEYSCANNER);
                                if (LavishScriptObject.IsNullOrInvalid(surveyWindow))
                                {
                                    Core.Metatron.Ship.Activate_ModuleList(Core.Metatron.Ship.Modules_SurveyScanners, true);
                                }
                                else
                                {
                                    //Set completed true, close window. This will make the entity pop up.
                                    _surveyScanCompleted = true;
                                    surveyWindow.Close();
                                }
                            }
                            else
                            {*/
                            //Mission's complete.
								//Stack before turnin
                            var inventoryWindow = _eveWindowProvider.GetInventoryWindow();
							inventoryWindow.StackAll();
								_missionCompleted = true;
							//}
						}
					}
				}
			}
			else
			{
				//if at the mission dropoff
				if (isAtMissionEndBookmark)
				{
					//if I haven't done final dropoff
					if (!_finalCargoUnloadComplete)
					{
						//Unload
						if (Core.Metatron.MeCache.Ship.Ship.HasOreHold)
						{
							if (!Core.Metatron.Ship.IsOreHoldActive)
							{
								LogMessage(methodName, LogSeverityTypes.Standard, "Making the active ship's ore hold active.");
								Core.Metatron.Ship.MakeOreHoldActive();
							}
							else
							{
								Core.Metatron.Ship.TransferOreHoldToStationHangar();
								_finalCargoUnloadComplete = true;
							}
						}
						else
						{
							if (!Core.Metatron.Ship.IsCargoHoldActive)
							{
								LogMessage(methodName, LogSeverityTypes.Standard, "Making the active ship's cargo hold active.");
								Core.Metatron.Ship.MakeCargoHoldActive();
							}
							else
							{
								Core.Metatron.Ship.TransferOreInCargoHoldToStationHangar();
								_finalCargoUnloadComplete = true;
							}
						}
					}
					else
					{
						//We're done.
						_missionCompleted = false;
						_finalCargoUnloadComplete = false;
						_surveyScanCompleted = false;
						_missionRunnerState = MissionRunnerStates.TurnInMission;
					}
				}
				else
				{
					//move to it
					MoveToMissionEnd();
				}
			}
		}

		private void RunEncounterMission()
		{
			var methodName = "RunEncounterMission";
			LogTrace(methodName);

			//If the mission isn't complete
			if (!_missionCompleted)
			{
				//If the mission processor isn't running
				if (Core.Metatron.MissionProcessor.MissionProcessorState == MissionProcessorStates.Idle)
				{
					//Start the mission processor. It will wait 'til we're there to do its thing.
					Core.Metatron.MissionProcessor.Start();
				}

				//If we're not currently processing a mission...
			    if (Core.Metatron.MissionProcessor.MissionProcessorState == MissionProcessorStates.ProcessMission) return;

			    //If the mission processor has completed the mission
			    if (Core.Metatron.MissionProcessor.MissionProcessorState == MissionProcessorStates.Finished)
			    {
			        //Mission is complete.
			        _missionCompleted = true;
			        return;
			    }

			    //It's not processing and we're not done. If not at the mission bookmark...
			    var isAtMissionStartBookmark = _missionCache.IsAtMissionStartBookmark(ActiveMission);
			    if (isAtMissionStartBookmark) return;

			    //And we have a matching mission...
			    var mission = Core.Metatron.MissionDatabase.GetMissionByName(ActiveMission.Name);
			    if (mission == null)
			    {
			        LogMessage(methodName, LogSeverityTypes.Standard, "Could not find entry in MissionDatabase.xml for mission \"{0}\".",
			                   ActiveMission.Name);
			        return;
			    }

			    var level = Core.Metatron.AgentCache.GetCachedAgent(ActiveMission.AgentId).Level;
			    var actionSet = mission.GetActionSetByLevel(level);

			    if (actionSet == null)
			    {
			        LogMessage(methodName, LogSeverityTypes.Standard, "Could not find entry in MissionDatabase.xml for mission \"{0}\", level {1}.",
			                   ActiveMission.Name, level);
			        return;
			    }

			    //Move there.
			    MoveToMissionStart();
			}
			else
			{
			    //Mission's complete - if we're not at the end bookmark...
			    var isAtMissionEndBookmark = _missionCache.IsAtMissionEndBookmark(ActiveMission);
			    if (isAtMissionEndBookmark)
			    {
			        //We're done and at the end bookmark. Reset variables and change state.
			        _missionCompleted = false;
			        Core.Metatron.MissionProcessor.Reset();
			        _missionRunnerState = MissionRunnerStates.TurnInMission;
			        return;
			    }

			    //Move there
			    MoveToMissionEnd();
			}
		}

        private void TurnInMission()
        {
            var methodName = "TurnInMission";
			LogTrace(methodName, "missionTurnInState: {0}", _missionTurnInState);

            switch (_missionTurnInState)
            {
                case MissionTurnInStates.Idle:
                    _missionTurnInState = MissionTurnInStates.TurnInMission;
                    goto case MissionTurnInStates.TurnInMission;
                case MissionTurnInStates.TurnInMission:
                    var missionTurnedIn = _missionCache.TurnInMission(SelectedAgent);

                    if (missionTurnedIn)
                    {
                        CallSalvager();
                        _missionTurnInState = MissionTurnInStates.UpdateCache;
                    }
                    break;
                case MissionTurnInStates.UpdateCache:
                    var cacheUpdated = _missionCache.UpdateCache(true);

                    if (cacheUpdated)
                    {
                        _missionTurnInState = MissionTurnInStates.Idle;
                        _missionRunnerState = MissionRunnerStates.SelectAgent;
                    }
                    break;
            }
        }

        private bool MoveToMissionStart()
        {
            var methodName = "MoveToMissionStart";
			LogTrace(methodName);

            var isAtMissionStartBookmark = _missionCache.IsAtMissionStartBookmark(ActiveMission);
            if (isAtMissionStartBookmark)
        	{
        		//We're there, do the mission
        		//Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Standard,
        		//methodName, string.Format("At mission start bookmark; will run mission.")));
        		//_missionRunnerState = MissionRunnerStates.RunMission;
        		return true;
        	}

        	//Need to behave a bit differently for each mission type.
        	//For Couriers, we need to go to the "objective.source"
        	//If I did this right it'll be handled by MissionCache
        	//Get the bookmark for the active mission
        	var missionStartBookmark = _missionCache.GetMissionStartBookmark(ActiveMission);
        	//build and add a destination for it
			LogMessage(methodName, LogSeverityTypes.Standard, "Moving to mission start bookmark \"{0}\" ({1}, {2}, {3}).",
				missionStartBookmark.Label, missionStartBookmark.X, missionStartBookmark.Y, missionStartBookmark.Z);

        	var bookmarkDestination = new Destination(DestinationTypes.MissionBookmark)
        	                                  	{
        	                                  		BookMarkTypeTag = missionStartBookmark.LocationType,
        	                                  		MissionAgentId = ActiveMission.AgentId
        	                                  	};
        	Core.Metatron.Movement.QueueDestination(bookmarkDestination);
        	return false;
        }

        private bool MoveToMissionEnd()
        {
            var methodName = "MoveToMissionEnd";
			LogTrace(methodName);

            var isAtMissionEndBookmark = _missionCache.IsAtMissionEndBookmark(ActiveMission);
            if (isAtMissionEndBookmark)
        	{
        		//If we don't require another trip...
        		return true;
        	}

        	//Get the bookmark for the active mission
        	var missionEndBookmark = _missionCache.GetMissionEndBookmark(ActiveMission);

        	//build and add a destination for it
			LogMessage(methodName, LogSeverityTypes.Standard, "Moving to mission end bookmark \"{0}\".",
				missionEndBookmark.Label);
        	var bookmarkDestination = new Destination(DestinationTypes.MissionBookmark)
        	                                  	{
        	                                  		BookMarkTypeTag = missionEndBookmark.LocationType,
        	                                  		MissionAgentId = ActiveMission.AgentId
        	                                  	};
        	Core.Metatron.Movement.QueueDestination(bookmarkDestination);
        	return false;
        }

        private void SelectAgent()
        {
            var methodName = "SelectAgent";
            LogTrace(methodName);

            //First get a list of missions to work with, sorted by state. Accepted missions are higher priority.
            var missions = _missionCache.CachedMissions
                .Where(cm => _configuration.MissionConfig.DoStorylineMissions || !_missionCache.IsStorylineMission(cm.Type))
                .OrderByDescending(cm => BoolToInt(cm.State == (int)MissionStates.Accepted))
                .ThenByDescending(cm => BoolToInt(_missionCache.IsStorylineMission(cm.Type)))
                .ThenBy(cm => _configuration.MissionConfig.Agents.IndexOf(_agentCache.GetCachedAgent(cm.AgentId).Name))
                .ToList();

            //List of agents to skip due to decline timers
            var skipList = new List<int>();

            //Iterate all of the missions and see if I have any accepted or offered
            foreach (var mission in missions)
            {
                //Make sure we can run the mission. Meaning not blacklisted, not lowsec.
                //if (mission.CheckMission())
                if (_missionCache.IsMissionAcceptible(mission))
                {
                    if (mission.State == (int)MissionStates.Accepted || _missionCache.IsStorylineMission(mission.Type))
                    {
                        //Set the active agent.
                        SelectedAgent = _agentCache.GetCachedAgent(mission.AgentId);
                        var statusString = "unknown";

                        //Hotswap the "status" of the mission
                        if (mission.State == (int)MissionStates.Offered)
                            statusString = "offered";
                        else if (mission.State == (int)MissionStates.Accepted)
                            statusString = "accepted";

                        LogMessage(methodName, LogSeverityTypes.Debug, "Setting agent {0} for {1} mission {2} active.",
                            SelectedAgent.Name, statusString, mission.Name);
                        return;
                    }
                }
                else
                {
                    var agent = _agentCache.GetCachedAgent(mission.AgentId);

                    //Check if I have to skip-list the agent.
                    //If I'm not ignoring the decine timer and it's too soon to decline...
                    if (!_configuration.MissionConfig.IgnoreMissionDeclineTimer && DateTime.Now.CompareTo(agent.NextMissionDeclinable) < 0)
                    {
                        //Nope, can't decline this one. Skip list it for now.
                        LogMessage(methodName, LogSeverityTypes.Debug, "Skiplisting agent {0} ({1}).",
                            agent.Name, agent.Id);
                        skipList.Add(agent.Id);
                    }
                }
            }

            //No valid missions in the journal apparently. Pick an agent.
            //Iterate the ResearchAgent entries in Config.Mission.ResearchAgents
            foreach (var agentName in _configuration.MissionConfig.ResearchAgents)
            {
                //If the agent is available and not skiplisted, set it active.
                var agent = _agentCache.GetCachedAgent(agentName);

                if (DateTime.Now.CompareTo(agent.NextResearchMissionAvailable) < 0 || skipList.Contains(agent.Id))
                    continue;

                SelectedAgent = agent;

                LogMessage(methodName, LogSeverityTypes.Debug, "Setting research agent {0} active.",
                    SelectedAgent.Name);
                return;
            }

            //Iterate the Agent entries in Config.Mission.Agents
            foreach (var agentName in _configuration.MissionConfig.Agents)
            {
                //If the skip list doesn't contain this agent, make it active.
                var agent = _agentCache.GetCachedAgent(agentName);
                if (skipList.Contains(agent.Id))
                    continue;

                SelectedAgent = agent;

                LogMessage(methodName, LogSeverityTypes.Debug, "Setting agent {0} active.",
                    SelectedAgent.Name);
                return;
            }

            SelectedAgent = null;
        }
    }

    public enum MissionRunnerStates
    {
        Idle,
		Fleeing,
        UpdateMissionCache,
        SelectAgent,
        MoveToAgent,
        GetMissionFromAgent,
        RunMission,
        TurnInMission
    }

    public enum MissionTurnInStates
    {
        Idle,
        TurnInMission,
        UpdateCache,
    }

    public enum GetMissionStates
    {
        Idle,
        GetMission,
        CheckLoadout
    }

	public enum CourierMissionStates
	{
		Idle,
		MoveToStart,
		LoadCourierCargo,
		MoveToEnd,
		UnloadCourierCargo,
		StackCargo
	}
}
