using System.Collections.Generic;
using EVE.ISXEVE;

namespace Metatron.Core.Interfaces
{
    public interface IAnomalyProvider
    {
        IList<SystemAnomaly> GetAnomalies();
    }
}