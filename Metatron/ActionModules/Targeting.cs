using System;
using System.Collections.Generic;
using System.Linq;
using EVE.ISXEVE;
using Metatron.Core;
using Metatron.Core.Config;
using Metatron.Core.Interfaces;

namespace Metatron.ActionModules
{
    public sealed class Targeting : ModuleBase, ITargeting
    {
        private bool _modulesBuilt;
        private readonly RandomWaitObject _randomWait;

		public Dictionary<Int64, int> ModulesOnEntity = new Dictionary<Int64, int>();

        private readonly Dictionary<Int64, QueueTarget> _queueTargetsByEntityId = new Dictionary<long, QueueTarget>();
        public Dictionary<Int64, QueueTarget> QueueTargetsByEntityId
        {
            get { return _queueTargetsByEntityId; }
        }

        //Have we changed active target this pulse?
		private uint _pulseDuringWhichBlockTargetChangeWasSet;

        public bool IsTargetChangeNextFrameBlocked { get; private set; }

        public void BlockTargetChangeNextFrame()
        {
            IsTargetChangeNextFrameBlocked = true;
            _pulseDuringWhichBlockTargetChangeWasSet = Core.Metatron.Pulses;
        }

    	public bool WasTargetChangedThisFrame { get; private set; }

        private readonly IMeCache _meCache;
        private readonly IShip _ship;
        private readonly IDrones _drones;
        private readonly IAlerts _alerts;
        private readonly IModuleManager _moduleManager;
        private readonly ITargetQueue _targetQueue;
        private readonly IEntityProvider _entityProvider;
        private readonly IMovement _movement;

        public Targeting(ILogging logging, IMaxRuntimeConfiguration maxRuntimeConfiguration, IMeCache meCache, IShip ship,
            IDrones drones, IAlerts alerts, IModuleManager moduleManager, ITargetQueue targetQueue, IEntityProvider entityProvider, IMovement movement)
            : base(logging)
        {
            _meCache = meCache;
            _ship = ship;
            _drones = drones;
            _alerts = alerts;
            _moduleManager = moduleManager;
            _targetQueue = targetQueue;
            _entityProvider = entityProvider;
            _movement = movement;

            ModuleManager.ModulesToPulse.Add(this);
			ModuleName = "Targeting";

            _randomWait = new RandomWaitObject("Targeting", logging, maxRuntimeConfiguration);
			_randomWait.AddWait(new KeyValuePair<int, int>(3, 5), 5);
			_randomWait.AddWait(new KeyValuePair<int, int>(1, 2), 15);
        }

        public override void Pulse()
        {
			var methodName = "Pulse";
			LogTrace(methodName);

        	if (!ShouldPulse())
				return;

        	if (!_meCache.InSpace || _meCache.InStation ||
        	    (_movement.IsMoving && _movement.MovementType != MovementTypes.Approach))
        	{
        		return;
        	}

        	StartPulseProfiling();

            var wasTargetUnlocked = UnlockUnqueuedTargets();
            if (wasTargetUnlocked)
            {
                EndPulseProfiling();
                return;
            }

        	WasTargetChangedThisFrame = false;

        	//reset the modules dictionary
        	_modulesBuilt = false;
        	ModulesOnEntity.Clear();

            _queueTargetsByEntityId.Clear();

            if (_targetQueue.Targets.Count == 0)
            {
                _ship.DeactivateModuleList(_ship.SensorBoosterModules, true);
                EndPulseProfiling();
                return;
            }

            //Force active target if we don't have one
            if (_meCache.Targets.Count > 0 && _meCache.ActiveTarget == null)
            {
                _meCache.Targets.FirstOrDefault().MakeActiveTarget();
            }

            //Activate sensor boosters
            _ship.ActivateModuleList(_ship.SensorBoosterModules, true);

            //TargetQueue.SortQueue();
            //StartMethodProfiling("BuildE_QT_Dictionary");
            foreach (QueueTarget queueTarget in _targetQueue.Targets)
            {
                _queueTargetsByEntityId.Add(queueTarget.Id, queueTarget);
            }

            //EndMethodProfiling();
            //StartMethodProfiling("TargetNext");
            if (_randomWait.ShouldWait())
            {
                EndPulseProfiling();
                return;
            }

            if (IsTargetJammed)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Targeting jammed, not targeting");
                _alerts.TargetJammed();
                return;
            }
            ProcessTargetQueue();
            //EndMethodProfiling();
            EndPulseProfiling();
        }


