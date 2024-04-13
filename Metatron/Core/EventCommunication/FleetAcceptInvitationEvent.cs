using System.Text;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core.EventCommunication
{
    public class FleetAcceptInvitationEvent : BaseEvent<FleetAcceptInvitationEventArgs>
    {
        public FleetAcceptInvitationEvent(ILogging logging, string relayGroup, string eventName) :
            base(logging, relayGroup, eventName)
        {

        }

        protected override FleetAcceptInvitationEventArgs GetEventArgs(LSEventArgs e)
        {
            return new FleetAcceptInvitationEventArgs(_logging, e);
        }
    }

    public class FleetAcceptInvitationEventArgs : BaseEventArgs
    {
		public string AcceptFrom { get; private set; }

		public FleetAcceptInvitationEventArgs(ILogging logging, long sendingFleetMemberId, int solarSystemId, string acceptFrom) :
			base(logging, sendingFleetMemberId, solarSystemId)
		{
			ObjectName = "Notify_AcceptFleetInviteEventArgs";
			AcceptFrom = acceptFrom;
		}

        public FleetAcceptInvitationEventArgs(ILogging logging, LSEventArgs copy)
			: base(logging, copy)
		{
			ObjectName = "Notify_AcceptFleetInviteEventArgs";
		}

		protected override void Initialize(LSEventArgs copy)
		{
		    base.Initialize(copy);

			AcceptFrom = copy.Args[2];
		}

        public override string GetFieldCsv()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(AcceptFrom);

            return string.Concat(base.GetFieldCsv(), ',', stringBuilder.ToString());
        }
    }
}
