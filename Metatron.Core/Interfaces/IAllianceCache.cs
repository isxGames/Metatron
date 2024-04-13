using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Metatron.Core.Interfaces
{
    public interface IAllianceCache : IModule
    {
        ReadOnlyCollection<CachedAlliance> CachedAlliances { get; }
        Dictionary<Int64, CachedAlliance> CachedAlliancesById { get; }
        void GetAllianceInfo(Int64 corpId);
        void RemoveAlliance(Int64 corpId);
    }
}
