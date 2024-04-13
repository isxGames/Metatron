using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Metatron.Core.Interfaces
{
    public interface ICorporationCache : IModule
    {
        ReadOnlyCollection<CachedCorporation> CachedCorporations { get; }
        Dictionary<Int64, CachedCorporation> CachedCorporationsById { get; }
        void GetCorporationInfo(Int64 corpId);
        void RemoveCorporation(Int64 corpId);
    }
}
