using System;
using System.Collections.Generic;

namespace Metatron.Core.Config
{
    public interface IConfigurationBase
    {
        void UpdateConfigurationProperties(Dictionary<string, ConfigProperty> configProperties);
    }

    public abstract class ConfigurationBase : IConfigurationBase
    {
        /// <summary>
        /// Contains all of the set non-default configuration properties.
        /// </summary>
        protected Dictionary<string, ConfigProperty> ConfigProperties { get; set; }
        /// <summary>
        /// Contains the default values of all configuration properties.
        /// </summary>
        public static Dictionary<string, ConfigProperty> DefaultConfigProperties = new Dictionary<string, ConfigProperty>();

        protected ConfigurationBase(Dictionary<string, ConfigProperty> configProperties)
        {
            ConfigProperties = configProperties;
        }

        public void UpdateConfigurationProperties(Dictionary<string, ConfigProperty> configProperties)
        {
            ConfigProperties = configProperties;
        }

        protected ConfigProperty GetConfigProperty(string propertyName)
        {
            if (ConfigProperties == null)
                throw new InvalidOperationException("No configuration profile is loaded.");

            //First check ConfigProperties
            lock (ConfigProperties)
            {
                if (ConfigProperties.ContainsKey(propertyName))
                {
                    var configProperty = ConfigProperties[propertyName];

                    if (configProperty.UntypedValue != null)
                        return ConfigProperties[propertyName];

                    ConfigProperties.Remove(propertyName);
                }

                //Check DefaultConfigProperties
                if (DefaultConfigProperties.ContainsKey(propertyName))
                {
                    ConfigProperties.Add(propertyName, DefaultConfigProperties[propertyName]);
                    return ConfigProperties[propertyName];
                }

                //This should never happen
                throw new ArgumentException(
                    string.Format("Cannot find config property \"{0}\" in the active config or the default config.", propertyName));
            }
        }

        protected T GetConfigValue<T>(string propertyName)
        {
            var configProperty = GetConfigProperty(propertyName);

            return (T) configProperty.UntypedValue;
        }

        protected void SetConfigValue<T>(string propertyName, T value)
        {
            if (value == null)
                throw new ArgumentNullException("value", string.Format("Value for config property {0} cannot be null.", propertyName));

            var configProperty = GetConfigProperty(propertyName);
            configProperty.UntypedValue = value;
        }

        protected static void AddDefaultConfigProperty(ConfigProperty configProperty)
        {
            if (!DefaultConfigProperties.ContainsKey(configProperty.Name))
            {
                DefaultConfigProperties.Add(configProperty.Name, configProperty);
            }
        }

        public abstract void AddDefaultConfigProperties();
    }
}