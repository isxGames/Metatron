using System.Collections.Generic;

namespace Metatron.Core.Config
{
    public interface ICargoConfiguration : IConfigurationBase
    {
        Location DropoffLocation { get; set; }
        Location PickupLocation { get; set; }
        string PickupSystemBookmark { get; set; }
        int CargoFullThreshold { get; set; }
        bool AlwaysPopCans { get; set; }
        string CanNameFormat { get; set; }
    }

    public sealed class CargoConfiguration : ConfigurationBase, ICargoConfiguration
    {
        private static readonly string DROPOFF_LOCATION = "Cargo_DropoffLocation",
                                       PICKUP_LOCATION = "Cargo_PickupLocation",
                                       PICKUP_SYSTEM_BOOKMARK = "Cargo_PickupSystemBookmark",
                                       CARGO_FULL_THRESHOLD = "Cargo_CargoFullThreshold",
                                       ALWAYS_POP_CANS = "Cargo_AlwaysPopCans",
                                       CAN_NAME_FORMAT = "Cargo_CanNameFormat";

        public Location DropoffLocation
        {
            get
            {
                var configValue = GetConfigValue<Location>(DROPOFF_LOCATION);
                if (configValue == null)
                {
                    configValue = Location.CreateDefault();
                    DropoffLocation = configValue;
                }

                return configValue;
            }
            set { SetConfigValue(DROPOFF_LOCATION, value); }
        }

        public Location PickupLocation
        {
            get
            {
                var configValue = GetConfigValue<Location>(PICKUP_LOCATION);
                if (configValue == null)
                {
                    configValue = Location.CreateDefault();
                    PickupLocation = configValue;
                }

                return configValue;
            }
            set { SetConfigValue(PICKUP_LOCATION, value); }
        }

        public string PickupSystemBookmark
        {
            get { return GetConfigValue<string>(PICKUP_SYSTEM_BOOKMARK); }
            set { SetConfigValue(PICKUP_SYSTEM_BOOKMARK, value); }
        }

        public int CargoFullThreshold
        {
            get { return GetConfigValue<int>(CARGO_FULL_THRESHOLD); }
            set { SetConfigValue(CARGO_FULL_THRESHOLD, value); }
        }

        public bool AlwaysPopCans
        {
            get { return GetConfigValue<bool>(ALWAYS_POP_CANS); }
            set { SetConfigValue(ALWAYS_POP_CANS, value); }
        }

        public string CanNameFormat
        {
            get { return GetConfigValue<string>(CAN_NAME_FORMAT); }
            set { SetConfigValue(CAN_NAME_FORMAT, value); }
        }

        public CargoConfiguration(Dictionary<string, ConfigProperty> configProperties)
            : base(configProperties)
        {
            AddDefaultConfigProperties();
        }

        public override void AddDefaultConfigProperties()
        {
            AddDefaultConfigProperty(new ConfigProperty<Location>(DROPOFF_LOCATION, Location.CreateDefault()));
            AddDefaultConfigProperty(new ConfigProperty<Location>(PICKUP_LOCATION, Location.CreateDefault()));
            AddDefaultConfigProperty(new ConfigProperty<string>(PICKUP_SYSTEM_BOOKMARK, string.Empty));
            AddDefaultConfigProperty(new ConfigProperty<int>(CARGO_FULL_THRESHOLD, 0));
            AddDefaultConfigProperty(new ConfigProperty<bool>(ALWAYS_POP_CANS, true));
            AddDefaultConfigProperty(new ConfigProperty<string>(CAN_NAME_FORMAT, "CORP HH:MM FULL"));
        }
    }
}