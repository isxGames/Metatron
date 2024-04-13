using System;
using System.Text;
using LavishScriptAPI;

namespace Metatron.Core.EventCommunication
{
    public sealed class FleetNeedAssistEvent : BaseEvent<FleetNeedAssistEventArgs>
    {
        public FleetNeedAssistEvent(string relayGroup, string eventName) :
            base(relayGroup, eventName)
        {
            
        }

        protected override FleetNeedAssistEventArgs GetEventArgs(LSEventArgs e)
        {
            return new FleetNeedAssistEventArgs(e);
        }

        public override void SendEvent(FleetNeedAssistEventArgs e)
        {
            SendEventFromArgs(e);
        }
    }

    public sealed class FleetNeedAssistEventArgs : BaseEventArgs
    {
        public QueueTarget Target { get; private set; }

        public FleetNeedAssistEventArgs(Int64 sendingFleetMemberID, int solarSystemID, QueueTarget target) :
            base(sendingFleetMemberID, solarSystemID)
        {
            ObjectName = "NeedAssistEventArgs";
            Target = target;
        }

        public FleetNeedAssistEventArgs(LSEventArgs copy)
            : base(copy)
        {
            ObjectName = "NeedAssistEventArgs";
            Initialize(copy);
        }

        private void Initialize(LSEventArgs copy)
        {
            var methodName = "Initialize";

            Int64 targetEntityID = -1;
            int targetPriority = 0, targetSubPriority = 0;
            DateTime targetTimeQueued;
            TargetTypes targetType;

            if (copy.Args.Length < 7)
            {
                Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Args array is is missing elements.");
                return;
            }

            if (!Int64.TryParse(copy.Args[2], out targetEntityID))
            {
                Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse target entity ID {0}", copy.Args[2]);
            }

            if (!int.TryParse(copy.Args[3], out targetPriority))
            {
                Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse target priority {0}", copy.Args[3]);
            }

            if (!int.TryParse(copy.Args[4], out targetSubPriority))
            {
                Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse target sub priority {0}", copy.Args[4]);
            }

            if (!DateTime.TryParse(copy.Args[5], out targetTimeQueued))
            {
                Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse target time queued {0}", copy.Args[5]);
            }

            if (!TargetTypes.TryParse(copy.Args[6], out targetType))
            {
                Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse target type {0}", copy.Args[6]);
            }

            Target = new QueueTarget(targetEntityID, targetType, targetPriority, targetSubPriority);
        }

        public override string GetFieldCsv()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(Target.ID);
            stringBuilder.Append(',');
            stringBuilder.Append(Target.Priority);
            stringBuilder.Append(',');
            stringBuilder.Append(Target.SubPriority);
            stringBuilder.Append(',');
            stringBuilder.Append(Target.Type);

            return string.Concat(base.GetFieldCsv(), ',', stringBuilder.ToString());
        }
    }
}
