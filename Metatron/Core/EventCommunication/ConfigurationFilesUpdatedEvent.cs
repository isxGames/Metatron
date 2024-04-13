using System.Text;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core.EventCommunication
{
    public class ConfigurationFilesUpdatedEvent : BaseEvent<ConfigurationFilesUpdatedEventArgs>
    {
        public ConfigurationFilesUpdatedEvent(ILogging logging, string relayGroup, string eventName) :
            base(logging, relayGroup, eventName)
        {

        }

        protected override ConfigurationFilesUpdatedEventArgs GetEventArgs(LSEventArgs e)
        {
            return new ConfigurationFilesUpdatedEventArgs(_logging, e);
        }
    }

    public class ConfigurationFilesUpdatedEventArgs : BaseEventArgs
    {
		public string UplinkName { get; private set; }
		public string OldFileName { get; private set; }
		public string NewFileName { get; private set; }

        public ConfigurationFilesUpdatedEventArgs(ILogging logging, LSEventArgs copy)
            : base(logging)
        {
			ObjectName = "Notify_UpdateConfigurationFileEventArgs";
			Initialize(copy);
		}

		protected override void Initialize(LSEventArgs copy)
		{
			UplinkName = copy.Args[0];
			OldFileName = copy.Args[1];
			NewFileName = copy.Args[2];
		}

        public override string GetFieldCsv()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(UplinkName);
            stringBuilder.Append(',');
            stringBuilder.Append(OldFileName);
            stringBuilder.Append(',');
            stringBuilder.Append(NewFileName);

            return stringBuilder.ToString();
        }
    }
}
