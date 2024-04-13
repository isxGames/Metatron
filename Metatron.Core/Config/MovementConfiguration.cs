using System;
using System.Collections.Generic;

namespace Metatron.Core.Config
{
    public interface IMovementConfiguration : IConfigurationBase
    {
        bool UseBounceWarp { get; set; }
        int MaxSlowboatTime { get; set; }
        bool UseTempBeltBookmarks { get; set; }
        string SafeBookmarkPrefix { get; set; }
        string AsteroidBeltBookmarkPrefix { get; set; }
        string IceBeltBookmarkPrefix { get; set; }
        string TemporaryBeltBookMarkPrefix { get; set; }
        string TemporaryCanBookMarkPrefix { get; set; }
        string SalvagingPrefix { get; set; }
        bool OnlyUseBeltBookmarks { get; set; }
        bool UseRandomBeltOrder { get; set; }
        bool UseBeltSubsets { get; set; }
        int NumBeltsInSubset { get; set; }
        BeltSubsetModes BeltSubsetMode { get; set; }
        int PropModMinCapPct { get; set; }
        int PropModResumeCapPct { get; set; }
        string JumpStabilityTestStartBookmark { get; set; }
        string JumpStabilityTestEndBookmark { get; set; }
        bool UseCustomOrbitDistance { get; set; }
        int CustomOrbitDistance { get; set; }
        bool UseKeepAtRangeInsteadOfOrbit { get; set; }
        string HomeStation { get; set; }
        bool OrbitCollidableNotTarget { get; set; }
    }

    public sealed class MovementConfiguration : ConfigurationBase, IMovementConfiguration
    {
        // ReSharper disable ConvertToConstant.Local
        private static readonly string ONLY_USE_BELT_BOOKMARKS = "Movement_OnlyUseBeltBookmarks",
                                       USE_RANDOM_BELT_ORDER = "Movement_UseRandomBeltOrder",
                                       BOUNCE_WARP = "Movement_BounceWarp",
                                       MAX_SLOWBOAT_TIME = "Movement_MaxSlowboatTime",
                                       SAFE_BOOKMARK_PREFIX = "Movement_SafeBookmarkPrefix",
                                       ASTEROID_BELT_BOOKMARK_PREFIX = "Movement_AsteroidBeltBookmarkPrefix",
                                       ICE_BELT_BOOKMARK_PREFIX = "Movement_IceBeltBookmarkPrefix",
                                       TEMPORARY_BELT_BOOKMARK_PREFIX = "Movement_TemporaryBeltBookmarkPrefix",
                                       TEMPORARY_CAN_BOOKMARK_PREFIX = "Movement_TemporaryCanBookmarkPrefix",
                                       USE_BELT_SUBSETS = "Movement_UseBeltSubsets",
                                       NUM_BELTS_IN_SUBSET = "Movement_NumBeltsInSubset",
                                       BELT_SUBSET_MODE = "Movement_BeltSubsetMode",
                                       PROPULSION_MOD_MIN_CAP_PCT = "Movement_PropulsionModMinCapPct",
                                       PROPULSION_MOD_RESUME_CAP_PCT = "Movement_PropulsionModResumeCapPct",
                                       USE_TEMP_BELT_BOOKMARKS = "Movement_UseTempBeltBookmarks",
                                       JST_START_BOOKMARK = "Movement_JST_StartBookmark",
                                       JST_END_BOOKMARK = "Movement_JST_EndBookmark",
                                       HOME_STATION = "Movement_HomeStation",
                                       USE_CUSTOM_ORBIT_DISTANCE = "Movement_UseCustomOrbitDistance",
                                       CUSTOM_ORBIT_DISTANCE = "Movement_CustomOrbitDistance",
                                       SALVAGING_PREFIX = "Movement_SalvagingPrefix",
                                       USE_KEEPATRANGE_INSTEAD_OF_ORBIT = "Movement_UseKeepAtRangeInsteadOfOrbit",
                                       ORBIT_COLLIDABLE_NOT_TARGET = "Movement_OrbitCollidableNotTarget";

// ReSharper restore ConvertToConstant.Local

        #region Bounce Warping
        public bool UseBounceWarp
        {
            get { return GetConfigValue<bool>(BOUNCE_WARP); }
            set { SetConfigValue(BOUNCE_WARP, value); }
        }

