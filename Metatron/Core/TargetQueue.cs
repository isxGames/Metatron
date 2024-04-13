using System;   
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Metatron.Core.Config;
using Metatron.Core.EventCommunication;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    public sealed class TargetQueue : ModuleBase, ITargetQueue
    {
        private EventHandler<FleetNeedCombatAssistEventArgs> _fleetNeedCombatAssist;
        private readonly List<FleetNeedCombatAssistEventArgs> _fleetNeedCombatAssistEventArgsToValidate = new List<FleetNeedCombatAssistEventArgs>(); 

        private readonly List<QueueTarget> _targets = new List<QueueTarget>(); 

        public ReadOnlyCollection<QueueTarget> Targets
        {
            get { return _targets.AsReadOnly(); }
        }

        private readonly IMeCache _meCache;
        private readonly IEntityProvider _entityProvider;
        private readonly IMiningConfiguration _miningConfiguration;
        private readonly IMainConfiguration _mainConfiguration;

        public TargetQueue(IMeCache meCache, IEntityProvider entityProvider, IMiningConfiguration miningConfiguration, IMainConfiguration mainConfiguration)
        {
			IsEnabled = true;
			ModuleName = "TargetQueue";
			PulseFrequency = 1;
            ModuleManager.ModulesToPulse.Add(this);
            ActiveTargetActiveSince = DateTime.MaxValue;

            _meCache = meCache;
            _entityProvider = entityProvider;
            _miningConfiguration = miningConfiguration;
            _mainConfiguration = mainConfiguration;
        }

        public override bool Initialize()
        {
            _fleetNeedCombatAssist = OnFleetNeedCombatAssist;
            Metatron.EventCommunications.FleetNeedCombatAssistEvent.EventRaised += _fleetNeedCombatAssist;

            IsInitialized = true;
            return IsInitialized;
        }

        public override bool OutOfFrameCleanup()
        {
            if (Metatron.EventCommunications.FleetNeedCombatAssistEvent != null)
// ReSharper disable DelegateSubtraction
                Metatron.EventCommunications.FleetNeedCombatAssistEvent.EventRaised -= _fleetNeedCombatAssist;
// ReSharper restore DelegateSubtraction

            _fleetNeedCombatAssist = null;

            IsCleanedUpOutOfFrame = true;
            return IsCleanedUpOutOfFrame;
        }

        public override void Pulse()
		{
            var methodName = "TargetQueue";
			LogTrace(methodName);

			if (!ShouldPulse()) 
				return;

			StartPulseProfiling();

            PruneQueue();

            TrackActiveTargetTimer();

            ValidateFleetNeedCombatAssistEventArgs();

			EndPulseProfiling();
		}

        // Declare a private field to store the last known value of _meCache.ActiveTargetId
        private long _lastKnownActiveTargetId = -1;

        // Declare a public property to store the DateTime when the active target was last changed
        public DateTime ActiveTargetActiveSince { get; private set; }

        private void TrackActiveTargetTimer()
        {
            var methodName = "TrackActiveTargetTimer";
            LogTrace(methodName);

            if (_targets.Count > 0)
            {
                // Check if _meCache.ActiveTargetId has changed
                if (_meCache.ActiveTargetId != _lastKnownActiveTargetId)
                {
                    // Update ActiveTargetActiveSince to the current time
                    ActiveTargetActiveSince = DateTime.Now;

                    // Update _lastKnownActiveTargetId to the current _meCache.ActiveTargetId
                    _lastKnownActiveTargetId = _meCache.ActiveTargetId;
                }
            }
        }


        private void PruneQueue()
        {
            var methodName = "PruneQueue";
			LogTrace(methodName);

			//Since the MasterCache now holds cached versions of -all- valid entities, check if our QueueTarget is
			//in the cache. If it isn't, it isn't a valid entity and we can remove it.
            for (var index = 0; index < _targets.Count; index++)
            {
                //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
				//	"PruneQueue", String.Format("Checking entity {0} ({1}), distance {2}",
				//	tempEntity.Name, tempEntity.ID, tempEntity._distance)));
                var queueTarget = _targets[index];
                if (!_entityProvider.EntityWrappersById.ContainsKey(queueTarget.Id) ||
                    _entityProvider.EntityWrappersById[queueTarget.Id].Distance >= (int)Ranges.Warp * 0.95)
                {
                    //Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                    //    "PruneQueue", String.Format("Removing QueueTarget {0}",
                    //    Targets[idx].ID)));
                    _targets.RemoveAt(index);
                    index--;
                }
            }
        }

        public bool IsQueued(Int64 entityId)
        {
            var methodName = "IsQueued";
			LogTrace(methodName);

            return _targets.Any(target => target.Id == entityId);
        }

        /// <summary>
        /// Enqueue a target.
        /// </summary>
        /// <param name="entityId">Target's ID</param>
        /// <param name="priority">Priority of the target, lower number is higher priority</param>
        /// <param name="type">Type of the target, determines what handles it</param>
        //public void EnqueueTarget(Int64 entityId, int priority, TargetTypes type)
        //{
        //    EnqueueTarget(entityId, priority, GetTargetSubpriority(entityId), type);
        //}

        /// <summary>
        /// Enqueue a target.
        /// </summary>
        /// <param name="entityId">Target's ID</param>
        /// <param name="priority">Priority of the target, lower number is higher priority</param>
        /// <param name="subPriority">Sub-priority for sorting</param>
        /// <param name="type">Type of the target, determines what handles it</param>
        public void EnqueueTarget(Int64 entityId, int priority, TargetTypes type)
        {
            var methodName = "EnqueueTarget";
			LogTrace(methodName, "{0},{1},{2}", entityId, priority, type);
            var subPriority = GetTargetSubpriority(entityId);
            var trackingScore = GetTrackingScore(entityId);
            if (!IsQueued(entityId))
            {
				LogMessage(methodName, LogSeverityTypes.Debug, "Queueing entity with id {0}, priority {1}, sub priority {2}, type {3}",
					entityId, priority, subPriority, type);
                _targets.Add(new QueueTarget(entityId, priority, subPriority, type, trackingScore));
            }
            else
            {
                var existingTarget = _targets.FirstOrDefault(queueTarget => queueTarget.Id == entityId);

            	if (existingTarget == null) return;

            	var shouldUpdate = false;

            	if (priority < existingTarget.Priority)
            	{
            		LogMessage(methodName, LogSeverityTypes.Debug, "Queue target {0} has increased in priority to {1}.",
            		           existingTarget.Id, priority);
            		shouldUpdate = true;
            	}
            	else if (priority == existingTarget.Priority && subPriority > existingTarget.SubPriority)
            	{
            		LogMessage(methodName, LogSeverityTypes.Debug, "Queue target {0} has increased in subpriority to {1}.",
            		           existingTarget.Id, subPriority);
            		shouldUpdate = true;
            	}
            	else if (type != existingTarget.Type)
            	{
            		LogMessage(methodName, LogSeverityTypes.Debug, "Queue target {0} has changed type to {1}.",
            		           existingTarget.Id, type);
            		shouldUpdate = true;
            	}

            	if (shouldUpdate)
            	{
                    existingTarget.UpdateTarget(priority, subPriority, type, trackingScore);
            	}
            }
        }

        public double GetTrackingScore(Int64 entityId)
        {
            var methodName = "GetTrackingScore";
            LogTrace(methodName, entityId.ToString());

            var entity = _entityProvider.EntityWrappersById[entityId];
            var velocity = entity.ToEntity.Velocity;

            var trackingSpeedSelect = Core.Metatron.Ship.WeaponModules.Select(w => w.TrackingSpeed).Min();
            double trackingSpeed;
            if (!trackingSpeedSelect.HasValue) trackingSpeed = 5;
            else trackingSpeed = trackingSpeedSelect.Value;
            double signatureRadius;

            if (Core.Metatron.EntityCache.CachedEntitiesById.ContainsKey(entity.TypeID))
            {
                signatureRadius = Core.Metatron.EntityCache.CachedEntitiesById[entity.TypeID].DogmaAttributes.FirstOrDefault
                    (d => d.Id == (int)DogmaAttributes.SignatureRadius).Value;
            }
            else
            {
                Core.Metatron.EntityCache.GetEntityInfo(entity.TypeID);
                signatureRadius = 30;
            }


            // Safely handle division by zero
            double trackingScore;
            if (velocity == 0)
            {
                trackingScore = trackingSpeed * signatureRadius;
            }
            else
            {
                trackingScore = (trackingSpeed * signatureRadius) / velocity;
            }
            LogTrace(methodName, $"({trackingSpeed} * {signatureRadius}) / {velocity} = {trackingScore}");
            return trackingScore;
        }

        public void DequeueTarget(Int64 entityId)
        {
            var methodName = "DequeueTarget";
			LogTrace(methodName, "Entity: {0}", entityId);

            for (var index = 0; index < _targets.Count; index++)
            {
                if (_targets[index].Id != entityId) 
					continue;

                _targets.RemoveAt(index);
            	return;
            }
        }

        /// <summary>
        /// Handle the FleetNeedCombatAssist event. Queue the eventargs for in-pulse validation.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see>
        ///                         <cref>Metatron.Core.EventCommunication.FleetNeedCombatAssistEventArgs</cref>
        ///                     </see>
        ///     instance containing the event data.</param>
        private void OnFleetNeedCombatAssist(object sender, FleetNeedCombatAssistEventArgs e)
        {
            var methodName = "OnFleetNeedCombatAssist";
            LogTrace(methodName, "Sender: {0}, FleetMember: {1}, SolarSystem: {2}, Target: {3}",
                     sender, e.SendingFleetMemberCharId, e.SolarSystemId, e.Target.Id);

            lock (_fleetNeedCombatAssistEventArgsToValidate)
            {
                if (!_fleetNeedCombatAssistEventArgsToValidate.Contains(e))
                {
                    _fleetNeedCombatAssistEventArgsToValidate.Add(e);
                }
            }
        }

        /// <summary>
        /// Validates any fleet combat assistance requests and if valid, queues the requested target.
        /// </summary>
        private void ValidateFleetNeedCombatAssistEventArgs()
        {
            var methodName = "ValidateFleetNeedCombatAssistEventArgs";
            LogTrace(methodName);

            lock (_fleetNeedCombatAssistEventArgsToValidate)
            {
                if (!_fleetNeedCombatAssistEventArgsToValidate.Any())
                    return;

                var inCombatCapableMode = InCombatCapableMode();

                if (!inCombatCapableMode)
                {
                    //LogMessage(methodName, LogSeverityTypes.Debug, "Not in a combat-capable mode - ignoring combat assist requests.");
                    return;
                }
                
                foreach (var eventArgs in _fleetNeedCombatAssistEventArgsToValidate)
                {
                    if (_meCache.SolarSystemId != eventArgs.SolarSystemId)
                    {
                        //LogMessage(methodName, LogSeverityTypes.Debug, "Combat assist request from player {0} is invalid - request solar system {1}, current solar system {2}.",
                        //    eventArgs.SendingFleetMemberId, eventArgs.SolarSystemId, _meCache.SolarSystemId);
                        continue;
                    }

                    if (!_entityProvider.EntityWrappersById.ContainsKey(eventArgs.Target.Id))
                    {
                        //LogMessage(methodName, LogSeverityTypes.Debug, "Combat assist request from player {0} is invalid - requested entity ID {1} doesn't exist.",
                        //    eventArgs.SendingFleetMemberId, eventArgs.Target.Id);
                        continue;
                    }

                    var existingTarget = _targets.FirstOrDefault(target => target.Id == eventArgs.Target.Id);

                    if (existingTarget == null)
                        _targets.Add(eventArgs.Target);
                }

                _fleetNeedCombatAssistEventArgsToValidate.Clear();
            }
        }

        private bool InCombatCapableMode()
        {
            var methodName = "InCombatCapableMode";
            LogTrace(methodName);

            switch (_mainConfiguration.ActiveBehavior)
            {
                case BotModes.Mining:
                    return _meCache.Ship.Drones.Count > 0 &&
                           !_miningConfiguration.UseMiningDrones;
                case BotModes.Ratting:
                    return true;
                case BotModes.Missioning:
                    return true;
                case BotModes.CombatAssist:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Clear the target queue.
        /// </summary>
        public void ClearQueue()
        {
            _targets.Clear();
        }

        public int GetTargetSubpriority(long entityId)
        {
            var methodName = "GetTargetPriority";
            LogTrace(methodName);

            var entity = _entityProvider.EntityWrappersById[entityId];
            if (entity == null)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, $"We tried to get priority on non-existent target: {entityId}");
                return (int)TargetPriorities.None;
            }
            else
            {
                var cachedEntity = Core.Metatron.EntityCache.CachedEntitiesById.ContainsKey(entity.TypeID) ? Core.Metatron.EntityCache.CachedEntitiesById[entity.TypeID] : null;
                if (cachedEntity == null)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, $"Tried to get priority on unknown target {entity.Name} ({entityId}) - Asking ESI what it is");
                    Core.Metatron.EntityCache.GetEntityInfo(entity.TypeID);
                    return (int)TargetPriorities.CombatDefault;
                }
                else
                {
                    return (int)DoDogmaCalculations(cachedEntity.DogmaAttributes);
                }
            }
        }

        private double DoDogmaCalculations(List<CachedEntity.DogmaAttribute> dogmaAttributes)
        {
            var methodName = "DoDogmaCalculations";

            // Calculate resistances
            double trackingScore = CalculateTrackingScore(dogmaAttributes);
            // Calculate other attributes like bounty, agility, etc.
            double rewardScore = CalculateRewardScore(dogmaAttributes);

            // Combine these into a single priority value
            var priority = (0.8 * trackingScore) + 
                           (0.3 * rewardScore);

            LogTrace(methodName, priority.ToString());
            return priority;
        }

        private double CalculateRewardScore(List<CachedEntity.DogmaAttribute> dogmaAttributes)
        {
            var methodName = "CalculateRewardScore";

            var bounty = dogmaAttributes.FirstOrDefault(a => a.Id == (int)DogmaAttributes.BountyReward)?.Value ?? 0;    
            //var maxSecurityGain = dogmaAttributes.FirstOrDefault(a => a.Id == (int)DogmaAttributes.EntitySecurityMaxGain)?.Value ?? 0;
            var securityStatusKillBonus = dogmaAttributes.FirstOrDefault(a => (int)a.Id == (int)DogmaAttributes.EntitySecurityStatusKillBonus)?.Value ?? 0;

            var rewardScore = bounty + (securityStatusKillBonus * 1000);
            LogTrace(methodName, $"{bounty} + ({securityStatusKillBonus} * 1000) == {rewardScore}");
            return rewardScore;
        }

        private double CalculateTrackingScore(List<CachedEntity.DogmaAttribute> dogmaAttributes)
        {
            var methodName = "CalculateTrackingScore";

            var agility = dogmaAttributes.FirstOrDefault(a => a.Id == (int)DogmaAttributes.Agility)?.Value ?? 0;
            var signatureRadius = dogmaAttributes.FirstOrDefault(a => (int)a.Id == (int)DogmaAttributes.SignatureRadius)?.Value ?? 0;
            var scanResolution = dogmaAttributes.FirstOrDefault(a => (int)a.Id == (int)DogmaAttributes.ScanResolution)?.Value ?? 0;

            var trackingScore = (scanResolution + signatureRadius) * (agility + 1);
            LogTrace(methodName, $"({scanResolution} + {signatureRadius}) * ({agility} + 1) == {trackingScore}");
            return trackingScore;
        }

        private enum TargetPriorities
        {
            None = 0,
            NonCombat = 1,
            CombatDefault = 50,
        }
    }
}
