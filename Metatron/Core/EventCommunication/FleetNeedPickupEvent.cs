using System;
using System.Text;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core.EventCommunication
{
    public class FleetNeedPickupEvent : BaseEvent<FleetNeedPickupEventArgs>
    {
        public FleetNeedPickupEvent(ILogging logging, string relayGroup, string eventName) :
            base(logging, relayGroup, eventName)
        {

        }

        protected override FleetNeedPickupEventArgs GetEventArgs(LSEventArgs e)
        {
            return new FleetNeedPickupEventArgs(_logging, e);
        }
    }

    public sealed class FleetNeedPickupEventArgs : BaseEventArgs
    {
        public Int64 TargetCanEntityId { get; private set; }
        public Int64 SendingFleetMemberEntityId { get; private set; }
        public string SendingFleetMemberName { get; private set; }

        public FleetNeedPickupEventArgs(ILogging logging, Int64 sendingFleetMemberId, int solarSystemId, Int64 targetCanEntityId, Int64 sendingFleetMemberEntityId, string sendingFleetMemberName) :
            base(logging, sendingFleetMemberId, solarSystemId)
        {
            ObjectName = "NeedPickupEventArgs";
            TargetCanEntityId = targetCanEntityId;
            SendingFleetMemberEntityId = sendingFleetMemberEntityId;
            SendingFleetMemberName = sendingFleetMemberName;
        }

        public FleetNeedPickupEventArgs(ILogging logging, LSEventArgs copy)
            : base(logging, copy)
        {
            ObjectName = "NeedPickupEventArgs";
        }

        protected override void Initialize(LSEventArgs copy)
        {
            var methodName = "Initialize";
            base.Initialize(copy);

            Int64 targetCanEntityId, sendingFleetMemberEntityId;

            if (!Int64.TryParse(copy.Args[2], out targetCanEntityId))
            {
                _logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse target can entity ID {0}", copy.Args[2]);
            }
            if (!Int64.TryParse(copy.Args[3], out sendingFleetMemberEntityId))
            {
                _logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse sending fleet member entity ID {0}", copy.Args[3]);
            }
            TargetCanEntityId = targetCanEntityId;
            SendingFleetMemberEntityId = sendingFleetMemberEntityId;
        }

        public override string GetFieldCsv()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(TargetCanEntityId);
            stringBuilder.Append(',');
            stringBuilder.Append(SendingFleetMemberEntityId);
            stringBuilder.Append(',');
            stringBuilder.Append(SendingFleetMemberName);

            return string.Concat(base.GetFieldCsv(), ',', stringBuilder.ToString());
        }
    }
}
