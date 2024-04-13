using System.Collections.Generic;
using System.Collections.ObjectModel;
using EVE.ISXEVE.Interfaces;

namespace Metatron.Core.Interfaces
{
    public interface IInventoryProvider
    {
        IEveInvWindow GetInventoryWindow();
        bool IsInventoryOpen { get; }
        void OpenInventory();

        bool IsCargoHoldActive { get; }
        double CargoCapacity { get; }
        double UsedCargoCapacity { get; }
        ReadOnlyCollection<IItem> Cargo { get; }
        IEveInvChildWindow GetCargoHoldWindow();
        void MakeCargoHoldActive();

        bool IsOreHoldActive { get; }
        double OreHoldCapacity { get; }
        double OreHoldUsedCapacity { get; }
        ReadOnlyCollection<IItem> OreHoldCargo { get; }
        IEveInvChildWindow GetOreHoldWindow();
        void MakeOreHoldActive();
    }
}