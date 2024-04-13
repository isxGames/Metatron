using System;
using System.Collections.Generic;
using Metatron.Core;
using EVE.ISXEVE;
using System.Linq;
using Metatron.ActionModules;
using Metatron.Core.Config;
using Metatron.Core.Interfaces;
using IShip = Metatron.Core.IShip;
using Metatron.BehaviorModules.PartialBehaviors;
using Metatron.Core.EventCommunication;
using System.Reflection;

namespace Metatron.BehaviorModules
{
    internal class Salvaging : BehaviorBase
    {
        private SalvagingStates _salvageState = SalvagingStates.Idle;
        private bool _wreckCheck, _isWipingThisSpawn, _doWarpInCheck = true;

        private int _warpInCheckCounter = 5;
        private bool _dropCargo = false;

        private long? _selectedBookmarkId;

        private readonly List<IEntityWrapper> _wreckNpcEntities = new List<IEntityWrapper>();

        private readonly RandomWaitObject _randomWaitObject;

        private readonly ISocial _social;
        private readonly IMeCache _meCache;
        private readonly IBookmarks _bookmarks;
        private readonly ISalvageConfiguration _salvageConfiguration;
        private readonly IEntityProvider _entityProvider;
        private readonly ISafespots _safespots;
        private readonly IShip _ship;
        private readonly IMovementConfiguration _movementConfiguration;
        private readonly IAlerts _alerts;
        private readonly ITargetQueue _targetQueue;
        private readonly IAttackers _attackers;
        private readonly IInventoryProvider _inventoryProvider;
        private readonly IIsxeveProvider _isxEveProvider;
        private readonly MoveToDropOffLocationPartialBehavior _moveToDropOffLocationPartialBehavior;
        private readonly DropOffCargoPartialBehavior _dropOffCargoPartialBehavior;
        private volatile List<SalvageBookmarkReadyEventArgs> _eventsToValidate = new List<SalvageBookmarkReadyEventArgs>();
        private readonly List<CachedBookMark> _queuedSalvageSites = new List<CachedBookMark>();
        private CachedBookMark _currentSalvageSite;
        private bool _arrivedAtSite;

        private readonly IMovement _movement;

        /// <summary>
        /// Npcs with these tags in their name are known warp disrupt / ewar npcs.
        /// </summary>

        public Salvaging(ISocial social, IMeCache meCache, IBookmarks bookmarks, ISalvageConfiguration salvageConfiguration,
            IEntityProvider entityProvider, ISafespots safespots, IMovement movement, IShip ship, IMovementConfiguration movementConfiguration,
            IAlerts alerts, ITargetQueue targetQueue, IAttackers attackers, IInventoryProvider inventoryProvider, IIsxeveProvider isxeveProvider)
        {
            _social = social;
            _arrivedAtSite = false;
            _meCache = meCache;
            _bookmarks = bookmarks;
            _salvageConfiguration = salvageConfiguration;
            _entityProvider = entityProvider;
            _safespots = safespots;
            _movement = movement;
            _ship = ship;
            _movementConfiguration = movementConfiguration;
            _alerts = alerts;
            _targetQueue = targetQueue;
            _attackers = attackers;
            _inventoryProvider = inventoryProvider;
            _isxEveProvider = isxeveProvider;
            _moveToDropOffLocationPartialBehavior = new MoveToDropOffLocationPartialBehavior(_movement, _entityProvider, Core.Metatron.Config.CargoConfig, Core.Metatron.BookMarkCache, _bookmarks, _movementConfiguration, _meCache, new MathUtility(), _isxEveProvider);
            _dropOffCargoPartialBehavior = new DropOffCargoPartialBehavior(Core.Metatron.EveWindowProvider, Core.Metatron.Config.CargoConfig, Core.Metatron.Config.MainConfig, Core.Metatron.Config.MiningConfig, _meCache, _ship, Core.Metatron.Station, Core.Metatron.JetCan, _entityProvider, Core.Metatron.EventCommunications);
            Core.Metatron.EventCommunications.SalvageBookmarkReadyEvent.EventRaised += QueueSalvageBookmark;

            BehaviorManager.BehaviorsToPulse.Add(BotModes.Salvaging, this);
            ModuleName = "Salvaging";
            IsEnabled = true;
            PulseFrequency = 2;

            _randomWaitObject = new RandomWaitObject(ModuleName);
            _randomWaitObject.AddWait(new KeyValuePair<int, int>(30, 70), 5);
            _randomWaitObject.AddWait(new KeyValuePair<int, int>(5, 15), 10);
        }

