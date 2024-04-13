using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EVE.ISXEVE;
using Metatron.Core;
using LavishScriptAPI;
using Metatron.Core.Config;
using Metatron.Core.Interfaces;
using System.Speech.Synthesis.TtsEngine;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace Metatron.ActionModules
{
    // ReSharper disable CompareOfFloatsByEqualityOperator
    // ReSharper disable PossibleMultipleEnumeration
    // ReSharper disable ConvertToConstant.Local
    internal sealed class NonOffensive : ModuleBase
    {
        //bool canChangeTarget = false;
        private DateTime? _nextShortCycleDeactivation;
        private double _capacitor;

        private readonly RandomWaitObject _randomWait = new RandomWaitObject("NonOffensive");

        private readonly IMeCache _meCache;

        private readonly IMiningConfiguration _miningConfiguration;
        private readonly IDefensiveConfiguration _defensiveConfiguration;

        private readonly IEntityProvider _entityProvider;
        private readonly ITargetQueue _targetQueue;

        private readonly IShip _ship;
        private readonly IDrones _drones;

        private readonly ITargeting _targeting;
        private readonly IMovement _movement;

        public NonOffensive(IMeCache meCache, IMiningConfiguration miningConfiguration, IDefensiveConfiguration defensiveConfiguration, IEntityProvider entityProvider,
            ITargetQueue targetQueue, IShip ship, IDrones drones, ITargeting targeting, IMovement movement)
        {
            _meCache = meCache;
            _miningConfiguration = miningConfiguration;
            _defensiveConfiguration = defensiveConfiguration;
            _entityProvider = entityProvider;
            _targetQueue = targetQueue;
            _ship = ship;
            _drones = drones;
            _targeting = targeting;
            _movement = movement;

            ModuleManager.ModulesToPulse.Add(this);
            PulseFrequency = 1;
            ModuleName = "NonOffensive";

            _randomWait.AddWait(new KeyValuePair<int, int>(16, 30), 1);
            _randomWait.AddWait(new KeyValuePair<int, int>(6, 15), 3);
            _randomWait.AddWait(new KeyValuePair<int, int>(3, 5), 6);
            _randomWait.AddWait(new KeyValuePair<int, int>(1, 2), 10);
        }

        public override void Pulse()
        {
            var methodName = "Pulse";
            LogTrace(methodName);

            if (!ShouldPulse())
                return;

            if (!_meCache.InSpace || _meCache.InStation)
                return;

            if (_movement.IsMoving && _movement.MovementType == MovementTypes.Warp)
            {
                //Make sure all modules are off
                _ship.DeactivateModuleList(_ship.MiningLaserModules, true);
                _ship.DeactivateModuleList(_ship.SalvagerModules, true);

                if (Core.Metatron.Config.MiningConfig.UseMiningDrones && _drones.DronesInSpace > 0)
                {
                    _drones.RecallAllDrones(true);
                }
                return;
            }

            if (_randomWait.ShouldWait()) return;

            var activeTargetId = _meCache.ActiveTargetId;
            if (activeTargetId <= 0) return;

            StartPulseProfiling();
            _capacitor = _meCache.Ship.Capacitor;

            var activeTarget = _entityProvider.EntityWrappersById[activeTargetId];
            var activeQueueTarget = _targeting.GetActiveQueueTarget();

            if (activeQueueTarget != null && !_targeting.WasTargetChangedThisFrame)
            {
                switch (activeQueueTarget.Type)
                {
                    case TargetTypes.Mine:
                        //If it's not in range, approach
                        //Otherwise mine
                        if (_entityProvider.EntityWrappersById[activeQueueTarget.Id].Distance > _ship.MaximumMiningRange &&
                            (_meCache.ToEntity.Approaching == null || _meCache.ToEntity.Approaching.ID != activeQueueTarget.Id))
                        {
                            //Dequeue it, shouldn't be queued any more
                            _targetQueue.DequeueTarget(activeQueueTarget.Id);
                            //Unlock it
                            _targeting.UnlockTarget(activeTarget);
                        }
                        break;
                    case TargetTypes.LootSalvage:
                        TractorTarget(activeQueueTarget);
                        break;
                }
            }

            MineTargets();
            EndPulseProfiling();
        }

        /// <summary>
        /// Mine queued mining targets by order of priority.
        /// </summary>
        private void MineTargets()
        {
            var methodName = "MineTargets";
            LogTrace(methodName);

            var miningTargets = GetMiningTargets();

            if (!miningTargets.Any()) return;

            UseMiningDrones(miningTargets);

            UseMiningLasers(miningTargets);
        }

        /// <summary>
        /// Use all mining lasers on targets ordered by priority, optionally distributing them.
        /// </summary>
        /// <param name="miningTargets"></param>
        private void UseMiningLasers(ICollection<IEntityWrapper> miningTargets)
        {
            var methodName = "UseMiningLasers";
            LogTrace(methodName);

            var miningLasers = _ship.MiningLaserModules;
            var lowestMaximumLaserRange = GetLowestMaximumLaserRange();

            if (lowestMaximumLaserRange == 0)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Error: All mining lasers had an invalid optimal range.");
                return;
            }

            var takeCount = _miningConfiguration.DistributeLasers ? miningLasers.Count : 1;

            var miningTargetsInRange = miningTargets
                .Where(target => target.Distance <= lowestMaximumLaserRange);

            var chosenTargets = miningTargetsInRange
                .Take(takeCount);

            LogMessage(methodName, LogSeverityTypes.Debug, "Targets in range: {0}, chosen targets: {1}",
                miningTargetsInRange.Count(), chosenTargets.Count());

            //var chosenTargets = miningTargets
            //    .Where(target => target.Distance <= lowestMaximumLaserRange)
            //    .Take(takeCount);

            if (!chosenTargets.Any())
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "No mining targets were within the maximum range of {0}m.",
                    lowestMaximumLaserRange);
                return;
            }

            //If we're short cycling and need to cut lasers, deactivate lasers and return.
            if (_miningConfiguration.ShortCycle)
            {
                //Short cycling should do nothing or deactivate all lasers. If we deactivate lasers, there's nothing else to do this pulse, return early.
                var wereLasersDeactivated = ShortCycleLasers(miningLasers);
                if (wereLasersDeactivated) return;
            }

            //Distribute out my lasers
            var activeModuleCountByTargetId = new Dictionary<Int64, int>();
            var intendedModuleCountByTargetId = new Dictionary<Int64, int>();
            DetermineModuleCountsForTargets(chosenTargets, miningLasers, activeModuleCountByTargetId, intendedModuleCountByTargetId);

            //If there are no intended modules on my chosen targets, I'm done.
            if (intendedModuleCountByTargetId.Values.Sum() == 0) return;

            //Track which lasers to activate and activate them outside the loop.
            //This way, we don't accidentally activate laser A then rearm laser B in the same pulse. A human can't do that (easily).
            var miningLasersToActivateOnActiveTarget = new List<EVE.ISXEVE.Interfaces.IModule>();

            foreach (var miningLaser in miningLasers)
            {
                //If the laser's target is invalid, deactivate it.
                if (miningLaser.IsActive)
                {
                    if (!miningLaser.IsDeactivating && !miningTargets.Any(miningTarget => miningTarget.ID == miningLaser.TargetID))
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Deactivating module \"{0}\" ({1}) because its target is invalid.",
                            miningLaser.ToItem.Name, miningLaser.ToItem.GroupID);
                        miningLaser.Click();
                    }
                    continue;
                }

                //Pick a target for this laser
                var chosenTarget = GetBestTargetForMiningLaser(chosenTargets, activeModuleCountByTargetId, intendedModuleCountByTargetId);

                if (chosenTarget == null)
                {
                    //LogMessage(methodName, LogSeverityTypes.Debug, "Error: Unable to determine a target for mining laser \"{0}\" ({1}).");
                    continue;
                }

                LogMessage(methodName, LogSeverityTypes.Debug, "Chosen target for laser: \"{0}\" ({1})", chosenTarget.Name, chosenTarget.ID);

                //Do I need to re-arm this laser?
                var wasLaserRearmed = EnsureMiningLaserIsArmedForTarget(miningLaser, chosenTarget);
                if (wasLaserRearmed) return; // If a laser is re-armed we can do no other operation this pulse.

                //If the active target was changed this frame, the "active target" is no longer valid. Don't process any more.
                if (_targeting.WasTargetChangedThisFrame)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "The active target was changed this pulse.");
                    continue;
                }

                //Do we have capacitor to activate the laser?
                var canSafelyActivate = CanSafelyActivateMiningLaser(miningLaser);
                if (!canSafelyActivate) continue;

                //Do I need to change target?
                if (_meCache.ActiveTargetId != chosenTarget.ID)
                {
                    //Only change target if we don't have ANY lasers we plan on activating on the current target
                    if (miningLasersToActivateOnActiveTarget.Count == 0 && chosenTarget.IsLockedTarget && _targeting.CanChangeTarget)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Changing target to \"{0}\" ({1}) for module \"{2}\" ({3}).",
                            chosenTarget.Name, chosenTarget.ID, miningLaser.ToItem.Name, miningLaser.ToItem.ID);
                        _targeting.ChangeTargetTo(chosenTarget, true);
                    }

                    continue;
                }

                //It's armed correctly and we have its target active - activate it.
                miningLasersToActivateOnActiveTarget.Add(miningLaser);
                LogMessage(methodName, LogSeverityTypes.Debug, "We should activate mining laser \"{0}\" ({1}) on active target \"{2}\" ({3}).",
                    miningLaser.ToItem.Name, miningLaser.ID, _meCache.ActiveTarget.Name, _meCache.ActiveTargetId);
            }

            //why are we using lasers bakcwards?

            foreach (var miningLaser in miningLasersToActivateOnActiveTarget)
            {
                miningLaser.Click();

                //If I'm short cycling then update the next short cycle delay.
                //This will reset with the last laser activated, so as to err on the side of cap safety.
                if (Core.Metatron.Config.MiningConfig.ShortCycle)
                {
                    UpdateNextShortCycleDeactivation(miningLasers);
                }
            }
        }

        /// <summary>
        /// Determine the best of the given targets for the next mining laser.
        /// </summary>
        /// <param name="chosenTargets"></param>
        /// <param name="activeModuleCountByTargetId"></param>
        /// <param name="intendedModuleCountByTargetId"></param>
        /// <returns></returns>
        private IEntityWrapper GetBestTargetForMiningLaser(IEnumerable<IEntityWrapper> chosenTargets, IDictionary<long, int> activeModuleCountByTargetId,
                                                           IDictionary<long, int> intendedModuleCountByTargetId)
        {
            var methodName = "GetBestTargetForMiningLaser";
            LogTrace(methodName);

            IEntityWrapper chosenTarget = null;
            foreach (var target in chosenTargets)
            {
                var activeModuleCount = activeModuleCountByTargetId[target.ID];
                var intendedModuleCount = intendedModuleCountByTargetId[target.ID];

                var neededModules = intendedModuleCount - activeModuleCount;
                if (neededModules <= 0) continue;

                chosenTarget = target;
                activeModuleCountByTargetId[target.ID]++;
                break;
            }
            return chosenTarget;
        }

        /// <summary>
        /// Determine the counts of modules active against a target and intended for a target.
        /// </summary>
        /// <param name="chosenTargets"></param>
        /// <param name="miningLasers"></param>
        /// <param name="activeModuleCountByTargetId"></param>
        /// <param name="intendedModuleCountByTargetId"></param>
        private void DetermineModuleCountsForTargets(IEnumerable<IEntityWrapper> chosenTargets, ICollection<EVE.ISXEVE.Interfaces.IModule> miningLasers,
                                                     IDictionary<long, int> activeModuleCountByTargetId,
                                                     IDictionary<long, int> intendedModuleCountByTargetId)
        {
            var methodName = "DetermineModuleCountsForTargets";
            LogTrace(methodName);

            var temporaryTargetList = new List<IEntityWrapper>(chosenTargets);
            for (var index = 0; index < temporaryTargetList.Count; temporaryTargetList.RemoveAt(index))
            {
                var chosenTarget = temporaryTargetList[index];

                var countOnTarget = miningLasers.Count(module => module.IsActive && module.TargetID == chosenTarget.ID);
                //# lasers on a given target = ceiling(# lasers / # targets)
                //e.g. 4 lasers, 3 targets: laserson first target = ceiling(4/3) = 2, now have 2 lasers left, lasers on 2nd target = ceiling(2/2) = 1
                var intendedLaserCount = (int)Math.Ceiling((double)miningLasers.Count / temporaryTargetList.Count);

                activeModuleCountByTargetId.Add(chosenTarget.ID, countOnTarget);
                intendedModuleCountByTargetId.Add(chosenTarget.ID, intendedLaserCount);
            }
        }

        /// <summary>
        /// Determine whether or not we can activate a mining laser without going critically low on capacitor.
        /// </summary>
        /// <param name="miningLaser"></param>
        private bool CanSafelyActivateMiningLaser(EVE.ISXEVE.Interfaces.IModule miningLaser)
        {
            var methodName = "CanSafelyActivateMiningLaser";
            LogTrace(methodName);

            if (miningLaser.ActivationCost == null)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Error: Module \"{0}\" ({1}) has an invalid activation cost.",
                    miningLaser.ToItem.Name, miningLaser.ID);
                return true;
            }

            var minimumCapacitor = CalculateMinimumCapacitor();

            LogMessage(methodName, LogSeverityTypes.Debug, "Capacitor: {0}, minimumCapacitor: {1}, activationCost: {2}",
                _capacitor, minimumCapacitor, miningLaser.ActivationCost.Value);

            var canSafelyActivateMiningLaser = _capacitor > minimumCapacitor + miningLaser.ActivationCost.Value;
            if (canSafelyActivateMiningLaser)
            {
                _capacitor -= miningLaser.ActivationCost.Value;
            }

            return canSafelyActivateMiningLaser;
        }

        /// <summary>
        /// Calculate the minimum capacitor amount we need for safe operation.
        /// </summary>
        /// <returns></returns>
        private double CalculateMinimumCapacitor()
        {
            var methodName = "CalculateMinimumCapacitor";
            LogTrace(methodName);

            var minimumCapacitorPercent = ((double)(_defensiveConfiguration.MinimumCapPct + 5)) / 100;
            var minimumCapacitor = _meCache.Ship.MaxCapacitor * minimumCapacitorPercent;

            var hardenerActivationCosts = _ship.ActiveHardenerModules.Sum(module => module.ActivationCost.GetValueOrDefault(0));
            minimumCapacitor += hardenerActivationCosts;

            var boosterActivationCosts = _ship.ShieldBoosterModules.Sum(module => module.ActivationCost.GetValueOrDefault(0));
            minimumCapacitor += boosterActivationCosts;

            LogMessage(methodName, LogSeverityTypes.Debug, "minimumCapPct: {0:F}, maxCap: {1}, hardenerCosts: {2:F}, boosterCosts: {3:F}, minimumCap: {4:F}",
                minimumCapacitorPercent, _meCache.Ship.MaxCapacitor, hardenerActivationCosts, boosterActivationCosts, minimumCapacitor);

            return minimumCapacitor;
        }

        /// <summary>
        /// Generate a value to offset delays between laser activation or deactivation.
        /// </summary>
        /// <param name="canBeNegative">True if the fudge factor can be negative, false if it cannot.</param>
        /// <returns>The value, in seconds, to offset laser activation. </returns>
        private int GenerateFudgeFactor(bool canBeNegative)
        {
            var fudgeFactorRandom = new Random();
            var fudgeFactor = fudgeFactorRandom.Next(0, 15);

            if (canBeNegative)
            {
                var positiveOrNegativeRandom = new Random();
                var positiveOrNegativeInt = positiveOrNegativeRandom.Next(0, 1);
                var isPositive = positiveOrNegativeInt == 1;

                if (!isPositive)
                    fudgeFactor *= -1;
            }

            return fudgeFactor;
        }

        /// <summary>
        /// Handle short-cycling of mining lasers. 
        /// </summary>
        /// <param name="miningLasers"></param>
        /// <returns></returns>
        private bool ShortCycleLasers(IEnumerable<EVE.ISXEVE.Interfaces.IModule> miningLasers)
        {
            var methodName = "ShortCycleLasers";
            LogTrace(methodName);

            if (_nextShortCycleDeactivation == null || DateTime.Now < _nextShortCycleDeactivation.Value) return false;

            var totalActivationCost = miningLasers.Select(module => module.ActivationCost.GetValueOrDefault(0)).Sum();
            var minimumCapacitor = CalculateMinimumCapacitor();

            //Only short cycle if I have capacitor for it
            if (_meCache.Ship.Capacitor - totalActivationCost < minimumCapacitor)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Skipping short cycle because the total activation cost of {0} would put our current cap of {1} below minimum cap of {2}.",
                    totalActivationCost, _meCache.Ship.Capacitor, minimumCapacitor);
                return false;
            }

            //Un-set the short cycle time 
            _nextShortCycleDeactivation = null;

            //Welp, deactivate the lasers.
            _ship.DeactivateModuleList(miningLasers, true);
            return true;
        }

        /// <summary>
        /// Set the time of the next possible short cycle laser deactivation.
        /// </summary>
        /// <param name="miningLasers"></param>
        private void UpdateNextShortCycleDeactivation(IEnumerable<EVE.ISXEVE.Interfaces.IModule> miningLasers)
        {
            var methodName = "UpdateNextShortCycleDeactivation";
            LogTrace(methodName);

            var miningLaser = miningLasers.FirstOrDefault(laser => laser.ActivationTime != null);

            if (miningLaser == null)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Error: No mining laser had a valid activation time.");
                return;
            }

            var activationTime = miningLaser.ActivationTime.GetValueOrDefault(0);

            //Use 1/3 cycle as the short cycle for ore harvesters and 
            double secondsUntilNextDelay;
            var typeId = miningLaser.ToItem.TypeID;
            if (typeId == (int)TypeIDs.Ice_Harvester_II || typeId == (int)TypeIDs.Ice_Harvester_I)
            {
                //Exhumers can short-cycle ice and still get a unit of ice, as their cycles yield two units.
                //Include a fudge factor to make sure we're halfway done with the cycle.
                var fudgeFactor = GenerateFudgeFactor(false);
                secondsUntilNextDelay = (activationTime / 2) + fudgeFactor;
            }
            else
            {
                var fudgeFactor = GenerateFudgeFactor(true);
                secondsUntilNextDelay = (activationTime / 3) + fudgeFactor;
            }

            _nextShortCycleDeactivation = DateTime.Now.AddSeconds(secondsUntilNextDelay);
        }

        /// <summary>
        /// Ensure the given mining laser is armed with a crystal appropriate for the given target, if such crystal is available.
        /// </summary>
        /// <param name="miningLaser"></param>
        /// <param name="target"></param>
        /// <returns>True if the loaded charge was modified, otherwise false.</returns>
        private bool EnsureMiningLaserIsArmedForTarget(EVE.ISXEVE.Interfaces.IModule miningLaser, IEntityWrapper target)
        {
            var methodName = "EnsureMiningLaserIsArmedForTarget";
            LogTrace(methodName, "Module: {0}, Target: {1}", miningLaser.ID, target.ID);

            if (miningLaser.ToItem.GroupID != (int)GroupIDs.FrequencyMiningLaser) return false;
            if (miningLaser.IsActive) return false;

            //Get the best possible mining crystal, including the crystal loaded
            var bestMiningCrystal = _ship.GetBestMiningCrystal(target, miningLaser);
            if (bestMiningCrystal == null) return false;

            if (!ShouldMiningLaserChangeToCrystal(miningLaser, bestMiningCrystal)) return false;

            //Get a matching reference from the module's available ammo
            var availableAmmo = miningLaser.GetAvailableAmmo();
            if (availableAmmo == null) return false;

            //If the charge isn't available, there's nothing more to do
            if (availableAmmo.All(item => item.ID != bestMiningCrystal.ID)) return false;

            LogMessage(methodName, LogSeverityTypes.Standard, "Changing the loaded crystal of module \"{0}\" ({1}) to \"{2}\".",
                miningLaser.ToItem.Name, miningLaser.ID, bestMiningCrystal.Name);
            miningLaser.ChangeAmmo(bestMiningCrystal.ID, 1);
            return true;
        }

        /// <summary>
        /// Determine if a given mining laser should use the given crystal.
        /// </summary>
        /// <param name="miningLaser"></param>
        /// <param name="bestMiningCrystal"></param>
        /// <returns>True if so, false otherwise.</returns>
        private static bool ShouldMiningLaserChangeToCrystal(EVE.ISXEVE.Interfaces.IModule miningLaser, Item bestMiningCrystal)
        {
            return LavishScriptObject.IsNullOrInvalid(miningLaser.Charge) || bestMiningCrystal.TypeID != miningLaser.Charge.TypeId;
        }

        /// <summary>
        /// Determine the lowest maximum range of our mining lasers.
        /// </summary>
        /// <returns></returns>
        private double GetLowestMaximumLaserRange()
        {
            // ReSharper disable PossibleInvalidOperationException
            var lowestMaximumLaserRange = _ship.MiningLaserModules.Select(laser => laser.OptimalRange)
                .Where(val => val.HasValue)
                .Select(val => val.Value)
                .Min();
            // ReSharper restore PossibleInvalidOperationException

            //Apply a margin of error
            //lowestMaximumLaserRange *= 0.95;
            return lowestMaximumLaserRange;
        }

        /// <summary>
        /// Make use of mining drones against the given prioritized mining targets.
        /// </summary>
        /// <param name="miningTargets">Mining targets, ordered by priority.</param>
        private void UseMiningDrones(IEnumerable<IEntityWrapper> miningTargets)
        {
            var methodName = "UseMiningDrones";
            LogTrace(methodName);

            if (!_miningConfiguration.UseMiningDrones) return;
            if (_miningConfiguration.IsIceMining) return;
            if (_drones.TotalDrones <= 0) return;

            //Firstly, determine the first target within a margin of error of drone range because drones should be focused on one asteroid.
            var effectiveDroneRange = _meCache.DroneControlDistance * 0.95;
            var firstTarget = miningTargets.FirstOrDefault(entity => entity.Distance < effectiveDroneRange);

            if (firstTarget == null)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "No mining targets were within the drone control range of {0}m.", effectiveDroneRange);
                return;
            }

            //Launch drones if necessary
            if (_drones.DronesInBay > 0 && _drones.CanLaunchDrones())
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Launching mining drones.");
                _drones.LaunchAllDrones();
                return;
            }

            //If there aren't drones in space, there's nothing more I can do.
            if (_drones.DronesInSpace == 0)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "No drones are in space.");
                return;
            }

            //If the drones are already on our target, we're done.
            if (!_drones.IsAnyDroneIdle && _drones.DroneTargetEntityId == firstTarget.ID) return;

            //Drones require an active target - ensure the chosen target is our active target.
            if (_meCache.ActiveTargetId != firstTarget.ID)
            {
                if (!_targeting.CanChangeTarget || !firstTarget.IsLockedTarget) return;

                //Make the chosen target the active target.
                LogMessage(methodName, LogSeverityTypes.Standard, "Making target \"{0}\" ({1}) active for mining drones.",
                    firstTarget.Name, firstTarget.ID);
                _targeting.ChangeTargetTo(firstTarget, true);
                return;
            }

            //Send the drones!
            _drones.SendAllDrones();
        }

        /// <summary>
        /// Obtain a list of mining targets, ordered by priority and sub-priority.
        /// Warning: These may not yet be locked targets.
        /// </summary>
        /// <returns></returns>
        private ICollection<IEntityWrapper> GetMiningTargets()
        {
            var methodName = "GetMiningTargets";
            LogTrace(methodName);

            var miningTargets = _targetQueue.Targets
                .Join(_entityProvider.EntityWrappers, queueTarget => queueTarget.Id, entity => entity.ID, (queueTarget, entity) => new { queueTarget, entity })
                .Where(pair => pair.queueTarget.Type == TargetTypes.Mine)
                .OrderBy(pair => pair.queueTarget.Priority)
                .ThenByDescending(pair => pair.queueTarget.SubPriority)
                .ThenBy(pair => pair.entity.Distance)
                .Select(pair => pair.entity)
                .ToList();

            return miningTargets;
        }

        private void TractorTarget(IQueueTarget queueTarget)
        {
            var methodName = "TractorTarget";
            LogTrace(methodName, "QueueTarget: {0}", queueTarget.Id);

            //If I have no tractor beams just abort here.
            //if (_ship.TractorBeamModules.Count == 0)
            //{
            //	LogMessage(methodName, LogSeverityTypes.Debug, "Tractor target queued but we have no tractor beams; error. Dequeueing target.");
            //	_targetQueue.DequeueTarget(queueTarget.Id);
            //	return;
            //}

            //Make sure this entity is requested for update
            _entityProvider.EntityWrappersById[queueTarget.Id].RequestObjectRefresh();

            //First, if our object is within loot range, we can turn off any active tractor beams.
            var target = _entityProvider.EntityWrappersById[queueTarget.Id];
            if (target.Distance <= (int)Ranges.LootActivate)
            {
                //Loop all tractor beams, find one active and on this target.
                foreach (var module in _ship.TractorBeamModules.Where(module => module.IsActive && module.TargetID == target.ID))
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Tractor target \'{0}\' ({1}) is within loot range, deactivating tractor beam.",
                        target.Name, target.ID);
                    module.Deactivate();
                    break;
                }
                if (!target.ToEntity.IsWreckEmpty)
                {
                    var cargoWindow = Core.Metatron.EveWindowProvider.GetWindowByItemId(target.ID);
                    if (LavishScriptObject.IsNullOrInvalid(cargoWindow))
                    {
                        LogMessage(methodName, LogSeverityTypes.Standard, "Opening cargo of target wreck {0} ({1})",
                                   target.Name, target.ID);
                        target.Open();
                    }
                    cargoWindow.LootAll();
                }
                //De-queue and unlock the target.
                //_targetQueue.DequeueTarget(queueTarget.Id);
                //target.UnlockTarget();
            }
            if (target.Distance <= _ship.SalvagerModules.Select(m => m.OptimalRange).Max())
            {
                var salvagerNeeded = _ship.SalvagerModules.Any(module => module.IsActive && module.TargetID == target.ID) && target.GroupID == (int)GroupIDs.Wreck;
                if (!salvagerNeeded)
                {
                    foreach (var module in _ship.SalvagerModules.Where(module => !module.IsActive && target.Distance <= module.OptimalRange.GetValueOrDefault(0)))
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Activating a free salvager on target \'{0}\' ({1}).",
                                target.Name, target.ID);
                        module.Activate();
                        break;
                    }
                }
            }


            if (_ship.TractorBeamModules.Count > 0 && target.Distance <= _ship.TractorBeamModules.Select(m => m.OptimalRange).Max() && target.Distance >= (int)Ranges.LootActivate)
            {
                //It's -not- in range, so iterate all modules and see if I have a tractor on this target.
                var tractorActive = _ship.TractorBeamModules.Any(module => module.IsActive && module.TargetID == target.ID);
                //if (_movement.IsMoving && _movement.MovementType != MovementTypes.Warp)
                //{
                //    _movement.StopCurrentMovement(true);
                //}
                //If no tractor beam is active on this target, activate -one-.
                if (!tractorActive)
                {
                    //Activate the first inactive tractor.
                    foreach (var module in _ship.TractorBeamModules.Where(module => !module.IsActive && target.Distance <= module.OptimalRange.GetValueOrDefault(0)))
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Activating a free tractor beam on tractor target \'{0}\' ({1}).",
                            target.Name, target.ID);
                        module.Activate();
                        break;
                    }
                }
            }
            else
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "We have nothing in range of tractor beams or no tractor beams. Approaching target.");
                var destination = new Destination(DestinationTypes.Entity, target.ID, 500.0);
                _movement.QueueDestination(destination);
            }
            ChangeSalvageTarget(target);

        }

        private void ChangeSalvageTarget(IEntityWrapper target)
        {
            var methodName = "ChangeSalvageTarget";
            LogTrace(methodName, "Target: {0}", target.ID);


            //If I can't change target just return
            if (!_targeting.CanChangeTarget) return;

            var sortedTargets = GetSortedTractorTargets();

            //If there are no sortedTargets just return.
            if (sortedTargets.Count == 0) return;

            foreach (var sortedTarget in sortedTargets)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Target: \'{0}\' ({1}, {2})",
                    sortedTarget.Name, sortedTarget.ID, _targeting.GetNumModulesOnEntity(sortedTarget.ID));
            }

            IEntityWrapper targetEntity;
            //Change target if necessary.
            if (_ship.TractorBeamModules.Any(m => m.Target.Distance < (int)Ranges.LootActivate) && _ship.TractorBeamModules.All(m => m.IsActive))
            {
                targetEntity = sortedTargets.OrderBy(t => t.Distance).First();
            }
            else
            {
                targetEntity = sortedTargets.First();
            }
            if (targetEntity.ID != target.ID)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Making highest priority tractor target \'{0}\' ({1}) the active target.",
                    targetEntity.Name, targetEntity.ID);
                _targeting.ChangeTargetTo(targetEntity, false);
            }


        }

        private List<IEntityWrapper> GetSortedTractorTargets()
        {
            return GetSortedTargets(true, true, TargetTypes.LootSalvage);
        }

        private List<IEntityWrapper> GetSortedTargets(bool sortByNumModules, bool restrictByType, TargetTypes type)
        {
            var methodName = "GetSortedTargets";
            _logging.LogTrace(ModuleName, methodName, "Type: {0}, SortByNumModules: {1}", type, sortByNumModules);

            //Get a list of locked asteroids, sorted first by # of lasers and second by priority.
            List<IEntityWrapper> targets;
            if (restrictByType)
            {
                switch (type)
                {
                    case TargetTypes.Mine:
                        targets = _targeting.GetLockedMiningTargets();
                        break;
                    case TargetTypes.LootSalvage:
                        targets = _targeting.GetLockedTractorTargets();
                        break;
                    case TargetTypes.Kill:
                        targets = _targeting.GetLockedKillingTargets();
                        break;
                    default:
                        goto case TargetTypes.Mine;
                }
            }
            else
            {
                targets = Core.Metatron.MeCache.Targets.ToList();
            }

            if (sortByNumModules)
            {
                targets = (from IEntityWrapper ce in targets
                           join QueueTarget qt in _targetQueue.Targets on ce.ID equals qt.Id
                           orderby Core.Metatron.Targeting.GetNumModulesOnEntity(ce.ID) ascending, qt.Priority ascending,
                            qt.SubPriority descending, BoolToInt(_meCache.AttackersById.ContainsKey(ce.ID)) descending, qt.TimeQueued ascending
                           select ce).ToList();
            }
            else
            {
                targets = (from IEntityWrapper ce in targets
                           join QueueTarget qt in _targetQueue.Targets on ce.ID equals qt.Id
                           orderby qt.Priority ascending, qt.SubPriority descending, BoolToInt(_meCache.AttackersById.ContainsKey(ce.ID)) descending, qt.TimeQueued
                           select ce).ToList();
            }

            return targets;
        }

        public int TotalUnusedTractors
        {
            get
            {
                return _ship.TractorBeamModules.Count(module => !module.IsActive);
            }
        }
    }
    // ReSharper restore CompareOfFloatsByEqualityOperator
    // ReSharper restore PossibleMultipleEnumeration
    // ReSharper restore ConvertToConstant.Local
}
