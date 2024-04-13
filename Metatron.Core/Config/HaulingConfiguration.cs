using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Metatron.Core.Config
{
    public class HaulingConfiguration : ConfigurationBase
    {
        private static readonly string HAULER_MODE = "Hauler_Mode";
        private static readonly string CYCLE_FLEET_DELAY = "Hauler_Cycle_Fleet_Delay";

        public HaulerModes HaulerMode
        {
            get { return GetConfigValue<HaulerModes>(HAULER_MODE); }
            set { SetConfigValue(HAULER_MODE, value); }
        }

        public int HaulerCycleFleetDelay
        {
            get { return GetConfigValue<int>(CYCLE_FLEET_DELAY); }
            set { SetConfigValue(CYCLE_FLEET_DELAY, value); }
        }

        public HaulingConfiguration(Dictionary<string, ConfigProperty> configProperties)
            : base(configProperties)
        {
            AddDefaultConfigProperties();
        }

        public override void AddDefaultConfigProperties()
        {
            AddDefaultConfigProperty(new ConfigProperty<HaulerModes>(HAULER_MODE, HaulerModes.CycleFleetMembers));
            AddDefaultConfigProperty(new ConfigProperty<int>(CYCLE_FLEET_DELAY, 0));
        }
    }
}
