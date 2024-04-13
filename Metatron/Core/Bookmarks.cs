using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EVE.ISXEVE;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    // ReSharper disable ConvertToConstant.Local
    // ReSharper disable InconsistentNaming
    internal class Bookmarks : ModuleBase, IBookmarks
    {
		private readonly string StationTag = "Station";

		private readonly List<CachedBookMark> _cachedBookmarks = new List<CachedBookMark>();
    	public ReadOnlyCollection<CachedBookMark> CachedBookmarks
    	{
			get { return _cachedBookmarks.AsReadOnly(); }
    	}

// ReSharper disable InconsistentNaming
        protected int _lastSolarSystem, _lastBookmarkCount;
// ReSharper restore InconsistentNaming

        public CachedBookMark TempMiningBookmark { get; set; }
        public CachedBookMark TempCanBookmark { get; set; }

        protected readonly IMeCache _meCache;
        private readonly IStation _station;
        private readonly IConfiguration _configuration;
        protected readonly IBookMarkCache _bookMarkCache;
        private readonly IAsteroidBelts _asteroidBelts;
        private readonly IIsxeveProvider _isxeveProvider;

        internal Bookmarks(IMeCache meCache, IStation station, IConfiguration configuration, IBookMarkCache bookMarkCache, IAsteroidBelts asteroidBelts, IIsxeveProvider isxeveProvider)
        {
            _meCache = meCache;
            _station = station;
            _configuration = configuration;
            _bookMarkCache = bookMarkCache;
            _asteroidBelts = asteroidBelts;
            _isxeveProvider = isxeveProvider;

            ModuleManager.ModulesToPulse.Add(this);
			ModuleName = "Bookmarks";
			PulseFrequency = 1;
        }

        public override void Pulse()
        {
            var methodName = "Pulse";
			LogTrace(methodName);

			if (!ShouldPulse() || !_meCache.IsValid) 
				return;

        	StartPulseProfiling();

        	GetBookmarkList();

        	EndPulseProfiling();
        }

        public bool IsAtBookmark()
        {
            var methodName = "IsAtBookmark";
			LogTrace(methodName);

            return _cachedBookmarks.Any(IsAtBookmark);
        }

        public bool IsAtBookmark(CachedBookMark bookMark)
        {
            var methodName = "IsAtBookmark";
            LogTrace(methodName, bookMark.Label);

            if (IsStationBookMark(bookMark))
            {
                return _station.IsDockedAtStation(bookMark.ItemId);
            }

            bool isAtBookmark = _meCache.InSpace && _meCache.SolarSystemId == bookMark.SolarSystemId &&
                DistanceTo(bookMark.X, bookMark.Y, bookMark.Z) < (int)Ranges.Warp;
            LogTrace(methodName, isAtBookmark.ToString());
            return isAtBookmark;
        }

        public bool IsStationBookMark(BookMark bookMark)
		{
			return bookMark.ItemID >= 0 && (_station.StationTypeIDs.Contains(bookMark.TypeID) || bookMark.Type.Contains(StationTag));
        }

        public bool IsStationBookMark(CachedBookMark bookMark)
        {
            return bookMark.ItemId >= 0 && (_station.StationTypeIDs.Contains(bookMark.TypeId) || bookMark.Type.Contains(StationTag));
        }

        public void CreateSalvagingBookmark(IEntityWrapper entityWrapper)
        {
            var methodName = "CreateSalvagingBookmark";
            LogTrace(methodName);

            if (_configuration.SalvageConfig.CreateSalvageBookmarks)
            {
                var currentBookmarks = _isxeveProvider.Eve.GetBookmarks();
                if (currentBookmarks.Any(b => b.ItemID == entityWrapper.ID)) return;
                var bookmarkLabel = String.Format("{0}{1}", _configuration.MovementConfig.SalvagingPrefix,
                    String.Format("{0:00}:{1:00}", _meCache.GameHour, _meCache.GameMinute));
                var folderLabel = _configuration.SalvageConfig.SalvageBookmarkFolderLabel;
                if (_configuration.SalvageConfig.SaveBookmarksForCorporation)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Creating corporate salvage bookmark with label \"{0}\".", bookmarkLabel);
                    entityWrapper.ToEntity.CreateBookmark(bookmarkLabel,"", folderLabel, Entity.BookmarkExpiry.ThreeHours);
                } 
				else
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Creating personal salvage bookmark with label \"{0}\".", bookmarkLabel);
                    entityWrapper.ToEntity.CreateBookmark(bookmarkLabel, "", folderLabel, Entity.BookmarkExpiry.ThreeHours);
                }

            }

        }

        public void CreateTemporaryHaulingBookmark(IEntityWrapper canToBookmark)
        {
			var methodName = "CreateTemporaryHaulingBookmark";
			LogTrace(methodName);

            //Format: "{prefix} {can name}" i.e. "temp can - John Shitfucker 19:21"
        	if (canToBookmark == null) 
				return;

        	var bookmarkLabel = String.Format("{0}{1}", _configuration.MovementConfig.TemporaryCanBookMarkPrefix, canToBookmark.Name);
			LogMessage(methodName, LogSeverityTypes.Standard, "Creating temporary hauling bookmark by entity \"{0}\" ({1}) with label \"{2}\".",
                canToBookmark.Name, canToBookmark.ID, bookmarkLabel);
        	canToBookmark.CreateBookmark(bookmarkLabel);
        }

        public void RemoveTemporaryHaulingBookmarks()
        {
			var methodName = "RemoveTemporaryHaulingBookmarks";
        	LogTrace(methodName);

        	var bookMarks = _bookMarkCache.GetBookMarksStartingWith(_configuration.MovementConfig.TemporaryCanBookMarkPrefix, true);

            foreach (var bookMark in bookMarks)
            {
				LogMessage(methodName, LogSeverityTypes.Standard, "Removing temporary hauling bookmark with label \"{0}\".",
					bookMark.Label);
                _bookMarkCache.RemoveCachedBookMark(bookMark);
            }
        }

        protected virtual void GetBookmarkList()
        {
            GetBookmarkList(String.Empty, true);
        }

        protected virtual void GetBookmarkList(string prefix)
        {
            GetBookmarkList(prefix, true);
        }

        protected virtual void GetBookmarkList(string prefix, bool checkSystemId)
        {
            var methodName = "GetBookmarkList";
        	LogTrace(methodName, "Prefix: {0}, CheckSystemID: {1}", prefix, checkSystemId);

            prefix = prefix.ToLower();

            if (_cachedBookmarks.Count > 0)
                _cachedBookmarks.Clear();

            _cachedBookmarks.AddRange(_bookMarkCache.GetBookMarksStartingWith(prefix, checkSystemId));

            if (_lastSolarSystem == _meCache.SolarSystemId && _lastBookmarkCount == _cachedBookmarks.Count) return;

        	LogMessage(methodName, LogSeverityTypes.Standard, $"Found {_cachedBookmarks.Count} bookmarks matching prefix \"{prefix}\", checking system ID: {checkSystemId}");
            _lastBookmarkCount = _cachedBookmarks.Count;
        	_lastSolarSystem = _meCache.SolarSystemId;
        }

		public CachedBookMark GetHaulerPickupSystemBookMark()
		{
			var methodName = "GetHaulerPickupSystemBookMark";
			LogTrace(methodName);

			if (_configuration.CargoConfig.PickupSystemBookmark == string.Empty)
			{
				return null;
			}

            return _bookMarkCache.FirstBookMarkStartingWith(
                _configuration.CargoConfig.PickupSystemBookmark, false);
		}

        public CachedBookMark GetTempCanBookmark()
        {
            var methodName = "GetTempCanBookmark";
			LogTrace(methodName);

            if (_configuration.MovementConfig.TemporaryCanBookMarkPrefix == string.Empty)
            {
                return null;
            }

            return _bookMarkCache.FirstBookMarkStartingWith(
                _configuration.MovementConfig.TemporaryCanBookMarkPrefix, true);
        }
    }
    // ReSharper restore ConvertToConstant.Local
    // ReSharper restore InconsistentNaming
}
