using System.Collections.Generic;

namespace Metatron.Core.Interfaces
{
    public interface IRandomWaitObject
    {
        void AddWait(KeyValuePair<int, int> rangeToWait, double chance);
        bool ShouldWait();
    }
}
