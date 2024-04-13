using System.Collections.ObjectModel;
using EVE.ISXEVE;

namespace Metatron.Core.Interfaces
{
    public interface ISocial
    {
        bool IsLocalSafe { get; }
        ReadOnlyCollection<Pilot> LocalPilots { get; }
    }
}
