using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Media3D;
using EVE.ISXEVE;
using EVE.ISXEVE.Interfaces;
using Metatron.Core;
using LavishScriptAPI;
using Metatron.Core.CustomEventArgs;
using Metatron.Core.Interfaces;
using IShip = Metatron.Core.IShip;
using System.Diagnostics;
using EVE.ISXEVE.Extensions;

namespace Metatron.ActionModules
{
    internal sealed class Movement : ModuleBase, ICriticallyBlocking, IMovement
    {
        public event EventHandler<SessionChangedEventArgs> PreSessionChanged;
        public event EventHandler<SessionChangedEventArgs> SessionChanged;

        public bool IsNearbyCollidableAvoidanceEnabled { get; private set; }
        public bool IsAutopilotEnabled { get; private set; }

        private readonly List<Destination> _destinationQueue = new List<Destination>();
        public ReadOnlyCollection<Destination> DestinationQueue
        {
            get { return _destinationQueue.AsReadOnly(); }
        }

        //cache destination entity id to avoid entity lookup/iteration
        private MovementTypes _movementType = MovementTypes.Approach;
        private int _pulsesWaitedForDrones = 0;
        private int _maxPulsesToWaitForDrones = 10;
        private int _lastSolarSystemID = -1;
        private int _undockSanityCounter = 5;
        private int _dockSanityCounter = 2;
        private int _systemChangeSanityCounter = 2;

        private int _lastOrbitDistance = -1;
        private int _orbitDistanceOverride;

        readonly int DelayAfterUndock = 10,
            DelayAfterDock = 10,
            DelayAfterSystemChange = 10,
            DelayInProcess = 10;

        private readonly int WarpTo0KM = 0,
                             WarpTo10KM = 10000,
                             WarpTo20KM = 20000,
                             WarpTo30KM = 30000,
                             WarpTo50KM = 50000,
                             WarpTp70KM = 70000,
                             WarpTo100KM = 100000;

        private readonly IIsxeveProvider _isxeveProvider;
        private readonly IEntityProvider _entityProvider;
        private readonly IMeCache _meCache;
        private readonly IAnomalyProvider _anomalyProvider;
        private readonly ITargetQueue _targetQueue;
        private readonly IShip _ship;
        private readonly IDrones _drones;

        internal Movement(IIsxeveProvider isxeveProvider, IEntityProvider entityProvider, IMeCache meCache, IAnomalyProvider anomalyProvider, ITargetQueue targetQueue, IShip ship, IDrones drones)
        {
            _isxeveProvider = isxeveProvider;
            _entityProvider = entityProvider;
            _meCache = meCache;
            _anomalyProvider = anomalyProvider;
            _targetQueue = targetQueue;
            _ship = ship;
            _drones = drones;

            IsNearbyCollidableAvoidanceEnabled = true;
            IsAutopilotEnabled = false;

            ModuleManager.ModulesToPulse.Add(this);
            ModuleManager.CriticallyBlockingModules.Add(this);
            PulseFrequency = 1;
            ModuleName = "Movement";
        }

        public override void Pulse()
        {
            var methodName = "Pulse";
            LogTrace(methodName);

            if (!ShouldPulse())
                return;

            StartPulseProfiling();

            CheckCovOpsCloak();

            //Don't try to do any movement if we're in warp or on autopilot
            if (_meCache.ToEntity.Mode == (int)Modes.Warp || _meCache.Me.AutoPilotOn)
            {
                EndPulseProfiling();
                return;
            }

            //If we're not moving and are near something, queue an orbit-dequeue for 1.5km
            if (!IsMoving && IsNearbyCollidableAvoidanceEnabled)
            {
                var closeCollidable = GetNearbyCollidableEntity();

                if (closeCollidable != null)
                {
                    //Make sure we're not already orbiting this bastard
                    var entityApproaching = _meCache.ToEntity.Approaching;
                    if (entityApproaching == null || entityApproaching.ID != closeCollidable.ID)
                    {
                        LogMessage(methodName, LogSeverityTypes.Standard, "We're within 1000m of entity \"{0}\" ({1}). Orbiting it at 1500m to get away...",
                            closeCollidable.Name, closeCollidable.ID);

                        var avoidanceDestination = new Destination(DestinationTypes.Entity, closeCollidable.ID)
                        {
                            ApproachType = ApproachTypes.Orbit,
                            Distance = 1500,
                            IsObstacleAvoidanceMovement = true, //Indicate it's emergency obstacle avoidance
                            MinimumDistance = 1000              //Don't use a goal location - we just want to get away from the close collidable
                        };

                        _destinationQueue.Insert(0, avoidanceDestination);
                    }
                }
            }

            //If we have destinations to process, do so
            if (_destinationQueue.Count > 0)
            {
                ProcessDestinationQueue();
            }

            if (MovementType == MovementTypes.Approach && _meCache.Ship.CapacitorPct >= Core.Metatron.Config.MovementConfig.PropModResumeCapPct && !_ship.GangLinkModules.Any(m => m.IsActive))
            {
                _ship.ActivateModuleList(_ship.AfterBurnerModules, false);
            }
            else if (MovementType != MovementTypes.Approach || _meCache.Ship.CapacitorPct < Core.Metatron.Config.MovementConfig.PropModMinCapPct || _ship.GangLinkModules.Any(m => m.IsActive))
            {
                DeactivatePropulsionMods();
            }
            EndPulseProfiling();
        }

        public void DisableNearbyCollidableAvoidance()
        {
            var methodName = "DisableNearbyCollidableAvoidance";
            LogTrace(methodName);

            IsNearbyCollidableAvoidanceEnabled = false;
        }

        public void EnableNearbyCollidableAvoidance()
        {
            var methodName = "EnableNearbyCollidableAvoidance";
            LogTrace(methodName);

            IsNearbyCollidableAvoidanceEnabled = true;
        }

        private IEntityWrapper GetNearbyCollidableEntity()
        {
            var methodName = "GetNearbyCollidableEntity";
            LogTrace(methodName);

            return _entityProvider.EntityWrappers
                .FirstOrDefault(entity => entity.Distance < 1000 && IsEntityCollidable(entity));
        }

        public bool IsEntityCollidable(IEntityWrapper entity)
        {
            if (entity.TypeID == (int)TypeIDs.GuristasGreatWall) return false;

            if (entity.CategoryID == (int)CategoryIDs.Asteroid ||
                entity.CategoryID == (int)CategoryIDs.Structure ||
                entity.GroupID == (int)GroupIDs.LargeCollidableObject ||
                entity.GroupID == (int)GroupIDs.LargeCollidableStructure)
                return true;

            return false;
        }

        public bool CriticallyBlock()
        {
            var methodName = "CriticallyBlock";
            LogTrace(methodName);

            if (MovementType == MovementTypes.Dock ||
                MovementType == MovementTypes.Undock ||
                MovementType == MovementTypes.SystemChange)
            {
                switch (MovementType)
                {
                    //If we're supposed to be docking, handle the dock process
                    case MovementTypes.Dock:
                        DockAtStation();
                        break;
                    //If we're supposed to be undocking, handle the undock process
                    case MovementTypes.Undock:
                        UndockFromStation();
                        break;
                    //If we're supposed to be changing systems, handle the system change process
                    case MovementTypes.SystemChange:
                        ChangeSolarSystem();
                        break;
                }
                return true;
            }
            return false;
        }

