using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Metatron.Core.Config
{
    public class ShipHubConfiguration : ConfigurationBase
    {
        private static readonly string LOCATION = "ShipHub_Location";

        public Location HubLocation
        {
            get
            {
                var configValue = GetConfigValue<Location>(LOCATION);
                if (configValue == null)
                {
                    configValue = Location.CreateDefault();
                    HubLocation = configValue;
                }

                return configValue;
            }
            set { SetConfigValue(LOCATION, value); }
        }

        public ShipHubConfiguration(Dictionary<string, ConfigProperty> configProperties)
            : base(configProperties)
        {
            AddDefaultConfigProperties();
        }

        public override void AddDefaultConfigProperties()
        {
            AddDefaultConfigProperty(new ConfigProperty<Location>(LOCATION, Location.CreateDefault()));
        }
    }
}
