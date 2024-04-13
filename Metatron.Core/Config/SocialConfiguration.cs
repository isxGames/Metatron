using System.Collections.Generic;

namespace Metatron.Core.Config
{
    public sealed class SocialConfiguration : ConfigurationBase
    {
        static readonly string MINIMUM_PILOT_STANDING = "Social_MinimumPilotStanding",
                               MINIMUM_CORP_STANDING = "Social_MinimumCorpStanding",
                               MINIMUM_ALLIANCE_STANDING = "Social_MinimumAllianceStanding",
                               PILOT_WHITELIST = "Social_PilotWhitelist",
                               CORP_WHITELIST = "Social_CorpWhitelist",
                               ALLIANCE_WHITELIST = "Social_AllianceWhitelist",
                               PILOT_BLACKLIST = "Social_PilotBlacklist",
                               CORP_BLACKLIST = "Social_CorpBlacklist",
                               ALLIANCE_BLACKLIST = "Social_AllianceBlacklist",
                               USE_CHAT_READING = "Social_UseChatReading";

        public SocialConfiguration(Dictionary<string, ConfigProperty> configProperties)
            : base(configProperties)
        {
            AddDefaultConfigProperties();
        }

        //Minimum standings
        public int MinimumPilotStanding
        {
            get { return GetConfigValue<int>(MINIMUM_PILOT_STANDING); }
            set { SetConfigValue(MINIMUM_PILOT_STANDING, value); }
        }
        public int MinimumCorpStanding
        {
            get { return GetConfigValue<int>(MINIMUM_CORP_STANDING); }
            set { SetConfigValue(MINIMUM_CORP_STANDING, value); }
        }
        public int MinimumAllianceStanding
        {
            get { return GetConfigValue<int>(MINIMUM_ALLIANCE_STANDING); }
            set { SetConfigValue(MINIMUM_ALLIANCE_STANDING, value); }
        }

        //Whitelists and blacklists
        public List<string> PilotWhitelist
        {
            get
            {
                return GetConfigValue<List<string>>(PILOT_WHITELIST);
            }
            set { SetConfigValue(PILOT_WHITELIST, value); }
        }

        public List<string> CorpWhitelist
        {
            get
            {
                return GetConfigValue<List<string>>(CORP_WHITELIST);
            }
            set { SetConfigValue(CORP_WHITELIST, value); }
        }

        public List<string> AllianceWhitelist
        {
            get
            {
                return GetConfigValue<List<string>>(ALLIANCE_WHITELIST);
            }
            set { SetConfigValue(ALLIANCE_WHITELIST, value); }
        }

        public List<string> PilotBlacklist
        {
            get
            {
                return GetConfigValue<List<string>>(PILOT_BLACKLIST);
            }
            set { SetConfigValue(PILOT_BLACKLIST, value); }
        }

        public List<string> CorpBlacklist
        {
            get
            {
                return GetConfigValue<List<string>>(CORP_BLACKLIST);
            }
            set { SetConfigValue(CORP_BLACKLIST, value); }
        }
        public List<string> AllianceBlacklist
        {
            get
            {
                return GetConfigValue<List<string>>(ALLIANCE_BLACKLIST);
            }
            set { SetConfigValue(ALLIANCE_BLACKLIST, value); }
        }

        public bool UseChatReading
        {
            get { return GetConfigValue<bool>(USE_CHAT_READING); }
            set { SetConfigValue(USE_CHAT_READING, value); }
        }

        public override void AddDefaultConfigProperties()
        {
            AddDefaultConfigProperty(new ConfigProperty<int>(MINIMUM_PILOT_STANDING, 0));
            AddDefaultConfigProperty(new ConfigProperty<int>(MINIMUM_CORP_STANDING, 0));
            AddDefaultConfigProperty(new ConfigProperty<int>(MINIMUM_ALLIANCE_STANDING, 0));
            AddDefaultConfigProperty(new ConfigProperty<List<string>>(PILOT_WHITELIST, new List<string>()));
            AddDefaultConfigProperty(new ConfigProperty<List<string>>(PILOT_BLACKLIST, new List<string>()));
            AddDefaultConfigProperty(new ConfigProperty<List<string>>(CORP_WHITELIST, new List<string>()));
            AddDefaultConfigProperty(new ConfigProperty<List<string>>(CORP_BLACKLIST, new List<string>()));
            AddDefaultConfigProperty(new ConfigProperty<List<string>>(ALLIANCE_WHITELIST, new List<string>()));
            AddDefaultConfigProperty(new ConfigProperty<List<string>>(ALLIANCE_BLACKLIST, new List<string>()));
            AddDefaultConfigProperty(new ConfigProperty<bool>(USE_CHAT_READING, false));
        }
    }
}