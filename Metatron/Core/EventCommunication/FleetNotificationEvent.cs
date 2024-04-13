using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core.EventCommunication
{
    public class FleetNotificationEvent : BaseEvent<BaseEventArgs>
    {
        public FleetNotificationEvent(ILogging logging, string relayGroup, string eventName) :
            base(logging, relayGroup, eventName, false)
        {

        }

        protected override BaseEventArgs GetEventArgs(LSEventArgs e)
        {
            return new BaseEventArgs(_logging, e);
        }
    }
}