        public void CheckCovOpsCloak()
        {
            var methodName = "_checkCovOpsCloak";
            LogTrace(methodName);

            //Depending on the  move type, we can do covops cloaking!
            if (IsCriticalMoving)
                return;

            var entitiesTooClose = _entityProvider.EntityWrappers.Count(
                entity => entity.ID != _meCache.ToEntity.Id &&
                    ((entity.GroupID == (int)GroupIDs.ControlTower && entity.Distance <= 30000) || (entity.GroupID != (int)GroupIDs.ControlTower && entity.Distance <= 2500)));

            if (entitiesTooClose != 0)
                return;

            if (IsMoving)
            {
                switch (_movementType)
                {
                    case MovementTypes.Approach:
                    case MovementTypes.Warp:
                        _ship.ActivateCovertOpsCloak();
                        break;
                    default:
                        //DEACTIVAET CLOAK
                        _ship.DeactivateCovertOpsCloak();
                        break;
                }
            }
            else
            {
                _ship.ActivateCovertOpsCloak();
            }
        }

        #region Critical Movement handler methods
        private void DockAtStation()
        {
            var methodName = "DockAtStation";
            LogTrace(methodName);

            if (_meCache.InStation &&
                _meCache.StationId >= 0)
            {
                //Reset the sanity counter
                _dockSanityCounter = 2;
                //Give it 5 more seconds after we're in
                LogMessage(methodName, LogSeverityTypes.Standard, "Done docking, resetting movement type.");
                _movementType = MovementTypes.None;
                Core.Metatron.ModuleManager.DelayPulseByHighestTime(DelayAfterDock);

                if (SessionChanged != null)
                    SessionChanged(this, new SessionChangedEventArgs(false));
            }
            else
            {
                if (_meCache.InSpace)
                {
                    if (--_dockSanityCounter <= 0)
                    {
                        _dockSanityCounter = 4;

                        using (var station = new Entity(string.Format("ID = \"{0}\"", _destinationQueue[0].EntityId)))
                        {
                            if (!LavishScriptObject.IsNullOrInvalid(station))
                            {
                                station.Dock();
                                LogMessage(methodName, LogSeverityTypes.Standard, "Still in space, re-calling Dock. F*cking lag...");
                                Core.Metatron.ModuleManager.DelayPulseByHighestTime(DelayInProcess);
                            }
                            else
                            {
                                LogMessage(methodName, LogSeverityTypes.Standard,
                                           "Could not get valid Entity for station entity ID {0}! Something went very wrong!",
                                           _destinationQueue[0].EntityId);
                            }
                        }
                    }
                    else
                    {
                        LogMessage(methodName, LogSeverityTypes.Standard, "Still in space. Sanity counter: {0}", _dockSanityCounter);
                    }
                }
                else
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Currently docking; please wait...");
                }
            }
        }

