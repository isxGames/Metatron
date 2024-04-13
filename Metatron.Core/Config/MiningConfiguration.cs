using System.Collections.Generic;
using System.Linq;

namespace Metatron.Core.Config
{
    public interface IMiningConfiguration : IConfigurationBase
    {
        Dictionary<string, bool> StatusByOre { get; set; }
        List<string> PriorityByOreType { get; set; }
        Dictionary<string, bool> StatusByIce { get; set; }
        List<string> PriorityByIceType { get; set; }
        bool DistributeLasers { get; set; }
        bool IsIceMining { get; set; }
        bool StripMine { get; set; }
        bool ShortCycle { get; set; }
        int NumCrystalsToCarry { get; set; }
        int MinDistanceToPlayers { get; set; }
        bool UseMiningDrones { get; set; }
        string BoostOrcaBoostLocationLabel { get; set; }
    }

    public sealed class MiningConfiguration : ConfigurationBase, IMiningConfiguration
    {
        static readonly string DISTRIBUTE_LASERS = "Mining_DistributeLasers",
                               ICE_MINING = "Mining_IceMining",
                               STRIP_MINE = "Mining_StripMine",
                               ORE_DO_MINE = "Mining_Ore_DoMine",
                               ORE_PRIORITY = "Mining_Ore_Priority",
                               ICE_DO_MINE = "Mining_Ice_DoMine",
                               ICE_PRIORITY = "Mining_Ice_Priority",
                               SHORT_CYCLE = "Mining_ShortCycle",
                               DELAY_ACTIVATION = "Mining_DelayActivation",
                               NUM_CRYSTALS_TO_CARRY = "Mining_NumCrystalsToCarry",
                               MIN_DISTANCE_TO_PLAYERS = "Mining_MinDistanceToPlayers",
                               USE_MINING_DRONES = "Mining_UseMiningDrones",
                               BoostOrcaBoostLocationLabelTag = "Mining_BoostOrcaBoostLocationLabel";

        #region Ore and Ice Selection
        //Long fucking list of types of ore and ice to mine
        public Dictionary<string, bool> StatusByOre
        {
            get { return GetConfigValue<Dictionary<string, bool>>(ORE_DO_MINE); }
            set { SetConfigValue(ORE_DO_MINE, value); }
        }

        public List<string> PriorityByOreType
        {
            get { return GetConfigValue<List<string>>(ORE_PRIORITY); }
            set { SetConfigValue(ORE_PRIORITY, value); }
        }

        //Now for ice
        public Dictionary<string, bool> StatusByIce
        {
            get { return GetConfigValue<Dictionary<string, bool>>(ICE_DO_MINE); }
            set { SetConfigValue(ICE_DO_MINE, value); }
        }

        public List<string> PriorityByIceType
        {
            get { return GetConfigValue<List<string>>(ICE_PRIORITY); }
            set { SetConfigValue(ICE_PRIORITY, value); }
        }
        #endregion

        #region Mining Options
        public bool DistributeLasers
        {
            get { return GetConfigValue<bool>(DISTRIBUTE_LASERS); }
            set { SetConfigValue(DISTRIBUTE_LASERS, value); }
        }

        public bool IsIceMining
        {
            get { return GetConfigValue<bool>(ICE_MINING); }
            set { SetConfigValue(ICE_MINING, value); }
        }

        public bool StripMine
        {
            get { return GetConfigValue<bool>(STRIP_MINE); }
            set { SetConfigValue(STRIP_MINE, value); }
        }

        public bool ShortCycle
        {
            get { return GetConfigValue<bool>(SHORT_CYCLE); }
            set { SetConfigValue(SHORT_CYCLE, value); }
        }

        public int NumCrystalsToCarry
        {
            get { return GetConfigValue<int>(NUM_CRYSTALS_TO_CARRY); }
            set { SetConfigValue(NUM_CRYSTALS_TO_CARRY, value); }
        }

        public int MinDistanceToPlayers
        {
            get { return GetConfigValue<int>(MIN_DISTANCE_TO_PLAYERS); }
            set { SetConfigValue(MIN_DISTANCE_TO_PLAYERS, value); }
        }

        public bool UseMiningDrones
        {
            get { return GetConfigValue<bool>(USE_MINING_DRONES); }
            set { SetConfigValue(USE_MINING_DRONES, value); }
        }
        #endregion

