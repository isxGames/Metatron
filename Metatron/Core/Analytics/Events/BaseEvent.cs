using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Metatron.Core.Analytics.Events
{
    public class BaseEvent
    {
        public string Category { get; set; }
        public string Action { get; set; }
        public string Label { get; set; }
        public string Value { get; set; }
    }

    public enum Category
    {
        None,
        Session,
        UIEvent,
        Error,
    }
}
