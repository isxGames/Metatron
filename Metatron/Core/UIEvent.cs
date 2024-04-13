using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Metatron.Core.Analytics.Events
{
    public class UIEvent : BaseEvent
    {
        public enum EventAction
        {
            ButtonClick,
            DropdownSelect,
            RadialSelect,
            CheckboxClick
        }

        public UIEvent(EventAction action, string label, string value)
        {
            this.Label = label;
            this.Value = value;
            this.Category = Analytics.Events.Category.UIEvent.ToString();
            this.Action = action.ToString();
        }

        public UIEvent(EventAction action, string label)
        {
            this.Label = label;
            this.Category = Analytics.Events.Category.UIEvent.ToString();
            this.Action = action.ToString();
        }
    }
}
