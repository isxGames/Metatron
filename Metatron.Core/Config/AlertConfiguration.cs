using System.Collections.Generic;

namespace Metatron.Core.Config
{
    /// <summary>
    /// Hold any alert-related configuration variables
    /// </summary>
    public sealed class AlertConfiguration : ConfigurationBase
    {
        static readonly string USE_ALERTS = "Alert_UseAlerts",
                               ALERT_ON_LOCAL_UNSAFE = "Alert_AlertOnLocalUnsafe",
                               ALERT_ON_LOCAL_CHAT = "Alert_AlertOnLocalChat",
                               ALERT_ON_FACTION_SPAWN = "Alert_AlertOnFactionSpawn",
                               ALERT_ON_LOW_AMMO = "Alert_AlertOnLowAmmo",
                               ALERT_ON_FREIGHTER_NO_PICKUP = "Alert_AlertOnFreighterNoPickup",
                               ALERT_ON_LONG_RANDOM_WAIT = "Alert_AlertOnLongRandomWait",
                               ALERT_ON_PLAYER_NEAR = "Alert_AlertOnPlayerNear",
                               ALERT_ON_FLEE = "Alert_AlertOnFlee",
                               ALERT_ON_TARGET_JAMMED = "Alert_AlertOnTargetJammed",
                               ALERT_ON_WARP_JAMMED = "Alert_AlertOnWarpJammed";

        public bool UseAlerts
        {
            get { return GetConfigValue<bool>(USE_ALERTS); }
            set { SetConfigValue(USE_ALERTS, value); }
        }

        public bool AlertOnLocalUnsafe
        {
            get { return GetConfigValue<bool>(ALERT_ON_LOCAL_UNSAFE); }
            set { SetConfigValue(ALERT_ON_LOCAL_UNSAFE, value); }
        }
        public bool AlertOnLocalChat
        {
            get { return GetConfigValue<bool>(ALERT_ON_LOCAL_CHAT); }
            set { SetConfigValue(ALERT_ON_LOCAL_CHAT, value); }
        }
        public bool AlertOnFactionSpawn
        {
            get { return GetConfigValue<bool>(ALERT_ON_FACTION_SPAWN); }
            set { SetConfigValue(ALERT_ON_FACTION_SPAWN, value); }
        }
        public bool AlertOnLowAmmo
        {
            get { return GetConfigValue<bool>(ALERT_ON_LOW_AMMO); }
            set { SetConfigValue(ALERT_ON_LOW_AMMO, value); }
        }
        public bool AlertOnFreighterNoPickup
        {
            get { return GetConfigValue<bool>(ALERT_ON_FREIGHTER_NO_PICKUP); }
            set { SetConfigValue(ALERT_ON_FREIGHTER_NO_PICKUP, value); }
        }
        public bool AlertOnLongRandomWait
        {
            get { return GetConfigValue<bool>(ALERT_ON_LONG_RANDOM_WAIT); }
            set { SetConfigValue(ALERT_ON_LONG_RANDOM_WAIT, value); }
        }
        public bool AlertOnPlayerNear
        {
            get { return GetConfigValue<bool>(ALERT_ON_PLAYER_NEAR); }
            set { SetConfigValue(ALERT_ON_PLAYER_NEAR, value); }
        }
        public bool AlertOnFlee
        {
            get { return GetConfigValue<bool>(ALERT_ON_FLEE); }
            set { SetConfigValue(ALERT_ON_FLEE, value); }
        }
        public bool AlertOnTargetJammed
        {
            get { return GetConfigValue<bool>(ALERT_ON_TARGET_JAMMED); }
            set { SetConfigValue(ALERT_ON_TARGET_JAMMED, value); }
        }
        public bool AlertOnWarpJammed
        {
            get { return GetConfigValue<bool>(ALERT_ON_WARP_JAMMED); }
            set { SetConfigValue(ALERT_ON_WARP_JAMMED, value); }
        }

        public AlertConfiguration(Dictionary<string, ConfigProperty> configProperties)
            : base(configProperties)
        {
            AddDefaultConfigProperties();
        }

        public override void AddDefaultConfigProperties()
        {
            AddDefaultConfigProperty(new ConfigProperty<bool>(USE_ALERTS, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(ALERT_ON_LOCAL_UNSAFE, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(ALERT_ON_LOCAL_CHAT, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(ALERT_ON_FACTION_SPAWN, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(ALERT_ON_LOW_AMMO, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(ALERT_ON_FREIGHTER_NO_PICKUP, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(ALERT_ON_LONG_RANDOM_WAIT, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(ALERT_ON_PLAYER_NEAR, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(ALERT_ON_FLEE, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(ALERT_ON_TARGET_JAMMED, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(ALERT_ON_WARP_JAMMED, true));
        }
    }
}