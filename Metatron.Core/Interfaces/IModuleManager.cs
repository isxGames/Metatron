using System;

namespace Metatron.Core.Interfaces
{
    public interface IModuleManager : IModule, IDisposable
    {
        string UplinkName { get; }
        bool IsNonCombatMode { get; }
        void DelayPulseByTicks(int pulses);
        void DelayPulseByHighestTime(int seconds);
        void DelayPulseByTotalTime(int seconds);
    }
}
