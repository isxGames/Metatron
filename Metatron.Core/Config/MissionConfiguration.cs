using System.Collections.Generic;

namespace Metatron.Core.Config
{
    public interface IMissionConfiguration
    {
        bool RunCourierMissions { get; set; }
        bool RunTradeMissions { get; set; }
        bool RunMiningMissions { get; set; }
        bool RunEncounterMissions { get; set; }
        bool AvoidLowSec { get; set; }
        List<string> Agents { get; set; }
        List<string> ResearchAgents { get; set; }
        bool DoOreMiningMissions { get; set; }
        bool DoIceMiningMissions { get; set; }
        bool DoGasMiningMissions { get; set; }
        bool DoEmpireKillMissions { get; set; }
        bool DoPirateKillMissions { get; set; }
        bool DoStorylineMissions { get; set; }
        bool DoChainCouriers { get; set; }
        bool IgnoreMissionDeclineTimer { get; set; }
        List<string> MissionBlacklist { get; set; }
    }

    public sealed class MissionConfiguration : ConfigurationBase, IMissionConfiguration
    {
// ReSharper disable ConvertToConstant.Local
        private static readonly string RUN_COURIER_MISSIONS = "Mission_RunCourierMissions",
                                       RUN_TRADE_MISSIONS = "Mission_RunTradeMissions",
                                       RUN_MINING_MISSIONS = "Mission_RunMiningMissions",
                                       RUN_ENCOUNTER_MISSIONS = "Mission_RunEncounterMissions",
                                       AVOID_LOW_SEC = "Mission_AvoidLowSec",
                                       AGENTS = "Mission_Agents",
                                       RESEARCH_AGENTS = "Mission_ResearchAgents",
                                       DO_GAS_MINING_MISSIONS = "Mission_Mission_DoGasMiningMissions",
                                       DO_ORE_MINING_MISSIONS = "Mission_DoOreMiningMissions",
                                       DO_ICE_MINING_MISSIONS = "Mission_DoIceMiningMissions",
                                       MISSION_BLACKLIST = "Mission_Mission Blacklist",
                                       DO_EMPIRE_KILL_MISSIONS = "Mission_DoEmpireKillMissions",
                                       DO_PIRATE_KILL_MISSIONS = "Mission_DoPirateKillMissions",
                                       DO_STORYLINE_MISSIONS = "Mission_DoStorylineMissions",
                                       DO_CHAIN_COURIERS = "Mission_DoChainCouriers",
                                       IGNORE_MISSION_DECLINE_TIMER = "Mission_IgnoreMissionDeclineTimer";
// ReSharper restore ConvertToConstant.Local

        #region Mission Types to Run
        public bool RunCourierMissions
        {
            get { return GetConfigValue<bool>(RUN_COURIER_MISSIONS); }
            set { SetConfigValue(RUN_COURIER_MISSIONS, value); }
        }
        public bool RunTradeMissions
        {
            get { return GetConfigValue<bool>(RUN_TRADE_MISSIONS); }
            set { SetConfigValue(RUN_TRADE_MISSIONS, value); }
        }
        public bool RunMiningMissions
        {
            get { return GetConfigValue<bool>(RUN_MINING_MISSIONS); }
            set { SetConfigValue(RUN_MINING_MISSIONS, value); }
        }
        public bool RunEncounterMissions
        {
            get { return GetConfigValue<bool>(RUN_ENCOUNTER_MISSIONS); }
            set { SetConfigValue(RUN_ENCOUNTER_MISSIONS, value); }
        }
        #endregion

        public bool AvoidLowSec
        {
            get { return GetConfigValue<bool>(AVOID_LOW_SEC); }
            set { SetConfigValue(AVOID_LOW_SEC, value); }
        }

        public List<string> Agents
        {
            get
            {
                return GetConfigValue<List<string>>(AGENTS);
            }
            set { SetConfigValue(AGENTS, value); }
        }

