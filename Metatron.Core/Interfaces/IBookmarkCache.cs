using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EVE.ISXEVE;
using EVE.ISXEVE.Interfaces;

namespace Metatron.Core.Interfaces
{
    public interface IBookMarkCache
    {
        /// <summary>
        /// List of BookMark objects, refreshed every pulse
        /// </summary>
        ReadOnlyCollection<IBookMark> BookMarks { get; }

        /// <summary>
        /// Table of BookMark IDs mapped to BookMark objects, refreshed every pulse
        /// </summary>
        Dictionary<long, IBookMark> BookMarksById { get; }

        /// <summary>
        /// List of CachedBookMark objects built using the BookMark objects gotten this pulse
        /// </summary>
        ReadOnlyCollection<CachedBookMark> CachedBookMarks { get; }

        List<CachedBookMark> GetBookMarksStartingWith(string prefix, bool restrictToCurrentSystem);
        CachedBookMark FirstBookMarkStartingWith(string prefix, bool restrictToCurrentSystem);
        List<CachedBookMark> GetBookMarksMatching(string label, bool restrictToCurrentSystem);
        CachedBookMark FirstBookMarkMatching(string label, bool restrictToCurrentSystem);
        void RemoveCachedBookMark(CachedBookMark bookMark);
        IBookMark GetBookMarkFor(CachedBookMark cachedBookMark);
    }
}