        private void QueueSalvageBookmark(object sender, SalvageBookmarkReadyEventArgs e)
        {
            var methodName = "QueueSalvageBookmark";
            LogTrace(methodName);

            if (Core.Metatron.Config.MainConfig.ActiveBehavior != BotModes.Salvaging)
            {
                return;
            }

            lock (this)
            {
                _eventsToValidate.Add(e);
            }

            LogMessage(methodName, LogSeverityTypes.Debug, $"Received salvaging request from {e.SendingFleetMemberCharId}: {e.BookmarkId}");
        }

        public override void Pulse()
        {
            if (!ShouldPulse())
                return;

            //if I'm moving in any way other than approaching something, don't do any other logic.
            if (_movement.IsMoving && (_movement.MovementType != MovementTypes.Approach))
            {
                //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                //methodName, "Not pulsing due to moving in some way other than approach."));
                return;
            }

            if (!_ship.IsInventoryReady) return;

            StartPulseProfiling();

            //if in space, repopulate wrecks
            if (_meCache.InSpace)
            {
                GetEntities();

                _wreckCheck = CheckWrecks();
            }

            if (_salvageConfiguration.WaitForSafetyConfirmation)
            {
                ValidateSalvageRequests();
            }
            else
            {
                BuildSalvageList();
            }
            //CleanupOldSites();

            SetPulseState();

            ProcessPulseState();

            EndPulseProfiling();
        }

        private void BuildSalvageList()
        {
            var methodName = "BuildSalvageList";
            LogTrace(methodName);

            var salvageBookmarks = Core.Metatron.BookMarkCache.GetBookMarksStartingWith(Core.Metatron.Config.MovementConfig.SalvagingPrefix, false);
            foreach (var bookmark in salvageBookmarks)
            {
                if (_queuedSalvageSites.Contains(bookmark)) continue;
                _queuedSalvageSites.Add(bookmark);
                LogMessage(methodName, LogSeverityTypes.Standard, $"Queued up found bookmark {bookmark.Label}");
            }
        }
        private void CleanupOldSites()
        {
            var methodName = "CleanupOldSites";
            LogTrace(methodName);
            
            foreach(var bookmark in _queuedSalvageSites.ToList())
            {
                if (Core.Metatron.BookMarkCache.GetBookMarksStartingWith(Core.Metatron.Config.MovementConfig.SalvagingPrefix, false).Any(b => b.Id == bookmark.Id)) continue;
                _queuedSalvageSites.RemoveAll(b => b.Id == bookmark.Id);
            }
        }
        private void ValidateSalvageRequests()
        {
            var methodName = "ValidateSalvageRequests";
            LogTrace(methodName);

            var salvageBookmarks = Core.Metatron.BookMarkCache.GetBookMarksStartingWith(Core.Metatron.Config.MovementConfig.SalvagingPrefix, false);

            lock (this)
            {
                //Iterate all pickup requests and validate them if possible
                foreach (var e in _eventsToValidate)
                {
                    foreach (var bookMark in salvageBookmarks)
                    {
                        if (_queuedSalvageSites.Contains(bookMark)) continue;
                        if (bookMark != null && bookMark.Id == e.BookmarkId)
                        {
                            _queuedSalvageSites.Add(bookMark);
                            LogMessage(methodName, LogSeverityTypes.Standard, $"Queued up reported safe bookmark: {bookMark.Label}");
                        }
                        else
                        {
                            LogMessage(methodName, LogSeverityTypes.Debug, $"Could not find reported bookmark with id {bookMark.Id}. This is usually because the bot doesn't have access to the same folder as the reporter.");
                        }
                    }
                }
                _eventsToValidate.Clear();
            }
        }

        private bool CheckCargo()
        {
            return _meCache.Ship.UsedCargoCapacity > Core.Metatron.Config.CargoConfig.CargoFullThreshold;
        }