        public List<string> ResearchAgents
        {
            get
            {
                return GetConfigValue<List<string>>(RESEARCH_AGENTS) ?? new List<string>();
            }
            set { SetConfigValue(RESEARCH_AGENTS, value); }
        }

        public bool DoOreMiningMissions
        {
            get { return GetConfigValue<bool>(DO_ORE_MINING_MISSIONS); }
            set { SetConfigValue(DO_ORE_MINING_MISSIONS, value); }
        }
        public bool DoIceMiningMissions
        {
            get { return GetConfigValue<bool>(DO_ICE_MINING_MISSIONS); }
            set { SetConfigValue(DO_ICE_MINING_MISSIONS, value); }
        }
        public bool DoGasMiningMissions
        {
            get { return GetConfigValue<bool>(DO_GAS_MINING_MISSIONS); }
            set { SetConfigValue(DO_GAS_MINING_MISSIONS, value); }
        }

        public bool DoEmpireKillMissions
        {
            get { return GetConfigValue<bool>(DO_EMPIRE_KILL_MISSIONS); }
            set { SetConfigValue(DO_EMPIRE_KILL_MISSIONS, value); }
        }
        public bool DoPirateKillMissions
        {
            get { return GetConfigValue<bool>(DO_PIRATE_KILL_MISSIONS); }
            set { SetConfigValue(DO_PIRATE_KILL_MISSIONS, value); }
        }

        public bool DoStorylineMissions
        {
            get { return GetConfigValue<bool>(DO_STORYLINE_MISSIONS); }
            set { SetConfigValue(DO_STORYLINE_MISSIONS, value); }
        }

        public bool DoChainCouriers
        {
            get { return GetConfigValue<bool>(DO_CHAIN_COURIERS); }
            set { SetConfigValue(DO_CHAIN_COURIERS, value); }
        }

        public bool IgnoreMissionDeclineTimer
        {
            get { return GetConfigValue<bool>(IGNORE_MISSION_DECLINE_TIMER); }
            set { SetConfigValue(IGNORE_MISSION_DECLINE_TIMER, value); }
        }

        public List<string> MissionBlacklist
        {
            get
            {
                return GetConfigValue<List<string>>(MISSION_BLACKLIST);
            }
            set { SetConfigValue(MISSION_BLACKLIST, value); }
        } 


        public MissionConfiguration(Dictionary<string, ConfigProperty> configProperties)
            : base(configProperties)
        {
            AddDefaultConfigProperties();
        }

        public override void AddDefaultConfigProperties()
        {
            AddDefaultConfigProperty(new ConfigProperty<bool>(AVOID_LOW_SEC, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(RUN_ENCOUNTER_MISSIONS, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(RUN_COURIER_MISSIONS, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(RUN_TRADE_MISSIONS, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(RUN_MINING_MISSIONS, false));

            AddDefaultConfigProperty(new ConfigProperty<List<string>>(AGENTS, new List<string>()));
            AddDefaultConfigProperty(new ConfigProperty<List<string>>(RESEARCH_AGENTS, new List<string>()));

            AddDefaultConfigProperty(new ConfigProperty<bool>(DO_ORE_MINING_MISSIONS, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(DO_ICE_MINING_MISSIONS, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(DO_GAS_MINING_MISSIONS, false));

            AddDefaultConfigProperty(new ConfigProperty<List<string>>(MISSION_BLACKLIST, new List<string>()));

            AddDefaultConfigProperty(new ConfigProperty<bool>(DO_EMPIRE_KILL_MISSIONS, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(DO_PIRATE_KILL_MISSIONS, true));

            AddDefaultConfigProperty(new ConfigProperty<bool>(DO_STORYLINE_MISSIONS, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(DO_CHAIN_COURIERS, true));

            AddDefaultConfigProperty(new ConfigProperty<bool>(IGNORE_MISSION_DECLINE_TIMER, false));
        }
    }
}