using System;

namespace Metatron.Core.Interfaces
{
    public interface IAnomalyClaimTracker
    {
        /// <summary>
        /// Looks for a claim to a given anomaly belonging to a player other than myself.
        /// </summary>
        /// <param name="anomalyId"></param>
        /// <returns></returns>
        bool IsAnomalyClaimedByOther(Int64 anomalyId);

        /// <summary>
        /// Send a claim request for the given anomaly.
        /// </summary>
        /// <param name="anomalyId"></param>
        void ClaimAnomaly(Int64 anomalyId);

        /// <summary>
        /// Send a claim request for the given anomaly on behalf of the given entity.
        /// </summary>
        /// <param name="anomalyId"></param>
        /// <param name="entityId"></param>
        void ClaimAnomaly(long anomalyId, long entityId);
    }
}