        private void GetEntities()
        {
            var methodName = "GetEntities";
            LogTrace(methodName);

            ClearEntities();

            _wreckNpcEntities.AddRange(
                Core.Metatron.EntityProvider.EntityWrappers.Where(
                    entity => (entity.GroupID == (int)GroupIDs.Wreck || entity.GroupID == (int)GroupIDs.CargoContainer) && entity.Distance < (int)Ranges.Warp && entity.HaveLootRights));
        }

        private void ClearEntities()
        {
            _wreckNpcEntities.Clear();
        }

        protected override void SetPulseState()
        {
            var methodName = "SetPulseState";
            LogTrace(methodName);

            //If defense has us fleeing we probably shouldn't be doing anything.);
            if (Core.Metatron.Defense.IsFleeing)
            {
                _salvageState = SalvagingStates.Defend;
                return;
            }
            LogTrace(methodName, "ArrivedAtSite:", _arrivedAtSite);

            //If it's an error state, return.
            if (_salvageState == SalvagingStates.Error)
            {
                _moveToDropOff = true;

                if (HasMaxRuntimeExpired()) return;
            }
            if (CheckCargo())
            {
                _moveToDropOff = true;
            }

            if (_moveToDropOff)
            {
                _salvageState = SalvagingStates.MoveToDropoff;
                return;
            }

            if (_dropCargo)
            {
                _salvageState = SalvagingStates.Dropoff;

                return;
            }

            //if I'm in station, I should probably focus on getting to a belt.
            if (_meCache.InStation)
            {
                //var bookmarks = Core.Metatron.BookMarkCache.GetBookMarksStartingWith(Core.Metatron.Config.MovementConfig.SalvagingPrefix, false);

                if (_queuedSalvageSites.Count > 0)
                {
                    _salvageState = SalvagingStates.ChangeSpots;
                    return;
                }
                else
                {
                    _salvageState = SalvagingStates.Idle;
                    return;
                }
            }
            else
            {
                if (_currentSalvageSite != null && _bookmarks.IsAtBookmark(_currentSalvageSite))
                {
                    _arrivedAtSite = true;
                }
                //If the NPC check and player check passed, kill shit
                if (_wreckCheck)
                {
                    _salvageState = SalvagingStates.Salvage;
                    return;
                }
                else
                {
                    //Either no rats or a player here, change belts
                    _salvageState = SalvagingStates.ChangeSpots;
                    return;
                }
            }
        }

        protected override void ProcessPulseState()
        {
            var methodName = "ProcessPulseState";
            LogTrace(methodName);

            //Switch the state and handle it
            switch (_salvageState)
            {
                case SalvagingStates.Idle:
                    //Do nothing during idle.
                    break;
                case SalvagingStates.Defend:
                    //Again do nothing; we're fleeing or hiding.
                    ClearEntities();
                    break;
                case SalvagingStates.Dropoff:
                    DropOffCargo();
                    break;
                case SalvagingStates.MoveToDropoff:
                    MoveToDropOff();
                    break;
                case SalvagingStates.Salvage:
                    SalvageWrecks();
                    break;
                case SalvagingStates.ChangeSpots:
                    ChangeBookmark();
                    break;
                case SalvagingStates.Error:
                    if (_safespots.IsSafe()) return;

                    var safespot = _safespots.GetSafeSpot();
                    _movement.QueueDestination(safespot);
                    break;
            }
        }

        private bool _moveToDropOff = false;
        private void MoveToDropOff()
        {
            var methodName = "MoveToDropOff";
            LogTrace(methodName);

            LogMessage(methodName, LogSeverityTypes.Standard, "Moving to dropoff location");
            var result = _moveToDropOffLocationPartialBehavior.Execute();

            if (result == BehaviorExecutionResults.Complete) { _moveToDropOff = false; _dropCargo = true; }
        }

        private void DropOffCargo()
        {
            var methodName = "DropOffCargo";
            LogTrace(methodName);

            //LogMessage(methodName, LogSeverityTypes.Standard, "Dropping cargo at station");
            var result = _dropOffCargoPartialBehavior.Execute();

            if (result == BehaviorExecutionResults.Complete) { _dropCargo = false; }
        }

        protected override void _processCleanupState()
        {

        }

        protected override void _setCleanupState()
        {

        }

