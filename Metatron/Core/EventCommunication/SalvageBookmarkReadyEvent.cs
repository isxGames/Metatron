using System;
using System.Text;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core.EventCommunication
{
    public class SalvageBookmarkReadyEvent : BaseEvent<SalvageBookmarkReadyEventArgs>
    {
        public SalvageBookmarkReadyEvent(ILogging logging, string relayGroup, string eventName) :
            base(logging, relayGroup, eventName)
        {

        }

        protected override SalvageBookmarkReadyEventArgs GetEventArgs(LSEventArgs e)
        {
            return new SalvageBookmarkReadyEventArgs(_logging, e);
        }
    }

    public sealed class SalvageBookmarkReadyEventArgs : BaseEventArgs
    {
        public Int64 BookmarkId { get; private set; }
        public string SendingFleetMemberName { get; private set; }

        public SalvageBookmarkReadyEventArgs(ILogging logging, Int64 bookmarkId, string sendingFleetMemberName, long sendingFleetMemberId, int solarSystemId) :
            base(logging, sendingFleetMemberId, solarSystemId)
        {
            ObjectName = "SalvageBookmarkReadyEventArgs";
            BookmarkId = bookmarkId;
            SendingFleetMemberName = sendingFleetMemberName;
        }

        public SalvageBookmarkReadyEventArgs(ILogging logging, LSEventArgs copy)
            : base(logging, copy)
        {
            ObjectName = "NeedPickupEventArgs";
        }

        protected override void Initialize(LSEventArgs copy)
        {
            var methodName = "Initialize";
            base.Initialize(copy);

            Int64 bookmarkId;

            if (!Int64.TryParse(copy.Args[2], out bookmarkId))
            {
                _logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse target can entity ID {0}", copy.Args[2]);
            }
            BookmarkId = bookmarkId;
            SendingFleetMemberName = copy.Args[3];
        }

        public override string GetFieldCsv()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(BookmarkId);
            stringBuilder.Append(',');
            stringBuilder.Append(SendingFleetMemberName);

            return string.Concat(base.GetFieldCsv(), ',', stringBuilder.ToString());
        }
    }
}
