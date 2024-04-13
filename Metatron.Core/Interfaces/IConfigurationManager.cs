using System;
using System.Collections.Generic;
using Metatron.Core.Config;

namespace Metatron.Core.Interfaces
{
    public interface IConfigurationManager
    {
        Dictionary<string, Dictionary<string, ConfigProperty>> ConfigProfilesByName { get; }
        Dictionary<string, ConfigProperty> ActiveConfigProfile { get; set; }
        string ActiveConfigName { get; }
        event EventHandler ConfigLoaded;
        void LoadConfigurationFiles();
        void RenameConfigProfile(string currentProfileName, string newProfileName);
        void AddConfigProfile(string configProfileName, bool reloadConfig);
        void CopyConfigProfile(string configProfileName);
        void RemoveConfigProfile(string configProfileName);
        void LoadConfigProfile(string configProfileName);
        void SaveConfigProfile(string configProfileName);
        void OnConfigLoaded();
    }
}