        private void SalvageWrecks()
        {
            var methodName = "SalvageWrecks";
            LogTrace(methodName);

            //If no rats are present, just return early.
            if (_wreckNpcEntities.Count == 0)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "No wrecks detected.");
                return;
            }

            var closestEntity = _wreckNpcEntities.OrderBy(e => e.Distance).First();

            if (closestEntity.Distance > _ship.MaxTargetRange)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "All wrecks are out of range, moving closer...");
                Destination destination = new Destination(DestinationTypes.Entity, closestEntity.ID, 500.0);
                _movement.QueueDestination(destination);
            }
            else
            {
                //Queue the entities
                if (_wreckNpcEntities.Count > 0)
                {
                    QueueEntities(_wreckNpcEntities);
                }
            }
        }

        private void ChangeBookmark()
        {
            var methodName = "ChangeBookmark";
            LogTrace(methodName);

            if (_randomWaitObject.ShouldWait())
                return;

            //if I'm in station, I need to undock.
            if (_meCache.InStation)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Undocking to move to a bookmark.");
                _movement.QueueDestination(new Destination(DestinationTypes.Undock, string.Empty));
                return;
            }

            //reset the wipe stuff
            _isWipingThisSpawn = false;
            //Also reset the warp-in check
            _doWarpInCheck = true;

            //gotta work differently depending on type of belts I'm using.
            //Only allow reseting of IsBeltEmpty if not in anomaly mode

            MoveToNextBookmark();

        }

        private void MoveToNextBookmark()
        {
            var methodName = "MoveToNextBookmark";
            LogTrace(methodName);

            //_dropCargo = true;

            if (_currentSalvageSite != null && _arrivedAtSite)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Site is clear");
                _arrivedAtSite = false;
                Core.Metatron.BookMarkCache.RemoveCachedBookMark(_currentSalvageSite);
                _queuedSalvageSites.RemoveAll(b => b.Id == _currentSalvageSite.Id);
            }
            if (Core.Metatron.EveWindowProvider.GetInventoryWindow().IsValid)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Stacking cargo");
                Core.Metatron.EveWindowProvider.GetInventoryWindow().StackAll();
            }

            if (_queuedSalvageSites == null || _queuedSalvageSites.Count == 0)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "No salvage bookmarks found. Dropping off cargo...");
                _moveToDropOff = true;
                return;
            }
            else
            {
                _currentSalvageSite = _queuedSalvageSites.OrderBy(b => GetJumpsToBookmark(b)).FirstOrDefault();

                var destination = new Destination(DestinationTypes.BookMark, _currentSalvageSite.Id);
                _movement.QueueDestination(destination);
            }
        }

        private int GetJumpsToBookmark(CachedBookMark b)
        {
            var methodName = "GetJumpsToBookmark";
            LogTrace(methodName);

            var jumpsToBookmark = _isxEveProvider.Eve.GetJumpsBetween(_meCache.SolarSystemId, b.SolarSystemId);
            LogTrace($"Current: {_meCache.SolarSystemId} Dest: {b.SolarSystemId} TotalJumps: {jumpsToBookmark}");
            return jumpsToBookmark;
        }

        private void QueueEntities(IEnumerable<IEntityWrapper> entitiesToQueue)
        {
            var methodName = "QueueEntities";
            LogTrace(methodName);

            //Iterate all entities
            foreach (var entity in entitiesToQueue.Where(entity => !_targetQueue.IsQueued(entity.ID)))
            {
                _targetQueue.EnqueueTarget(entity.ID, (int)TargetPriorities.Wreck_TractorSalvage, TargetTypes.LootSalvage);
            }
        }

        /// <summary>
        /// Determine if there are sufficient NPCs to stay and fight, meaning:
        /// 1) Rats are present,
        /// 2) Not chaning -or- (special/faction/ewar spawn or bad chain [meaning more than just one-ship-type and not on do-not-kill list])
        /// </summary>
        private bool CheckWrecks()
        {
            var methodName = "CheckWrecks";
            LogTrace(methodName);

            //Firstly determine if rats are present
            if (_wreckNpcEntities.Count > 0)
            {
                return true;
            }
            else { return false; }
        }
    }

    public enum SalvagingStates
    {
        Idle,
        Defend,
        MoveToDropoff,
        Dropoff,
        Salvage,
        ChangeSpots,
        Error
    }
}
