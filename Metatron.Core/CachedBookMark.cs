using System;
using EVE.ISXEVE.Interfaces;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    public class CachedBookMark
    {
        public string Label { get; private set; }
        public string Type { get; private set; }
        public int SolarSystemId { get; private set; }
        public int TypeId { get; private set; }
        public Int64 Id { get; private set; }
        public Int64 ItemId { get; private set; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }
        public long OwnerId { get; private set; }
        public long CreatorId { get; private set; }

        public CachedBookMark(IEntityProvider entityProvider, bool isPlayerInStation, IBookMark bookMark)
        {
            Label = bookMark.Label.TrimEnd('\t');
            Id = bookMark.ID;
            ItemId = bookMark.ItemID;
            SolarSystemId = bookMark.SolarSystemID;
            TypeId = bookMark.TypeID;
            Type = bookMark.Type;
            OwnerId = bookMark.OwnerID;
            CreatorId = bookMark.CreatorID;

            if (ItemId > 0 && entityProvider.EntityWrappersById.ContainsKey(ItemId))
            {
                var entity = entityProvider.EntityWrappersById[ItemId];
                X = entity.X;
                Y = entity.Y;
                Z = entity.Z;
            }
            else
            {
                try
                {
                    X = bookMark.X;
                    Y = bookMark.Y;
                    Z = bookMark.Z;
                }
                catch (FormatException)
                {
                    X = 0;
                    Y = 0;
                    Z = 0;
                }
            }
        }
    }
}
