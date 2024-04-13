using System;
using System.Collections.Generic;

namespace Metatron.Core.Interfaces
{
    public interface ITargeting : IModule
    {
        /// <summary>
        /// Number of locked and locking targets
        /// </summary>
        int TargetCount { get; }

        bool IsTargetJammed { get; }
        Dictionary<long, QueueTarget> QueueTargetsByEntityId { get; }
        bool WasTargetChangedThisFrame { get; }
        bool IsTargetChangeNextFrameBlocked { get; }

        /// <summary>
        /// Indicates whether or not we can change targets this pulse.
        /// </summary>
        bool CanChangeTarget { get; }

        bool HasActiveTargetBeenActiveTooLong();
        void TargetEntity(IEntityWrapper entity);
        QueueTarget GetActiveQueueTarget();
        void ProcessTargetQueue();
        void ChangeTargetTo(IEntityWrapper newTarget, bool blockTargetChangeNextPulse);
        int GetNumModulesOnEntity(Int64 entityId);
        void UnlockTarget(IEntityWrapper target);
        List<IEntityWrapper> GetLockedMiningTargets();
        List<IEntityWrapper> GetLockedTractorTargets();
        List<IEntityWrapper> GetLockedKillingTargets();
        List<QueueTarget> GetQueuedAsteroids();
        void BlockTargetChangeNextFrame();
        Dictionary<TargetTypes, int> CalculateIntendedSlotsByType(IEnumerable<QueueTarget> targets);

        /// <summary>
        /// Unlock an unqueued target, if any exist.
        /// </summary>
        /// <returns>True if a target was unlocked, otherwise false.</returns>
        bool UnlockUnqueuedTargets();
    }
}
