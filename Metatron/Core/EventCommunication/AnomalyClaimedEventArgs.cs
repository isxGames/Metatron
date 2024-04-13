using System;
using System.Text;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core.EventCommunication
{
    public class AnomalyClaimedEvent : BaseEvent<AnomalyClaimedEventArgs>
    {
        public AnomalyClaimedEvent(ILogging logging, string relayGroup, string eventName) :
            base(logging, relayGroup, eventName, false)
        {

        }

        protected override AnomalyClaimedEventArgs GetEventArgs(LSEventArgs e)
        {
            return new AnomalyClaimedEventArgs(_logging, e);
        }
    }

    public class AnomalyClaimedEventArgs : BaseEventArgs
    {
        public Int64 AnomalyId { get; private set; }

        public DateTime ClaimTime { get; private set; }

		public AnomalyClaimedEventArgs(ILogging logging, long sendingFleetMemberId, int solarSystemId, Int64 anomalyId, DateTime claimTime) :
			base(logging, sendingFleetMemberId, solarSystemId)
		{
			ObjectName = "AnomalyClaimedEventArgs";

		    AnomalyId = anomalyId;
		    ClaimTime = claimTime;
		}

        public AnomalyClaimedEventArgs(ILogging logging, LSEventArgs copy)
			: base(logging, copy)
		{
            ObjectName = "AnomalyClaimedEventArgs";
		}

		protected override void Initialize(LSEventArgs copy)
		{
		    base.Initialize(copy);

		    Int64 anomalyId;
            if (!Int64.TryParse(copy.Args[2], out anomalyId))
            {
                _logging.LogMessage(ObjectName, "Initialize", LogSeverityTypes.Standard, "Error: Could not parse AnomalyId \"{0}\" as an Int64.", copy.Args[2]);
            }
		    AnomalyId = anomalyId;

		    DateTime claimTime;
            if (!DateTime.TryParse(copy.Args[3], out claimTime))
            {
                _logging.LogMessage(ObjectName, "Initialize", LogSeverityTypes.Standard, "Error: Could not parse ClaimTime \"{0}\" as a DateTime.", copy.Args[3]);
            }
		    ClaimTime = claimTime;
		}

        public override string GetFieldCsv()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(AnomalyId);
            stringBuilder.Append(',');
            stringBuilder.Append(ClaimTime);

            return string.Concat(base.GetFieldCsv(), ',', stringBuilder.ToString());
        }
    }
}
