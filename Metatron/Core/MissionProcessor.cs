using System;
using System.Collections.Generic;
using System.Linq;
using EVE.ISXEVE.Interfaces;
using Metatron.ActionModules;
using LavishScriptAPI;
using EVE.ISXEVE;
using Metatron.Core.Extensions;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    // ReSharper disable ConvertToConstant.Local
    // ReSharper disable InconsistentNaming
    // ReSharper disable PossibleMultipleEnumeration
    // ReSharper disable ImplicitlyCapturedClosure
    //Can't yet pull an interface due to dependency on Attackers
	internal class MissionProcessor : ModuleBase
	{
	    private IEntityProvider _entityProvider;
	    private IAttackers _attackers;

		private MissionProcessorStates _missionProcessorState = MissionProcessorStates.Idle;
		public MissionProcessorStates MissionProcessorState
		{
			get { return _missionProcessorState; }
		}

		//Stuff used for Loot
        private readonly int MaxLootTimeouts = 3;  //Obfuscatable const

		private readonly List<Int64> _lootCanBlacklist = new List<long>();
		private int _lootTimeouts = 3;

		//Used for re-queueing NextRoom actions after flee
		private int _currentRoom;

		//For the ClearRoom action
		private DateTime _timeoutExpired = DateTime.MinValue;
		private int _timesClear = 3;
		private readonly int MinimumTimesClear = 3;

		//For Approach BreakOnSpawn
		private int _lastNpcCount;

		//Keep track of whether or not we've queued the gate for the NextRoom action
		private bool _nextRoomGateQueued;
		//Also keep track of the entity ID of the last gate
		private Int64 _nextRoomGateEntityId = -1;
        //Track whether or not we've killed something and should salvage
	    private bool _shouldSalvageRoom = false;

		//Keep track of actions for this mission
		List<Action> _actions = new List<Action>();

	    private readonly IEveWindowProvider _eveWindowProvider;
	    private readonly IMovement _movement;

		public MissionProcessor(IEveWindowProvider eveWindowProvider, IMovement movement)
		{
		    _eveWindowProvider = eveWindowProvider;
		    _movement = movement;

		    ModuleManager.ModulesToPulse.Add(this);
			ModuleName = "MissionProcessor";
			IsEnabled = true;
			PulseFrequency = 2;
		}

        public override bool Initialize()
        {
            if (!IsInitialized)
            {
                _entityProvider = Metatron.EntityProvider;
                _attackers = Metatron.Attackers;

                IsInitialized = true;
            }

            return IsInitialized;
        }

		public override void Pulse()
		{
			if (!ShouldPulse()) 
				return;

			StartPulseProfiling();

			switch (_missionProcessorState)
			{
				case MissionProcessorStates.Idle:
					//Do nothing
					break;
				case MissionProcessorStates.GetMissionRecord:
					//If we finished getting mission record..
					if (GetMissionRecord())
					{
						//And the current state is waitForArrival
						if (_missionProcessorState == MissionProcessorStates.WaitForArrival)
						{
							//goto the case
							goto case MissionProcessorStates.WaitForArrival;
						}
					}
					break;
				case MissionProcessorStates.WaitForArrival:
					//If I'm at the mission bookmark, go ahead and start
					//if (Metatron.MissionRunner.ActiveMission.IsAtMissionStartBookmark())
			        var isAtMissionStartbookmark = Metatron.MissionCache.IsAtMissionStartBookmark(Metatron.MissionRunner.ActiveMission);
                    if (isAtMissionStartbookmark)
					{
						_missionProcessorState = MissionProcessorStates.ProcessMission;
						goto case MissionProcessorStates.ProcessMission;
					}
					break;
				case MissionProcessorStates.Fleeing:
					if (!Metatron.Defense.IsFleeing)
					{
						//If we finished reloading mission record..
						if (ReloadMissionRecord())
						{
							//And the current state is waitForArrival
							if (_missionProcessorState == MissionProcessorStates.WaitForArrival)
							{
								//goto the case
								goto case MissionProcessorStates.WaitForArrival;
							}
						}
					}
					break;
				case MissionProcessorStates.ProcessMission:
					if (Metatron.Defense.IsFleeing)
					{
						_missionProcessorState = MissionProcessorStates.Fleeing;
						goto case MissionProcessorStates.Fleeing;
					}

					ProcessMission();
					break;
				case MissionProcessorStates.Finished:
					//Also do nothing
					break;
			}

			EndPulseProfiling();
		}

		public void Start()
		{
			var methodName = "Start";
			LogTrace(methodName);

			if (_missionProcessorState == MissionProcessorStates.Idle ||
				_missionProcessorState == MissionProcessorStates.Finished)
			{
				_missionProcessorState = MissionProcessorStates.GetMissionRecord;
			}
		}

		public void Reset()
		{
			var methodName = "Reset";
			LogTrace(methodName);

			//Reset all variables
			_missionProcessorState = MissionProcessorStates.Idle;
			_timeoutExpired = DateTime.MinValue;
			_timesClear = 3;
			_currentRoom = 0;
			_nextRoomGateQueued = false;
			_actions.Clear();
			_lootCanBlacklist.Clear();
		}

		private bool GetMissionRecord()
		{
			var methodName = "GetMissionRecord";
			LogTrace(methodName);

		    var actions = GetActionsForMission();
            if (actions == null)
                return false;

		    _actions = actions;

		    LogMessage(methodName, LogSeverityTypes.Standard, "Got action set for mission \"{0}\", waiting for arrival at mission.",
                Metatron.MissionRunner.ActiveMission.Name);
			_missionProcessorState = MissionProcessorStates.WaitForArrival;
			return true;
		}

	    private List<Action> GetActionsForMission()
	    {
	        var methodName = "GetActionsForMission";
            LogTrace(methodName);

	        var missionName = Metatron.MissionRunner.ActiveMission.Name;
	        var mission = Metatron.MissionDatabase.GetMissionByName(missionName);

	        //Make sure we got a valid mission
	        if (mission == null)
	        {
                LogMessage(methodName, LogSeverityTypes.Standard, "Could not find mission entry for \"{0}\".",
                    missionName);
	            Metatron.MissionDatabase.ReloadMissionDatabase();
	            return null;
	        }

	        var level = Metatron.AgentCache.GetCachedAgent(Metatron.MissionRunner.ActiveMission.AgentId).Level;
	        var actionSet = mission.GetActionSetByLevel(level);

	        //Make sure we got a valid action set
	        if (actionSet == null)
	        {
	            LogMessage(methodName, LogSeverityTypes.Standard, "Could not find action set in mission entry for \"{0}\" level {1}.",
                    missionName, level);
	            Metatron.MissionDatabase.ReloadMissionDatabase();
	            return null;
	        }

	        //Copy the actions from the database
	        return new List<Action>(actionSet.Actions);
	    }

	    private bool ReloadMissionRecord()
		{
			var methodName = "ReloadMissionRecord";
			LogTrace(methodName);

	        var actions = GetActionsForMission();
            if (actions == null)
                return false;

			//Clear the old action list
			_actions.Clear();

			foreach (var action in actions)
			{
				if (_currentRoom > 0)
				{
					if (action.Name == "NextRoom")
					{
						_currentRoom--;
						_actions.Add(action);
					}
				}
				else
				{
					_actions.Add(action);
				}
			}

			LogMessage(methodName, LogSeverityTypes.Standard, "Re-loaded action set for mission \"{0}\" after fleeing.",
                Metatron.MissionRunner.ActiveMission.Name);
			_missionProcessorState = MissionProcessorStates.WaitForArrival;
			return true;
		}

		private void ProcessMission()
		{
			var methodName = "ProcessMission";
			LogTrace(methodName);

			if (_actions.Count > 0)
			{
                if (_lastNpcCount == 0)
                    _lastNpcCount = GetCurrentNpcCount();

				var action = _actions[0];
				var removeAction = ProcessAction(action);

				if (!removeAction)
				{
					/* Uncomment this later when I want to experiment with it
					if (_actions.Count > 1)
					{
						Action firstAction = _actions[0], secondAction = _actions[1];

						//If I can "look ahead"
						if ((firstAction.Name == "ClearRoom" || firstAction.Name == "Kill") &&
							!Core.Metatron.Movement.IsMoving)
						{

						}
					}
					 */
				}
				else
				{
					//If this is the last clear room before a gate, or the last clear room, create a salvage bookmark
					if (_shouldSalvageRoom && !action.PreventSalvageBookmark)
					{
						var shouldCreateBookmark = false;

						//If this is the last action period, create a salvage bookmark
					    if (_actions.Count == 1)
					    {
                            LogMessage(methodName, LogSeverityTypes.Debug, "This action is the last action. We should create a salvage bookmark.");
					        shouldCreateBookmark = true;
					    }
					    else
					    {
					        //If the next action is 'next room', create a salvage bookmark
					        var nextAction = _actions[1];

					        if (nextAction.Name == "NextRoom")
					        {
                                LogMessage(methodName, LogSeverityTypes.Debug, "The action after this action is a NextRoom. We should create a salvage bookmark.");
					            shouldCreateBookmark = true;
					        }
					    }

					    if (shouldCreateBookmark)
					    {
					        _shouldSalvageRoom = false;
							var wreck = _entityProvider.EntityWrappers.Where(e => e.GroupID == (int)GroupIDs.Wreck).First();
                            Metatron.Bookmarks.CreateSalvagingBookmark(wreck);
						}
					}

					LogMessage(methodName, LogSeverityTypes.Standard, "Action \"{0}\" completed; removing.",
						action.Name);
					_actions.RemoveAt(0);
				}

				return;
			}

		    _lastNpcCount = 0;

			LogMessage(methodName, LogSeverityTypes.Standard, "No more actions for this mission; finished.");
			_missionProcessorState = MissionProcessorStates.Finished;
		}

		private bool ProcessAction(Action action)
		{
			var methodName = "ProcessAction";
			LogTrace(methodName, "Action: {0}", action.Name);

		    var target = GetTargetEntityForAction(action);

            //If we're breaking on spawn and npcs have spawned, we're done.
            var npcCount = GetCurrentNpcCount();
		    var returnForBreakOnSpawn = false;
            if (npcCount > _lastNpcCount)
            {
                if (action.BreakOnSpawn)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Breaking on spawn and npc count increased by {0}.",
                        npcCount - _lastNpcCount);
                    BreakAction(target);
                    returnForBreakOnSpawn = true;
                }
            }
		    _lastNpcCount = npcCount;

            if (returnForBreakOnSpawn)
                return true;

            //If we're breaking on targeted, and are being targeted by NPCs, we're done.
            if (action.BreakOnTargeted)
            {
                var npcsTargetingMe = _entityProvider.EntityWrappers.Count(x => x.IsNPC && x.IsTargetingMe);
                if (npcsTargetingMe > 0)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Breaking on targeted and targeted by {0} npcs.",
                        npcsTargetingMe);
                    BreakAction(target);
                    return true;
                }
            }

			switch (action.Name)
			{
				case "NextRoom":
					return ProcessNextRoom(action);
				case "ClearRoom":
					return ProcessClearRoom(action);
				case "Approach":
					return ProcessApproach(action, target);
                case "Loot":
			        {
			            var lootComplete = ProcessLoot(action);

                        if (!lootComplete)
                        {
                            if (_movement.IsNearbyCollidableAvoidanceEnabled)
                                _movement.DisableNearbyCollidableAvoidance();
                        }
                        else
                        {
                            if (!_movement.IsNearbyCollidableAvoidanceEnabled)
                                _movement.EnableNearbyCollidableAvoidance();
                        }

			            return lootComplete;
			        }
			    case "Kill":
					return ProcessKill(action, target);
				default:
					return false;
			}
		}

	    private int GetCurrentNpcCount()
	    {
	        return _entityProvider.EntityWrappers.Count(x => x.IsNPC && x.CategoryID != (int)CategoryIDs.Charge);
	    }

	    private bool ProcessNextRoom(Action action)
		{
			var methodName = "ProcessNextRoom";
			LogTrace(methodName, "GateName: {0}", action.GateName);

			if (Metatron.Movement.IsMoving || Metatron.MeCache.ToEntity.Mode == (int)Modes.Warp)
				return false;

	        var gateEntities = _entityProvider.EntityWrappers.Where(entity => entity.GroupID == (int) GroupIDs.WarpGate);

            if (!string.IsNullOrEmpty(action.GateName))
            {
                gateEntities = gateEntities.Where(entity => entity.Name.Contains(action.GateName, StringComparison.InvariantCultureIgnoreCase));
            }

	        var gateEntity = gateEntities.FirstOrDefault();
			
			if (_nextRoomGateQueued)
			{
				if (gateEntity == null || gateEntity.ID != _nextRoomGateEntityId)
				{
					_nextRoomGateEntityId = 0;
					_nextRoomGateQueued = false;
					if (gateEntity == null)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "No gate found; done with action.");
					}
					else
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Different gate found; done with action.");
					}
					return true;
				}
			}
			else
			{
				if (gateEntity == null)
				{
					_nextRoomGateEntityId = 0;
					_nextRoomGateQueued = false;
					LogMessage(methodName, LogSeverityTypes.Standard, "No gate found; done with action.");
					return true;
				}

				LogMessage(methodName, LogSeverityTypes.Standard, "Moving to and using gate \"{0}\" ({1}) for NextRoom action.",
					gateEntity.Name, gateEntity.ID);
				var gateDestination = new Destination(DestinationTypes.Entity, gateEntity.ID) { UseGate = true };
				Metatron.Movement.QueueDestination(gateDestination);
				
				_currentRoom++;
				_nextRoomGateQueued = true;
				_nextRoomGateEntityId = gateEntity.ID;
			}
			return false;
		}

		private bool ProcessClearRoom(Action action)
		{
			var methodName = "ProcessClearRoom";
			LogTrace(methodName);

			//So, I'm in a mission room, how do I decide what gets shot?
			//If I have non-triggers...
				//and any are shooting me, queue 'em all and prepare for RAEP.
				//otherwise, queue only the first one.
			//otherwise, queue only the first non-trigger.

			//This way, I take down any aggro, and when out of aggro will only shoot things one by one,
			//so I only end up pulling one group at a time instead of a rat from group A and a rat from group B,
			//thus pulling the whole goddamn room.

            var validEntities = _entityProvider.EntityWrappers
                .Where(entity => !Metatron.Attackers.IsConcordTarget(entity.GroupID) && _attackers.IsRatTarget(entity) &&
                    !action.DoNotKillNames.Any(doNotKillName => entity.Name.Contains(doNotKillName, StringComparison.InvariantCultureIgnoreCase)));

			//Get the list of entities which are possible triggers

            var possibleTriggerNpcEntities = validEntities
                .Where(entity => action.PossibleTriggerNames.Any(possibleTriggerName => entity.Name.Contains(possibleTriggerName, StringComparison.InvariantCultureIgnoreCase)));

		    var npcCountsByName = possibleTriggerNpcEntities
		        .GroupBy(entity => entity.Name)
		        .Select(list => new {Name = list.Key, Count = list.Count()})
		        .ToDictionary(entry => entry.Name, entry => entry.Count);

		    possibleTriggerNpcEntities = possibleTriggerNpcEntities.OrderByDescending(
		        entity => npcCountsByName[entity.Name]);

			//Get the list of entities which are not possible triggers
            var nonTriggerNpcEntities = validEntities
                .Where(entity => !action.PossibleTriggerNames.Any(possibleTriggerName => entity.Name.Contains(possibleTriggerName, StringComparison.InvariantCultureIgnoreCase)))
                .OrderBy(entity => entity.Distance).ThenBy(entity => entity.Bounty);

            var npcEntitiesToKill = new List<IEntityWrapper>();
			//if I have non-triggers...
			if (nonTriggerNpcEntities.Any())
			{
				//See how many are shooting me!
			    var nonTriggerNpcEntitiesShootingMe = nonTriggerNpcEntities
			        .Where(entity => _attackers.ThreatEntities.Any(threatEntity => threatEntity.ID == entity.ID));

				//If I have some shooting me, kill 'em all!
				if (nonTriggerNpcEntitiesShootingMe.Any())
				{
					npcEntitiesToKill.AddRange(nonTriggerNpcEntitiesShootingMe);
				}
				//otherwise, kill only the FIRST entity not shooting us so we don't get aggroraepd!
				else
				{
					npcEntitiesToKill.Add(nonTriggerNpcEntities.First());
				}
			}
			//no non-triggers? Kill oly the FIRST trigger so we don't get aggro raepd!
			else if (possibleTriggerNpcEntities.Any())
			{
                //Don't add another thing to kill unless we're out of things to kill
                if (Metatron.TargetQueue.Targets.Count == 0)
				    npcEntitiesToKill.Add(possibleTriggerNpcEntities.First());
			}

			//If I got anything to kill, do so.
			if (npcEntitiesToKill.Count > 0)
			{
                //We're killing something; we should salvage
			    _shouldSalvageRoom = true;

				//Approach if I need to, otherwise kill shit.
				var targetEntity = npcEntitiesToKill.First();

				if (!Metatron.Movement.IsMoving && NeedToApproachEntity(targetEntity))
				{
					var distance = Metatron.Ship.GetMaximumWeaponRange();
					if (distance > Metatron.Ship.MaxTargetRange)
					{
						distance = Metatron.Ship.MaxTargetRange;
					}

					var entityDestination = new Destination(DestinationTypes.Entity, targetEntity.ID) { Distance = distance * 0.95 };
					LogMessage(methodName, LogSeverityTypes.Standard, "Closest entity \"{0}\" ({1}) is out of maximum weapon range - getting within {2} of it.",
						targetEntity.Name, targetEntity.ID, entityDestination.Distance);
					Metatron.Movement.QueueDestination(entityDestination);
				}
				else
				{
					//Queue all entities listed in entitiesToKill
					foreach (var entity in npcEntitiesToKill)
					{
						//If we have a valid QueueTarget built in Attackers, use it, otherwise build our own
						if (Metatron.Attackers.QueueTargetsByEntityId.ContainsKey(entity.ID))
						{
							//Get the ship type, queue the entity with ship type
							var target = Metatron.Attackers.QueueTargetsByEntityId[entity.ID];
							Metatron.TargetQueue.EnqueueTarget(target.Id, target.Priority, target.Type);
						}
						else
						{
							Metatron.TargetQueue.EnqueueTarget(entity.ID, Metatron.Attackers.GetTargetPriority(entity), TargetTypes.Kill);
						}
					}
				}
			}

			//Wow, no entities at all? Shit sucks. Check for the timeouts!
			if (!validEntities.Any())
			{
				//If there's no timeout specified...
				if (action.TimeoutSeconds == 0)
				{
					if (_timesClear-- <= 0)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "All entities killed, ClearRoom action complete.");
						_timesClear = MinimumTimesClear;

						return true;
					}
				}
				else
				{
					if (_timeoutExpired == DateTime.MinValue)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "All current entities killed, waiting for timeout or spawn.");
						_timeoutExpired = DateTime.Now.AddSeconds(action.TimeoutSeconds);
					}
					else
					{
						if (DateTime.Now.CompareTo(_timeoutExpired) >= 0)
						{
							LogMessage(methodName, LogSeverityTypes.Standard, "All new entities killed or timeout expired, ClearRoom action complete.");
							_timeoutExpired = DateTime.MinValue;
							return true;
						}
					}
				}
			}

			return false;
		}

		private bool ProcessApproach(Action action, IEntityWrapper target)
		{
			var methodName = "ProcessApproach";
			LogTrace(methodName);

			if (target == null)
			{
				LogMessage(methodName, LogSeverityTypes.Standard, "Error: Could not find entity with name \"{0}\" to approach.",
					action.TargetName);
				return true;
			}

			//So, if not moving and too far away, approach
			if (target.Distance > action.Distance)
			{
                //If I'm moving there's nothing else to do for now
                if (Metatron.Movement.IsMoving)
                    return false;

				LogMessage(methodName, LogSeverityTypes.Standard, "Approaching entity \"{0}\" ({1}). Break on targeted: {2}, spawn: {3}",
					target.Name, target.ID, action.BreakOnTargeted, action.BreakOnSpawn);

				var destination = new Destination(DestinationTypes.Entity, target.ID) { Distance = action.Distance };
				Metatron.Movement.QueueDestination(destination);
			}
			else
			{
				//Otherwise we're done
				LogMessage(methodName, LogSeverityTypes.Standard, "Within target range {0} of target entity \"{1}\" ({2}). Dequeueing action.",
					action.Distance, target.Name, target.Distance);
			    BreakAction(target);
				return true;
			}
			return false;
		}

		private bool ProcessLoot(Action action)
		{
			var methodName = "ProcessLoot";
			LogTrace(methodName, "Action: {0}", action.Name);

			if (Metatron.Movement.IsMoving)
				return false;

			//Get a list of entities of matching groupID
		    var possibleTargetEntities = Metatron.EntityProvider.EntityWrappers.Where(entity => !_lootCanBlacklist.Contains(entity.ID) && 
                (entity.HaveLootRights || entity.GroupID == (int)GroupIDs.SpawnContainer));

			//If we don't have a specific target...
			if (action.TargetName == string.Empty)
			{
                possibleTargetEntities = possibleTargetEntities.Where(entity => entity.GroupID == action.GroupId);

				if (!possibleTargetEntities.Any())
				{
					LogMessage(methodName, LogSeverityTypes.Standard, "Could not find any cans on grid, error. Dequeueing action.");
					return true;
				}

				//If a "near wreck" option is defined...
				if (action.NearWreck != string.Empty)
				{
					//Get the specified wreck...
					var wreckEntity = Metatron.EntityProvider.EntityWrappers.FirstOrDefault(x => x.Name.Contains(action.NearWreck, StringComparison.InvariantCultureIgnoreCase));

					//If we found no result we have issues.
					if (wreckEntity == null)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Could not find wreck/can matching NearWreck \"{0}\", error. Dequeueing action.",
							action.NearWreck);
						return true;
					}

					//Sort the cans by distance to the wreck, ascending.
					possibleTargetEntities = possibleTargetEntities.OrderBy(
						x => Distance(wreckEntity.X, wreckEntity.Y, wreckEntity.Z, x.X, x.Y, x.Z));
				}
			}

			//Get the first match
			IEntityWrapper targetEntity;
			if (action.TargetName == string.Empty)
			{
				targetEntity = possibleTargetEntities.FirstOrDefault();

				if (targetEntity == null)
				{
					LogMessage(methodName, LogSeverityTypes.Standard, "Could not find any non-blacklisted cans on grid, error. Dequeueing action.");
					return true;
				}
			}
			else
			{
    			//This is excepting due to a null name. No easy way to figure out which entity it is either.
                targetEntity = possibleTargetEntities.FirstOrDefault(
					x => x.Name != null && x.Name.Contains(action.TargetName) &&
                    ((action.GroupId == 0 && (x.GroupID == (int)GroupIDs.CargoContainer || x.GroupID == (int)GroupIDs.SpawnContainer || x.GroupID == (int)GroupIDs.Wreck)) ||
					(action.GroupId > 0 && x.GroupID == action.GroupId)));
                if (targetEntity == null)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard,
                                "Could not find wreck/can matching name \"{0}\", error. Dequeueing action.",
                                action.TargetName);
                    return true;
                }
			}

			//If we're too far, approach.
			if (targetEntity.Distance > (int)Ranges.LootActivate)
			{
				LogMessage(methodName, LogSeverityTypes.Standard, "Moving to target wreck/can \"{0}\" ({1}) for looting.",
					targetEntity.Name, targetEntity.ID);
				var destination = new Destination(DestinationTypes.Entity, targetEntity.ID)
				    {
				        Distance = (int)Ranges.LootActivate
				    };
				Metatron.Movement.QueueDestination(destination);
			}
			else
			{
				var inventoryWindow = _eveWindowProvider.GetInventoryWindow();

				//If there's no loot window for it, open one
				var childWindow = inventoryWindow.GetChildWindow(targetEntity.ID);
				if (LavishScriptObject.IsNullOrInvalid(childWindow))
				{
					LogMessage(methodName, LogSeverityTypes.Standard, "Opening cargo of target wreck/can \"{0}\" ({1}).",
						targetEntity.Name, targetEntity.ID);
					targetEntity.Open();
					return false;
				}

				if (inventoryWindow.ActiveChild.ItemId != targetEntity.ID)
				{
					LogMessage(methodName, LogSeverityTypes.Standard, "Making target wreck/can \"{0}\" ({1}) loot window active.",
						targetEntity.Name, targetEntity.ID);
					childWindow.MakeActive();
					return false;
				}

				//Get all items, try to move them to cargo.
				var cargo = targetEntity.ToEntity.GetCargo();

				//If there are no items in the cargo, wait a few seconds
				if (cargo.Count == 0)
				{
					if (_lootTimeouts-- <= 0)
					{
						//reset teh loot timeout and dequeue action
						_lootTimeouts = MaxLootTimeouts;

						LogMessage(methodName, LogSeverityTypes.Standard, "Did not find any cargo in can \"{0}\" ({1}) after 3 pulses, blacklisting and trying another...",
						           targetEntity.Name, targetEntity.ID);
						if (!_lootCanBlacklist.Contains(targetEntity.ID))
						{
							_lootCanBlacklist.Add(targetEntity.ID);
						}
					}
					else
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "No items found in target wreck/can, waiting a pulse...");
						Metatron.ModuleManager.DelayPulseByHighestTime(1);
					}
				}
				else
				{
					//If the required item / volume are defined just look for them.
					var typeId = -1;

				    if (Metatron.MissionRunner.ActiveMission.TypeId > -1)
					{
						typeId = Metatron.MissionRunner.ActiveMission.TypeId;
					}

					List<IItem> itemQueryResults;

					//First, check against the specified item name.
					if (action.ItemName != string.Empty)
					{
					    itemQueryResults = cargo
					        .Where(item => item.Name.Contains(action.ItemName, StringComparison.InvariantCultureIgnoreCase))
					        .ToList();

						//If we got a match, move it to cargo.
						if (itemQueryResults.Count > 0)
						{
							foreach (var item in itemQueryResults)
							{
								LogMessage(methodName, LogSeverityTypes.Standard, "Attempting to move item \"{0}\" matching item name \"{1}\" from target wreck/can \"{2}\" ({3}) to our ship.",
								           item.Name, action.ItemName, targetEntity.Name, targetEntity.ID);
								item.MoveTo(Metatron.MeCache.Ship.Id, ToDestinationNames.CargoHold.ToString());
							}

							//We were able to find the item we needed for this action; we're done here.
							LogMessage(methodName, LogSeverityTypes.Standard, "Found action target item and looted it; dequeueing action.");
							return true;
						}
					}
						//Next, check if it matches the typeID for the mission.
					else if (typeId >= 0)
					{
					    itemQueryResults = cargo
					        .Where(item => item.TypeID == typeId)
					        .ToList();

						//If we have results, move them to cargo.
						if (itemQueryResults.Count > 0)
						{
							foreach (var item in itemQueryResults)
							{
								LogMessage(methodName, LogSeverityTypes.Standard, "Attempting to move item \"{0}\" matching required type ID {1} from target wreck/can \"{2}\" ({3}) to our ship.",
								           item.Name, typeId, targetEntity.Name, targetEntity.ID);
								item.MoveTo(Metatron.MeCache.Ship.Id, ToDestinationNames.CargoHold.ToString());
							}

							//We were able to find the item we needed for the mission; we're done here.
							LogMessage(methodName, LogSeverityTypes.Standard, "Found mission objective item and looted it; dequeueing action.");
							return true;
						}
					}
						//otherwise, it means target name was defined for this can/wreck. Just loot and be done with it.
					else
					{
					    itemQueryResults = cargo
					        .OrderByDescending(item => item.Volume*item.Quantity)
					        .ToList();

						//If we got a match, move it to cargo.
						if (itemQueryResults.Count > 0)
						{
							foreach (var item in itemQueryResults)
							{
								LogMessage(methodName, LogSeverityTypes.Standard, "Attempting to move item \"{0}\" from target wreck/can \"{1}\" ({2}) to our ship.",
								           item.Name, targetEntity.Name, targetEntity.ID);
								item.MoveTo(Metatron.MeCache.Ship.Id, ToDestinationNames.CargoHold.ToString());
							}

							//We were able to find the item we needed for this action; we're done here.
							LogMessage(methodName, LogSeverityTypes.Standard, "Found and looted name-specified Loot target \"{0}\" ({1}); dequeueing action.",
							           targetEntity.Name, targetEntity.ID);
							return true;
						}
					}

					if (!_lootCanBlacklist.Contains(targetEntity.ID))
					{
						//If we got here, we didn't find the item, so blacklist the can and try another.
						LogMessage(methodName, LogSeverityTypes.Standard, "Did not find item with typeID {0} or ItemName \"{1}\" in can \"{2}\" ({3}), blacklisting and trying another...",
						           typeId, action.ItemName, targetEntity.Name, targetEntity.ID);
						_lootCanBlacklist.Add(targetEntity.ID);
					}
				}
			}

			return false;
		}

		private bool ProcessKill(Action action, IEntityWrapper targetEntity)
		{
			var methodName = "ProcessKill";
			LogTrace(methodName, "Action: {0}", action.Name);

		    //If the target is a wreck or not found...
			if (targetEntity == null || targetEntity.GroupID == (int)GroupIDs.Wreck)
			{
				//We're done
				LogMessage(methodName, LogSeverityTypes.Standard, "Kill target \"{0}\" not found or is a wreck; dequeueing action.",
					action.TargetName);
				return true;
			}

			if (!Metatron.Movement.IsMoving && NeedToApproachEntity(targetEntity))
			{
				var distance = Metatron.Ship.GetMaximumWeaponRange();
				if (distance > Metatron.Ship.MaxTargetRange)
				{
					distance = Metatron.Ship.MaxTargetRange;
				}

				var entityDestination = new Destination(DestinationTypes.Entity, targetEntity.ID) { Distance = distance * 0.95 };
				LogMessage(methodName, LogSeverityTypes.Standard, "Closest entity \"{0}\" ({1}) is out of maximum weapon range - getting within {2} of it.",
					targetEntity.Name, targetEntity.ID, entityDestination.Distance);
				Metatron.Movement.QueueDestination(entityDestination);
			}

			//If it's not queued, queue it
			if (!Metatron.TargetQueue.IsQueued(targetEntity.ID))
			{
                //We're killing something; we should salvage.
			    _shouldSalvageRoom = true;
				Metatron.TargetQueue.EnqueueTarget(targetEntity.ID, (int)TargetPriorities.Kill_Other, TargetTypes.Kill);
			}
			return false;
		}

	    private IEntityWrapper GetTargetEntityForAction(Action action)
	    {
	        var matchingEntities = (from entity in Metatron.EntityProvider.EntityWrappers
	                                select entity);

            if (action.Name == "Kill")
                matchingEntities = matchingEntities.Where(entity => entity.CategoryID == (int)CategoryIDs.Entity &&
                    entity.Name.Contains(action.TargetName, StringComparison.InvariantCultureIgnoreCase));
            else if (action.Name == "Approach")
                matchingEntities = matchingEntities.Where(entity => entity.Name.Equals(action.TargetName, StringComparison.InvariantCultureIgnoreCase));

            if (action.TypeId > 0)
                matchingEntities = matchingEntities.Where(entity => entity.TypeID == action.TypeId);

	        return matchingEntities.FirstOrDefault();
	    }

	    private bool NeedToApproachEntity(IEntityWrapper entity)
		{
			//If this entity is outside targeting range, we do indeed need to approach.
			//Offensive will handle approaching for optimal range.
			return entity.Distance > Metatron.Ship.MaxTargetRange || entity.Distance > Metatron.Ship.GetMaximumWeaponRange();
		}

		private void BreakAction(IEntityWrapper targetEntity)
		{
			var methodName = "BreakAction";
// ReSharper disable SpecifyACultureInStringConversionExplicitly
			LogTrace(methodName, "Target Entity: {0}", targetEntity == null ? "null" : targetEntity.ID.ToString());
// ReSharper restore SpecifyACultureInStringConversionExplicitly

            if (Metatron.Movement.IsMoving)
			    Metatron.Movement.StopCurrentMovement(true);

            if (targetEntity == null)
				return;

		    if (Metatron.TargetQueue.IsQueued(targetEntity.ID))
		        Metatron.TargetQueue.DequeueTarget(targetEntity.ID);

		    if (targetEntity.IsLockedTarget)
		        targetEntity.UnlockTarget();
		}
	}
    // ReSharper restore ImplicitlyCapturedClosure
    // ReSharper restore PossibleMultipleEnumeration
    // ReSharper restore InconsistentNaming
    // ReSharper restore ConvertToConstant.Local
}
