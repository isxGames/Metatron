using System;

namespace Metatron.Core.Interfaces
{
    public interface IDrones
    {
        Int64 DroneTargetEntityId { get; }
        IEntityWrapper DroneTarget { get; }
        bool RecentlyLaunchedDrones { get; }
        bool DroneTargetIsValid { get; }
        int MaxDronesInSpace { get; }
        bool DronesRecalled { get; }
        int DronesInSpace { get; }
        int DronesInBay { get; }
        int TotalDrones { get; }
        bool IsAnyDroneIdle { get; }
        void LaunchAllDrones();
        bool CanLaunchDrones();
        bool SendAllDrones();
        void RecallAllDrones(bool toBay);
        bool CanAttackEntity(IEntityWrapper entity);
    }
}
