using System.Collections.Generic;
using Metatron.Core.Interfaces;

namespace Metatron.Core.Config
{
    public sealed class DefensiveConfiguration : ConfigurationBase, IDefensiveConfiguration
    {
        static readonly string MINIMUM_SHIELD_PCT = "Defense_MinimumShieldPct",
                               MINIMUM_ARMOR_PCT = "Defense_MinimumArmorPct",
                               MINIMUM_CAP_PCT = "Defense_MinimumCapPct",
                               RESUME_SHIELD_PCT = "Defense_ResumeShieldPct",
                               RESUME_CAP_PCT = "Defense_ResumeCapPct",
                               RUN_ON_NON_WHITELIST = "Defense_RunOnNonWhitelistedPilot",
                               RUN_ON_BLACKLIST = "Defense_RunOnBlacklistedPilot",
                               RUN_ON_LOW_TANK = "Defense_RunOnLowTank",
                               RUN_ON_LOW_CAP = "Defense_RunOnLowCap",
                               RUN_IF_TARGET_JAMMED = "Defense_RunIfTargetJammed",
                               RUN_ON_LOW_AMMO = "Defense_RunOnLowAmmo",
                               PREFER_STATIONS_OVER_SAFES = "Defense_PreferStationsOverSafespots",
                               ALWAYS_SHIELD_BOOST = "Defense_AlwaysShieldBoost",
                               ALWAYS_RUN_TANK = "Defense_AlwaysRunTank",
                               RUN_ON_ME_TO_PILOT = "Defense_RunOnMeToPilot",
                               RUN_ON_CORP_TO_PILOT = "Defense_RunOnCorpToPilot",
                               RUN_ON_ME_TO_CORP = "Defense_RunOnMeToCorp",
                               RUN_ON_CORP_TO_CORP = "Defense_RunOnCorpToCorp",
                               RUN_ON_CORP_TO_ALLIANCE = "Defense_RunOnCorpToAlliance",
                               RUN_ON_ALLIANCE_TO_ALLIANCE = "Defense_RunOnAllianceToAlliance",
                               RUN_ON_LOW_DRONES = "Defense_RunOnLowDrones",
                               MINIMUM_NUM_DRONES = "Defense_MinimumNumDrones",
                               DISABLE_STANDINGS_CHECKS = "Defense_DisableStandingsChecks",
                               WAIT_AFTER_FLEEING = "Defense_WaitAfterFleeing",
                               MINUTES_TO_WAIT = "Defense_MinutesToWait";

        public int MinimumShieldPct
        {
            get { return GetConfigValue<int>(MINIMUM_SHIELD_PCT); }
            set { SetConfigValue(MINIMUM_SHIELD_PCT, value); }
        }
        public int MinimumArmorPct
        {
            get { return GetConfigValue<int>(MINIMUM_ARMOR_PCT); }
            set { SetConfigValue(MINIMUM_ARMOR_PCT, value); }
        }
        public int MinimumCapPct
        {
            get { return GetConfigValue<int>(MINIMUM_CAP_PCT); }
            set { SetConfigValue(MINIMUM_CAP_PCT, value); }
        }
        public int ResumeShieldPct
        {
            get { return GetConfigValue<int>(RESUME_SHIELD_PCT); }
            set { SetConfigValue(RESUME_SHIELD_PCT, value); }
        }
        public int ResumeCapPct
        {
            get { return GetConfigValue<int>(RESUME_CAP_PCT); }
            set { SetConfigValue(RESUME_CAP_PCT, value); }
        }

        public bool RunOnNonWhitelistedPilot
        {
            get { return GetConfigValue<bool>(RUN_ON_NON_WHITELIST); }
            set { SetConfigValue(RUN_ON_NON_WHITELIST, value); }
        }
        public bool RunOnBlacklistedPilot
        {
            get { return GetConfigValue<bool>(RUN_ON_BLACKLIST); }
            set { SetConfigValue(RUN_ON_BLACKLIST, value); }
        }
        public bool RunOnLowTank
        {
            get { return GetConfigValue<bool>(RUN_ON_LOW_TANK); }
            set { SetConfigValue(RUN_ON_LOW_TANK, value); }
        }
        public bool RunOnLowCap
        {
            get { return GetConfigValue<bool>(RUN_ON_LOW_CAP); }
            set { SetConfigValue(RUN_ON_LOW_CAP, value); }
        }
        public bool RunIfTargetJammed
        {
            get { return GetConfigValue<bool>(RUN_IF_TARGET_JAMMED); }
            set { SetConfigValue(RUN_IF_TARGET_JAMMED, value); }
        }
        public bool RunOnLowAmmo
        {
            get { return GetConfigValue<bool>(RUN_ON_LOW_AMMO); }
            set { SetConfigValue(RUN_ON_LOW_AMMO, value); }
        }

        //Misc options
        public bool PreferStationsOverSafespots
        {
            get { return GetConfigValue<bool>(PREFER_STATIONS_OVER_SAFES); }
            set { SetConfigValue(PREFER_STATIONS_OVER_SAFES, value); }
        }
        public bool AlwaysShieldBoost
        {
            get { return GetConfigValue<bool>(ALWAYS_SHIELD_BOOST); }
            set { SetConfigValue(ALWAYS_SHIELD_BOOST, value); }
        }
        public bool AlwaysRunTank
        {
            get { return GetConfigValue<bool>(ALWAYS_RUN_TANK); }
            set { SetConfigValue(ALWAYS_RUN_TANK, value); }
        }

