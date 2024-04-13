using System;
using System.Collections.Generic;
using System.Linq;
using Metatron.BehaviorModules.PartialBehaviors;
using Metatron.Core;
using LavishScriptAPI;
using System.Diagnostics;
using Metatron.ActionModules;
using Metatron.Core.Config;
using Metatron.Core.EventCommunication;
using Metatron.Core.Interfaces;
using IShip = Metatron.Core.IShip;

namespace Metatron.BehaviorModules
{
    internal sealed class Mining : BehaviorBase
    {
        private readonly int MinimumWarpinDistance = 10000;
        private readonly int DistanceToCorporateHangarArray = 25000;
        private readonly int PlanetWarpInDistance = 30000;

        private readonly MoveToDropOffLocationPartialBehavior _moveToDropOffLocationPartialBehavior;
        private readonly DropOffCargoPartialBehavior _dropOffCargoPartialBehavior;

        private MiningStates _miningState = MiningStates.Idle;
        private MoveToDropoffStates _moveToDropoffState = MoveToDropoffStates.Idle;
        private BounceWarpStates _bounceWarpState = BounceWarpStates.Idle;

        #region Random Wait Objects
        private readonly RandomWaitObject _doMiningRandomWait;
        private readonly RandomWaitObject _goToAsteroidsRandomWait;
        private readonly RandomWaitObject _goToDropoffRandomWait;
        #endregion

        private readonly Stopwatch _timeForTrip = new Stopwatch();
        private bool _needStarted = true;
        private int _timesBookmarkInvalid;
        //Track warp-in distance semily since we can't just tag a bookmark with it.
        private int _warpInDistance;
        //counter for and max # of pulses to wait for acknowledgement
        private int MaxPulsesWaitForAck = 2;
        private int _pulsesWaitedForAck;
        private Int64 _idOfCanRequested;
        private bool _gotAcknowledgement;

        private string _temporaryMiningBookmarkLabel;
        private bool _wereOldTemporaryMiningBookMarksRemoved;

        //Handle pickup request acknowledgements
        readonly EventHandler<FleetNeedPickupEventArgs> _fleetNeedPickupConfirmed;
        volatile List<FleetNeedPickupEventArgs> _queuedPickupAcknowledgements = new List<FleetNeedPickupEventArgs>();

        private readonly ISafespots _safespots;
        private readonly IMovement _movement;
        private readonly IShip _ship;
        private readonly IMeCache _meCache;
        private readonly IMainConfiguration _mainConfiguration;
        private readonly ISocial _social;
        private readonly IMovementConfiguration _movementConfiguration;
        private readonly IAsteroidBelts _asteroidBelts;
        private readonly ICargoConfiguration _cargoConfiguration;
        private readonly IEntityProvider _entityProvider;
        private readonly IMiningConfiguration _miningConfiguration;
        private readonly IIsxeveProvider _isxeveProvider;
        private readonly IBookMarkCache _bookMarkCache;
        private readonly ITargetQueue _targetQueue;

        public Mining(ICargoConfiguration cargoConfiguration, IMainConfiguration mainConfiguration, IMeCache meCache, IShip ship,
            IEntityProvider entityProvider, ISafespots safespots, IMovement movement, ISocial social, IMovementConfiguration movementConfiguration,
            IAsteroidBelts asteroidBelts, MoveToDropOffLocationPartialBehavior moveToDropOffLocationPartialBehavior, DropOffCargoPartialBehavior dropOffCargoPartialBehavior,
            IMiningConfiguration miningConfiguration, IIsxeveProvider isxeveProvider, IBookMarkCache bookMarkCache, ITargetQueue targetQueue)
        {
            _safespots = safespots;
            _movement = movement;
            _social = social;
            _movementConfiguration = movementConfiguration;
            _asteroidBelts = asteroidBelts;
            _moveToDropOffLocationPartialBehavior = moveToDropOffLocationPartialBehavior;
            _dropOffCargoPartialBehavior = dropOffCargoPartialBehavior;
            _miningConfiguration = miningConfiguration;
            _isxeveProvider = isxeveProvider;
            _bookMarkCache = bookMarkCache;
            _targetQueue = targetQueue;
            _ship = ship;
            _meCache = meCache;
            _mainConfiguration = mainConfiguration;
            _cargoConfiguration = cargoConfiguration;
            _entityProvider = entityProvider;

            PulseFrequency = 2;
            BehaviorManager.BehaviorsToPulse.Add(BotModes.Mining, this);

            ModuleName = "Mining";
            CanSendCombatAssistanceRequests = true;

            _doMiningRandomWait = new RandomWaitObject(ModuleName);
            _doMiningRandomWait.AddWait(new KeyValuePair<int, int>(11, 30), 5);
            _doMiningRandomWait.AddWait(new KeyValuePair<int, int>(5, 10), 25);

            _goToAsteroidsRandomWait = new RandomWaitObject(ModuleName);
            _goToAsteroidsRandomWait.AddWait(new KeyValuePair<int, int>(10, 15), 10);

            _goToDropoffRandomWait = new RandomWaitObject(ModuleName);
            _goToDropoffRandomWait.AddWait(new KeyValuePair<int, int>(10, 30), 33);

            _fleetNeedPickupConfirmed = OnFleetNeedPickupConfirmed;
            Core.Metatron.EventCommunications.FleetNeedPickupConfirmedEvent.EventRaised += _fleetNeedPickupConfirmed;
        }

        public override void Pulse()
        {
            var methodName = "Pulse";
            LogTrace(methodName);

            if (!ShouldPulse())
                return;

            if (_mainConfiguration.ActiveBehavior != BotModes.Mining || _meCache.ToEntity.Mode == (int)Modes.Warp ||
                (_movement.IsMoving && (_movement.MovementType != MovementTypes.Approach || _miningState == MiningStates.MoveToDropoff)))
            {
                return;
            }

            if (!_wereOldTemporaryMiningBookMarksRemoved)
            {
                RemoveTemporaryMiningBookmarks();
                _wereOldTemporaryMiningBookMarksRemoved = true;
                return;
            }

            if (!_ship.IsInventoryReady) return;

            //If defense has to do something, reset any dropoff states and return
            if (Core.Metatron.Defense.IsFleeing)
            {
                _dropOffCargoPartialBehavior.Reset();
                return;
            }

            StartPulseProfiling();

            if (_needStarted)
            {
                _timeForTrip.Start();
                _needStarted = false;
            }

            //StartMethodProfiling("SetPulseState");
            SetPulseState();
            //EndMethodProfiling();
            //StartMethodProfiling("ProcessPulseState");
            ProcessPulseState();
            //EndMethodProfiling();

            EndPulseProfiling();
        }

