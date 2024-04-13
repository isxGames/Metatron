using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Metatron.ActionModules;
using Metatron.Core;
using Metatron.Core.Config;
using Metatron.Core.Interfaces;

namespace Metatron.BehaviorModules.PartialBehaviors
{
    public class SalvagePartialBehavior : PartialBehaviorBase
    {
        // ReSharper disable ConvertToConstant.Local
        private readonly int MinimumDistanceToArray = 25000;
        // ReSharper restore ConvertToConstant.Local

        private readonly IMovement _movement;
        private readonly IEntityProvider _entityProvider;
        private readonly ICargoConfiguration _cargoConfiguration;
        private readonly IBookMarkCache _bookMarkCache;
        private readonly IBookmarks _bookmarks;
        private readonly IMovementConfiguration _movementConfiguration;
        private readonly IMeCache _meCache;
        private readonly MathUtility _mathUtility;
        private readonly IIsxeveProvider _isxeveProvider;
        private readonly ITargetQueue _targetQueue;

        public SalvagePartialBehavior(IMovement movement, IEntityProvider entityProvider, IBookMarkCache bookMarkCache, IBookmarks bookmarks,
            IMovementConfiguration movementConfiguration, IMeCache meCache, ITargetQueue targeting)
        {
            _movement = movement;
            _entityProvider = entityProvider;
            _bookMarkCache = bookMarkCache;
            _bookmarks = bookmarks;
            _movementConfiguration = movementConfiguration;
            _meCache = meCache;
            _targetQueue = targeting;

            ModuleName = "SalvagePartialBehavior";
        }

        public override BehaviorExecutionResults Execute()
        {
            var methodName = "Execute";
            LogTrace(methodName);

            if (_movement.IsMoving)
                return BehaviorExecutionResults.Incomplete;

            if (WrecksPresentLocally())
                return ProcessLocalWrecksForSalvage();

            return BehaviorExecutionResults.Complete;
        }

        private BehaviorExecutionResults ProcessLocalWrecksForSalvage()
        {
            var methodName = "ProcessLocalWrecksForSalvage";
            LogTrace(methodName);

            LogMessage(methodName, LogSeverityTypes.Debug, "We have stuff to loot. Processing targets.");

            var wrecks = GetLocalWrecks();
            if (wrecks.All(w => w.Distance <= Core.Metatron.Ship.MaxTargetRange))
            {
                foreach (var wreck in wrecks)
                {
                    _targetQueue.EnqueueTarget(wreck.ID, (int)TargetPriorities.Wreck_TractorSalvage, TargetTypes.LootSalvage);
                    LogTrace($"Queued new target: {wreck.ID}");
                }
            }
            else
            {
                var destination = new Destination(DestinationTypes.Entity, wrecks.Where(w => w.Distance > Core.Metatron.Ship.MaxTargetRange).OrderBy(w => w.Distance).First().ID, 100.0);
                _movement.QueueDestination(destination);
            }
            return BehaviorExecutionResults.Incomplete;
        }

        private bool WrecksPresentLocally()
        {
            var methodName = "WrecksPresentLocally";
            LogTrace(methodName);

            LogMessage(methodName, LogSeverityTypes.Debug, "Looting enabled. Checking for local loot");
            LogTrace("CombatSalvage");
            var wrecks = GetLocalWrecks();
            return wrecks.Any();
        }

        private List<IEntityWrapper> GetLocalWrecks()
        {
            var methodName = "GetLocalWrecks";
            LogTrace(methodName);

            var wrecks = _targetQueue.Targets
                .Join(_entityProvider.EntityWrappers, queueTarget => queueTarget.Id, entity => entity.ID, (queueTarget, entity) => new { queueTarget, entity })
                .Where(pair => pair.queueTarget.Type == TargetTypes.LootSalvage)
                //.Where(pair => pair.entity.Distance <= Core.Metatron.Ship.MaxTargetRange)
                .OrderBy(pair => pair.queueTarget.Priority)
                .ThenByDescending(pair => pair.queueTarget.SubPriority)
                .ThenBy(pair => pair.entity.Distance)
                .Select(pair => pair.entity)
                .ToList();

            return wrecks.ToList();
        }
    }
}