        public override bool Initialize()
        {
            IsInitialized = true;
            return true;
        }

        public override void InFrameCleanup()
        {
            base.InFrameCleanup();

            if (Core.Metatron.Pulses > _pulseDuringWhichBlockTargetChangeWasSet)
                IsTargetChangeNextFrameBlocked = false;
        }

        public override bool OutOfFrameCleanup()
        {
            IsCleanedUpOutOfFrame = true;
            return true;
        }

		public bool IsTargetLocked(Int64 entityId)
		{
			return _meCache.Targets.Any(entity => entity.ID == entityId);
		}

    	/// <summary>
        /// Number of locked and locking targets
        /// </summary>
    	public int TargetCount
        {
            get { return _meCache.Targets.Count + _meCache.Me.GetTargeting().Count; }
        }

        public bool IsTargetJammed
        {
            get { return (int) _ship.MaxLockedTargets == 0; }
        }

        public void TargetEntity(IEntityWrapper entity)
        {
            var methodName = "TargetEntity";
			LogTrace(methodName, "Entity: {0}", entity.ID);

            if (entity.Name.Length == 0)
            {
				LogMessage(methodName, LogSeverityTypes.Standard, "Entity {0} invalid, cannot target it.",
					entity.ID);
                _targetQueue.DequeueTarget(entity.ID);
                return;
            }

			LogMessage(methodName, LogSeverityTypes.Standard, "Locking target {0} ({1}) at distance {2}",
				entity.Name, entity.ID, entity.Distance);
            entity.LockTarget();
        }

        public QueueTarget GetActiveQueueTarget()
        {
            if (_meCache.ActiveTargetId == -1 || !_queueTargetsByEntityId.ContainsKey(_meCache.ActiveTargetId))
                return null;

            return _queueTargetsByEntityId[_meCache.ActiveTargetId];
        }

        public bool HasActiveTargetBeenActiveTooLong()
        {
            var methodName = "HasActiveTargetBeenActiveTooLong";

            // Check if the active target has been active for more than 60 seconds
            var returnVal = DateTime.Now.Subtract(_targetQueue.ActiveTargetActiveSince).TotalSeconds > 60;
            LogTrace(methodName, returnVal.ToString());
            return returnVal;
        }


        /// <summary>
        /// Unlock an unqueued target, if any exist.
        /// </summary>
        /// <returns>True if a target was unlocked, otherwise false.</returns>
        public bool UnlockUnqueuedTargets()
        {
            var lockedTarget = _entityProvider.EntityWrappers.FirstOrDefault(entity => entity.IsLockedTarget && !_targetQueue.Targets.Any(target => target.Id == entity.ID));
            if (lockedTarget != null)
            {
                UnlockTarget(lockedTarget);
                return true;
            }

            return false;
        }

