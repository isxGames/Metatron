using System;
using System.Collections.ObjectModel;
using EVE.ISXEVE;
using EVE.ISXEVE.Interfaces;

namespace Metatron.Core.Interfaces
{
    public interface IShipCache
    {
        MyShip Ship { get; }
        Int64 Id { get; }
        string Name { get; }
        double ArmorPct { get; }
        double StructurePct { get; }
        double ShieldPct { get; }
        double CapacitorPct { get; }
        double CargoCapacity { get; }
        double UsedCargoCapacity { get; }
        double MaxTargetRange { get; }
        double MaxLockedTargets { get; }
        double Capacitor { get; }
        double Armor { get; }
        double Shield { get; }
        double MaxArmor { get; }
        double MaxShield { get; }
        double MaxCapacitor { get; }
        double DronebayCapacity { get; }
        ReadOnlyCollection<IItem> Cargo { get; }
        ReadOnlyCollection<IItem> Drones { get; }
        ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> Modules { get; }
        ReadOnlyCollection<IItem> OreHoldCargo { get; }
    }
}