        public string BoostOrcaBoostLocationLabel
        {
            get { return GetConfigValue<string>(BoostOrcaBoostLocationLabelTag); }
            set { SetConfigValue(BoostOrcaBoostLocationLabelTag, value); }
        }

        public MiningConfiguration(Dictionary<string, ConfigProperty> configProperties)
            : base(configProperties)
        {
            AddDefaultConfigProperties();
        }

        public override void AddDefaultConfigProperties()
        {
            var ore_doMine = new Dictionary<string, bool>
                                 {
                                     {"Vitreous Mercoxit", false},
                                     {"Magma Mercoxit", false},
                                     {"Mercoxit", false},
                                     {"Prime Arkonor", true},
                                     {"Crimson Arkonor", true},
                                     {"Arkonor", true},
                                     {"Monoclinic Bistot", true},
                                     {"Triclinic Bistot", true},
                                     {"Bistot", true},
                                     {"Crystalline Crokite", true},
                                     {"Sharp Crokite", true},
                                     {"Crokite", true},
                                     {"Gleaming Spodumain", true},
                                     {"Bright Spodumain", true},
                                     {"Spodumain", true},
                                     {"Obsidian Ochre", true},
                                     {"Onyx Ochre", true},
                                     {"Dark Ochre", true},
                                     {"Prismatic Gneiss", true},
                                     {"Iridescent Gneiss", true},
                                     {"Gneiss", true},
                                     {"Glazed Hedbergite", true},
                                     {"Vitric Hedbergite", true},
                                     {"Hedbergite", true},
                                     {"Radiant Hemorphite", true},
                                     {"Vivid Hemorphite", true},
                                     {"Hemorphite", true},
                                     {"Pristine Jaspet", true},
                                     {"Pure Jaspet", true},
                                     {"Jaspet", true},
                                     {"Fiery Kernite", true},
                                     {"Luminous Kernite", true},
                                     {"Kernite", true},
                                     {"Golden Omber", true},
                                     {"Silvery Omber", true},
                                     {"Omber", true},
                                     {"Rich Plagioclase", true},
                                     {"Azure Plagioclase", true},
                                     {"Plagioclase", true},
                                     {"Viscous Pyroxeres", true},
                                     {"Solid Pyroxeres", true},
                                     {"Pyroxeres", true},
                                     {"Massive Scordite", true},
                                     {"Condensed Scordite", true},
                                     {"Scordite", true},
                                     {"Dense Veldspar", true},
                                     {"Concentrated Veldspar", true},
                                     {"Veldspar", true}
                                 };
            //TOO MUCH ORE
            AddDefaultConfigProperty(new ConfigProperty<Dictionary<string, bool>>(ORE_DO_MINE, ore_doMine));

            var ore_priority = ore_doMine.Keys.ToList();
            AddDefaultConfigProperty(new ConfigProperty<List<string>>(ORE_PRIORITY, ore_priority));

            var ice_doMine = new Dictionary<string, bool>
                                 {
                                     {"Krystallos", true},
                                     {"Gelidus", true},
                                     {"Dark Glitter", true},
                                     {"Glare Crust", true},
                                     {"Enriched Clear Icicle", true},
                                     {"Clear Icicle", true},
                                     {"Thick Blue Ice", true},
                                     {"Blue Ice", true},
                                     {"Smooth Glacial Mass", true},
                                     {"Glacial Mass", true},
                                     {"Pristine White Glaze", true},
                                     {"White Glaze", true}
                                 };
            AddDefaultConfigProperty(new ConfigProperty<Dictionary<string, bool>>(ICE_DO_MINE, ice_doMine));

            var ice_priority = ice_doMine.Keys.ToList();
            AddDefaultConfigProperty(new ConfigProperty<List<string>>(ICE_PRIORITY, ice_priority));

            AddDefaultConfigProperty(new ConfigProperty<bool>(SHORT_CYCLE, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(DELAY_ACTIVATION, false));
            AddDefaultConfigProperty(new ConfigProperty<int>(NUM_CRYSTALS_TO_CARRY, 4));
            AddDefaultConfigProperty(new ConfigProperty<int>(MIN_DISTANCE_TO_PLAYERS, -1));
            AddDefaultConfigProperty(new ConfigProperty<bool>(USE_MINING_DRONES, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(DISTRIBUTE_LASERS, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(ICE_MINING, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(STRIP_MINE, true));
            AddDefaultConfigProperty(new ConfigProperty<string>(BoostOrcaBoostLocationLabelTag, null));
        }
    }
}