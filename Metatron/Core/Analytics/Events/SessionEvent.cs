using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Metatron.Core.Analytics.Events
{
    public class SessionEvent : BaseEvent
    {
        public enum EventAction
        {
            Open,
            Start,
            Stop,
            Pause,
            Quit
        }

        public SessionEvent(EventAction action, string activeBotBehavior)
        {
            this.Action = action.ToString();
            this.Category = Events.Category.Session.ToString();
            this.Label = "BotStart";
            this.Value = activeBotBehavior;
        }
    }
}
