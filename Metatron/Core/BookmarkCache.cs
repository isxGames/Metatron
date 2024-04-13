using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EVE.ISXEVE;
using EVE.ISXEVE.Interfaces;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    // ReSharper disable CompareOfFloatsByEqualityOperator
    internal sealed class BookMarkCache : ModuleBase, IBookMarkCache
    {
        private readonly List<IBookMark> _bookMarks = new List<IBookMark>();

	    /// <summary>
	    /// List of BookMark objects, refreshed every pulse
	    /// </summary>
        public ReadOnlyCollection<IBookMark> BookMarks
        {
            get { return _bookMarks.AsReadOnly(); }
        }

        private readonly Dictionary<Int64, IBookMark> _bookMarksById = new Dictionary<long, IBookMark>(); 

	    /// <summary>
	    /// Table of BookMark IDs mapped to BookMark objects, refreshed every pulse
	    /// </summary>
        public Dictionary<long, IBookMark> BookMarksById
        {
            get { return _bookMarksById; }
        }

	    private readonly List<CachedBookMark> _cachedBookMarks = new List<CachedBookMark>();

	    /// <summary>
	    /// List of CachedBookMark objects built using the BookMark objects gotten this pulse
	    /// </summary>
        public ReadOnlyCollection<CachedBookMark> CachedBookMarks
        {
            get { return _cachedBookMarks.AsReadOnly(); }
        }

        private readonly IMeCache _meCache;
        private readonly IEntityProvider _entityProvider;
        private readonly IIsxeveProvider _isxeveProvider;

		internal BookMarkCache(IMeCache meCache, IEntityProvider entityProvider, ILogging logging, IIsxeveProvider isxeveProvider)
            : base(logging)
		{
		    _meCache = meCache;
		    _entityProvider = entityProvider;
		    _isxeveProvider = isxeveProvider;

		    ModuleName = "BookmarkCache";
			PulseFrequency = 1;
			IsEnabled = true;
			ModuleManager.ModulesToPulse.Add(this);
		}

		public override void Pulse()
		{
			var methodName = "Pulse";
			LogTrace(methodName);

			if (!ShouldPulse()) 
				return;

			//Get a list of bookmarks to work with this frame
			//StartMethodProfiling("GetBookmarks");
            var eveBookMarks = _isxeveProvider.Eve.GetBookmarks();

            if (eveBookMarks == null)
			{
				LogMessage(methodName, LogSeverityTypes.Debug, "Error: GetBookmarks returned a null list.");
				return;
			}

		    _bookMarks.AddRange(eveBookMarks);

			//EndMethodProfiling();
			//StartMethodProfiling("RepopulateTable");
			var bookMarkIDs = new List<Int64>();

            foreach (var bookMark in _bookMarks)
			{
				var bookMarkId = bookMark.ID;

				if (bookMarkId < 0)
				{
					LogMessage(methodName, LogSeverityTypes.Debug, "Got bookmark with negative ID {0}. Label: {1}, Index (0-based): {2}, Count: {3}",
                        bookMarkId, bookMark.Label, _bookMarks.IndexOf(bookMark), _bookMarks.Count);
					continue;
				}

				if (!bookMark.IsValid)
				{
					LogMessage(methodName, LogSeverityTypes.Debug, "Got invalid bookmark. ID: {0}, Index (0-based): {1}, Count: {2}",
                        bookMarkId, _bookMarks.IndexOf(bookMark), _bookMarks.Count);
					continue;
				}

				bookMarkIDs.Add(bookMarkId);
                _bookMarksById.Add(bookMarkId, bookMark);
				//CachedBookMarks.Add(new CachedBookMark(bookMark));
			}
			//EndMethodProfiling();

			//StartMethodProfiling("PruneDeadBookmarks");
			//First remove any invalid bookmarks
            for (var index = 0; index < _cachedBookMarks.Count; index++)
			{
                var bookmark = _cachedBookMarks[index];

                var remove = bookMarkIDs.All(id => id != _cachedBookMarks[index].Id) ||
					(_meCache.InSpace && bookmark.X == 0 && bookmark.Y == 0 && bookmark.Z == 0);

				if (!remove) 
					continue;

				//_logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
				//methodName, string.Format("Pruning dead bookmark {0}", CachedBookMarks[idx].ID)));
				_cachedBookMarks.RemoveAt(index);
				index--;
			}
			//EndMethodProfiling();

			StartMethodProfiling("BuildNewBookmarks");
			//Next add any missing
			for (var index = 0; index < bookMarkIDs.Count; index++)
			{
                var found = _cachedBookMarks.Any(cachedBookMark => cachedBookMark.Id == bookMarkIDs[index]);

				if (found) 
					continue;

                var newBookMark = new CachedBookMark(_entityProvider, _meCache.InStation, _bookMarks[index]);

				LogMessage(methodName, LogSeverityTypes.Debug, "Adding or updating bookmark \"{0}\" ({1}) [{2},{3},{4}]",
					newBookMark.Label, newBookMark.Id, newBookMark.X, newBookMark.Y, newBookMark.Z);
				_cachedBookMarks.Add(newBookMark);
			}
			EndMethodProfiling();
		}

		public override void InFrameCleanup()
		{
            _bookMarksById.Clear();
            foreach (var bookMark in _bookMarks)
			{
				bookMark.Invalidate();
			}
			_bookMarks.Clear();
		}

		public List<CachedBookMark> GetBookMarksStartingWith(string prefix, bool restrictToCurrentSystem)
		{
			var methodName = "GetBookMarksStartingWith";
			LogTrace(methodName, "Prefix: {0}, RestrictToCurrentSystem: {1}", prefix, restrictToCurrentSystem);

            var sameSystemBookmarks = _cachedBookMarks.AsEnumerable();

			//If restricting to the current system, make sure we start off with a list of in-system bms
			if (restrictToCurrentSystem)
			{
				sameSystemBookmarks = sameSystemBookmarks.Where(bookMark => bookMark.SolarSystemId == _meCache.SolarSystemId);
			}
			
            //Filter based on the prefix we want to match
            sameSystemBookmarks = sameSystemBookmarks.Where(x => x.Label.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase));
			return sameSystemBookmarks.ToList();
		}

		public CachedBookMark FirstBookMarkStartingWith(string prefix, bool restrictToCurrentSystem)
		{
			var methodName = "FirstBookMarkStartingWith";
			LogTrace(methodName, "Prefix: {0}, RestrictToCurrentSystem: {1}", prefix, restrictToCurrentSystem);

			return GetBookMarksStartingWith(prefix, restrictToCurrentSystem).FirstOrDefault();
		}

		public List<CachedBookMark> GetBookMarksMatching(string label, bool restrictToCurrentSystem)
		{
			var methodName = "GetBookMarksMatching";
			LogTrace(methodName, "Label: {0}, RestrictToCurrentSystem: {1}", label, restrictToCurrentSystem);

            var sameSystemBookmarks = _cachedBookMarks.AsEnumerable();

			if (restrictToCurrentSystem)
			{
				sameSystemBookmarks = sameSystemBookmarks.Where(x => x.SolarSystemId == _meCache.SolarSystemId);
			}

            sameSystemBookmarks = sameSystemBookmarks.Where(x => x.Label.Equals(label, StringComparison.InvariantCultureIgnoreCase));
			return sameSystemBookmarks.ToList();
		}

		public CachedBookMark FirstBookMarkMatching(string label, bool restrictToCurrentSystem)
		{
			var methodName = "FirstBookMarkMatching";
			LogTrace(methodName, "Label: {0}, RestrictToCurrentSystem: {1}", label, restrictToCurrentSystem);

			return GetBookMarksMatching(label, restrictToCurrentSystem).FirstOrDefault();
		}

        public void RemoveCachedBookMark(CachedBookMark bookMark)
        {
            var methodName = "Remove";
            LogTrace(methodName, "BookMark: {0} ({1})", bookMark.Label, bookMark.Id);

            if (!_bookMarksById.ContainsKey(bookMark.Id))
            {
                _logging.LogMessage("CachedBookMark", "ToBookMark", LogSeverityTypes.Standard, "Error; Bookmark cache did not contain bookmark for ID {0}.",
                    bookMark.Id);
                return;
            }

            _logging.LogMessage("CachedBookMark", methodName, LogSeverityTypes.Debug, "Removing CachedBookMark \"{0}\" ({1}).",
                bookMark.Label, bookMark.Id);

            //Delete it in EVE
            var realBookMark = _bookMarksById[bookMark.Id];
            realBookMark.Remove();

            //Remove it from caches
            _bookMarks.Remove(realBookMark);
            _bookMarksById.Remove(bookMark.Id);

            _cachedBookMarks.Remove(bookMark);
        }

        public IBookMark GetBookMarkFor(CachedBookMark cachedBookMark)
        {
            return _bookMarksById[cachedBookMark.Id];
        }
	}
    // ReSharper restore CompareOfFloatsByEqualityOperator
}
