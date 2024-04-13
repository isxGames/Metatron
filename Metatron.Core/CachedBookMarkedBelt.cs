using System;

namespace Metatron.Core
{
    public sealed class CachedBookMarkedBelt
    {
        public Int64 Id { get; private set; }
        public string BookmarkLabel { get; private set; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }
        public bool IsIceBelt { get; private set; }

        public bool IsBeltEmpty { get; set; }

        public CachedBookMarkedBelt(CachedBookMark bookmark, bool isIceBelt)
        {
            Id = bookmark.Id;

            BookmarkLabel = bookmark.Label;
            X = bookmark.X;
            Y = bookmark.Y;
            Z = bookmark.Z;

            IsIceBelt = isIceBelt;
        }
    }
}