        public int MaxSlowboatTime
        {
            get { return GetConfigValue<int>(MAX_SLOWBOAT_TIME); }
            set { SetConfigValue(MAX_SLOWBOAT_TIME, value); }
        }

        public bool UseTempBeltBookmarks
        {
            get { return GetConfigValue<bool>(USE_TEMP_BELT_BOOKMARKS); }
            set { SetConfigValue(USE_TEMP_BELT_BOOKMARKS, value); }
        }
        #endregion

        #region Bookmark Prefixes
        public string SafeBookmarkPrefix
        {
            get { return GetConfigValue<string>(SAFE_BOOKMARK_PREFIX); }
            set { SetConfigValue(SAFE_BOOKMARK_PREFIX, value); }
        }

        public string AsteroidBeltBookmarkPrefix
        {
            get { return GetConfigValue<string>(ASTEROID_BELT_BOOKMARK_PREFIX); }
            set { SetConfigValue(ASTEROID_BELT_BOOKMARK_PREFIX, value); }
        }

        public string IceBeltBookmarkPrefix
        {
            get { return GetConfigValue<string>(ICE_BELT_BOOKMARK_PREFIX); }
            set { SetConfigValue(ICE_BELT_BOOKMARK_PREFIX, value); }
        }

        public string TemporaryBeltBookMarkPrefix
        {
            get { return GetConfigValue<string>(TEMPORARY_BELT_BOOKMARK_PREFIX); }
            set { SetConfigValue(TEMPORARY_BELT_BOOKMARK_PREFIX, value); }
        }

        public string TemporaryCanBookMarkPrefix
        {
            get { return GetConfigValue<string>(TEMPORARY_CAN_BOOKMARK_PREFIX); }
            set { SetConfigValue(TEMPORARY_CAN_BOOKMARK_PREFIX, value); }
        }

        public string SalvagingPrefix
        {
            get { return GetConfigValue<string>(SALVAGING_PREFIX); }
            set { SetConfigValue(SALVAGING_PREFIX, value); }
        }
        #endregion

        #region Asteroid Belts
        public bool OnlyUseBeltBookmarks
        {
            get { return GetConfigValue<bool>(ONLY_USE_BELT_BOOKMARKS); }
            set { SetConfigValue(ONLY_USE_BELT_BOOKMARKS, value); }
        }

        public bool UseRandomBeltOrder
        {
            get { return GetConfigValue<bool>(USE_RANDOM_BELT_ORDER); }
            set { SetConfigValue(USE_RANDOM_BELT_ORDER, value); }
        }

        public bool UseBeltSubsets
        {
            get { return GetConfigValue<bool>(USE_BELT_SUBSETS); }
            set { SetConfigValue(USE_BELT_SUBSETS, value); }
        }
        public int NumBeltsInSubset
        {
            get { return GetConfigValue<int>(NUM_BELTS_IN_SUBSET); }
            set { SetConfigValue(NUM_BELTS_IN_SUBSET, value); }
        }

        public BeltSubsetModes BeltSubsetMode
        {
            get { return GetConfigValue<BeltSubsetModes>(BELT_SUBSET_MODE); }
            set { SetConfigValue(BELT_SUBSET_MODE, value); }
        }
        #endregion

        #region Propulsion Modules
        public int PropModMinCapPct
        {
            get { return GetConfigValue<int>(PROPULSION_MOD_MIN_CAP_PCT); }
            set { SetConfigValue(PROPULSION_MOD_MIN_CAP_PCT, value); }
        }

        public int PropModResumeCapPct
        {
            get { return GetConfigValue<int>(PROPULSION_MOD_RESUME_CAP_PCT); }
            set { SetConfigValue(PROPULSION_MOD_RESUME_CAP_PCT, value); }
        }
        #endregion

        #region Jump Stability Test
        public string JumpStabilityTestStartBookmark
        {
            get { return GetConfigValue<string>(JST_START_BOOKMARK); }
            set { SetConfigValue(JST_START_BOOKMARK, value); }
        }

        public string JumpStabilityTestEndBookmark
        {
            get { return GetConfigValue<string>(JST_END_BOOKMARK); }
            set { SetConfigValue(JST_END_BOOKMARK, value); }
        }
        #endregion

