using System.Collections.Generic;

namespace Metatron.Core.Config
{
    /// <summary>
    /// Hold all fleet-related variables
    /// </summary>
    public sealed class FleetConfiguration : ConfigurationBase
    {
        static readonly string BUDDY_CHARIDS_TO_INVITE = "Fleet_BuddyCharIDsToInvite",
                               DO_FLEET_INVITES = "Fleet_DoFleetInvites",
                               FLEET_CHARIDS_TO_SKIP = "Fleet_FleetCharIDsToSkip",
                               ONLY_HAUL_FOR_SKIP_LIST = "Fleet_OnlyHaulForSkipList";

        public List<long> BuddyCharIDsToInvite
        {
            get
            {
                return GetConfigValue<List<long>>(BUDDY_CHARIDS_TO_INVITE);
            }
            set { SetConfigValue(BUDDY_CHARIDS_TO_INVITE, value); }
        }

        public bool DoFleetInvites
        {
            get { return GetConfigValue<bool>(DO_FLEET_INVITES); }
            set { SetConfigValue(DO_FLEET_INVITES, value); }
        }

        public List<long> FleetCharIDsToSkip
        {
            get
            {
                return GetConfigValue<List<long>>(FLEET_CHARIDS_TO_SKIP);
            }
            set { SetConfigValue(FLEET_CHARIDS_TO_SKIP, value); }
        }

        public bool OnlyHaulForSkipList
        {
            get { return GetConfigValue<bool>(ONLY_HAUL_FOR_SKIP_LIST); }
            set { SetConfigValue(ONLY_HAUL_FOR_SKIP_LIST, value); }
        }

        public FleetConfiguration(Dictionary<string, ConfigProperty> configProperties)
            : base(configProperties)
        {
            AddDefaultConfigProperties();
        }

        public override void AddDefaultConfigProperties()
        {
            AddDefaultConfigProperty(new ConfigProperty<List<long>>(BUDDY_CHARIDS_TO_INVITE, new List<long>()));
            AddDefaultConfigProperty(new ConfigProperty<bool>(DO_FLEET_INVITES, false));
            AddDefaultConfigProperty(new ConfigProperty<List<long>>(FLEET_CHARIDS_TO_SKIP, new List<long>()));
            AddDefaultConfigProperty(new ConfigProperty<bool>(ONLY_HAUL_FOR_SKIP_LIST, false));
        }
    }
}