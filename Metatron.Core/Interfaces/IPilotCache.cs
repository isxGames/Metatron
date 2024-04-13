using System;
using System.Collections.Generic;
using EVE.ISXEVE;

namespace Metatron.Core.Interfaces
{
    public interface IPilotCache : IModule
    {
        Dictionary<Int64, CachedPilot> CachedPilotsById { get; }
        void AddPilot(Pilot pilot, string corporationName, string allianceName);
        void AddPilot(CachedPilot cp);
        void LoadPilotCache(string charName);
    }
}
