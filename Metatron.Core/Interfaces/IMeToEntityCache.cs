using System;

namespace Metatron.Core.Interfaces
{
    public interface IMeToEntityCache
    {
        IEntityWrapper Approaching { get; }
        string Name { get; }
        int Mode { get; }
        int TypeId { get; }
        int GroupId { get; }
        Int64 Id { get; }
        bool IsCloaked { get; }
        bool IsWarpScrambled { get; }
        double Velocity { get; }
        double MaxVelocity { get; }
        double X { get; }
        double Y { get; }
        double Z { get; }
        bool IsValid { get; }
        double Radius { get; }
        double DistanceTo(Int64 entityId);
        double DistanceTo(double x, double y, double z);
    }
}