        //Flee onstandings
        public bool RunOnMeToPilot
        {
            get { return GetConfigValue<bool>(RUN_ON_ME_TO_PILOT); }
            set { SetConfigValue(RUN_ON_ME_TO_PILOT, value); }
        }
        public bool RunOnCorpToPilot
        {
            get { return GetConfigValue<bool>(RUN_ON_CORP_TO_PILOT); }
            set { SetConfigValue(RUN_ON_CORP_TO_PILOT, value); }
        }
        public bool RunOnMeToCorp
        {
            get { return GetConfigValue<bool>(RUN_ON_ME_TO_CORP); }
            set { SetConfigValue(RUN_ON_ME_TO_CORP, value); }
        }
        public bool RunOnCorpToCorp
        {
            get { return GetConfigValue<bool>(RUN_ON_CORP_TO_CORP); }
            set { SetConfigValue(RUN_ON_CORP_TO_CORP, value); }
        }
        public bool RunOnCorpToAlliance
        {
            get { return GetConfigValue<bool>(RUN_ON_CORP_TO_ALLIANCE); }
            set { SetConfigValue(RUN_ON_CORP_TO_ALLIANCE, value); }
        }
        public bool RunOnAllianceToAlliance
        {
            get { return GetConfigValue<bool>(RUN_ON_ALLIANCE_TO_ALLIANCE); }
            set { SetConfigValue(RUN_ON_ALLIANCE_TO_ALLIANCE, value); }
        }

        //Min Drones
        public bool RunOnLowDrones
        {
            get { return GetConfigValue<bool>(RUN_ON_LOW_DRONES); }
            set { SetConfigValue(RUN_ON_LOW_DRONES, value); }
        }
        public int MinimumNumDrones
        {
            get { return GetConfigValue<int>(MINIMUM_NUM_DRONES); }
            set { SetConfigValue(MINIMUM_NUM_DRONES, value); }
        }

        public bool DisableStandingsChecks
        {
            get { return GetConfigValue<bool>(DISABLE_STANDINGS_CHECKS); }
            set { SetConfigValue(DISABLE_STANDINGS_CHECKS, value); }
        }

        public bool WaitAfterFleeing
        {
            get { return GetConfigValue<bool>(WAIT_AFTER_FLEEING); }
            set { SetConfigValue(WAIT_AFTER_FLEEING, value); }
        }
        public int MinutesToWait
        {
            get { return GetConfigValue<int>(MINUTES_TO_WAIT); }
            set { SetConfigValue(MINUTES_TO_WAIT, value); }
        }

        public DefensiveConfiguration(Dictionary<string, ConfigProperty> configProperties)
            : base(configProperties)
        {
            AddDefaultConfigProperties();
        }

        public override void AddDefaultConfigProperties()
        {
            //Min Tank Pcts
            AddDefaultConfigProperty(new ConfigProperty<int>(MINIMUM_ARMOR_PCT, 95));
            AddDefaultConfigProperty(new ConfigProperty<int>(MINIMUM_CAP_PCT, 25));
            AddDefaultConfigProperty(new ConfigProperty<int>(MINIMUM_NUM_DRONES, 0));
            AddDefaultConfigProperty(new ConfigProperty<int>(MINIMUM_SHIELD_PCT, 50));
            AddDefaultConfigProperty(new ConfigProperty<int>(RESUME_CAP_PCT, 75));
            AddDefaultConfigProperty(new ConfigProperty<int>(RESUME_SHIELD_PCT, 90));

            //misc bools
            AddDefaultConfigProperty(new ConfigProperty<bool>(PREFER_STATIONS_OVER_SAFES, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(ALWAYS_SHIELD_BOOST, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(ALWAYS_RUN_TANK, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(DISABLE_STANDINGS_CHECKS, false));

            //standard fleeing
            AddDefaultConfigProperty(new ConfigProperty<bool>(RUN_ON_NON_WHITELIST, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(RUN_ON_BLACKLIST, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(RUN_IF_TARGET_JAMMED, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(RUN_ON_LOW_TANK, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(RUN_ON_LOW_CAP, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(RUN_ON_LOW_AMMO, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(RUN_ON_LOW_DRONES, false));

            //standings fleeing
            AddDefaultConfigProperty(new ConfigProperty<bool>(RUN_ON_ME_TO_PILOT, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(RUN_ON_ME_TO_CORP, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(RUN_ON_CORP_TO_PILOT, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(RUN_ON_CORP_TO_CORP, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(RUN_ON_CORP_TO_ALLIANCE, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(RUN_ON_ALLIANCE_TO_ALLIANCE, false));

            //waiting after fleeing
            AddDefaultConfigProperty(new ConfigProperty<bool>(WAIT_AFTER_FLEEING, true));
            AddDefaultConfigProperty(new ConfigProperty<int>(MINUTES_TO_WAIT, 5));
        }
    }
}