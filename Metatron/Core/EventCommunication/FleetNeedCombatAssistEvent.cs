using System;
using System.Text;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core.EventCommunication
{
    public sealed class FleetNeedCombatAssistEvent : BaseEvent<FleetNeedCombatAssistEventArgs>
    {
        public FleetNeedCombatAssistEvent(ILogging logging, string relayGroup, string eventName) :
            base(logging, relayGroup, eventName)
        {
            
        }

        protected override FleetNeedCombatAssistEventArgs GetEventArgs(LSEventArgs e)
        {
            return new FleetNeedCombatAssistEventArgs(_logging, e);
        }
    }

    public sealed class FleetNeedCombatAssistEventArgs : BaseEventArgs
    {
        public QueueTarget Target { get; private set; }

        public FleetNeedCombatAssistEventArgs(ILogging logging, Int64 sendingFleetMemberId, int solarSystemId, QueueTarget target) :
            base(logging, sendingFleetMemberId, solarSystemId)
        {
            ObjectName = "NeedAssistEventArgs";
            Target = target;
        }

        public FleetNeedCombatAssistEventArgs(ILogging logging, LSEventArgs copy)
            : base(logging, copy)
        {
            ObjectName = "NeedAssistEventArgs";
        }

        protected override void Initialize(LSEventArgs copy)
        {
            var methodName = "Initialize";
            base.Initialize(copy);

            Int64 targetEntityId;
            int targetPriority, targetSubPriority;
            DateTime targetTimeQueued;
            TargetTypes targetType;

            if (copy.Args.Length < 7)
            {
                _logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Args array is is missing elements.");
                return;
            }

            if (!Int64.TryParse(copy.Args[2], out targetEntityId))
            {
                _logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse target entity ID {0}", copy.Args[2]);
            }

            if (!int.TryParse(copy.Args[3], out targetPriority))
            {
                _logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse target priority {0}", copy.Args[3]);
            }

            if (!int.TryParse(copy.Args[4], out targetSubPriority))
            {
                _logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse target sub priority {0}", copy.Args[4]);
            }

            if (!DateTime.TryParse(copy.Args[5], out targetTimeQueued))
            {
                _logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse target time queued {0}", copy.Args[5]);
            }

            if (!TargetTypes.TryParse(copy.Args[6], out targetType))
            {
                _logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse target type {0}", copy.Args[6]);
            }

            Target = new QueueTarget(targetEntityId, targetPriority, targetSubPriority, targetType, Core.Metatron.TargetQueue.GetTrackingScore(targetEntityId));
        }

        public override string GetFieldCsv()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(Target.Id);
            stringBuilder.Append(',');
            stringBuilder.Append(Target.Priority);
            stringBuilder.Append(',');
            stringBuilder.Append(Target.SubPriority);
            stringBuilder.Append(',');
            stringBuilder.Append(Target.TimeQueued);
            stringBuilder.Append(',');
            stringBuilder.Append(Target.Type);

            return string.Concat(base.GetFieldCsv(), ',', stringBuilder.ToString());
        }
    }
}