        protected override void SetPulseState()
        {
            var methodName = "SetPulseState";
            LogTrace(methodName);

            if (Core.Metatron.Defense.IsFleeing || !_social.IsLocalSafe)
            {
                //If defense has to do its thing set state to idle
                _miningState = MiningStates.Idle;

                //Reset jetcan dropoff state so we don't drop a can at a safe pos like an idiot
                _dropOffCargoPartialBehavior.Reset();
                return;
            }

            if (_temporaryMiningBookmarkLabel == null)
            {
                var allBeltsEmpty = false;
                if (_movementConfiguration.OnlyUseBeltBookmarks)
                {
                    if (_asteroidBelts.AllBookMarkedBeltsEmpty)
                    {
                        allBeltsEmpty = true;
                    }
                }
                else
                {
                    if (_asteroidBelts.AllBeltsEmpty)
                    {
                        allBeltsEmpty = true;
                    }
                }

                if (allBeltsEmpty)
                {
                    LogMessage(methodName, LogSeverityTypes.Critical, "Error: All belts are empty.");
                    _miningState = MiningStates.Error;
                    return;
                }
            }

            if (_meCache.InStation && !LavishScriptObject.IsNullOrInvalid(_meCache.Me.Station) &&
                !_meCache.InSpace)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "We are docked. Checking for dropoff.");
                if (_cargoConfiguration.DropoffLocation.LocationType == LocationTypes.Station
                    || _cargoConfiguration.DropoffLocation.LocationType == LocationTypes.StationCorpHangar)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Dropoff is at station");
                    if (!_dropOffCargoPartialBehavior.IsComplete)
                    {
                        _miningState = MiningStates.DoDropoff;
                        return;
                    }

                    if (true)
                    {
                        if (!_ship.IsOreHoldActive)
                        {
                            LogMessage(methodName, LogSeverityTypes.Standard, "Making ore hold active before checking cargo capacities for state determination.");
                            _ship.MakeOreHoldActive();
                            return;
                        }
                    }
                    else if (!_ship.IsCargoHoldActive)
                    {
                        LogMessage(methodName, LogSeverityTypes.Standard, "Making cargo hold active before checking cargo capacities for state determination.");
                        _ship.MakeCargoHoldActive();
                        return;
                    }

                    LogMessage(methodName, LogSeverityTypes.Debug, "UsedMiningCapacity - {0}, CargoFullThreshold - {1}",
                        UsedMiningCapacity, _cargoConfiguration.CargoFullThreshold);

                    if (UsedMiningCapacity - MiningCapacityUsedByMiningCrystals > 0)
                    {

                        _miningState = MiningStates.DoDropoff;
                        return;
                    }
                }

