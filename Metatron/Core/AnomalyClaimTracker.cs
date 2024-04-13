using System;
using System.Collections.Generic;
using Metatron.Core.EventCommunication;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    public class AnomalyClaimTracker : ModuleBase, IAnomalyClaimTracker
    {
        private volatile Dictionary<Int64, KeyValuePair<Int64, DateTime>> _claimsByAnomalyId = new Dictionary<Int64, KeyValuePair<Int64, DateTime>>();

        private readonly IEventCommunications _eventCommunications;
        private readonly IMeCache _meCache;

        public AnomalyClaimTracker(ILogging logging, IEventCommunications eventCommunications, IMeCache meCache) : base(logging)
        {
            IsEnabled = true;
            ModuleManager.ModulesToPulse.Add(this);

            ModuleName = "AnomalyClaimTracker";

            _eventCommunications = eventCommunications;
            _meCache = meCache;

            _eventCommunications.AnomalyClaimedEvent.EventRaised += AnomalyClaimed;
        }

        /// <summary>
        /// Update our local cache of anomaly claims from an event-based claim request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="anomalyClaimedEventArgs"></param>
        private void AnomalyClaimed(object sender, AnomalyClaimedEventArgs anomalyClaimedEventArgs)
        {
            var methodName = "AnomalyClaimed";
            LogTrace(methodName, "AnomalyId: {0}, Who: {1}, When: {2}", anomalyClaimedEventArgs.AnomalyId, anomalyClaimedEventArgs.SendingFleetMemberCharId, anomalyClaimedEventArgs.ClaimTime);

            lock (_claimsByAnomalyId)
            {
                KeyValuePair<Int64, DateTime> pair;

                //If it's not yet claimed...
                if (!_claimsByAnomalyId.TryGetValue(anomalyClaimedEventArgs.AnomalyId, out pair))
                {
                    //claim it
                    _claimsByAnomalyId.Add(anomalyClaimedEventArgs.AnomalyId,
                        new KeyValuePair<long, DateTime>(anomalyClaimedEventArgs.SendingFleetMemberCharId, anomalyClaimedEventArgs.ClaimTime));
                }
                else
                {
                    //Otherwise...
                    //If the new claim is earlier than the existing claim, replace the existing with the earlier.
                    if (anomalyClaimedEventArgs.ClaimTime < pair.Value)
                    {
                        _claimsByAnomalyId.Remove(anomalyClaimedEventArgs.AnomalyId);
                        _claimsByAnomalyId.Add(anomalyClaimedEventArgs.AnomalyId,
                            new KeyValuePair<long, DateTime>(anomalyClaimedEventArgs.SendingFleetMemberCharId, anomalyClaimedEventArgs.ClaimTime));
                    }
                }
            }
        }

        public override void Pulse()
        {
            if (!ShouldPulse()) return;

            //Lock to prevent race condition
            lock (_claimsByAnomalyId)
            {
                var idsToRemove = new List<Int64>();
                var removalTime = DateTime.Now.Subtract(new TimeSpan(0, 0, 15, 0));

                //Get the IDs of all anoamlies whose claim has expired
                foreach (var claimByAnomalyId in _claimsByAnomalyId)
                {
                    if (removalTime > claimByAnomalyId.Value.Value)
                        idsToRemove.Add(claimByAnomalyId.Key);
                }

                //Prune them
                foreach (var id in idsToRemove)
                {
                    _claimsByAnomalyId.Remove(id);
                }
            }
        }

        /// <summary>
        /// Looks for a claim to a given anomaly belonging to a player other than myself.
        /// </summary>
        /// <param name="anomalyId"></param>
        /// <returns></returns>
        public bool IsAnomalyClaimedByOther(Int64 anomalyId)
        {
            //Lock to prevent race condition
            lock (_claimsByAnomalyId)
            {
                //Check for an anomaly claim for the given ID
                KeyValuePair<Int64, DateTime> pair;

                //If there's no claim record, it's not claimed
                if (!_claimsByAnomalyId.TryGetValue(anomalyId, out pair))
                    return false;

                //If it's claimed by someone else, consider it claimed.
                return pair.Key != _meCache.ToEntity.Id;
            }
        }

        /// <summary>
        /// Send a claim request for the given anomaly.
        /// </summary>
        /// <param name="anomalyId"></param>
        public void ClaimAnomaly(Int64 anomalyId)
        {
            var methodName = "ClaimAnomaly";
            _logging.LogTrace(ModuleName, methodName, "AnomalyId: {0}", anomalyId);

            _eventCommunications.AnomalyClaimedEvent.SendEventFromArgs(_meCache.ToEntity.Id, _meCache.SolarSystemId, anomalyId, DateTime.Now);
        }

        /// <summary>
        /// Send a claim request for the given anomaly on behalf of the given entity.
        /// </summary>
        /// <param name="anomalyId"></param>
        /// <param name="entityId"></param>
        public void ClaimAnomaly(long anomalyId, long entityId)
        {
            var methodName = "ClaimAnomaly";
            _logging.LogTrace(ModuleName, methodName, "AnomalyId: {0}, EntityId: {1}", anomalyId, entityId);

            _eventCommunications.AnomalyClaimedEvent.SendEventFromArgs(entityId, _meCache.SolarSystemId, anomalyId, DateTime.Now);
        }
    }
}