        private void UndockFromStation()
        {
            var methodName = "UndockFromStation";
            LogTrace(methodName);

            if (!_meCache.InStation && _meCache.InSpace &&
                _meCache.StationId == -1)
            {
                _movementType = MovementTypes.None;
                LogMessage(methodName, LogSeverityTypes.Standard, "Done undocking, dequeueing destination and resetting.");
                _destinationQueue.RemoveAt(0);
                _undockSanityCounter = 5;       //Reset the sanity couner

                if (SessionChanged != null)
                    SessionChanged(this, new SessionChangedEventArgs(true));
            }
            else
            {
                if (_meCache.InStation)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Still in station; looks like undock failed from lag. Undocking again.");
                    Core.Metatron.Station.Undock();
                    Core.Metatron.ModuleManager.DelayPulseByHighestTime(DelayInProcess);
                }
                else
                {
                    if (--_undockSanityCounter <= 0)
                    {
                        LogMessage(methodName, LogSeverityTypes.Standard, "Undock sanity check triggered, looks like we blackscreened. Exiting and killing session.");
                        //We're stuck undocking; exit EVE and restart if we can.
                        ExitAndRelaunch();
                    }
                    else
                    {
                        LogMessage(methodName, LogSeverityTypes.Standard, "Not done undocking yet. {0} pulses 'til sanity exit.",
                            _undockSanityCounter);
                    }
                }
            }
        }

        private Stopwatch _autopilotOffStopwatch = new Stopwatch();

        private void ChangeSolarSystem()
        {
            var methodName = "ChangeSolarSystem";
            LogTrace(methodName + ": Method Entry");

            if (!_meCache.InSpace)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Somewhere in the void... waiting for system change.");
                return;
            }

            if (_isxeveProvider.Eve.GetToDestinationPath() == null || _isxeveProvider.Eve.GetToDestinationPath().Count == 0)
            {
                LogMessage(methodName, LogSeverityTypes.Critical, "Solar System Change was requested but no destination is set.");
                _destinationQueue.RemoveAt(0);
                _lastSolarSystemID = _meCache.SolarSystemId;
                _movementType = MovementTypes.None;
                LogTrace(methodName + ": Removed destination and set movement type to none");
            }

            // If autopilot is not on and stopwatch is not running, engage autopilot and start stopwatch.
            if (!_meCache.Me.AutoPilotOn && !_autopilotOffStopwatch.IsRunning && _isxeveProvider.Eve.GetToDestinationPath().Count > 0)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Engaging autopilot");
                _isxeveProvider.Eve.Execute(ExecuteCommand.CmdToggleAutopilot);
                _autopilotOffStopwatch.Start();
                LogTrace(methodName + ": Engaged autopilot and started stopwatch");
            }
            // If autopilot is not on and stopwatch has been running for at least 5 seconds, reset and perform the action.
            else if (!_meCache.Me.AutoPilotOn && _autopilotOffStopwatch.Elapsed.TotalSeconds >= 5)
            {
                LogTrace(methodName + ": Autopilot off and 5 seconds passed");
                _autopilotOffStopwatch.Reset();

                // Perform your actions here.
                if (_destinationQueue.Count > 0)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Done changing system.");
                    _destinationQueue.RemoveAt(0);
                    _lastSolarSystemID = _meCache.SolarSystemId;
                    _movementType = MovementTypes.None;
                    LogTrace(methodName + ": Removed destination and set movement type to none");
                }
            }
            // If autopilot is on and stopwatch is running, reset the stopwatch.
            else if (_meCache.Me.AutoPilotOn && _autopilotOffStopwatch.IsRunning)
            {
                _autopilotOffStopwatch.Reset();
                LogTrace(methodName + ": Autopilot engaged, resetting stopwatch");
            }

            LogTrace(methodName + ": Method Exit");
        }
        #endregion

        public void QueueDestination(Destination destination)
        {
            var methodName = "AddDestination";
            LogTrace(methodName, "Destination: {0}", destination);
            _destinationQueue.Add(destination);
        }

        public void RemoveDestination(Destination destination)
        {
            var methodName = "RemoveDestination";
            LogTrace(methodName);

            _destinationQueue.Remove(destination);
        }

        public bool IsMoving
        {
            get
            {
                return (_destinationQueue.Count > 0);
            }
        }

        public bool IsCriticalMoving
        {
            get
            {
                return (_movementType == MovementTypes.Dock ||
                    _movementType == MovementTypes.Undock ||
                    (_movementType == MovementTypes.SystemChange && _meCache.Me.AutoPilotOn && _meCache.ToEntity.Mode != (int)Modes.Warp));
            }
        }

        public MovementTypes MovementType
        {
            get
            {
                return _movementType;
            }
        }

        private void DeactivatePropulsionMods()
        {
            var methodName = "DeactivatePropulsionMods";
            LogTrace(methodName);

            _ship.DeactivateModuleList(_ship.AfterBurnerModules, true);
        }

        public void ClearDestinations(bool deactivatePropulsionMods)
        {
            var methodName = "ClearDestinations";
            LogTrace(methodName);

            _destinationQueue.Clear();

            if (deactivatePropulsionMods)
                DeactivatePropulsionMods();
        }

        private void DequeueDestination(Destination destination)
        {
            var methodName = "DequeueDestination";
            LogTrace(methodName);

            _destinationQueue.Remove(destination);

            _lastOrbitDistance = -1;
            _orbitDistanceOverride = -1;

            _movementType = MovementTypes.None;
        }

        /// <summary>
        /// Processes the destination queue.
        /// </summary>
        private void ProcessDestinationQueue()
        {
            var methodName = "ProcessDestinationQueue";
            LogTrace(methodName);

            var destination = _destinationQueue.FirstOrDefault();

            if (destination == null)
                return;

            var processAnotherDestination = false;

            switch (destination.Type)
            {
                case DestinationTypes.BookMark:
                    processAnotherDestination = ProcessBookmark(destination);
                    break;
                case DestinationTypes.SolarSystem:
                    processAnotherDestination = ProcessSolarSystem(destination);
                    break;
                case DestinationTypes.Entity:
                    processAnotherDestination = ProcessEntity(destination);
                    break;
                case DestinationTypes.FleetMember:
                    processAnotherDestination = ProcessFleetMember(destination);
                    break;
                case DestinationTypes.Undock:
                    processAnotherDestination = ProcessUndock(destination);
                    break;
                case DestinationTypes.MissionBookmark:
                    processAnotherDestination = ProcessMissionBookmark(destination);
                    break;
                case DestinationTypes.CosmicAnomaly:
                    processAnotherDestination = ProcessCosmicAnomaly(destination);
                    break;
            }

            if (processAnotherDestination)
                ProcessDestinationQueue();
        }

        private bool ProcessCosmicAnomaly(Destination destination)
        {
            var methodName = "ProcessCosmicAnomaly";
            LogTrace(methodName);

            var anomaly = _anomalyProvider.GetAnomalies().FirstOrDefault(a => a.ID == destination.SystemAnomalyId);

            if (anomaly == null)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Error: Could not find matching cosmic anomaly for ID {0}.", destination.SystemAnomalyId);
                DequeueDestination(destination);
                return true;
            }

            var distance = DistanceTo(anomaly.X, anomaly.Y, anomaly.Z);
            if (distance <= (int)Ranges.Warp)
            {
                //remove destination
                DequeueDestination(destination);
                LogMessage(methodName, LogSeverityTypes.Standard, "We've arrived at cosmic anomaly \"{0} - {1}\" ({2}).",
                           anomaly.Name, anomaly.DungeonName, anomaly.ID);
                return true;
            }

            if (PrepareForWarp())
            {
                //It's not a celestial; which means we're warping to the bookmark.
                var eveWarpToDistance = GetEveWarpToDistance(destination.WarpToDistance);
                LogMessage(methodName, LogSeverityTypes.Standard, "Warping to cosmic anomaly \"{0} - {1}\" ({2}) at {3}km.",
                    anomaly.Name, anomaly.DungeonName, anomaly.ID, eveWarpToDistance);

                anomaly.WarpTo(eveWarpToDistance, false);

                _movementType = MovementTypes.Warp;
                PulseCounter += 2;
            }

            return false;
        }

        private bool ProcessMissionBookmark(Destination destination)
        {
            var methodName = "ProcessMissionBookmark";

            var cachedMission = Core.Metatron.MissionCache.GetCachedMissionForAgentId(destination.MissionAgentId);

            //If the mission is null, we've got issues
            if (cachedMission == null)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "MissionBookmark - CachedMission for agent id {0} null!",
                           destination.MissionAgentId);
                return false;
            }

            //Get the bookmark matching the tag
            var missionBookmark = Core.Metatron.MissionCache.GetBookmarkMatchingTag(cachedMission, destination.BookMarkTypeTag);

            //If we couldn't find a matching bookmark, we've got issues
            if (LavishScriptObject.IsNullOrInvalid(missionBookmark))
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "MissionBookmark - BookMark for tag \"{0}\" null!",
                           destination.BookMarkTypeTag);
                return false;
            }

            //If we're not in the right station, we need to undock.
            if (_meCache.InStation && Core.Metatron.Bookmarks.IsStationBookMark(missionBookmark) &&
                _meCache.StationId != missionBookmark.ItemID)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "MissionBookmark - In station \"{0}\", not \"{1}\" - Docked in the wrong station, inserting undock destination.",
                           _meCache.StationId, missionBookmark.ItemID);
                //Get the index of the current desti, get a destination to queue, and insert before current index
                var index = _destinationQueue.IndexOf(destination);
                var undockDestination = new Destination(DestinationTypes.Undock);
                _destinationQueue.Insert(index, undockDestination);

                return true;
            }

            //If we're in a different solarsystem, we should insert a desti to move to that system
            if (_meCache.SolarSystemId != missionBookmark.SolarSystemID)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "MissionBookmark - In solarsystem \"{0}\", not \"{1}\" - Not in the bookmark's solarsystem, inserting solarsystem destination for {1}.",
                           _meCache.SolarSystemId, missionBookmark.SolarSystemID);
                //Get the index of the current destination
                var index = _destinationQueue.IndexOf(destination);
                //Get a destination to queue
                var solarSystemDestination = new Destination(DestinationTypes.SolarSystem, missionBookmark.SolarSystemID);
                //Insert the desti in queue before the current desti
                _destinationQueue.Insert(index, solarSystemDestination);

                return true;
            }

            //Log some damn info frm the damn bookmark.
            LogMessage(methodName, LogSeverityTypes.Debug, "ItemID {0}, Type \"{1}\" ({2})",
                       missionBookmark.ItemID, missionBookmark.Type, missionBookmark.TypeID);

            //If the bookmark has an ItemID, re-queue it as an Entity destination.
            if (missionBookmark.ItemID > 0 && !LavishScriptObject.IsNullOrInvalid(missionBookmark.ToEntity) &&
                missionBookmark.ToEntity.CategoryID == (int)CategoryIDs.Station)
            {
                int index = _destinationQueue.IndexOf(destination);
                DequeueDestination(destination);
                if (_meCache.InSpace)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "MissionBookmark - Destination is a station, inserting entity destination for {0}.",
                               missionBookmark.ItemID);
                    var entityDestination = new Destination(DestinationTypes.Entity, missionBookmark.ItemID) { Dock = true, Distance = 200 };
                    _destinationQueue.Insert(index, entityDestination);

                    return true;
                }
            }
            else
            {
                //Time to move to the bookmark.
                if (_meCache.InStation)
                {
                    _destinationQueue.Insert(0, new Destination(DestinationTypes.Undock));

                    return true;
                }

                //We can come out of warp out of range of the bookmark but right on top of an acceleration gate -
                //if this happens we're as close to the bookmark as we can get - "natural phenomenon" error -
                //just dequeue, we have to use the gate and mission processor will handle that.
                //Actually, use the deadspace beacon.
                var accelerationGateEntity = _entityProvider.EntityWrappers.FirstOrDefault(
                    entity => entity.TypeID == (int)TypeIDs.Beacon);

                var distanceTo = DistanceTo(missionBookmark.X, missionBookmark.Y, missionBookmark.Z);

                if (distanceTo >= (int)Ranges.Warp && accelerationGateEntity == null)
                {
                    WarpToBookMark(missionBookmark, GetEveWarpToDistance(0));
                }
                else
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "MissionBookmark - At bookmark, dequeueing destination.");
                    //Don't actually need to approach the real bookmark. We're at it if on grid with it.
                    DequeueDestination(destination);
                }
            }
            return false;
        }

        private bool ProcessUndock(Destination destination)
        {
            var methodName = "ProcessUndock";

            if (_meCache.InStation && !_meCache.InSpace)
            {
                _movementType = MovementTypes.Undock;

                if (PreSessionChanged != null)
                    PreSessionChanged(this, new SessionChangedEventArgs(true));

                Core.Metatron.Station.Undock();

                LogMessage(methodName, LogSeverityTypes.Standard, "Undocking from station {0}.",
                           _meCache.Me.Station);
                //Wait for undock. Make the core pulse wait so we don't have system loading issues
                Core.Metatron.ModuleManager.DelayPulseByHighestTime(DelayAfterUndock);
            }
            else
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Removing undock destination due to not being in station. Trace and fix this.");
                DequeueDestination(destination);
            }

            return false;
        }
        private bool ProcessFleetMember(Destination destination)
        {
            var methodName = "ProcessFleetMember";

            if (!_meCache.InSpace && _meCache.InStation)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Inserting undock destination before attempting to go to fleet member.");
                _destinationQueue.Insert(0, new Destination(DestinationTypes.Undock));

                return true;
            }

            var fleetMemberBuddy = _meCache.Buddies.FirstOrDefault(buddy => buddy.CharID == destination.FleetMemberId);
            if (fleetMemberBuddy != null && !fleetMemberBuddy.IsOnline)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Destination fleet member {0} is offline; dequeueing.",
                           destination.FleetMemberId);

                DequeueDestination(destination);
                return false;
            }

            IEntityWrapper tempFleetMemberEntity = null;
            if (destination.FleetMemberEntityId > 0)
            {
                if (_entityProvider.EntityWrappersById.ContainsKey(destination.FleetMemberEntityId))
                {
                    tempFleetMemberEntity = _entityProvider.EntityWrappersById[destination.FleetMemberEntityId];
                }
                else
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Could not find matching Entity for fleet member EID {0}.",
                               destination.FleetMemberEntityId);
                }
            }
            else if (destination.FleetMemberName != string.Empty)
            {
                foreach (var entity in _entityProvider.EntityWrappers.Where(
                    entity => entity.Name == destination.FleetMemberName))
                {
                    tempFleetMemberEntity = entity;
                    break;
                }

                if (tempFleetMemberEntity == null)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Could not find matching Entity for fleet member name {0}",
                               destination.FleetMemberName);
                }
            }
            else
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Didn't have either entityID or entity name, something is boned.");
            }

            if (tempFleetMemberEntity == null)
            {
                //Ah damn, not on grid. Warp to 'em.
                foreach (var fleetMember in
                    _meCache.FleetMembers.Where(fleetMember => fleetMember.CharID == destination.FleetMemberId))
                {
                    WarpToFleetMember(fleetMember);
                    break;
                }
            }
            else
            {
                //Sweet, on grid. We're done here.
                DequeueDestination(destination);
                LogMessage(methodName, LogSeverityTypes.Standard, "Removing fleet member destination {0}.",
                           destination.FleetMemberId);
            }
            return false;
        }

        private bool ProcessEntity(Destination destination)
        {
            var methodName = "ProcessEntity";

            //If we're docked in the destination, dequeue it.
            if (destination.Dock && _meCache.StationId == destination.EntityId)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "We've arrived at entity station destination with ID {0}.", destination.EntityId);
                DequeueDestination(destination);
                return true;
            }

            //If we need to undock, do so.
            if (_meCache.InStation && !_meCache.InSpace)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Undocking in order to move to an entity destination.");
                _destinationQueue.Insert(0, new Destination(DestinationTypes.Undock));

                return true;
            }

            //Celestials are just glorifed entities.
            //we don't need to cache the entity ID for celestials and entities since that's what they're queued with
            //If the entity is a station it means we're wanting to dock at it. If it's a belt we just want to get
            //to it. otherwise, we want to get within loot range of it.

            //If the entity doesn't exist, dequeue it.
            if (!_entityProvider.EntityWrappersById.ContainsKey(destination.EntityId))
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Destination entity {0} not found; dequeueing.",
                    destination.EntityId);

                //Shut off prop mods, stop moving
                StopCurrentMovement(true);
                return true;
            }

            var destinationEntity = _entityProvider.EntityWrappersById[destination.EntityId];

            //Ok, standard procedure. If we're above warp range and it's warpable, warp to.
            if (!Core.Metatron.Attackers.IsRatTarget(destinationEntity) && destinationEntity.Distance > (int)Ranges.Warp &&
                (destinationEntity.GroupID != (int)GroupIDs.Planet || destinationEntity.Distance > destination.Distance))
            {
                var groupId = destinationEntity.GroupID;
                if (groupId == (int)GroupIDs.WarpGate)
                {
                    if (_meCache.ToEntity.Approaching == null || _meCache.ToEntity.Approaching.ID != destinationEntity.ID)
                    {
                        AlignToEntity(destinationEntity);
                    }
                }
                else
                {
                    if (PrepareForWarp(destinationEntity))
                        WarpToEntity(destinationEntity.ID, GetEveWarpToDistance(destination.WarpToDistance));
                }

                return false;
            }

            if (destination.Dock && (destination.Distance == 0 || destination.Distance > (int)Ranges.Dock))
            {
                destination.Distance = (int)Ranges.Dock;
            }
            else if (destination.UseGate && (destination.Distance == 0 || destination.Distance > (int)Ranges.LootActivate))
            {
                destination.Distance = (int)Ranges.LootActivate;
            }

            switch (destination.ApproachType)
            {
                case ApproachTypes.Approach:
                    if (destination.Distance > 0)
                    {
                        if (destinationEntity.Distance > destination.Distance)
                        {
                            if (_meCache.ToEntity.Approaching == null || _meCache.ToEntity.Approaching.ID != destinationEntity.ID)
                            {
                                ApproachEntity(destinationEntity);
                            }
                            return false;
                        } // We're in range - continue on.
                    }
                    break;
                case ApproachTypes.Orbit:
                    var continueCurrentDestination = PreProcessEntityOrbitDestination(destinationEntity, destination);

                    //If we can't continue processing the current destination for whatever reason,
                    //return early and indicate we can process another destination.
                    if (!continueCurrentDestination)
                        return true;

                    var orbitComplete = ProcessEntityOrbit(destinationEntity, destination);

                    if (!orbitComplete)
                        return false;
                    break;
                case ApproachTypes.KeepAtRange:
                    var distance = (int)destination.Distance;
                    if (_meCache.ToEntity.Approaching == null ||
                        _meCache.ToEntity.Approaching.ID != destinationEntity.ID ||
                        _lastOrbitDistance != distance)
                    {
                        KeepEntityAtRange(destinationEntity, distance);
                    }
                    return false;
            }

            //temp bool for blocking dequeueing if we weren't done warppreparing
            var dequeue = true;
            var dockedOrWarped = false;
            //Under warp range, check if we're trying a station
            if ((destinationEntity.GroupID == (int)GroupIDs.Station
                || destinationEntity.GroupID == (int)GroupIDs.EngineeringComplex
                || destinationEntity.GroupID == (int)GroupIDs.Refinery
                || destinationEntity.GroupID == (int)GroupIDs.Citadel)
                && destination.Dock)
            {
                //We're docking; can dequeue.
                dockedOrWarped = true;
                DockAtStation(destinationEntity);
                //Don't dequeue - need to keep the destination for re-attempts
                dequeue = false;
            }
            else
            {
                //IF it's a gate and we want to use it, do so.
                if (destination.UseGate)
                {
                    if (destinationEntity.Distance > (int)Ranges.LootActivate)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Approaching gate {0} ({1}) before jumping.",
                            destinationEntity.Name, destinationEntity.ID);
                        ApproachEntity(destinationEntity);
                        return false;
                    }

                    if (destinationEntity.GroupID == (int)GroupIDs.WarpGate)
                    {
                        if (PrepareForWarp(destinationEntity))
                        {
                            //Use it, dequeue it.
                            dockedOrWarped = true;
                            UseWarpGate(destinationEntity);
                        }
                        else
                        {
                            dequeue = false;
                        }
                    }
                    else if (destinationEntity.GroupID == (int)GroupIDs.Stargate)
                    {
                        dequeue = false; //will have to let the system-change dequeue

                        if (PrepareForWarp(destinationEntity))
                        {
                            //Use the stargate.
                            dockedOrWarped = true;
                            UseStarGate(destinationEntity);
                        }
                    }
                }
            }

            if (dequeue)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Done processing entity destination {0} ({1}); dequeueing.",
                    destinationEntity.Name, destinationEntity.ID);

                if (!dockedOrWarped && _meCache.ToEntity.Approaching != null)
                {
                    StopCurrentMovement(false);
                }

                DequeueDestination(destination);
            }
            return false;
        }

        private bool IsEntityInsideEntity(IEntityWrapper innerEntity, IEntityWrapper outerEntity)
        {
            var methodName = "IsEntityInsideEntity";
            LogTrace(methodName, "{0}, {1}", innerEntity.ID, outerEntity.ID);

            var outerEntityPoint = new Point3D(outerEntity.X, outerEntity.Y, outerEntity.Z);
            var innerEntityPoint = new Point3D(innerEntity.X, innerEntity.Y, innerEntity.Z);

            var resultVector = outerEntityPoint - innerEntityPoint;

            return outerEntity.Radius >= resultVector.Length;
        }

        /// <summary>
        /// Pre-process the entity orbit destination for any obstacle avoidance.
        /// </summary>
        /// <param name="destinationEntity"></param>
        /// <param name="destination"></param>
        /// <returns>true if processing can continue on the current destination, false if it should not.</returns>
        private bool PreProcessEntityOrbitDestination(IEntityWrapper destinationEntity, Destination destination)
        {
            var methodName = "AdjustForNearbyCollidables";
            LogTrace(methodName);

            //If we're moving for obstacle avoidance, see if we're clear of the obstacle
            return destination.IsObstacleAvoidanceMovement ? PreProcessAvoidanceOrbit(destinationEntity, destination) : PreProcessStandardOrbit(destinationEntity);
        }

        /// <summary>
        /// Pre-process a standard entity orbit, looking for nearby collidable entities and attempting to avoid them.
        /// This will insert a destination to avoid a nearby entity if necessary.
        /// </summary>
        /// <param name="destinationEntity"></param>
        /// <returns>true if processing can continue on the current destination, otherwise false.</returns>
        private bool PreProcessStandardOrbit(IEntityWrapper destinationEntity)
        {
            var methodName = "PreProcessStandardOrbit";
            LogTrace(methodName);

            var nearbyCollidable = GetNearbyCollidableEntity();
            if (nearbyCollidable == null) return true;

            LogMessage(methodName, LogSeverityTypes.Standard, "We're within 1000m of entity \"{0}\" ({1}). Inserting orbit destination at 1500m for avoidance before resuming entity \"{2}\" ({3})...",
                nearbyCollidable.Name, nearbyCollidable.ID, destinationEntity.Name, destinationEntity.ID);

            var avoidanceDestination = new Destination(DestinationTypes.Entity, nearbyCollidable.ID)
            {
                ApproachType = ApproachTypes.Orbit,
                Distance = 1500,
                IsObstacleAvoidanceMovement = true, //Indicate it's emergency obstacle avoidance
                GoalEntityId = destinationEntity.ID,
                MinimumDistance = 1000
            };

            _destinationQueue.Insert(0, avoidanceDestination);
            return false;
        }

        /// <summary>
        /// Pre-process an obstacle avoidance orbit destination, determining if we're clear to resume original orbit or not.
        /// If we're ok to resume original orbit, this will dequeue the avoidance destination.
        /// TODO: This should also see if I need to switch the avoidance orbit to another closer entity, e.g.
        /// if I avoid asteroid A, and in the proces, bounce of asteroid B - I should switch to avoiding B.
        /// </summary>
        /// <param name="destinationEntity"></param>
        /// <param name="destination"></param>
        /// <returns>true if processing can continue on the current destination, otherwise false.</returns>
        private bool PreProcessAvoidanceOrbit(IEntityWrapper destinationEntity, Destination destination)
        {
            var methodName = "PreProcessAvoidanceOrbit";
            LogTrace(methodName);

            LogMessage(methodName, LogSeverityTypes.Debug, "HasGoalEntity: {0}", destination.HasGoalEntity);

            //If we have a goal entity which is still valid...
            if (destination.HasGoalEntity && _entityProvider.EntityWrappersById.ContainsKey(destination.GoalEntityId))
            {
                if (destinationEntity.Distance <= destination.MinimumDistance)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "We're not yet past the minimum distance of {0}m to nearby collidable entity \"{1}\".",
                        destination.MinimumDistance, destinationEntity.Name);
                    return true;
                }

                //We're done with the emergency part of the movement. We've cleared the collidable.
                //No matter what else, this is no longer emergency movement.
                destination.IsObstacleAvoidanceMovement = false;

                var goalEntity = _entityProvider.EntityWrappersById[destination.GoalEntityId];

                if (IsEntityInsideEntity(goalEntity, destinationEntity))
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "We're past the minimum distance of {0}m to nearby collidable entity \"{1}\" but our goal entity \"{2}\" is inside it.",
                        destination.MinimumDistance, destinationEntity.Name, goalEntity.Name);
                    return true;
                }

                var myCenterPoint = new Point3D(_meCache.ToEntity.X, _meCache.ToEntity.Y, _meCache.ToEntity.Z);
                var destinationPoint = new Point3D(goalEntity.X, goalEntity.Y, goalEntity.Z);

                var collidableCenterPoint = new Point3D(destinationEntity.X, destinationEntity.Y, destinationEntity.Z);
                var adjustedCollidableRadius = destinationEntity.Radius + 500;

                var doesCollide = WillICollideWithSphere(myCenterPoint, destinationPoint, collidableCenterPoint,
                                                         adjustedCollidableRadius);

                if (doesCollide)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "We're past the minimum distance of {0}m to nearby collidable entity \"{1}\" but would collide with it en route to goal entity \"{2}\".",
                        destination.MinimumDistance, destinationEntity.Name, goalEntity.Name);
                    return true;
                }

                LogMessage(methodName, LogSeverityTypes.Debug, "We're past the minimum distance of {0}m to nearby collidable entity \"{1}\" and have a clear vector to goal entity \"{2}\". Removing obstacle avoidance destination.",
                    destination.MinimumDistance, destinationEntity.Name, goalEntity.Name);
                //TODO: Remove destination

                _destinationQueue.Remove(destination);

                if (!_destinationQueue.Any())
                    StopCurrentMovement(false);

                return false;
            }
            else if (destinationEntity.Distance > destination.MinimumDistance)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "We are past the minimum distance of {0}m to nearby collidable entity \"{1}\" ({2}). Removing obstacle avoidance destination.",
                    destination.MinimumDistance, destinationEntity.Name, destinationEntity.ID);

                //TODO: Remove destination
                _destinationQueue.Remove(destination);

                if (!_destinationQueue.Any())
                    StopCurrentMovement(false);

                return false;
            }

            return true;
        }

        private bool ProcessEntityOrbit(IEntityWrapper destinationEntity, Destination destination)
        {
            var methodName = "ProcessEntityOrbit";
            LogTrace(methodName, "tempEntity: {0}, destinationDistance: {1}", destinationEntity.ID, destination);

            //TODO: GIANT TODO!
            //This shouldn't be looking for a nearby collidable, but should be rather looking for collidables,
            //ANY collidable, in between me and the object I'm moving towards.

            //There are really two cases here - Approaching a target, I need to make sure I have a direct path to the target.
            //If I do, then I can approach. If not, I orbit the first obstacle until I have a clear path to either the target or the next obstacle.

            //When Orbiting, THEN I want to orbit a nearby collidable until I am clear, and then resume orbiting my original target.

            var distance = (int)destination.Distance;
            distance = GetAdjustedDestinationDistance(distance);

            if (_meCache.ToEntity.Approaching == null ||
                _meCache.ToEntity.Approaching.ID != destinationEntity.ID ||
                _lastOrbitDistance != distance)
            {
                OrbitEntity(destinationEntity, distance);
            }
            return false;
        }

        private bool WillICollideWithSphere(Point3D myCenterPoint, Point3D destinationPoint, Point3D collidableCenterPoint, double adjustedCollidableRadius)
        {
            var doesCollide = false;

            var destinationToMeVector = destinationPoint - myCenterPoint;

            var interpolationResolution = 5;
            var iterations = (int)destinationToMeVector.Length / interpolationResolution;

            for (var index = 0; index < iterations + 1 && !doesCollide; index++)
            {
                var distance = index * interpolationResolution;

                var pointOnVectorAtScale = GetPointOnLine(myCenterPoint, destinationPoint, distance);
                var doSpheresIntersect = DoSpheresIntersect(pointOnVectorAtScale, _meCache.ToEntity.Radius, collidableCenterPoint, adjustedCollidableRadius);

                if (doSpheresIntersect)
                {
                    doesCollide = true;
                }
            }

            return doesCollide;
        }

        private bool DoSpheresIntersect(Point3D firstCenter, double firstRadius, Point3D secondCenter, double secondRadius)
        {
            var vectorBetweenCenters = secondCenter - firstCenter;
            return vectorBetweenCenters.Length <= firstRadius + secondRadius;
        }

        private Point3D GetPointOnLine(Point3D start, Point3D end, double distance)
        {
            //Get vector BA
            var destinationToMeVector = end - start;

            //Normalize the vector
            destinationToMeVector.Normalize();

            //Figure out the point 5 units from myCenterPoint along destinationToMeVector
            var result = distance * destinationToMeVector + start;
            return result;
        }

        private int GetAdjustedDestinationDistance(int destinationDistance)
        {
            var methodName = "GetAdjustedDestinationDistance";
            LogTrace(methodName, "DestinationDistance: {0}", destinationDistance);

            var entityCountEcmingMe = Core.Metatron.Attackers.QueueTargetsByEntityId.Values
                .Count(queueTarget => queueTarget.Priority == (int)TargetPriorities.Kill_RemoteSensorDampener);

            if (entityCountEcmingMe > 0)
            {
                var maxTargetRange = _ship.MaxTargetRange;
                if (maxTargetRange < 0)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Error: MaxTargetRange was less than 0 ({0}).",
                        maxTargetRange);
                    return destinationDistance;
                }

                var effectiveMaxTargetRange = (int)(maxTargetRange * 0.9);

                if (effectiveMaxTargetRange < destinationDistance &&
                    (_orbitDistanceOverride <= 0 || effectiveMaxTargetRange < _orbitDistanceOverride))
                {
                    _orbitDistanceOverride = effectiveMaxTargetRange;
                }

                if (_orbitDistanceOverride > 0)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug,
                               "Overriding orbit distance from {0}m to {1}m due to remote sensor damps.",
                               destinationDistance, _orbitDistanceOverride);
                    destinationDistance = _orbitDistanceOverride;
                }
            }
            else
            {
                _orbitDistanceOverride = 0;
            }

            return destinationDistance;
        }

        private bool ProcessSolarSystem(Destination destination)
        {
            var methodName = "ProcessSolarSystem";

            if (_meCache.InStation && !_meCache.InSpace)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Inserting undock destination before attempting to go to a solar system.");
                _destinationQueue.Insert(0, new Destination(DestinationTypes.Undock));

                return true;
            }

            //If we're not curently in the same solar system as the destination, we need to get there
            //Currently this wll fire every time we move in a system other than the desti, i.e. we're one jump out.
            //tempDestination is always the first destination system in queue. So, it's quite likely to be a system we're not 
            //in. Because of this we should handle moving to a different system's gate IF WE'RE NOT IN THAT SYSTEM, not
            //trying to re-queue to that system
            if (destination.SolarSystemId == _meCache.SolarSystemId)
            {
                //If we're in target solar system, dequeue.
                LogMessage(methodName, LogSeverityTypes.Standard, "Arrived at destination solar system {0}; dequeueing.",
                           destination.SolarSystemId);
                DequeueDestination(destination);
                return true;
            }
            else
            {
                var destinationPath = _isxeveProvider.Eve.GetToDestinationPath();
                if (destinationPath == null || destinationPath.Count == 0)
                {
                    _isxeveProvider.Eve.AddWaypoint(destination.SolarSystemId);
                }
                _movementType = MovementTypes.SystemChange;
                return false;
            }
            return false;
        }

        /// <summary>
        /// Process a bookmark destination.
        /// </summary>
        /// <param name="destination"></param>
        /// <returns>True if we need to process another destination this pulse, otherwise false.</returns>
        private bool ProcessBookmark(Destination destination)
        {
            var methodName = "ProcessBookmark";

            if (!Core.Metatron.BookMarkCache.BookMarksById.ContainsKey(destination.BookMarkId))
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Error; Could not find bookmark with ID {0}. Removing destination.",
                    destination.BookMarkId);
                DequeueDestination(destination);
                return true;
            }

            //Get a reference to the bookmark we're moving to
            var bookMark = Core.Metatron.BookMarkCache.BookMarksById[destination.BookMarkId];

            LogMessage(methodName, LogSeverityTypes.Standard, "Processing destination bookmark \"{0}\"...",
                bookMark.Label);

            //If I'm docked...
            if (_meCache.InStation && !_meCache.InSpace)
            {
                //if this bookmark is the current station and I should be docked...
                if (destination.Dock && bookMark.ItemID > 0 && _meCache.StationId == bookMark.ItemID)
                {
                    //Just dequeue it, we're at the bookmark.
                    LogMessage(methodName, LogSeverityTypes.Standard, "We've arrived at station bookmark \"{0}\".",
                        bookMark.Label);
                    DequeueDestination(destination);
                    return true;
                }

                LogMessage(methodName, LogSeverityTypes.Debug, "Inserting undock destination before attempting to go to a bookmark.");
                _destinationQueue.Insert(0, new Destination(DestinationTypes.Undock));
                return true;
            }

            //If the bookmark is in another system, get a list of SolarSystemIDs from here to destination solar.
            if (bookMark.SolarSystemID != _meCache.SolarSystemId)
            {
                //Temporarily remove the destination
                //RemoveAt(0) because tempDestination isalways the first in queue
                DequeueDestination(destination);
                LogMessage(methodName, LogSeverityTypes.Standard, "Moving from current solarsystem {0} to bookmark solarsytem {1}.",
                    _meCache.SolarSystemId, bookMark.SolarSystemID);

                //Add a solarsystem destination, acounting for curent index
                if (!QueuePathToSolarSystem(bookMark.SolarSystemID))
                    return false;

                //update tempDestination and goto case solarsystem
                //Should always have desetinations after QPTSS, but might not
                if (_destinationQueue.Count > 0)
                    return true;
            }

            //If a bookmark has a valid ItemID, then it's a station. Yay for EVE oddities.
            //if the bookmark is a station, queue it as a entity. IT'll auto dock if the entity is a station.
            if (bookMark.ItemID > 0 && bookMark.TypeID != (int)TypeIDs.SolarSystem)
            {
                var indexOfDest = _destinationQueue.IndexOf(destination);
                //Temporarily remove the destination
                DequeueDestination(destination);
                //Queue it as an Entity
                var newDesti = new Destination(DestinationTypes.Entity, bookMark.ItemID)
                {
                    Dock = destination.Dock,
                    Distance = destination.Distance
                };
                _destinationQueue.Insert(indexOfDest, newDesti);

                //update the defense fleeDestination if necessary
                if (Core.Metatron.Defense.FleeDestination != null &&
                    Core.Metatron.Defense.FleeDestination == destination)
                {
                    Core.Metatron.Defense.FleeDestination = newDesti;
                }

                //update tempDestination and goto case Entity so it'll process the entity destination
                return true;
            }

            var dist = DistanceFromBookmarkToEntity(bookMark, _meCache.ToEntity);
            if (dist > (int)Ranges.Warp)
            {
                IEntityWrapper entity = null;
                if (bookMark.ItemID > 0 && _entityProvider.EntityWrappersById.ContainsKey(bookMark.ItemID))
                    entity = _entityProvider.EntityWrappersById[bookMark.ItemID];

                //Prepare to warp to the entity if possible. Entity CAN be null, i.e. for when there's no
                //Station for this bookmark.
                if ((entity != null && PrepareForWarp(entity)) || (entity == null && PrepareForWarp(bookMark)))
                {
                    //It's not a celestial; which means we're warping to the bookmark.
                    WarpToBookMark(bookMark, GetEveWarpToDistance(destination.WarpToDistance));
                }
            }
            else
            {
                //remove destination
                DequeueDestination(destination);
                LogMessage(methodName, LogSeverityTypes.Standard, "We've arrived at bookmark \"{0}\".",
                    bookMark.Label);
                return true;
            }
            return false;
        }

        public int GetEveWarpToDistance(int warpToDistance)
        {
            LogTrace("GetEveWarpToDistance", warpToDistance.ToString());
            if (warpToDistance < WarpTo10KM)
                return WarpTo0KM;

            if (warpToDistance < WarpTo20KM)
                return WarpTo10KM;

            if (warpToDistance < WarpTo30KM)
                return WarpTo20KM;

            if (warpToDistance < WarpTo50KM)
                return WarpTo30KM;

            if (warpToDistance < WarpTp70KM)
                return WarpTo50KM;

            if (warpToDistance < WarpTo100KM)
                return WarpTp70KM;

            return WarpTo100KM;
        }

        private List<int> GetSolarSystemsToDestination(int solarSystemId)
        {
            var methodName = "GetSolarSystemsToDestination";
            LogTrace(methodName);

            var waypoints = _isxeveProvider.Eve.GetToDestinationPath();

            //Need to 'set destination' if we have no systems, or the last system isn't our destination
            if (waypoints.Count == 0 || waypoints.Last() != solarSystemId)
            {
                if (waypoints.Count > 0)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Have {0} systems in route, but the desti is {1} (not {2})!",
                        waypoints.Count, waypoints.Last(), solarSystemId);
                }

                var system = Universe.ByID(solarSystemId);

                if (LavishScriptObject.IsNullOrInvalid(system))
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Error: Could not resolve solar system ID \"{0}\" to a system.", solarSystemId);
                    return null;
                }

                LogMessage(methodName, LogSeverityTypes.Debug, "Setting destination to solarsystem \"{0}\"",
                    system.Name);
                system.SetDestination();
                return null;
            }
            return waypoints;
        }

        private bool QueuePathToSolarSystem(int solarSystemId)
        {
            var methodName = "QueuePathToSolarSystem";
            LogTrace(methodName, "SolarSystemId: {0}", solarSystemId);

            _destinationQueue.Add(new Destination(DestinationTypes.SolarSystem, solarSystemId));

            return true;
        }

        private void WarpToEntity(Int64 entityId, int warpToMeters)
        {
            var methodName = "WarpToEntity";
            LogMessage(methodName, LogSeverityTypes.Trace, "{0},{1}", entityId, warpToMeters);

            if (_meCache.ToEntity.Mode == (int)Modes.Warp)
                return;

            //Entity tempEntity = new Entity(entityID);
            var tempEntity = _entityProvider.EntityWrappersById[entityId];

            LogMessage(methodName, LogSeverityTypes.Standard, "Warping to entity {0} ({1}) at {2} meters",
                tempEntity.Name, tempEntity.ID, warpToMeters);
            tempEntity.WarpTo(warpToMeters);
            _movementType = MovementTypes.Warp;
            PulseCounter += 2;
        }

        //TODO: I need to re-evaluate these. They're weird. If they're not ready for warp, they'll try to align... otherwise they won't do shit. It's weird.
        private bool PrepareForWarp(IEntityWrapper entity)
        {
            var methodName = "PrepareForWarp";
            LogTrace(methodName, "Entity: {0} ({1})", entity.Name, entity.ID);

            //Don't do shit if we're warp jammed
            if (_meCache.ToEntity.IsWarpScrambled)
            {
                _logging.LogMessage(ModuleName, methodName, LogSeverityTypes.Debug, "Can't yet prepare for warp due to being warp jammed.");
                return false;
            }

            //If I'm not warp scrambled then I have no need to have any targets queued. Clear the queue.
            if (PrepareForWarp()) return true;

            if (_meCache.ToEntity.Approaching == null || _meCache.ToEntity.Approaching.ID != entity.ID)
            {
                entity.AlignTo();
            }
            return false;
        }

        private bool PrepareForWarp(IBookMark bookMark)
        {
            var methodName = "PrepareForWarp";
            LogTrace(methodName, "BookMark: {0} ({1})", bookMark.Label, bookMark.ID);

            //Don't even do bookmark warp prep if we're warp jammed
            if (_meCache.ToEntity.IsWarpScrambled)
            {
                _logging.LogMessage(ModuleName, methodName, LogSeverityTypes.Debug, "Can't yet prepare for warp due to being warp jammed.");
                return false;
            }

            //If I'm not warp scrambled then I have no need to have any targets queued. Clear the queue.
            if (PrepareForWarp()) return true;

            bookMark.AlignTo();
            return false;
        }

        private bool PrepareForWarp()
        {
            var methodName = "PrepareForWarp";
            LogTrace(methodName);

            _targetQueue.ClearQueue();

            //if I have non-covertops-cloaks enabled, disable them
            _ship.DeactivateModuleList(_ship.OtherCloakingDeviceModules, true);
            DeactivatePropulsionMods();

            if ((!_drones.RecentlyLaunchedDrones && _drones.DronesInSpace == 0) || (Core.Metatron.Defense.IsFleeing && _pulsesWaitedForDrones >= _maxPulsesToWaitForDrones))
            {
                LogMessage(methodName, LogSeverityTypes.Debug,
                    _drones.DronesInSpace == 0
                               ? "No drones in space; warp prepare complete."
                               : "We've waited too long for drones, warp prep complete.");

                _pulsesWaitedForDrones = 0;
                return true;
            }

            _pulsesWaitedForDrones++;
            _drones.RecallAllDrones(true);
            return false;
        }

        private void WarpToBookMark(IBookMark bookMark, int warpToMeters)
        {
            var methodName = "WarpToBookMark";
            LogTrace(methodName, "Bookmark Label: {0}, distance: {1} ({2}, {3}, {4})", bookMark.Label, warpToMeters,
                bookMark.X, bookMark.Y, bookMark.Z);

            LogMessage(methodName, LogSeverityTypes.Standard, "Warping to bookmark {0} at {1} meters",
                bookMark.Label, warpToMeters);
            bookMark.WarpTo(warpToMeters);
            _movementType = MovementTypes.Warp;
            PulseCounter += 2;
        }

        private void WarpToFleetMember(FleetMember fleetMember)
        {
            var methodName = "WarpToFleetMember";
            LogTrace(methodName, "FleetMember: {0}", fleetMember.CharID);

            string pilotName;
            using (var pilot = fleetMember)
            {
                pilotName = pilot.Name;
            }

            LogMessage(methodName, LogSeverityTypes.Standard, "Warping to fleet member {0}",
                pilotName);
            fleetMember.WarpTo();
            _movementType = MovementTypes.Warp;
            PulseCounter += 2;
        }

        private void KeepEntityAtRange(IEntityWrapper entity, int distance)
        {
            var methodName = "KeepAtRange";
            LogTrace(methodName, "Entity: {0}, Distance: {1}", entity.ID, distance);

            LogMessage(methodName, LogSeverityTypes.Standard, "Keeping entity {0} ({1}) at range {2}.",
                       entity.Name, entity.ID, distance);

            entity.KeepAtRange(distance);

            _lastOrbitDistance = distance;

            _movementType = MovementTypes.Approach;
            PulseCounter += 1;
        }

        private void OrbitEntity(IEntityWrapper entity, int distance)
        {
            var methodName = "OrbitEntity";
            LogTrace(methodName, "Entity: {0}, Distance: {1}", entity.ID, distance);

            LogMessage(methodName, LogSeverityTypes.Standard, "Orbiting entity {0} ({1}) at distance {2}.",
                entity.Name, entity.ID, distance);

            entity.Orbit(distance);

            _lastOrbitDistance = distance;

            _movementType = MovementTypes.Approach;
            PulseCounter += 1;
        }

        private void ApproachEntity(IEntityWrapper entity)
        {
            var methodName = "ApproachEntity";
            LogTrace(methodName, "Entity: {0}", entity.ID);

            LogMessage(methodName, LogSeverityTypes.Standard, "Approaching entity {0} ({1})",
                entity.Name, entity.ID);

            entity.Approach();

            _movementType = MovementTypes.Approach;
            PulseCounter += 2;
        }

        private void AlignToEntity(IEntityWrapper entity)
        {
            var methodName = "ApproachEntity";
            LogTrace(methodName, "EntityID: {0}", entity.ID);

            LogMessage(methodName, LogSeverityTypes.Standard, "Approaching entity {0} ({1})",
                entity.Name, entity.ID);

            entity.AlignTo();

            _movementType = MovementTypes.Approach;
            PulseCounter += 2;
        }

        private void DockAtStation(IEntityWrapper entity)
        {
            var methodName = "DockAtStation";
            LogTrace(methodName, "Entity: {0}", entity.ID);

            if (PreSessionChanged != null)
                PreSessionChanged(this, new SessionChangedEventArgs(false));

            entity.ToEntity.Dock();

            _movementType = MovementTypes.Dock;
            LogMessage(methodName, LogSeverityTypes.Standard, "Docking at station {0} ({1})",
                entity.Name, entity.ID);
            Core.Metatron.ModuleManager.DelayPulseByHighestTime(DelayAfterDock);
        }

        private void UseStarGate(IEntityWrapper entity)
        {
            var methodName = "UseStarGate";
            LogTrace(methodName, "Entity: {0}", entity.ID);

            if (PreSessionChanged != null)
                PreSessionChanged(this, new SessionChangedEventArgs(true));

            LogMessage(methodName, LogSeverityTypes.Standard, "Jumping at stargate {0} ({1}, {2}).",
                entity.Name, entity.ID, entity.Distance);
            _movementType = MovementTypes.SystemChange;
            _lastSolarSystemID = _meCache.SolarSystemId;
            entity.ToEntity.Jump();
            Core.Metatron.ModuleManager.DelayPulseByHighestTime(DelayAfterSystemChange);
        }

        private void UseWarpGate(IEntityWrapper entity)
        {
            var methodName = "UseWarpGate";
            LogTrace(methodName, "Entity: {0}", entity.ID);

            LogMessage(methodName, LogSeverityTypes.Standard, "Activating warp gate {0} ({1}, {2}).",
                entity.Name, entity.ID, entity.Distance);
            entity.Activate();
            _movementType = MovementTypes.Warp;
            Core.Metatron.ModuleManager.PulseCounter += 2;
        }

        private double DistanceFromBookmarkToEntity(IBookMark bookMark, IMeToEntityCache entity)
        {
            var methodName = "DistanceFromBookmarkToEntity";
            LogTrace(methodName, "\'{0}\': {1}, {2}, {3}; \'{4}\': {5}, {6}, {7}",
                bookMark.Label, bookMark.X, bookMark.Y, bookMark.Z, entity.Name, entity.X, entity.Y, entity.Z);

            var distance = Distance(bookMark.X, bookMark.Y, bookMark.Z, entity.X, entity.Y, entity.Z);
            return distance;
        }

        public void StopCurrentMovement(bool dequeueDestination)
        {
            if (dequeueDestination)
            {
                DequeueDestination(_destinationQueue.First());
            }

            _isxeveProvider.Eve.Execute(ExecuteCommand.CmdStopShip);
            DeactivatePropulsionMods();
        }
    }
}
