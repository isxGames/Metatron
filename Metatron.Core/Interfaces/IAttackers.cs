using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Metatron.Core.Interfaces
{
    public interface IAttackers
    {
        bool HasFullAggro { get; }
        bool IsUnderDangerousEwarAttack { get; }
        ReadOnlyCollection<IEntityWrapper> ThreatEntities { get; }
        Dictionary<Int64, QueueTarget> QueueTargetsByEntityId { get; }
        bool HaveHostilesRecentlySpawned { get; }
        bool IsNpc(IEntityWrapper entity);
        int GetTargetPriority(IEntityWrapper entity);
        bool IsRatTarget(IEntityWrapper entity);
        bool IsConcordTarget(int groupId);
        DamageProfile GetDamageProfileFromNpc(int groupId);
        bool IsOfficer(int groupId);
        bool IsHauler(int groupId);
        bool IsCommander(int groupId);
        bool IsExhumerOrIndustrial(int groupId);
        bool IsDreadnoughtSpawn(int groupId);
    }
}