        #region Orbiting
        public bool UseCustomOrbitDistance
        {
            get { return GetConfigValue<bool>(USE_CUSTOM_ORBIT_DISTANCE); }
            set { SetConfigValue(USE_CUSTOM_ORBIT_DISTANCE, value); }
        }

        public int CustomOrbitDistance
        {
            get { return GetConfigValue<int>(CUSTOM_ORBIT_DISTANCE); }
            set { SetConfigValue(CUSTOM_ORBIT_DISTANCE, value); }
        }

        public bool UseKeepAtRangeInsteadOfOrbit
        {
            get { return GetConfigValue<bool>(USE_KEEPATRANGE_INSTEAD_OF_ORBIT); }
            set { SetConfigValue(USE_KEEPATRANGE_INSTEAD_OF_ORBIT, value); }
        }

        public bool OrbitCollidableNotTarget
        {
            get { return GetConfigValue<bool>(ORBIT_COLLIDABLE_NOT_TARGET); }
            set { SetConfigValue(ORBIT_COLLIDABLE_NOT_TARGET, value); }
        }
        #endregion

        public string HomeStation
        {
            get { return GetConfigValue<string>(HOME_STATION); }
            set { SetConfigValue(HOME_STATION, value); }
        }

        public MovementConfiguration(Dictionary<string, ConfigProperty> configProperties)
            : base(configProperties)
        {
            AddDefaultConfigProperties();
        }

        public override void AddDefaultConfigProperties()
        {
            AddDefaultConfigProperty(new ConfigProperty<bool>(USE_TEMP_BELT_BOOKMARKS, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(BOUNCE_WARP, true));
            AddDefaultConfigProperty(new ConfigProperty<int>(MAX_SLOWBOAT_TIME, 60));

            AddDefaultConfigProperty(new ConfigProperty<string>(SAFE_BOOKMARK_PREFIX, "Safe: "));
            AddDefaultConfigProperty(new ConfigProperty<string>(ASTEROID_BELT_BOOKMARK_PREFIX, "Belt: "));
            AddDefaultConfigProperty(new ConfigProperty<string>(ICE_BELT_BOOKMARK_PREFIX, "Ice: "));
            AddDefaultConfigProperty(new ConfigProperty<string>(TEMPORARY_BELT_BOOKMARK_PREFIX, "Temp Belt: "));
            AddDefaultConfigProperty(new ConfigProperty<string>(TEMPORARY_CAN_BOOKMARK_PREFIX, "Temp Can: "));
            AddDefaultConfigProperty(new ConfigProperty<string>(SALVAGING_PREFIX, "Salvage: "));

            AddDefaultConfigProperty(new ConfigProperty<bool>(ONLY_USE_BELT_BOOKMARKS, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(USE_RANDOM_BELT_ORDER, true));
            AddDefaultConfigProperty(new ConfigProperty<bool>(USE_BELT_SUBSETS, false));
            AddDefaultConfigProperty(new ConfigProperty<int>(NUM_BELTS_IN_SUBSET, 10));
            AddDefaultConfigProperty(new ConfigProperty<BeltSubsetModes>(BELT_SUBSET_MODE, BeltSubsetModes.First));

            AddDefaultConfigProperty(new ConfigProperty<int>(PROPULSION_MOD_MIN_CAP_PCT, 50));
            AddDefaultConfigProperty(new ConfigProperty<int>(PROPULSION_MOD_RESUME_CAP_PCT, 75));

            AddDefaultConfigProperty(new ConfigProperty<string>(JST_START_BOOKMARK, string.Empty));
            AddDefaultConfigProperty(new ConfigProperty<string>(JST_END_BOOKMARK, string.Empty));

            AddDefaultConfigProperty(new ConfigProperty<string>(HOME_STATION, string.Empty));

            AddDefaultConfigProperty(new ConfigProperty<bool>(USE_CUSTOM_ORBIT_DISTANCE, false));
            AddDefaultConfigProperty(new ConfigProperty<int>(CUSTOM_ORBIT_DISTANCE, 30000));
            AddDefaultConfigProperty(new ConfigProperty<bool>(USE_KEEPATRANGE_INSTEAD_OF_ORBIT, false));
            AddDefaultConfigProperty(new ConfigProperty<bool>(ORBIT_COLLIDABLE_NOT_TARGET, false));
        }
    }
}