using System;

namespace Metatron.Core
{
    public sealed class CachedBelt
    {
        public Int64 Id { get; private set; }
        public string Name { get; private set; }
        public bool IsIceBelt { get; private set; }

        public bool IsBeltEmpty { get; set; }

        public CachedBelt(IEntityWrapper entity)
        {
            Name = entity.Name;
            Id = entity.ID;

            if (Name.Contains("Ice Belt"))
            {
                IsIceBelt = true;
            }
        }
    }
}
