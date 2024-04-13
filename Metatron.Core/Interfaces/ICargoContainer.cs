using EVE.ISXEVE;
using EVE.ISXEVE.Interfaces;

namespace Metatron.Core.Interfaces
{
    public interface ICargoContainer
    {
        IEntityWrapper CurrentContainer { get; }
        bool IsCurrentContainerWindowOpen { get; }
        bool IsCurrentContainerWindowActive { get; }
        double Capacity { get; }
        double UsedCapacity { get; }
        void MakeCurrentContainerWindowActive();
        IEveInvChildWindow GetChildWindow();
    }
}
