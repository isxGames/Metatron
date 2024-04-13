using System;
using System.Text;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core.EventCommunication
{
    public class FleetMemberSkillsReceivedEvent : BaseEvent<FleetMemberSkillsReceivedEventArgs>
    {
        public FleetMemberSkillsReceivedEvent(ILogging logging, string relayGroup, string eventName) :
            base(logging, relayGroup, eventName)
        {

        }

        protected override FleetMemberSkillsReceivedEventArgs GetEventArgs(LSEventArgs e)
        {
            return new FleetMemberSkillsReceivedEventArgs(_logging, e);
        }
    }

    public class FleetMemberSkillsReceivedEventArgs : BaseEventArgs
    {
		public int InBoostShip { get; private set; }
		public int Leadership { get; private set; }
		public int WingCommand { get; private set; }
		public int FleetCommand { get; private set; }
		public int MiningDirector { get; private set; }
		public int MiningForeman { get; private set; }
		public int ArmoredWarfare { get; private set; }
		public int SkirmishWarfare { get; private set; }
		public int InformationWarfare { get; private set; }
		public int SiegeWarfare { get; private set; }
		public int WarfareLinkSpecialist { get; private set; }

		public FleetMemberSkillsReceivedEventArgs(ILogging logging, Int64 sendingFleetMemberId, int solarSystemId, int inBoostShip, int leadership,
			int wingCommand, int fleetCommand, int miningDirector, int miningForeman, int armoredWarfare,
			int skirmishWarfare, int informationWarfare, int siegeWarfare, int warfareLinkSpecialist) :
			base(logging, sendingFleetMemberId, solarSystemId)
		{
			ObjectName = "UpdateFleetSkillsEventArgs";
			InBoostShip = inBoostShip;
			Leadership = leadership;
			WingCommand = wingCommand;
			FleetCommand = fleetCommand;
			MiningDirector = miningDirector;
			MiningForeman = miningForeman;
			ArmoredWarfare = armoredWarfare;
			SkirmishWarfare = skirmishWarfare;
			InformationWarfare = informationWarfare;
			SiegeWarfare = siegeWarfare;
			WarfareLinkSpecialist = warfareLinkSpecialist;
		}

        public FleetMemberSkillsReceivedEventArgs(ILogging logging, LSEventArgs copy)
			: base(logging, copy)
		{
			ObjectName = "UpdateFleetSkillsEventArgs";
		}

		protected override void Initialize(LSEventArgs copy)
		{
			var methodName = "Initialize";
		    base.Initialize(copy);

			int inBoostShip, leadership, wingCommand, fleetCommand, miningDirector, miningForeman, armoredWarfare,
				skirmishWarfare, informationWarfare, siegeWarfare, warfareLinkSpecialist;

			if (!int.TryParse(copy.Args[2], out inBoostShip))
			{
				_logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse in boost ship {0}", copy.Args[2]);
			}
			if (!int.TryParse(copy.Args[3], out leadership))
			{
				_logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse leadership level {0}", copy.Args[2]);
			}
			if (!int.TryParse(copy.Args[4], out wingCommand))
			{
				_logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse wing command level {0}", copy.Args[3]);
			}
			if (!int.TryParse(copy.Args[5], out fleetCommand))
			{
				_logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse fleet command level {0}", copy.Args[4]);
			}
			if (!int.TryParse(copy.Args[6], out miningDirector))
			{
				_logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse mining director level {0}", copy.Args[5]);
			}
			if (!int.TryParse(copy.Args[7], out miningForeman))
			{
				_logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse mining foreman level {0}", copy.Args[6]);
			}
			if (!int.TryParse(copy.Args[8], out armoredWarfare))
			{
				_logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse armor warfare level {0}", copy.Args[7]);
			}
			if (!int.TryParse(copy.Args[9], out skirmishWarfare))
			{
				_logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse skirmish warfare level {0}", copy.Args[8]);
			}
			if (!int.TryParse(copy.Args[10], out informationWarfare))
			{
				_logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse information warfare level {0}", copy.Args[9]);
			}
			if (!int.TryParse(copy.Args[11], out siegeWarfare))
			{
				_logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse siege warfare level {0}", copy.Args[10]);
			}
			if (!int.TryParse(copy.Args[12], out warfareLinkSpecialist))
			{
				_logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse warfare link specialist level {0}", copy.Args[11]);
			}

			InBoostShip = inBoostShip;
			Leadership = leadership;
			WingCommand = wingCommand;
			FleetCommand = fleetCommand;
			MiningDirector = miningDirector;
			MiningForeman = miningForeman;
			ArmoredWarfare = armoredWarfare;
			SkirmishWarfare = skirmishWarfare;
			InformationWarfare = informationWarfare;
			SiegeWarfare = siegeWarfare;
			WarfareLinkSpecialist = warfareLinkSpecialist;
		}

        public override string GetFieldCsv()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(InBoostShip);
            stringBuilder.Append(',');
            stringBuilder.Append(Leadership);
            stringBuilder.Append(',');
            stringBuilder.Append(WingCommand);
            stringBuilder.Append(',');
            stringBuilder.Append(FleetCommand);
            stringBuilder.Append(',');
            stringBuilder.Append(MiningDirector);
            stringBuilder.Append(',');
            stringBuilder.Append(MiningForeman);
            stringBuilder.Append(',');
            stringBuilder.Append(ArmoredWarfare);
            stringBuilder.Append(',');
            stringBuilder.Append(SkirmishWarfare);
            stringBuilder.Append(',');
            stringBuilder.Append(InformationWarfare);
            stringBuilder.Append(',');
            stringBuilder.Append(SiegeWarfare);
            stringBuilder.Append(',');
            stringBuilder.Append(WarfareLinkSpecialist);

            return String.Concat(base.GetFieldCsv(), ',', stringBuilder.ToString());
        }
    }
}
