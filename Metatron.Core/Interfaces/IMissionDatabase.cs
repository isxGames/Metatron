using System.Collections.Generic;

namespace Metatron.Core.Interfaces
{
    public interface IMissionDatabase
    {
        void ReloadMissionDatabase();

        /// <summary>
        /// Get a mission from the database matching the given name, if it exists.
        /// Otherwise return null.
        /// </summary>
        /// <param name="missionName"></param>
        /// <returns></returns>
        Mission GetMissionByName(string missionName);

        List<DamageProfile> GetNpcResistanceProfiles(CachedMission cachedMission);
    }
}
