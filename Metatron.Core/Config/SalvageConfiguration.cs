using System.Collections.Generic;

namespace Metatron.Core.Config
{
    public interface ISalvageConfiguration : IConfigurationBase
    {
        bool CreateSalvageBookmarks { get; set; }
        bool SaveBookmarksForCorporation { get; set; }
        bool LootWrecksAfterCombat { get; set; }
        bool WaitForSafetyConfirmation { get; set; }
        string SalvageBookmarkFolderLabel { get; set; }
    }

    public sealed class SalvageConfiguration : ConfigurationBase, ISalvageConfiguration
    {
        private static readonly string CreateSalvageBookmarksTag = "Salvage_EnableBM",
                                       SaveBookmarksForCorporationTag = "Salvage_CorpBM",
                                       LootWrecksAfterCombatTag = "Salvage_LootAfterCombat",
                                       WaitForSafetyConfirmationTag = "Salvage_WaitForSafety",
                                       SalvageBookmarkFolderLabelTag = "Salvage_FolderLabel";
        #region Salvaging
        public bool CreateSalvageBookmarks
        {
            get { return GetConfigValue<bool>(CreateSalvageBookmarksTag); }
            set { SetConfigValue(CreateSalvageBookmarksTag, value); }
        }

        public bool SaveBookmarksForCorporation
        {
            get { return GetConfigValue<bool>(SaveBookmarksForCorporationTag); }
            set { SetConfigValue(SaveBookmarksForCorporationTag, value); }
        }
        public bool LootWrecksAfterCombat
        {
            get { return GetConfigValue<bool>(LootWrecksAfterCombatTag); }
            set { SetConfigValue(LootWrecksAfterCombatTag, value); }
        }
        public bool WaitForSafetyConfirmation
        {
            get { return GetConfigValue<bool>(WaitForSafetyConfirmationTag); }
            set { SetConfigValue(WaitForSafetyConfirmationTag, value); }
        }
        public string SalvageBookmarkFolderLabel
        {
            get { return GetConfigValue<string>(SalvageBookmarkFolderLabelTag); }
            set { SetConfigValue(SalvageBookmarkFolderLabelTag, value); }
        }
        #endregion

        public SalvageConfiguration(Dictionary<string, ConfigProperty> configProperties)
            : base(configProperties)
        {
            AddDefaultConfigProperties();
        }

        public override void AddDefaultConfigProperties()
        {
            AddDefaultConfigProperty(new ConfigProperty<bool>(CreateSalvageBookmarksTag, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(SaveBookmarksForCorporationTag, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(LootWrecksAfterCombatTag, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(WaitForSafetyConfirmationTag, false));
            AddDefaultConfigProperty(new ConfigProperty<string>(SalvageBookmarkFolderLabelTag, "Salvaging"));
        }
    }
}
