using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Metatron.Core.Interfaces
{
    public interface IAsteroidBelts
    {
        ReadOnlyCollection<IEntityWrapper> Asteroids { get; }
        ReadOnlyCollection<IEntityWrapper> AsteroidsInRange { get; }
        ReadOnlyCollection<IEntityWrapper> AsteroidsOutOfRange { get; }
        ReadOnlyCollection<CachedBelt> CachedBelts { get; }
        ReadOnlyCollection<CachedBookMarkedBelt> CachedBookMarkedBelts { get; }
        Dictionary<string, int> AsteroidTypeIdsByType { get; }
        Dictionary<int, string> AsteroidTypesByTypeId { get; }
        Dictionary<string, int> AsteroidGroupIdsByGroup { get; }
        Dictionary<int, string> AsteroidGroupsByGroupId { get; }
        bool AllBeltsEmpty { get; }
        bool AllBookMarkedBeltsEmpty { get; }
        int LastSolarSystem { get; }
        CachedBelt CurrentBelt { get; }
        CachedBookMarkedBelt CurrentBookMarkedBelt { get; }
        bool IsBeltEmpty { get; }
        bool IsAtAsteroidBelt();
        void ChangeBelts(bool canResetBelts, bool forceMarkEmpty);
    }
}
