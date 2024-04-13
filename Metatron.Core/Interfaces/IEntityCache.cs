using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Metatron.Core.Interfaces
{
    public interface IEntityCache
    {
        ReadOnlyCollection<CachedEntity> CachedEntities { get; }
        Dictionary<Int64, CachedEntity> CachedEntitiesById { get; }
        void GetEntityInfo(Int64 typeId);
        void RemoveEntity(Int64 typeId);

    }
}
