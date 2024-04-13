using System;
using System.Diagnostics;
using System.Threading;

namespace Metatron.Core
{
    public interface IQueueTarget
    {
        Int64 Id { get; }
        int Priority { get; }
        int SubPriority { get; }
        double TrackingScore { get; }
        TargetTypes Type { get; }
        DateTime TimeQueued { get; }
        void UpdateTarget(int priority, int subPriority, TargetTypes type, double trackingScore);
    }

    public sealed class QueueTarget : IQueueTarget
    {
        public Int64 Id { get; private set; }
        public int Priority { get; private set; }
        public int SubPriority { get; private set; }
        public TargetTypes Type { get; private set; }
        public double TrackingScore { get; private set; }
        public DateTime TimeQueued { get; private set; }

        public QueueTarget()
        {
        }

        public QueueTarget(long entityId, int priority, int subPriority, TargetTypes type, double trackingScore)
        {
            TimeQueued = DateTime.Now;

            Id = entityId;
            Type = type;
            Priority = priority;
            SubPriority = subPriority;
            TrackingScore = trackingScore;
        }

        public void UpdateTarget(int priority, int subPriority, TargetTypes type, double trackingScore)
        {
            Priority = priority;
            SubPriority = subPriority;
            Type = type;
            TrackingScore = trackingScore;
        }
    }
}
