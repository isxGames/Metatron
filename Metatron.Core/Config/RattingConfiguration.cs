using System;
using System.Collections.Generic;
using Metatron.Core.Collections.Generic;

namespace Metatron.Core.Config
{
    public interface IRattingConfiguration : IConfigurationBase
    {
        bool ChainBelts { get; set; }
        bool OnlyChainWhenAlone { get; set; }
        long MinimumChainBounty { get; set; }
        bool IsAnomalyMode { get; set; }
        List<Pair<string, bool>> StatusByAnomalyType { get; set; }
        int WarpInDistance { get; set; }
    }

    public sealed class RattingConfiguration : ConfigurationBase, IRattingConfiguration
    {
        private static readonly string CHAIN_BELTS = "Ratting_ChainBelts",
                                       ONLY_CHAIN_WHEN_ALONE = "Ratting_OnlyChainWhenAlone",
                                       MINIMUM_CHAIN_BOUNTY = "Ratting_MinimumChainBounty",
                                       IS_ANOMALY_MODE = "Ratting_IsAnomalyMode",
                                       StatusByAnomalyTypeTag = "Ratting_StatusByAnomalyType",
                                       WarpInDistanceTag = "Ratting_WarpInDistance";

        public bool ChainBelts
        {
            get { return GetConfigValue<bool>(CHAIN_BELTS); }
            set { SetConfigValue(CHAIN_BELTS, value); }
        }
        public bool OnlyChainWhenAlone
        {
            get { return GetConfigValue<bool>(ONLY_CHAIN_WHEN_ALONE); }
            set { SetConfigValue(ONLY_CHAIN_WHEN_ALONE, value); }
        }
        public long MinimumChainBounty
        {
            get { return GetConfigValue<long>(MINIMUM_CHAIN_BOUNTY); }
            set { SetConfigValue(MINIMUM_CHAIN_BOUNTY, value); }
        }
        public bool IsAnomalyMode
        {
            get { return GetConfigValue<bool>(IS_ANOMALY_MODE); }
            set { SetConfigValue(IS_ANOMALY_MODE, value); }
        }

        public List<Pair<string, bool>> StatusByAnomalyType
        {
            get { return GetConfigValue<List<Pair<string, bool>>>(StatusByAnomalyTypeTag); }
            set { SetConfigValue(StatusByAnomalyTypeTag, value); }
        }

        public RattingConfiguration(Dictionary<string, ConfigProperty> configProperties)
            : base(configProperties)
        {
            AddDefaultConfigProperties();
        }

        public int WarpInDistance
        {
            get { return GetConfigValue<int>(WarpInDistanceTag); }
            set { SetConfigValue(WarpInDistanceTag, value); }
        }

        public override void AddDefaultConfigProperties()
        {
            AddDefaultConfigProperty(new ConfigProperty<bool>(CHAIN_BELTS, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(ONLY_CHAIN_WHEN_ALONE, false));
            AddDefaultConfigProperty(new ConfigProperty<long>(MINIMUM_CHAIN_BOUNTY, 2000000));
            AddDefaultConfigProperty(new ConfigProperty<bool>(IS_ANOMALY_MODE, false));
            AddDefaultConfigProperty(new ConfigProperty<int>(WarpInDistanceTag, 30));

            var statusByAnomalyType = new List<Pair<string, bool>>
                {
                    new Pair<string, bool>("Hideaway", false),
                    new Pair<string, bool>("Hidden Hideaway", false),
                    new Pair<string, bool>("Forsaken Hideaway", false),
                    new Pair<string, bool>("Forlorn Hideaway", false),
                    new Pair<string, bool>("Cluster", false),
                    new Pair<string, bool>("Burrow", false),
                    new Pair<string, bool>("Collection", false),
                    new Pair<string, bool>("Refuge", false),
                    new Pair<string, bool>("Assembly", false),
                    new Pair<string, bool>("Den", false),
                    new Pair<string, bool>("Hidden Den", false),
                    new Pair<string, bool>("Forsaken Den", false),
                    new Pair<string, bool>("Forlorn Den", false),
                    new Pair<string, bool>("Gathering", false),
                    new Pair<string, bool>("Yard", false),
                    new Pair<string, bool>("Surveillance", false),
                    new Pair<string, bool>("Rally Point", false),
                    new Pair<string, bool>("Hidden Rally Point", false),
                    new Pair<string, bool>("Forsaken Rally Point", false),
                    new Pair<string, bool>("Forlorn Rally Point", false),
                    new Pair<string, bool>("Menagerie", false),
                    new Pair<string, bool>("Port", false),
                    new Pair<string, bool>("Herd", false),
                    new Pair<string, bool>("Hub", false),
                    new Pair<string, bool>("Hidden Hub", false),
                    new Pair<string, bool>("Forsaken Hub", false),
                    new Pair<string, bool>("Forlorn Hub", false),
                    new Pair<string, bool>("Squad", false),
                    new Pair<string, bool>("Haven", false),
                    new Pair<string, bool>("Patrol", false),
                    new Pair<string, bool>("Sanctum", false),
                    new Pair<string, bool>("Horde", false)
                };
            AddDefaultConfigProperty(new ConfigProperty<List<Pair<string, bool>>>(StatusByAnomalyTypeTag, statusByAnomalyType));
        }
    }
}