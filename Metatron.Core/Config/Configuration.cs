using System.Collections.Generic;
using Metatron.Core.Interfaces;

namespace Metatron.Core.Config
{
    public sealed class Configuration : IConfiguration
    {
        public SocialConfiguration SocialConfig { get; private set; }
        public IDefensiveConfiguration DefenseConfig { get; private set; }
        public MissionConfiguration MissionConfig { get; private set; }
        public IMiningConfiguration MiningConfig { get; private set; }
        public IMovementConfiguration MovementConfig { get; private set; }
        public IMainConfiguration MainConfig { get; private set; }
        public FleetConfiguration FleetConfig { get; private set; }
        public ICargoConfiguration CargoConfig { get; private set; }
        public HaulingConfiguration HaulingConfig { get; private set; }
        public AlertConfiguration AlertConfig { get; private set; }
        public IRattingConfiguration RattingConfig { get; private set; }
        public ShipHubConfiguration ShipHubConfig { get; private set; }
        public IMaxRuntimeConfiguration MaxRuntimeConfig { get; private set; }
        public ISalvageConfiguration SalvageConfig { get; private set; }

        private Dictionary<string, ConfigProperty> _configProperties;
        public Dictionary<string, ConfigProperty> ConfigProperties
        {
            get { return _configProperties; }
            set
            {
                _configProperties = value;

                UpdateConfigurationProperties(_configProperties);
            }
        }

        public Configuration()
        {
            InitializeConfiguration(_configProperties);
        }

        private void InitializeConfiguration(Dictionary<string,ConfigProperty> configurationProperties)
        {
            SalvageConfig = new SalvageConfiguration(configurationProperties);
            MaxRuntimeConfig = new MaxRuntimeConfiguration(configurationProperties);
            ShipHubConfig = new ShipHubConfiguration(configurationProperties);
            RattingConfig = new RattingConfiguration(configurationProperties);
            AlertConfig = new AlertConfiguration(configurationProperties);
            HaulingConfig = new HaulingConfiguration(configurationProperties);
            CargoConfig = new CargoConfiguration(configurationProperties);
            FleetConfig = new FleetConfiguration(configurationProperties);
            MainConfig = new MainConfiguration(configurationProperties);
            MovementConfig = new MovementConfiguration(configurationProperties);
            MiningConfig = new MiningConfiguration(configurationProperties);
            MissionConfig = new MissionConfiguration(configurationProperties);
            DefenseConfig = new DefensiveConfiguration(configurationProperties);
            SocialConfig = new SocialConfiguration(configurationProperties);
        }

        private void UpdateConfigurationProperties(Dictionary<string, ConfigProperty> configurationProperties)
        {
            SalvageConfig.UpdateConfigurationProperties(configurationProperties);
            MaxRuntimeConfig.UpdateConfigurationProperties(configurationProperties);
            ShipHubConfig.UpdateConfigurationProperties(configurationProperties);
            RattingConfig.UpdateConfigurationProperties(configurationProperties);
            AlertConfig.UpdateConfigurationProperties(configurationProperties);
            HaulingConfig.UpdateConfigurationProperties(configurationProperties);
            CargoConfig.UpdateConfigurationProperties(configurationProperties);
            FleetConfig.UpdateConfigurationProperties(configurationProperties);
            MainConfig.UpdateConfigurationProperties(configurationProperties);
            MovementConfig.UpdateConfigurationProperties(configurationProperties);
            MiningConfig.UpdateConfigurationProperties(configurationProperties);
            MissionConfig.UpdateConfigurationProperties(configurationProperties);
            DefenseConfig.UpdateConfigurationProperties(configurationProperties);
            SocialConfig.UpdateConfigurationProperties(configurationProperties);
        }
    }
}












