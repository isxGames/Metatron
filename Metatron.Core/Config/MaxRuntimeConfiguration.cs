using System.Collections.Generic;

namespace Metatron.Core.Config
{
    public interface IMaxRuntimeConfiguration : IConfigurationBase
    {
        bool UseMaxRuntime { get; set; }
        int MaxRuntimeMinutes { get; set; }
        bool ResumeAfterWaiting { get; set; }
        int ResumeWaitMinutes { get; set; }
        bool UseRandomWaits { get; set; }
        bool UseRelaunching { get; set; }
        string CharacterSetToRelaunch { get; set; }
        bool RelaunchAfterDowntime { get; set; }
    }

    public class MaxRuntimeConfiguration : ConfigurationBase, IMaxRuntimeConfiguration
    {
        private static readonly string USE_MAX_RUNTIME = "MaxRuntime_UseMaxRuntime",
                                       MAX_RUNTIME_MINUTES = "MaxRuntime_MaxRuntimeMinutes",
                                       RESUME_AFTER_WAITING = "MaxRuntime_ResumeAfterWaiting",
                                       RESUME_WAIT_MINUTES = "MaxRuntime_ResumeWaitMinutes",
                                       USE_RANDOM_WAITS = "MaxRuntime_UseRandomWaits",
                                       USE_RELAUNCHING = "MaxRuntime_UseRelaunching",
                                       CHARACTER_SET_TO_RELAUNCH = "MaxRuntime_CharacterSetToRelaunch",
                                       RELAUNCH_AFTER_DOWNTIME = "MaxRuntime_RelaunchAfterDowntime";

        #region MaxRuntime
        public bool UseMaxRuntime
        {
            get { return GetConfigValue<bool>(USE_MAX_RUNTIME); }
            set { SetConfigValue(USE_MAX_RUNTIME, value); }
        }

        public int MaxRuntimeMinutes
        {
            get { return GetConfigValue<int>(MAX_RUNTIME_MINUTES); }
            set { SetConfigValue(MAX_RUNTIME_MINUTES, value); }
        }

        public bool ResumeAfterWaiting
        {
            get { return GetConfigValue<bool>(RESUME_AFTER_WAITING); }
            set { SetConfigValue(RESUME_AFTER_WAITING, value); }
        }

        public int ResumeWaitMinutes
        {
            get { return GetConfigValue<int>(RESUME_WAIT_MINUTES); }
            set { SetConfigValue(RESUME_WAIT_MINUTES, value); }
        }

        public bool UseRandomWaits
        {
            get { return GetConfigValue<bool>(USE_RANDOM_WAITS); }
            set { SetConfigValue(USE_RANDOM_WAITS, value); }
        }
        #endregion

        #region Relaunching
        public bool UseRelaunching
        {
            get { return GetConfigValue<bool>(USE_RELAUNCHING); }
            set { SetConfigValue(USE_RELAUNCHING, value); }
        }

        public string CharacterSetToRelaunch
        {
            get { return GetConfigValue<string>(CHARACTER_SET_TO_RELAUNCH); }
            set { SetConfigValue(CHARACTER_SET_TO_RELAUNCH, value); }
        }

        public bool RelaunchAfterDowntime
        {
            get { return GetConfigValue<bool>(RELAUNCH_AFTER_DOWNTIME); }
            set { SetConfigValue(RELAUNCH_AFTER_DOWNTIME, value); }
        }
        #endregion

        public MaxRuntimeConfiguration(Dictionary<string, ConfigProperty> configProperties)
            : base(configProperties)
        {
            AddDefaultConfigProperties();
        }

        public override void AddDefaultConfigProperties()
        {
            AddDefaultConfigProperty(new ConfigProperty<bool>(USE_MAX_RUNTIME, false));
            AddDefaultConfigProperty(new ConfigProperty<int>(MAX_RUNTIME_MINUTES, 0));
            AddDefaultConfigProperty(new ConfigProperty<bool>(RESUME_AFTER_WAITING, false));
            AddDefaultConfigProperty(new ConfigProperty<int>(RESUME_WAIT_MINUTES, 0));
            AddDefaultConfigProperty(new ConfigProperty<bool>(USE_RANDOM_WAITS, true));

            AddDefaultConfigProperty(new ConfigProperty<bool>(USE_RELAUNCHING, false));
            AddDefaultConfigProperty(new ConfigProperty<string>(CHARACTER_SET_TO_RELAUNCH, string.Empty));
            AddDefaultConfigProperty(new ConfigProperty<bool>(RELAUNCH_AFTER_DOWNTIME, false));
        }
    }
}