                _miningState = HasMaxRuntimeExpired() ? MiningStates.Idle : MiningStates.GoToAsteroids;
            }
            else
            {
                //Make sure we actually have some mining lasers to use.
                if (_ship.MiningLaserModules.Count == 0)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Error: No mining lasers detected.");
                    _miningState = MiningStates.Error;
                    return;
                }

                //We're in space. Need to unload?
                LogMessage(methodName, LogSeverityTypes.Debug, "UsedMiningCapacity - {0}, CargoFullThreshold - {1}",
                    UsedMiningCapacity, _cargoConfiguration.CargoFullThreshold);

                if (_cargoConfiguration.CargoFullThreshold <= UsedMiningCapacity ||
                    !_dropOffCargoPartialBehavior.IsComplete)
                {
                    //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                    //	"SetPulseState", string.Format("UsedCargoCapacity - {0}, CargoFullThreshold - {1}",
                    //	Core.Metatron._Me.Ship.UsedCargoCapacity, _cargoConfiguration.CargoFullThreshold)));
                    //detrmine if we're at dropoff
                    switch (_cargoConfiguration.DropoffLocation.LocationType)
                    {
                        case LocationTypes.Station:
                            //WE're in space; meaning we're not there.
                            _miningState = MiningStates.MoveToDropoff;
                            return;
                        case LocationTypes.StationCorpHangar:
                            goto case LocationTypes.Station;
                        case LocationTypes.Jetcan:
                            //Not like we have to go anywhere, but make sure we're at a belt
                            LogMessage(methodName, LogSeverityTypes.Debug, "IsAtAsteroidBelt: {0}, IsBeltEmpty: {1}",
                                _asteroidBelts.IsAtAsteroidBelt(), _asteroidBelts.IsBeltEmpty);
                            if ((_asteroidBelts.IsAtAsteroidBelt() && !_asteroidBelts.IsBeltEmpty) ||
                                !_dropOffCargoPartialBehavior.IsComplete)
                            {
                                _miningState = MiningStates.DoDropoff;
                                return;
                            }
                            break;
                        case LocationTypes.ShipBay:
                            //See if we're in range of our dropoff
                            if (_entityProvider.EntityWrappersById.ContainsKey(
                                _cargoConfiguration.DropoffLocation.EntityID))
                            {
                                var ce = _entityProvider.EntityWrappersById[
                                    _cargoConfiguration.DropoffLocation.EntityID];
                                if (ce.Distance > (int)Ranges.LootActivate)
                                {
                                    _miningState = MiningStates.MoveToDropoff;
                                    return;
                                }

                                _miningState = MiningStates.DoDropoff;
                                return;
                            }

                            LogMessage(methodName, LogSeverityTypes.Standard, "Error: TrackDropoff ship with ID {0} not found.",
                                       _cargoConfiguration.DropoffLocation.EntityID);
                            _miningState = MiningStates.Error;
                            return;
                        case LocationTypes.CorpHangarArray:
                            //See if we're at the CHA
                            var hangar = Core.Metatron.BookMarkCache.FirstBookMarkStartingWith(
                                _cargoConfiguration.DropoffLocation.BookmarkLabel, true);

                            if (hangar == null)
                            {
                                LogMessage(methodName, LogSeverityTypes.Standard, "Error; could not find bookmark for corp hangar array \"{0}\".",
                                    _cargoConfiguration.DropoffLocation.BookmarkLabel);
                                break;
                            }

                            if (DistanceTo(hangar.X, hangar.Y, hangar.Z) > (int)Ranges.Warp)
                            {
                                _miningState = MiningStates.MoveToDropoff;
                                return;
                            }

                            if (_entityProvider.EntityWrappersById.ContainsKey(
                                _cargoConfiguration.DropoffLocation.EntityID))
                            {
                                var dropoffHangar = _entityProvider.EntityWrappersById[
                                    _cargoConfiguration.DropoffLocation.EntityID];
                                if (dropoffHangar.Distance >= DistanceToCorporateHangarArray)
                                {
                                    _miningState = MiningStates.MoveToDropoff;
                                    return;
                                }
                                _miningState = MiningStates.DoDropoff;
                                return;
                            }

                            LogMessage(methodName, LogSeverityTypes.Standard, "Error: At dropoff bookmark \"{0}\" but could not find entity with ID {1}.",
                                       hangar.Label, _cargoConfiguration.DropoffLocation.EntityID);
                            _miningState = MiningStates.Error;
                            return;
                    }
                }

                //See if we're at a belt
                if (_asteroidBelts.AsteroidsInRange.Count == 0)
                {
                    //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                    //	"SetPulseState", "No asteroids in range, going to belt or getting closer ."));
                    //If we are there's nothing nearby. Let's gtfo.
                    //Since we might land at this code right after doing a drop-off in-space, check runtime.
                    //If it returns true go ahead and go to asteroids - otherwise, do nada.
                    if (_cargoConfiguration.DropoffLocation.LocationType == LocationTypes.CorpHangarArray && HasMaxRuntimeExpired())
                    {
                        _miningState = MiningStates.Idle;
                    }
                    else
                    {
                        _miningState = MiningStates.GoToAsteroids;
                    }
                }
                else
                {
                    //If there are any other players near us, change belts.
                    //Todo: REwork this into three separate options:
                    //1) Flee on Player Too Near
                    //2) Alert on Player Too Near
                    //3) Min Distance to Players
                    //That way I can handle sylvester's feature request and have it alert
                    //but NOT FLEE on near player.
                    if (_miningConfiguration.MinDistanceToPlayers > -1)
                    {
                        var nearbyPlayerCount = GetNearbyPlayerCount();

                        if (nearbyPlayerCount > 0)
                        {
                            _miningState = MiningStates.GoToAsteroids;
                            return;
                        }
                    }

                    //Cool, eat rocks mang.
                    //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                    //	"SetPulseState", "Asteroids in range, eating rocks."));
                    _miningState = MiningStates.DoMining;
                }
            }
        }

        protected override void ProcessPulseState()
        {
            var methodName = "ProcessPulseState";
            LogTrace(methodName);

            //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
            //   "ProcessPulseState", String.Format("ProcessPulseState: State: {0}", MiningState.ToString())));
            var gotoCase = false;

            LogMessage(methodName, LogSeverityTypes.Debug, $"Mining state is {_miningState}");
            switch (_miningState)
            {
                case MiningStates.Idle:
                    //do nothing
                    break;
                case MiningStates.GoToAsteroids:
                    if (_movement.IsMoving)
                    {
                        return;
                    }

                    if (_needStarted)
                    {
                        _timeForTrip.Start();
                        _needStarted = false;
                    }

                    if (_goToAsteroidsRandomWait.ShouldWait())
                    {
                        return;
                    }

                    MoveToAsteroids();
                    break;
                case MiningStates.DoMining:
                    if (_doMiningRandomWait.ShouldWait())
                    {
                        return;
                    }

                    Mine();
                    break;
                case MiningStates.MoveToDropoff:
                    if (_movement.IsMoving)
                        return;

                    gotoCase = MoveToDropoff();

                    if (gotoCase)
                    {
                        if (_miningState == MiningStates.Error)
                            goto case MiningStates.Error;
                        if (_miningState == MiningStates.DoDropoff)
                            goto case MiningStates.DoDropoff;
                    }
                    break;
                case MiningStates.DoDropoff:
                    Dropoff();
                    break;
                case MiningStates.Error:
                    if (_safespots.IsSafe())
                    {
                        IsEnabled = false;
                        return;
                    }

                    var safeSpot = _safespots.GetSafeSpot();
                    _movement.QueueDestination(safeSpot);
                    LogMessage(methodName, LogSeverityTypes.Standard, "Moving to safespot due to an error condition.");
                    break;
                default:

                    break;
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

        public double UsedMiningCapacity
        {
            get
            {

                return _ship.UsedOreHoldCapacity;
            }
        }

        public double MiningCapacity
        {
            get
            {
                return _ship.OreHoldCapacity;
            }
        }

        public double MiningCapacityUsedByMiningCrystals
        {
            get
            {
                return _meCache.Ship.Cargo
                    .Where(item => item.GroupID == (int)GroupIDs.MiningCrystal || item.GroupID == (int)GroupIDs.MercoxitMiningCrystal)
                    .Sum(item => item.Volume * item.Quantity);
            }
        }

        private void MoveToAsteroids()
        {
            var methodName = "MoveToAsteroids";
            LogTrace(methodName);

            if (_meCache.InStation && !_meCache.InSpace)
            {
                _movement.QueueDestination(new Destination(DestinationTypes.Undock));
                return;
            }

            //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
            //    "ProcessPulseState", String.Format("GoToAsteroids: IsBeltEmpty(): {0}",
            //    _asteroidBelts.IsBeltEmpty)));
            if (_asteroidBelts.IsBeltEmpty)
            {
                //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                //	"ProcessPulseState", String.Format("GoToAsteroids: Belt is empty, firing GoToBelt")));
                MoveToAsteroidBelt(false);
                return;
            }

            //If there are any other players near us, change belts.
            //Todo: REwork this into three separate options:
            //1) Flee on Player Too Near
            //2) Alert on Player Too Near
            //3) Min Distance to Players
            //That way I can handle sylvester's feature request and have it alert
            //but NOT FLEE on near player.
            if (_miningConfiguration.MinDistanceToPlayers > -1)
            {
                var nearbyPlayerCount = GetNearbyPlayerCount();

                if (nearbyPlayerCount > 0)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "We have {0} players within minimum range of {1}; changing belts.",
                        nearbyPlayerCount, _miningConfiguration.MinDistanceToPlayers);
                    MoveToAsteroidBelt(true);

                    return;
                }
            }

            var asteroid = GetNearestAsteroid();
            if (asteroid == null)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Couldn't find a valid asteroid on grid.");
                return;
            }

            //If asteroid's too far to approach, act accordingly
            if (!IsAsteroidTooFar(asteroid))
            {
                //Looks like it's not too far to slowboat to.
                if (!_movement.IsMoving)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Approaching asteroid {0} B.",
                               asteroid);
                    ApproachAsteroid(asteroid);
                }

                return;
            }

            //If we're jetcanning, mark the can full and
            // wait for any "responses" for pickup. If there are none within a few seconds, 
            // continue moving to asteroids. If there is a response, wait until the
            // 'responding' fleet member is on grid to move.
            if (_cargoConfiguration.DropoffLocation.LocationType == LocationTypes.Jetcan)
            {
                //mark active can full if we have one
                //Need to do this process for bounce warping as well
                if (Core.Metatron.JetCan.CurrentContainer != null)
                {
                    _gotAcknowledgement = false;
                    _idOfCanRequested = Core.Metatron.JetCan.CurrentContainerId;
                    MarkJetcanFull();
                    //markCanFull changes the JetcanState to CreateCan thus causing a stall because we have nothing to drop off.
                    //Reset it to avoid the stall.
                    //JetcanDropoffState = JetcanDropoffStates.Idle;
                    //commented - refactored markCanFull to behave properly based on a flag
                    return;
                }

                //If we have acknowledgement, wait for them.
                if (_queuedPickupAcknowledgements.Count > 0)
                {
                    _pulsesWaitedForAck = 0;
                    if (_entityProvider.EntityWrappersById.ContainsKey(
                        _queuedPickupAcknowledgements[0].SendingFleetMemberEntityId))
                    {
                        LogMessage(methodName, LogSeverityTypes.Standard,
                                   "Fleet member {0} on grid to pickup can; moving.",
                                   _queuedPickupAcknowledgements[0].SendingFleetMemberCharId);
                        _queuedPickupAcknowledgements.Clear();
                    }
                    else
                    {
                        var memberFound =
                            _entityProvider.EntityWrappers.Any(
                                ce => ce.Name == _queuedPickupAcknowledgements[0].SendingFleetMemberName);

                        if (memberFound)
                        {
                            LogMessage(methodName, LogSeverityTypes.Standard,
                                       "Fleet member {0} on grid to pickup can; moving.",
                                       _queuedPickupAcknowledgements[0].SendingFleetMemberCharId);
                            _queuedPickupAcknowledgements.Clear();
                        }
                        else
                        {
                            LogMessage(methodName, LogSeverityTypes.Debug, "Waiting on pickup from {0}...",
                                       _queuedPickupAcknowledgements[0].SendingFleetMemberCharId);
                            return;
                        }
                    }
                }
                else if (!_gotAcknowledgement)
                {
                    //otherwise, check the counter and continue if we've waited too long
                    if (_pulsesWaitedForAck++ >= MaxPulsesWaitForAck)
                    {
                        _pulsesWaitedForAck = 0;
                        LogMessage(methodName, LogSeverityTypes.Standard,
                                   "Have waited too long for acknowledgement and got none; moving anyway.");
                    }
                }
            }

            //Bounce warp to a roid if we can
            if (asteroid.Distance >= (int)Ranges.Warp)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Moving to distant asteroid {0} ({1}).",
                           asteroid.Name, asteroid.ID);

                var asteroidWarpDestination = new Destination(DestinationTypes.Entity, asteroid.ID)
                {
                    WarpToDistance = CalculateAsteroidWarpInDistance(asteroid)
                };
                _movement.QueueDestination(asteroidWarpDestination);
            }
            else
            {
                if (_movementConfiguration.UseBounceWarp && _movementConfiguration.UseTempBeltBookmarks)
                {
                    BounceWarp(asteroid);
                }
                else
                {
                    //If we're using belt bookmarks and the asteroid is past belt bookmark max distance,
                    //go to a new belt.
                    if (_movementConfiguration.OnlyUseBeltBookmarks &&
                        IsAsteroidTooFar(asteroid))
                    {
                        MoveToAsteroidBelt(false);
                    }
                    else
                    {
                        //Guess who gets to slowboat?
                        if (!_movement.IsMoving)
                        {
                            LogMessage(methodName, LogSeverityTypes.Debug, "Approaching asteroid {0} A.",
                                       asteroid);
                            ApproachAsteroid(asteroid);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Attempt to mine on-grid asteroids.
        /// This should only be called if there are asteroids on grid.
        /// </summary>
        private void Mine()
        {
            var methodName = "Mine";
            LogTrace(methodName);

            if (true)
            {
                var isOreHoldActive = _ship.IsOreHoldActive;
                if (!isOreHoldActive)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Making ore hold active for mining.");
                    _ship.MakeOreHoldActive();
                    return;
                }
            }
            //else if (!_ship.IsCargoHoldActive)
            //{
            //    LogMessage(methodName, LogSeverityTypes.Standard, "Making cargo hold active for mining.");
            //    _ship.MakeCargoHoldActive();
            //    return;
            //}

            //If we're doing mining we're not waiting for ack.
            _pulsesWaitedForAck = 0;

            //StartMethodProfiling("DistanceLoop");
            var priorityList = _miningConfiguration.IsIceMining
                    ? _miningConfiguration.PriorityByIceType
                    : _miningConfiguration.PriorityByOreType;

            foreach (var asteroid in _asteroidBelts.AsteroidsInRange)
            {
                if (_targetQueue.IsQueued(asteroid.ID)) continue;

                if (asteroid.Distance > _ship.MaximumMiningRange) continue;

                var subPriority = 0;
                var type = _asteroidBelts.AsteroidTypesByTypeId[asteroid.TypeID];

                if (priorityList.Contains(type))
                {
                    subPriority = priorityList.IndexOf(type);
                }

                //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                //	"ProcessPulseState", String.Format("Enqueueing {0} ({1}) @ {2}km with priority {3}, subpriority {4} ({5}).",
                //	entity.Name, entity.ID, entity._distance, (int)TargetPriorities.Mining,
                //	subPriority, type)));
                _targetQueue.EnqueueTarget(asteroid.ID, (int)TargetPriorities.Mining, TargetTypes.Mine);
            }

            if (_movement.IsMoving) return;
            if (_miningConfiguration.IsIceMining) return;

            //Make sure there's nothing too close to me
            var minimumDistance = _miningConfiguration.StripMine ? 5000 : 8000;
            var isAnotherAsteroidClose = _asteroidBelts.Asteroids.Any(asteroid => asteroid.Distance <= minimumDistance);

            if (!_miningConfiguration.StripMine)
            {
                MoveToAsteroidsForHighestPriorityMining(isAnotherAsteroidClose);
            }
            else
            {
                MoveToAsteroidsForStripMining(isAnotherAsteroidClose);
            }
            //EndMethodProfiling();
        }

        /// <summary>
        /// Get the count of players within the Minimum Distance to Players range, excluding fleet members.
        /// </summary>
        /// <returns></returns>
	    private int GetNearbyPlayerCount()
        {
            var methodName = "ArePlayersNearby";
            LogTrace(methodName);

            var pilotNamesInFleet = _meCache.FleetMembers.Select(fleetMember => fleetMember)
                                        .Where(pilot => !LavishScriptObject.IsNullOrInvalid(pilot))
                                        .Select(pilot => pilot.Name);

            var nearbyPlayers = _entityProvider.EntityWrappers
                                    .Where(entity => entity.ID != _meCache.ToEntity.Id && entity.IsPC &&
                                                     !pilotNamesInFleet.Contains(entity.Name) &&
                                                     entity.Distance <= _miningConfiguration.MinDistanceToPlayers)
                                    .ToList();

            return nearbyPlayers.Count;
        }

        private void MoveToAsteroidsForStripMining(bool isAnotherAsteroidClose)
        {
            var methodName = "MoveToAsteroidsFoStripMining";
            LogTrace(methodName, "IsAnotherAsteroidClose: {0}", isAnotherAsteroidClose);

            if (_asteroidBelts.AsteroidsOutOfRange.Count == 0) return;
            if (isAnotherAsteroidClose) return;

            var asteroidsQueued = Core.Metatron.Targeting.GetQueuedAsteroids();

            //If I have few asteroids in range and few queued...
            if (_asteroidBelts.AsteroidsInRange.Count < _ship.MiningLaserModules.Count + 1 &&
                asteroidsQueued.Count < _ship.MiningLaserModules.Count + 1)
            {
                var maxDistanceBetweenAsteroids = _ship.MaximumMiningRange * 1.8;

                //Get a copy of AsteroidIDsOutOfRange sorted by distance rather than priority since we're strippin'
                var asteroidsNotInRange = _asteroidBelts.AsteroidsOutOfRange
                    .OrderBy(entity => entity.Distance)
                    .ToList();

                var queuedAsteroids = asteroidsQueued.Select(qt => _entityProvider.EntityWrappersById[qt.Id]).ToList();

                foreach (var asteroidNotInRange in asteroidsNotInRange)
                {
                    //Make sure it's within range of my current asteroids
                    var isNewAsteroidInRangeOfCurrentAsteroids = true;
                    foreach (var queuedAsteroid in queuedAsteroids)
                    {
                        //if the distance between the two asteroids is too far, or the combined distance between both
                        // asteroids puts me out of range of either asteroid, don't approach.
                        var distanceBetweenQueuedAsteroidAndNewAsteroid = Distance(asteroidNotInRange.X, asteroidNotInRange.Y, asteroidNotInRange.Z,
                                                                                   queuedAsteroid.X, queuedAsteroid.Y, queuedAsteroid.Z);
                        var distanceBetweenMeAndQueuedAsteroid = Distance(_meCache.ToEntity.X, _meCache.ToEntity.Y, _meCache.ToEntity.Z,
                                                                          queuedAsteroid.X, queuedAsteroid.Y, queuedAsteroid.Z);
                        var distanceBetweenMeAndNewAsteroid = Distance(_meCache.ToEntity.X, _meCache.ToEntity.Y, _meCache.ToEntity.Z,
                                                                       asteroidNotInRange.X, asteroidNotInRange.Y, asteroidNotInRange.Z);

                        if (distanceBetweenQueuedAsteroidAndNewAsteroid > maxDistanceBetweenAsteroids ||
                            distanceBetweenMeAndNewAsteroid + distanceBetweenMeAndQueuedAsteroid > maxDistanceBetweenAsteroids)
                        {
                            isNewAsteroidInRangeOfCurrentAsteroids = false;
                            break;
                        }
                    }

                    if (isNewAsteroidInRangeOfCurrentAsteroids && asteroidNotInRange.Distance > _ship.MaximumMiningRange)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Approaching asteroid {0}.", asteroidNotInRange.ID);
                        ApproachAsteroid(asteroidNotInRange);
                        break;
                    }
                }
            }
        }

        private void MoveToAsteroidsForHighestPriorityMining(bool isAnotherAsteroidClose)
        {
            var methodName = "MoveToAsteroidsForHighestPriorityMining";
            LogTrace(methodName, "IsAnotherAsteroidClose: {0}", isAnotherAsteroidClose);

            var priorityIndex = GetAsteroidPriorityIndex();
            var asteroids = _asteroidBelts.AsteroidsInRange
                .Concat(_asteroidBelts.AsteroidsOutOfRange)
                .OrderBy(entity => priorityIndex.IndexOf(_asteroidBelts.AsteroidTypesByTypeId[entity.TypeID]))
                .ToList();

            var asteroid = asteroids.First();
            var highestPriorityTypeId = asteroid.TypeID;

            //asteroids actually in range and out of range, sorted by the previous list
            var asteroidsMatchingTypeId = asteroids.Where(entity => entity.TypeID == highestPriorityTypeId);
            var matchingAsteroidsInRange = new List<IEntityWrapper>();
            var matchingAsteroidsNotInRange = new List<IEntityWrapper>();

            foreach (var entity in asteroidsMatchingTypeId)
            {
                if (entity.Distance <= _ship.MaximumMiningRange && entity.Distance <= _ship.MaxTargetRange)
                    matchingAsteroidsInRange.Add(entity);
                else
                    matchingAsteroidsNotInRange.Add(entity);
            }

            var type = _asteroidBelts.AsteroidTypesByTypeId[highestPriorityTypeId];
            LogMessage(methodName, LogSeverityTypes.Debug, "Highest priority ore type: {0}, matching asteroids in range: {1}, matching asteroids not in range: {2}",
                       type, matchingAsteroidsInRange.Count, matchingAsteroidsNotInRange.Count);

            if (matchingAsteroidsInRange.Count != 0 || matchingAsteroidsNotInRange.Count <= 0) return;

            var asteroidNotInRange = matchingAsteroidsNotInRange.First();

            //If it's within "approach" range and there's nothing in my way, try to approac.
            //Otherwise, if I can bookmark/bounce warp, do so.

            if (!IsAsteroidTooFar(asteroidNotInRange) && !isAnotherAsteroidClose)
            {
                ApproachAsteroid(asteroidNotInRange);
            }
            else
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Moving to distant out-of-range asteroid {0} ({1}).", asteroidNotInRange.Name, asteroidNotInRange.ID);

                //If the asteroid is past warp range, just warp to it.
                if (asteroidNotInRange.Distance > (int)Ranges.Warp)
                {
                    var asteroidWarpDestination = new Destination(DestinationTypes.Entity, asteroidNotInRange.ID)
                    {
                        WarpToDistance = CalculateAsteroidWarpInDistance(asteroidNotInRange)
                    };
                    _movement.QueueDestination(asteroidWarpDestination);
                    return;
                }

                if (_movementConfiguration.UseTempBeltBookmarks && _movementConfiguration.UseBounceWarp)
                {
                    BounceWarp(asteroidNotInRange);
                }
                else
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Approaching a distant asteroid. We can get stuck doing this. You should enable Use Temp Belt Bookmarks and Bounce Warp.");

                    //Approach it. Yes, this is a bad idea. 
                    ApproachAsteroid(asteroidNotInRange);
                }
            }
        }

        private List<string> GetAsteroidPriorityIndex()
        {
            var priorityIndex = _miningConfiguration.IsIceMining
                                    ? _miningConfiguration.PriorityByIceType
                                    : _miningConfiguration.PriorityByOreType;
            return priorityIndex;
        }

        private bool MoveToDropoff()
        {
            var methodName = "MoveToDropoff";
            LogTrace(methodName);

            switch (_moveToDropoffState)
            {
                case MoveToDropoffStates.Idle:
                    {
                        //If I'm not doing a random wait, continue.
                        if (_goToDropoffRandomWait.ShouldWait())
                            return false;

                        //Deactivate any active mining lasers
                        _ship.DeactivateModuleList(_ship.MiningLaserModules, true);

                        if (_movementConfiguration.UseTempBeltBookmarks)
                        {
                            if (_temporaryMiningBookmarkLabel != null)
                            {
                                _moveToDropoffState = MoveToDropoffStates.RemoveTemporaryMiningBookmarks;
                                goto case MoveToDropoffStates.RemoveTemporaryMiningBookmarks;
                            }

                            _moveToDropoffState = MoveToDropoffStates.CreateTemporaryMiningBookmark;
                            goto case MoveToDropoffStates.CreateTemporaryMiningBookmark;
                        }

                        _moveToDropoffState = MoveToDropoffStates.GoToDropoff;
                        goto case MoveToDropoffStates.GoToDropoff;
                    }
                case MoveToDropoffStates.RemoveTemporaryMiningBookmarks:
                    {
                        var temporaryMiningBookMarks = GetTemporaryMiningBookMarks();
                        var temporaryMiningBookmark = temporaryMiningBookMarks.FirstOrDefault();

                        //If I have a null bookmark or the last bookmark is >= 5km away, or the closest asteroid is FUCKING FAR, kill the existing bookmark.
                        if (temporaryMiningBookmark != null &&
                            (DistanceTo(temporaryMiningBookmark.X, temporaryMiningBookmark.Y, temporaryMiningBookmark.Z) >= 5000 || _asteroidBelts.Asteroids.First().Distance > _ship.MaximumMiningRange))
                        {
                            LogMessage(methodName, LogSeverityTypes.Standard, "Removing our temporary mining bookmark because it is too far or there are no asteroids near it.");
                            RemoveTemporaryMiningBookmarks();
                        }

                        //If there are any asteroids, create a temp. mining bookmark. Otherwise, just go straight to dropoff.
                        _moveToDropoffState = _asteroidBelts.Asteroids.Count > 0 ? MoveToDropoffStates.CreateTemporaryMiningBookmark : MoveToDropoffStates.GoToDropoff;
                        break;
                    }
                case MoveToDropoffStates.CreateTemporaryMiningBookmark:
                    //TODO: If an asteroid is in range, bookmark my location. Otherwise, bookmark an asteroid out of range.
                    // The plot thickens - if we're strip mining, we are ok staying where we currently are as long as there's something in range.
                    // If we're not strip mining, we need to move to the closest of whatever available asteroid is highest priority.

                    if (_miningConfiguration.StripMine)
                    {
                        CreateTemporaryMiningBookMarkForStripMining();
                    }
                    else
                    {
                        CreateTemporaryMiningBookMarkForCherryPicking();
                    }
                    //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                    //	"ProcessPulseState", "No asteroids to temp bookmark."));
                    _moveToDropoffState = MoveToDropoffStates.GoToDropoff;
                    goto case MoveToDropoffStates.GoToDropoff;
                case MoveToDropoffStates.GoToDropoff:
                    var result = _moveToDropOffLocationPartialBehavior.Execute();

                    if (result == BehaviorExecutionResults.Incomplete)
                        return false;

                    if (result == BehaviorExecutionResults.Error)
                        _miningState = MiningStates.Error;

                    _miningState = MiningStates.DoDropoff;
                    _moveToDropoffState = MoveToDropoffStates.Idle;
                    return true;
            }
            return false;
        }

        private void CreateTemporaryMiningBookMarkForCherryPicking()
        {
            var methodName = "CreateTemporaryMiningBookMarkForCherryPicking";
            LogTrace(methodName);

            var priorityIndex = GetAsteroidPriorityIndex();
            var highestPriorityAsteroid = _asteroidBelts.AsteroidsInRange.Concat(_asteroidBelts.AsteroidsOutOfRange)
                .OrderBy(entity => priorityIndex.IndexOf(_asteroidBelts.AsteroidTypesByTypeId[entity.TypeID]))
                .First();

            //If the closest asteroid of the highest priority type is in range, use the current location
            if (_asteroidBelts.AsteroidsInRange.Any(e => e.ID == highestPriorityAsteroid.ID))
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "We still have an asteroid of the highest priority type in range - creating a temporary mining bookmark at our current location.");
                CreateTemporaryMiningBookmark();
            }
            else
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Creating a temporary mining bookmark for asteroid \"{0}\" ({1}).", highestPriorityAsteroid.Name, highestPriorityAsteroid.ID);
                CreateTemporaryMiningBookmark(highestPriorityAsteroid);
            }
        }

        private void CreateTemporaryMiningBookMarkForStripMining()
        {
            var methodName = "CreateTemporaryMiningBookMarkForStripMining";
            LogTrace(methodName);

            if (_asteroidBelts.AsteroidsInRange.Any())
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "We still have asteroids in range - creating a temporary mining bookmark at our current location.");
                CreateTemporaryMiningBookmark();
            }
            else
            {
                //Are these sorted by priority or distance? Distance. Get the highest priority asteroid.
                var priorityIndex = GetAsteroidPriorityIndex();
                var closestAsteroid = _asteroidBelts.AsteroidsOutOfRange
                    .OrderBy(entity => priorityIndex.IndexOf(_asteroidBelts.AsteroidTypesByTypeId[entity.TypeID]))
                    .First();

                LogMessage(methodName, LogSeverityTypes.Standard, "Creating a temporary mining bookmark for asteorid \"{0}\" ({1}).", closestAsteroid.Name, closestAsteroid.ID);
                CreateTemporaryMiningBookmark(closestAsteroid);
            }
        }

        private void Dropoff()
        {
            var methodName = "TrackDropoff";
            LogTrace(methodName);

            var result = _dropOffCargoPartialBehavior.Execute();

            if (result == BehaviorExecutionResults.Incomplete)
                return;

            if (result == BehaviorExecutionResults.Error)
                _miningState = MiningStates.Error;

            RecordDropoff();

            _miningState = MiningStates.Idle;
        }

        private void RecordDropoff()
        {
            _timeForTrip.Stop();
            Core.Metatron.Statistics.TrackDropoff(_timeForTrip.Elapsed);
            _timeForTrip.Reset();
            _timeForTrip.Start();
        }

        private void MarkJetcanFull()
        {
            var methodName = "MarkJetcanFull";
            LogTrace(methodName);

            //Close the can.
            //Core.Metatron.JetCan.CargoWindow.Close();

            //Mark the time taken for statistics and reset the timer
            _timeForTrip.Stop();
            Core.Metatron.Statistics.TrackDropoff(_timeForTrip.Elapsed);
            _timeForTrip.Reset();
            _timeForTrip.Start();

            //Send a request for pickup
            Core.Metatron.EventCommunications.FleetNeedPickupEvent.SendEventFromArgs(
                _meCache.CharId, _meCache.SolarSystemId,
                Core.Metatron.JetCan.CurrentContainerId,
                _meCache.ToEntity.Id,
                _meCache.Name);

            Core.Metatron.JetCan.MarkActiveCanFull();
        }

        private void ApproachAsteroid(IEntityWrapper asteroid)
        {
            var methodName = "ApproachAsteroid";
            LogTrace(methodName, "asteroid: {0} ({1})", asteroid.Name, asteroid.ID);

            if (_movement.IsMoving && _movement.MovementType == MovementTypes.Approach) return;

            var distance = _ship.MaximumMiningRange <= _ship.MaxTargetRange ?
                _ship.MaximumMiningRange : _ship.MaxTargetRange;

            LogMessage(methodName, LogSeverityTypes.Standard, "Moving to within {0}m of asteroid \"{1}\" ({2}).", distance, asteroid.Name, asteroid.ID);

            _movement.QueueDestination(
                new Destination(DestinationTypes.Entity, asteroid.ID) { Distance = distance });
        }

        private void BounceWarp(IEntityWrapper asteroidToBookmark)
        {
            var methodName = "BounceWarp";
            LogTrace(methodName, "asteroid: {0} ({1})", asteroidToBookmark.Name, asteroidToBookmark.ID);

            switch (_bounceWarpState)
            {
                case BounceWarpStates.Idle:
                    _bounceWarpState = BounceWarpStates.RemoveTemporaryBookmarks;
                    goto case BounceWarpStates.RemoveTemporaryBookmarks;
                case BounceWarpStates.RemoveTemporaryBookmarks:
                    //RemoveBookmarkAndCacheEntry any old bookmarks
                    RemoveTemporaryMiningBookmarks();
                    _bounceWarpState = BounceWarpStates.CreateTemporaryBookmark;
                    break;
                case BounceWarpStates.CreateTemporaryBookmark:
                    CreateTemporaryMiningBookmark(asteroidToBookmark);
                    _bounceWarpState = BounceWarpStates.QueueDestinations;
                    break;
                case BounceWarpStates.QueueDestinations:
                    //Account for asteroids with a radius above 10 or even 20k.
                    _warpInDistance = CalculateAsteroidWarpInDistance(asteroidToBookmark);

                    //Well, couldn't find a safe spot; try a planet.
                    var planetEntity = _entityProvider.EntityWrappers.FirstOrDefault(ce => ce.GroupID == (int)GroupIDs.Planet && ce.Distance > (Int64)Ranges.PlanetWarpIn);

                    //If we got a planet, do something
                    if (planetEntity != null)
                    {
                        //If we're within 15,000,000 m of the planet, we're fucking close enough
                        _movement.QueueDestination(
                            new Destination(DestinationTypes.Entity, planetEntity.ID)
                            {
                                Distance = (Int64)Ranges.PlanetWarpIn,
                                WarpToDistance = PlanetWarpInDistance
                            });

                        //Warp to within our warpin distance
                        var temporaryMiningBookmarks = GetTemporaryMiningBookMarks();
                        var temporaryMiningBookmark = temporaryMiningBookmarks.First();
                        _movement.QueueDestination(
                            new Destination(DestinationTypes.BookMark, temporaryMiningBookmark.Id, _warpInDistance));
                    }
                    else
                    {
                        //Couldn't find -anything- to bounce warp to; turn off bounce warp
                        LogMessage(methodName, LogSeverityTypes.Standard, "No planets were available for bounce-warping. Disabling bounce warp.");
                        _movementConfiguration.UseBounceWarp = false;
                    }
                    //}
                    _bounceWarpState = BounceWarpStates.Idle;
                    break;
            }
        }

        private int CalculateAsteroidWarpInDistance(IEntityWrapper asteroid)
        {
            var methodName = "_calcAsteroidWarpInDist";
            LogTrace(methodName);

            var warpInDistance = (int)Math.Ceiling(asteroid.Radius / MinimumWarpinDistance) * MinimumWarpinDistance;

            if (warpInDistance < MinimumWarpinDistance)
            {
                warpInDistance = 10000;
            }

            //Match EVE warp-in distances
            if (warpInDistance == 40000)
            {
                warpInDistance = 50000;
            }
            else if (warpInDistance == 60000)
            {
                warpInDistance = 70000;
            }
            else if (warpInDistance == 80000 || warpInDistance == 90000 || warpInDistance > 100000)
            {
                warpInDistance = 100000;
            }

            return warpInDistance;
        }

        private IEntityWrapper GetNearestAsteroid()
        {
            var methodName = "GetNearestAsteroid";
            LogTrace(methodName);

            IEntityWrapper asteroid = null;
            //Get the best roid
            if (_asteroidBelts.AsteroidsInRange.Count > 0)
            {
                asteroid = _asteroidBelts.AsteroidsInRange.First();
            }
            else if (_asteroidBelts.AsteroidsOutOfRange.Count > 0)
            {
                asteroid = _asteroidBelts.AsteroidsOutOfRange.First();
            }

            return asteroid;
        }

        private bool IsAsteroidTooFar(IEntityWrapper asteroid)
        {
            var methodName = "IsAsteroidTooFar";
            LogTrace(methodName);

            return asteroid.Distance > _ship.MaxSlowboatDistance;
        }

        private void MoveToAsteroidBelt(bool playersClose)
        {
            var methodName = "MoveToAsteroidBelt";
            LogTrace(methodName);

            //If I have a temp mining bookmark, use it
            if (_movementConfiguration.UseTempBeltBookmarks && _temporaryMiningBookmarkLabel != null)
            {
                //var temporaryMiningBookMarks = GetTemporaryMiningBookMarks();
                var temporaryMiningBookMark = _bookMarkCache.FirstBookMarkStartingWith(Core.Metatron.Config.MovementConfig.TemporaryBeltBookMarkPrefix, true);

                //If players are close, or we're already at the belt and there are no asteroids, remove the temp bookmark
                //TODO: Cleanup Asteroid lists - _asteroidBelts.Asteroids is -all- asteroid entities, where AsteroidsInRange and ASteroidsOutOfRange are -selected- asteroids.
                // This is horribly unintuitive.
                if (playersClose || (_asteroidBelts.IsAtAsteroidBelt() && !_asteroidBelts.AsteroidsInRange.Any() && !_asteroidBelts.AsteroidsOutOfRange.Any()))
                {
                    RemoveTemporaryMiningBookmarks();
                }
                else
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Moving to temporary mining bookmark with label \"{0}\".",
                        temporaryMiningBookMark.Label);
                    _movement.QueueDestination(
                        new Destination(DestinationTypes.BookMark, temporaryMiningBookMark.Id, _warpInDistance));
                    return;
                }
            }

            _asteroidBelts.ChangeBelts(false, false);
            //Invalidate any existing temp mining bookmark since we changed belt
            //Core.Metatron.Bookmarks.TempMiningBookmark = null;
            //LogMessage(methodName, LogSeverityTypes.Debug, "Setting temp. mining bookmark null after changing belt.");

            if (_movementConfiguration.OnlyUseBeltBookmarks)
            {
                if (_asteroidBelts.CurrentBookMarkedBelt != null)
                {
                    _movement.QueueDestination(
                        new Destination(DestinationTypes.BookMark, _asteroidBelts.CurrentBookMarkedBelt.Id) { Distance = 15000 });
                }
                else
                {
                    LogMessage(methodName, LogSeverityTypes.Critical, "Error: Could not find an active bookmarked belt.");
                    _miningState = MiningStates.Error;
                }
            }
            else
            {
                if (_asteroidBelts.CurrentBelt != null)
                {
                    _movement.QueueDestination(
                        new Destination(DestinationTypes.Entity, _asteroidBelts.CurrentBelt.Id) { Distance = 15000 });
                }
                else
                {
                    LogMessage(methodName, LogSeverityTypes.Critical, "Error: Could not find an active belt.");
                    _miningState = MiningStates.Error;
                }
            }
        }

        private void OnFleetNeedPickupConfirmed(object sender, FleetNeedPickupEventArgs e)
        {
            var methodName = "OnFleetNeedPickupConfirmed";
            LogTrace(methodName);

            lock (this)
            {
                if (e.TargetCanEntityId != _idOfCanRequested) return;

                _gotAcknowledgement = true;
                _queuedPickupAcknowledgements.Add(e);
            }
        }

        private void CreateTemporaryMiningBookmark()
        {
            var methodName = "CreateTemporaryMiningBookmark";
            LogTrace(methodName);

            var bookmarkLabel = String.Format("{0}{1}", _movementConfiguration.TemporaryBeltBookMarkPrefix, string.Format("{0:00}:{1:00}", _meCache.GameHour, _meCache.GameMinute));

            LogMessage(methodName, LogSeverityTypes.Debug, "Creating temporary mining bookmark at current location with label \"{0}\".", bookmarkLabel);

            _isxeveProvider.Eve.CreateBookmark(bookmarkLabel);

            //Set the temporary mining bookmark label and warp-in distance
            _temporaryMiningBookmarkLabel = bookmarkLabel;
            _warpInDistance = 0;
        }

        private void RemoveTemporaryMiningBookmarks()
        {
            var methodName = "RemoveTemporaryMiningBookmarks";
            LogTrace(methodName);

            var bookMarks = GetTemporaryMiningBookMarks();

            foreach (var bookMark in bookMarks)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Removing temporary mining bookmark with label \"{0}\".", bookMark.Label);
                _bookMarkCache.RemoveCachedBookMark(bookMark);
            }

            _temporaryMiningBookmarkLabel = null;
        }

        private IEnumerable<CachedBookMark> GetTemporaryMiningBookMarks()
        {
            var methodName = "GetTemporaryMiningBookMarks";
            LogTrace(methodName);

            //var bookMarks = _bookMarkCache.CachedBookMarks.Where(cbm => cbm.SolarSystemId == _meCache.SolarSystemId && cbm.OwnerId == _meCache.CharId && cbm.CreatorId == _meCache.CharId &&
            //    cbm.Label.StartsWith(_movementConfiguration.TemporaryBeltBookMarkPrefix, StringComparison.InvariantCultureIgnoreCase))
            //    .ToList();

            var bookMarks = _bookMarkCache.GetBookMarksStartingWith(Core.Metatron.Config.MovementConfig.TemporaryBeltBookMarkPrefix, true).Where(b => b.OwnerId == _meCache.CharId && b.CreatorId == _meCache.CharId).ToList();

            return bookMarks;
        }

        /// <summary>
        /// Create a temporary mining bookmark for the given entity.
        /// </summary>
        /// <param name="entity"></param>
        private void CreateTemporaryMiningBookmark(IEntityWrapper entity)
        {
            var methodName = "CreateTemporaryMiningBookmark";
            LogTrace(methodName, "Entity: {0} ({1})", entity.Name, entity.ID);

            //Format: "{prefix} {asteroid name.tolower}" i.e. "temp belt - scordite"

            var bookmarkLabel = String.Format("{0}{1}", _movementConfiguration.TemporaryBeltBookMarkPrefix, _asteroidBelts.AsteroidTypesByTypeId[entity.TypeID].ToLower());
            LogMessage(methodName, LogSeverityTypes.Standard, "Creating temporary mining bookmark with label \"{0}\" for entity \"{1}\" ({2}).",
                bookmarkLabel, entity.Name, entity.ID);

            entity.CreateBookmark(bookmarkLabel);

            //Set the temporary mining bookmark label and warp-in distance
            _temporaryMiningBookmarkLabel = bookmarkLabel;
            _warpInDistance = CalculateAsteroidWarpInDistance(entity);
        }
    }

    public enum MiningStates
    {
        Error,              //Something broke or we're out of shit to mine
        Idle,               //Do(ing) nada.
        GoToAsteroids,      //Travel to an asteorid belt or a belt bookmark
        DoMining,           //Approach roids, Queue 'em up, repeat 'til cargo full
        MoveToDropoff,        //Cargo is full, travel to dropoff location
        DoDropoff           //Do the actual dropoff
    }
}
