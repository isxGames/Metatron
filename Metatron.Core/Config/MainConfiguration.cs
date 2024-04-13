using System.Collections.Generic;

namespace Metatron.Core.Config
{
    public interface IMainConfiguration : IConfigurationBase
    {
        bool Disable3DRender { get; set; }
        bool DisableUiRender { get; set; }
        bool DisableTextureLoading { get; set; }
        BotModes ActiveBehavior { get; set; }
        bool TraceLoggingEnabled { get; set; }
    }

    public sealed class MainConfiguration : ConfigurationBase, IMainConfiguration
    {
        private static readonly string DISABLE_3D_RENDER = "Main_Disable3DRender",
                                       DISABLE_UI_RENDER = "Main_DisableUIRender",
                                       DISABLE_TEXTURE_LOADING = "Main_DisableTextureLoading",
                                       ACTIVE_BEHAVIOR = "Main_ActiveBehavior",
                                       TRACE_LOGGING = "Main_TraceLogging";

        public bool Disable3DRender
        {
            get { return GetConfigValue<bool>(DISABLE_3D_RENDER); }
            set { SetConfigValue(DISABLE_3D_RENDER, value); }
        }

        public bool TraceLoggingEnabled
        {
            get { return GetConfigValue<bool>(TRACE_LOGGING); }
            set { SetConfigValue(TRACE_LOGGING, value); }
        }

        public bool DisableUiRender
        {
            get { return GetConfigValue<bool>(DISABLE_UI_RENDER); }
            set { SetConfigValue(DISABLE_UI_RENDER, value); }
        }

        public bool DisableTextureLoading
        {
            get { return GetConfigValue<bool>(DISABLE_TEXTURE_LOADING); }
            set { SetConfigValue(DISABLE_TEXTURE_LOADING, value); }
        }

        public BotModes ActiveBehavior
        {
            get { return GetConfigValue<BotModes>(ACTIVE_BEHAVIOR); }
            set { SetConfigValue(ACTIVE_BEHAVIOR, value); }
        }

        public MainConfiguration(Dictionary<string, ConfigProperty> configProperties)
            : base(configProperties)
        {
            AddDefaultConfigProperties();
        }

        public override void AddDefaultConfigProperties()
        {
            AddDefaultConfigProperty(new ConfigProperty<bool>(DISABLE_3D_RENDER, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(DISABLE_UI_RENDER, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(DISABLE_TEXTURE_LOADING, false));
            AddDefaultConfigProperty(new ConfigProperty<BotModes>(ACTIVE_BEHAVIOR, BotModes.Mining));
            AddDefaultConfigProperty(new ConfigProperty<bool>(TRACE_LOGGING, false));
        }
    }
}