        public void ProcessTargetQueue()
        {
            var methodName = "ProcessTargetQueue";
            LogTrace(methodName);

            /*
             * I need to keep track of how many I'm trying to target so I don't try to target too many
             * Also, use a linq query to get a list<entity> from targetQueue.Targets sorted first by priority then by
             * distance, then foreach the results and target until we can't target any more. This'll make it so 
             * we target the highest priority shit in range before moving on to the next priority. 
             */

            //StartMethodProfiling("GetSortedEntities");
            var queueTargetsWithEntities = _targetQueue.Targets
                .Join(_entityProvider.EntityWrappers, queueTarget => queueTarget.Id, entity => entity.ID, (queueTarget, entity) => new { queueTarget, entity });

            //A list of currently locked targets, sorted by priority and reversed
            var currentTargetsByLowestPriority = queueTargetsWithEntities
                .Where(x => x.entity.IsLockedTarget)
                .OrderBy(x => GetNumModulesOnEntity(x.entity.ID))
                .ThenByDescending(x => x.queueTarget.Priority)
                .ThenBy(x => x.queueTarget.SubPriority)
                .ThenBy(x => BoolToInt(x.entity.IsTargetingMe))
                .ThenByDescending(x => x.queueTarget.TimeQueued)
                .Select(x => x.queueTarget)
                .ToList();

            var queuedCountByType = _targetQueue.Targets
                .GroupBy(target => target.Type)
                .ToDictionary(group => group.Key, group => group.Count());

            //foreach (var pair in currentCountByTargetType)
            //{
            //    LogMessage(methodName, LogSeverityTypes.Debug, "Type: {0}, Count: {1}", pair.Key, pair.Value);
            //}

            var intendedCountByType = DetermineIntendedLockCountByTargetType(queuedCountByType);

            //foreach (var pair in intendedCountByTargetType)
            //{
            //    LogMessage(methodName, LogSeverityTypes.Debug, "Type: {0}, Intended Count: {1}", pair.Key, pair.Value);
            //}

            var lockedCountByType = queueTargetsWithEntities
                .Where(x => x.entity.IsLockedTarget || x.entity.BeingTargeted)
                .GroupBy(x => x.queueTarget.Type)
                .ToDictionary(x => x.Key, x => x.Count());

            //If I have targets and have more of a type locked than intended, find the lowest priority target of that type and unlock it
            if (currentTargetsByLowestPriority.Count > 0)
            {
                var wasTargetUnlocked = UnlockExtraTargetsByTypes(lockedCountByType, intendedCountByType, currentTargetsByLowestPriority);
                if (wasTargetUnlocked) return;
            }

            var lockableQueueTargetsByPriority = queueTargetsWithEntities
                .Where(x => !x.entity.IsLockedTarget && !x.entity.BeingTargeted && x.entity.Distance <= _ship.MaxTargetRange)
                .OrderBy(x => x.queueTarget.Priority)
                .ThenByDescending(x => x.queueTarget.SubPriority)
                .ThenByDescending(x => BoolToInt(x.entity.IsTargetingMe))
                .ThenBy(x => x.queueTarget.TimeQueued)
                .Select(x => x.queueTarget)
                .ToList();

            foreach (var pair in queuedCountByType)
            {
                var highestPriorityTarget = lockableQueueTargetsByPriority.FirstOrDefault(queueTarget => queueTarget.Type == pair.Key);

                //If there's nothing more we can lock of this type, continue
                if (highestPriorityTarget == null) continue;

                var highestPriorityTargetEntity = _entityProvider.EntityWrappersById[highestPriorityTarget.Id];

                //If nothing is locked, just lock a target.
                if (lockedCountByType.Count == 0 || !lockedCountByType.ContainsKey(pair.Key))
                {
                    TargetEntity(highestPriorityTargetEntity);
                    return;
                }

                var lockedCount = lockedCountByType[pair.Key];
                var intendedCount = intendedCountByType[pair.Key];

                //If I have too many targets of this type locked, skip it. Something went wrong.
                if (lockedCount > intendedCount) continue;

                //If I have less than the intended count locked, I can straight-up lock it.
                if (lockedCount < intendedCount)
                {
                    TargetEntity(highestPriorityTargetEntity);
                    return;
                }

                var currentTargetsMatchingType = currentTargetsByLowestPriority.Where(queueTarget => queueTarget.Type == pair.Key);

                //To get here, locked count must == intended count. That means I might need to unlock a target.
                foreach (var currentQueueTarget in currentTargetsMatchingType)
                {
                    //IF the Entity is of a lower priority, or same priority but lower subpriority, unlock lowest piority target
                    if (highestPriorityTarget.Priority < currentQueueTarget.Priority ||
                        (highestPriorityTarget.Priority == currentQueueTarget.Priority && highestPriorityTarget.SubPriority > currentQueueTarget.SubPriority))
                    {
                        var currentTarget = _entityProvider.EntityWrappersById[currentQueueTarget.Id];

                        LogMessage(methodName, LogSeverityTypes.Debug, "Unlocking {0} ({1}, {2}, {3}) in favor of {4} ({5}, {6}, {7})",
                            currentTarget.Name, currentQueueTarget.Priority, currentQueueTarget.SubPriority, currentQueueTarget.TimeQueued,
                            highestPriorityTargetEntity.Name, highestPriorityTarget.Priority, highestPriorityTarget.SubPriority, highestPriorityTarget.TimeQueued);
                        UnlockTarget(currentTarget);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Look for any target type where we have more targets of that type locked than we should.
        /// If we do, unlock the lowest priority target of that type.
        /// </summary>
        /// <param name="lockedCountByType"></param>
        /// <param name="intendedCountByType"></param>
        /// <param name="currentTargetsOfLowestPriority"></param>
        /// <returns>True if a target was unlocked, otherwise false.</returns>
        private bool UnlockExtraTargetsByTypes(IDictionary<TargetTypes, int> lockedCountByType, IDictionary<TargetTypes, int> intendedCountByType, IEnumerable<QueueTarget> currentTargetsOfLowestPriority)
        {
            var methodName = "UnlockExtraTargetsByTypes";
            LogTrace(methodName);

            foreach (var pair in lockedCountByType)
            {
                var lockedCount = pair.Value;
                var intendedCount = intendedCountByType[pair.Key];

                if (lockedCount > intendedCount)
                {
                    var lowestPriorityTargetOfType = currentTargetsOfLowestPriority
                        .First(queueTarget => queueTarget.Type == pair.Key);

                    var entity = _entityProvider.EntityWrappersById[lowestPriorityTargetOfType.Id];

                    LogMessage(methodName, LogSeverityTypes.Debug, "We have {0} {1} targets locked and should only have {2}. Unlocking target \"{3}\" ({4}).",
                        lockedCount, pair.Key, intendedCount, entity.Name, entity.ID);
                    UnlockTarget(entity);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determine the intended count of locked targets by target type.
        /// </summary>
        /// <param name="currentCountByTargetType"></param>
        /// <returns></returns>
        private Dictionary<TargetTypes, int> DetermineIntendedLockCountByTargetType(IDictionary<TargetTypes, int> currentCountByTargetType)
        {
            var methodName = "DetermineIntendedLockCountByTargetType";
            LogTrace(methodName);

            var intendedCountByTargetType = new Dictionary<TargetTypes, int>();
            //var availableTargetSlots = (int) _ship.MaxLockedTargets;

            foreach (var pair in currentCountByTargetType)
            {
                var intendedCountDouble = _ship.MaxLockedTargets / currentCountByTargetType.Count;

                int intendedCountInt;
                //If it divided evenly, we're done.
                if (intendedCountDouble%2 == 0)
                {
                    intendedCountInt = (int) intendedCountDouble;
                }
                    //Fudge it up.
                else if ((pair.Key != TargetTypes.Kill && _moduleManager.IsNonCombatMode) ||
                         (pair.Key == TargetTypes.Kill && !_moduleManager.IsNonCombatMode))
                {
                    intendedCountInt = (int) Math.Ceiling(intendedCountDouble);
                }
                    //Fudge it down.
                else
                {
                    intendedCountInt = (int) Math.Floor(intendedCountDouble);
                }

                //availableTargetSlots -= intendedCountInt;
                intendedCountByTargetType.Add(pair.Key, intendedCountInt);
            }
            return intendedCountByTargetType;
        }

        public Dictionary<TargetTypes, int> CalculateIntendedSlotsByType(IEnumerable<QueueTarget> targets)
        {
            var intendedCountsByType = new Dictionary<TargetTypes, int>();

            var targetTypes = targets.Select(target => target.Type).Distinct().ToList();
            var availableSlots = _ship.MaxLockedTargets;

            for (var index = 0; index < targetTypes.Count; targetTypes.RemoveAt(index))
            {
                var targetType = targetTypes[index];
                var realCount = availableSlots/targetTypes.Count;

                var adjustedCount = 0;

                if (realCount%2 == 0)
                {
                    adjustedCount = (int) realCount;
                }
                else if ((!_moduleManager.IsNonCombatMode && targetType == TargetTypes.Kill) ||
                    (_moduleManager.IsNonCombatMode && targetType != TargetTypes.Kill))
                {
                    adjustedCount = (int) Math.Ceiling(realCount);
                }
                else
                {
                    adjustedCount = (int) Math.Floor(realCount);
                }

                intendedCountsByType.Add(targetType, adjustedCount);
                availableSlots -= adjustedCount;
            }

            return intendedCountsByType;
        }

        public void ChangeTargetTo(IEntityWrapper newTarget, bool blockTargetChangeNextPulse)
        {
            var methodName = "ChangeTargetTo";
            LogTrace(methodName, "newTarget: {0}, blockTargetChangeNextPulse: {1}",
                newTarget.ID, blockTargetChangeNextPulse);

            if (WasTargetChangedThisFrame)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Error: We've already changed target this frame.");
                return;
            }

            if (IsTargetChangeNextFrameBlocked)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Error: Target change is blocked.");
                return;
            }

            if (!_meCache.Targets.Contains(newTarget))
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Error: The given target isn't in the list of targets. However, IsLockedTarget: {0}", newTarget.IsLockedTarget);
                return;
            }

		    newTarget.MakeActiveTarget();
		    WasTargetChangedThisFrame = true;
		    //Core.Metatron.Logging.LogMessage(ObjectName, new Core.LogEventArgs(Core.LogSeverityTypes.Debug,
		    //	"ChangeTargetTo", String.Format("Setting {0} ({1}) as active target.",
		    //	newTarget.Name, newTarget.ID)));

            if (blockTargetChangeNextPulse)
                IsTargetChangeNextFrameBlocked = true;
		}

		public int GetNumModulesOnEntity(Int64 entityId)
		{
			var methodName = "GetNumModulesOnEntity";
			LogTrace(methodName, "Entity: {0}", entityId);

			if (!_modulesBuilt)
			{
				//StartMethodProfiling("GetNumModulesOnEntity");
				foreach (var queueTarget in
                    _targetQueue.Targets.Where(queueTarget => !ModulesOnEntity.ContainsKey(queueTarget.Id)))
				{
					ModulesOnEntity.Add(queueTarget.Id, 0);
				}

				foreach (var module in _ship.AllModules)
				{
					if (!module.IsActive) 
						continue;

					//count++;
					var targetId = module.TargetID;
					if (_entityProvider.EntityWrappersById.ContainsKey(targetId) &&
					    ModulesOnEntity.ContainsKey(targetId))
					{
						ModulesOnEntity[targetId]++;
					}
				}

				if (_drones.TotalDrones > 0 && _drones.DroneTargetEntityId > 0)
				{
					if (ModulesOnEntity.ContainsKey(_drones.DroneTargetEntityId))
					{
						ModulesOnEntity[_drones.DroneTargetEntityId]++;
					}
					else
					{
						ModulesOnEntity.Add(_drones.DroneTargetEntityId, 1);
					}
				}
				_modulesBuilt = true;
			}
			//EndMethodProfiling();
			return ModulesOnEntity.ContainsKey(entityId) ? ModulesOnEntity[entityId] : 0;
		}

		public void UnlockTarget(IEntityWrapper target)
		{
            var methodName = "UnlockTarget";
			LogTrace(methodName, "Target: {0} ({1})", target.Name, target.ID);

		    if (WasTargetChangedThisFrame) return;

		    WasTargetChangedThisFrame = true;

			target.UnlockTarget();
			LogMessage(methodName, LogSeverityTypes.Standard, "Unlocking target {0} ({1})",
				target.Name, target.ID);
		}

    	public List<IEntityWrapper> GetLockedMiningTargets()
		{
			return (from IEntityWrapper entity in _meCache.Targets
                    where _queueTargetsByEntityId.ContainsKey(entity.ID) &&
                        _queueTargetsByEntityId[entity.ID].Type == TargetTypes.Mine
					select entity).ToList();
		}

		public List<IEntityWrapper> GetLockedTractorTargets()
		{
			return (from IEntityWrapper entity in _meCache.Targets
                    where _queueTargetsByEntityId.ContainsKey(entity.ID) &&
                        _queueTargetsByEntityId[entity.ID].Type == TargetTypes.LootSalvage
					select entity).ToList();
		}

		public List<IEntityWrapper> GetLockedKillingTargets()
		{
			return (from IEntityWrapper entity in _meCache.Targets
                    where _queueTargetsByEntityId.ContainsKey(entity.ID) &&
                        _queueTargetsByEntityId[entity.ID].Type == TargetTypes.Kill
					select entity).ToList();
		}

        public List<QueueTarget> GetQueuedAsteroids()
		{
			var tempList = new List<QueueTarget>();
            foreach (var queueTarget in _targetQueue.Targets)
			{
				if (queueTarget.Type == TargetTypes.Mine)
				{
					tempList.Add(queueTarget);
				}
			}
			return tempList;
		}

        /// <summary>
        /// Indicates whether or not we can change targets this pulse.
        /// </summary>
        public bool CanChangeTarget
        {
            get { return (!IsTargetChangeNextFrameBlocked && !WasTargetChangedThisFrame); }
        }
    }
}
