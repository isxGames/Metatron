using System;
using System.Collections.Generic;
using System.Linq;
using EVE.ISXEVE;
using Metatron.BehaviorModules;
using Metatron.Core;
using LavishScriptAPI;
using Metatron.Core.Interfaces;
using Metatron.BehaviorModules.PartialBehaviors;
using System.Runtime.InteropServices;
using System.Threading;

namespace Metatron.ActionModules
{
    public interface IOffensive
    {
    }

    public sealed class Offensive : ModuleBase, IOffensive
    {
        // ReSharper disable ConvertToConstant.Local
        // ReSharper disable CompareOfFloatsByEqualityOperator
        // ReSharper disable UnusedParameter.Local

        //Keep track of whether or not a combat target is locked to avoid multiple calls
        private bool _isCombatTargetLocked;
        //Get a list of sorted targets. Don't care if they have modules or not, just that we can hit them.
        private List<IEntityWrapper> _entitiesToKill = new List<IEntityWrapper>();
        private readonly Dictionary<Int64, Dictionary<int, int>> _ammoTypeIDsByModuleTypeIDsByTargets = new Dictionary<long, Dictionary<int, int>>();
        private int _unusedWeapons;
        private bool _usedPainters, _activatedLaunchers, _activatedTurrets;

        private Int64 _turretTargetId = -1, _launcherTargetId = -1;

        private DateTime _nextCombatAssistRequest = DateTime.Now;
        private readonly int _combatAssistRequestFrequency = 10;

        private readonly ITargetQueue _targetQueue;
        private readonly IEntityProvider _entityProvider;
        private readonly IShip _ship;

        public Offensive(ILogging logging, ITargetQueue targetQueue, IEntityProvider entityProvider, IShip ship)
            : base(logging)
        {
            _targetQueue = targetQueue;
            _entityProvider = entityProvider;
            _ship = ship;

            ModuleManager.ModulesToPulse.Add(this);
            ModuleName = "Offensive";
            PulseFrequency = 1;
        }

        private void _activeTargetActiveTooLongSwapCooldownTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _activeTargetActiveTooLongSwapCooldown = false;
        }

        public override void Pulse()
        {
            var methodName = "Pulse";
            LogTrace(methodName);

            if (!ShouldPulse())
                return;

            StartPulseProfiling();

            SendCombatAssistRequest();

            _entitiesToKill = GetKillQueueTargetsSortedByPriority();

            if (_entitiesToKill.All(t => t.Distance > _ship.GetMaximumWeaponRange()) || Core.Metatron.Movement.IsMoving)
            {
                _ship.DeactivateModuleList(_ship.GangLinkModules, true);
            }

            _isCombatTargetLocked = _entitiesToKill.Any(target => Core.Metatron.MeCache.Targets.Contains(target));
            if (_isCombatTargetLocked)
            {
                _unusedWeapons = CalculateTotalUnusedWeapons();
            }
            else if (ShouldBeFighting())
            {

                //If I'm using combat drones, have drones in space, and no drone target, recall drones.
                if (!Core.Metatron.Config.MiningConfig.UseMiningDrones && Core.Metatron.Drones.DronesInSpace > 0 &&
                    Core.Metatron.MeCache.ActiveDrones.All(drone => drone.State != (int)DroneStates.Fighting))
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "No combat targets locked and drones not fighting; recalling drones.");
                    Core.Metatron.Drones.RecallAllDrones(true);
                }
                foreach (var target in _targetQueue.Targets)
                {
                    if (target.Type != TargetTypes.Kill) _targetQueue.DequeueTarget(target.Id);
                }
            }


            if (Core.Metatron.MeCache.ActiveTargetId > 0)
            {
                KillLockedTargets(_entitiesToKill);
            }
            else
            {
                PrepareForUnlockedTargets(_entitiesToKill);
            }

            if (!ShouldBeFighting())
            {
                //If I'm using combat drones and have drones in space, recall drones.
                if (!Core.Metatron.Config.MiningConfig.UseMiningDrones && Core.Metatron.Drones.DronesInSpace > 0)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Shouldn't be fighting; recalling drones.");
                    Core.Metatron.Drones.RecallAllDrones(true);
                }

                //Make use of any target painters, tracking computers, etc.
                _ship.DeactivateModuleList(_ship.GangLinkModules, true);
                _ship.DeactivateModuleList(Core.Metatron.Ship.TrackingComputerModules, true);
                _ship.DeactivateModuleList(Core.Metatron.Ship.TargetPainterModules, true);
            }
            foreach(var target in _blacklistedEntities.ToList())
            {
                if (target.Value < DateTime.Now + TimeSpan.FromSeconds(30))
                {
                    _blacklistedEntities.Remove(target.Key);
                }
            }
            EndPulseProfiling();
        }

        private List<IEntityWrapper> GetKillQueueTargetsSortedByPriority()
        {
            var targets = _targetQueue.Targets
                .Join(_entityProvider.EntityWrappers, target => target.Id, wrapper => wrapper.ID, (queueTarget, entity) => new { queueTarget, entity })
                .Where(join => join.queueTarget.Type == TargetTypes.Kill)
                .Where(join => join.entity.Distance < _ship.GetMaximumWeaponRange())
                .OrderBy(join => join.queueTarget.Priority)  // First sort by Priority
                .ThenByDescending(join => join.queueTarget.SubPriority)  // Then sort by SubPriority
                .Select(join => join.entity);

            return targets.ToList();
        }

        private void PrepareForUnlockedTargets(ICollection<IEntityWrapper> targets)
        {
            var methodName = "PrepareForUnlockedTargets";
            LogTrace(methodName, "targetsCount: {0}", targets.Count);

            //We require queued targets to process
            if (targets.Count == 0) return;

            //if I'm already moving, do nothing
            if (Core.Metatron.Movement.IsMoving) return;

            //Don't bother approaching or orbiting threats if we're not in a combat mode. Barges chasing rats? NAH BRO.
            if (Core.Metatron.ModuleManager.IsNonCombatMode) return;

            if (_ship.GangLinkModules.Count > 0 &&
                    !_ship.GangLinkModules[0].IsActive && targets.Any(t => t.Distance < _ship.GetMaximumWeaponRange()))
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Activating bastion module.");
                _ship.ActivateModuleList(_ship.GangLinkModules, true);
            }

            //Get the first queued target and prepare for it
            var target = targets.First();

            if (Core.Metatron.MeCache.ActiveTargetId <= 0)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "We have no active target. Making entity \"{0}\" ({1}) the active target.",
                    target.Name, target.ID);
                target.MakeActiveTarget();
            }

            //Move for the entity
            MoveForTarget(target);
        }

        private bool _activeTargetActiveTooLongSwapCooldown = false;

        Dictionary<long, DateTime> _blacklistedEntities = new Dictionary<long, DateTime>();

        private void KillLockedTargets(ICollection<IEntityWrapper> targets)
        {
            var methodName = "KillLockedTargets";
            LogTrace(methodName);

            // Early exit conditions
            if (targets == null || targets.Count == 0) return;
            var activeQueueTarget = Core.Metatron.Targeting.GetActiveQueueTarget();
            if (activeQueueTarget == null) return;

            // Activate GangLinkModules if any
            if (_ship.GangLinkModules.Count > 0 && !_ship.GangLinkModules[0].IsActive && targets.Any(t => t.Distance < _ship.GetMaximumWeaponRange()))
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Activating bastion module.");
                _ship.ActivateModuleList(_ship.GangLinkModules, true);
            }

            // Get all kill queue targets sorted by priority
            var sortedKillQueueTargets = GetKillQueueTargetsSortedByPriority().ToList();
            if (sortedKillQueueTargets.Count == 0) return;

            // Find the active target among the locked targets
            var activeTarget = targets.FirstOrDefault(entity => entity.ID == activeQueueTarget.Id);

            // Initialize killTarget to the highest priority target by default
            IEntityWrapper killTarget = sortedKillQueueTargets.First();

            // Execute the kill command on the selected target
            Kill(killTarget);
        }

        public override bool ShouldPulse()
        {
            if (!base.ShouldPulse())
                return false;

            //If I'm critally moving, I shouldn't pulse.
            if (Core.Metatron.Movement.IsCriticalMoving)
                return false;

            //If I'm in station, I shouldn't pulse.
            if (!Core.Metatron.MeCache.InSpace || Core.Metatron.MeCache.InStation)
                return false;

            //If I'm physically in warp, or have a warp destination queued and aren't warp scrambled, I shouldn't pulse.
            if (Core.Metatron.Movement.IsMoving && Core.Metatron.Movement.MovementType == MovementTypes.Warp &&
                !Core.Metatron.MeCache.ToEntity.IsWarpScrambled)
                return false;

            return true;
        }

        private void SendCombatAssistRequest()
        {
            var methodName = "SendCombatAssistRequest";
            LogTrace(methodName);

            var canActiveBehaviorSendCombatAssistRequests = BehaviorManager.BehaviorsToPulse[Core.Metatron.Config.MainConfig.ActiveBehavior].CanSendCombatAssistanceRequests;

            if (!canActiveBehaviorSendCombatAssistRequests) return;

            if (Core.Metatron.TimeOfPulse < _nextCombatAssistRequest)
                return;

            _nextCombatAssistRequest = Core.Metatron.TimeOfPulse.AddSeconds(_combatAssistRequestFrequency);

            var queuedTargets = Core.Metatron.TargetQueue.Targets
                .Where(queueTarget => queueTarget.Type == TargetTypes.Kill);

            foreach (var queuedTarget in queuedTargets)
            {
                var eventArgs = new Core.EventCommunication.FleetNeedCombatAssistEventArgs(_logging,
                    Core.Metatron.MeCache.CharId, Core.Metatron.MeCache.SolarSystemId, queuedTarget);
                Core.Metatron.EventCommunications.FleetNeedCombatAssistEvent.SendEvent(eventArgs);
            }
        }

        public override void InFrameCleanup()
        {
            _activatedTurrets = false;
            _activatedLaunchers = false;
        }

        private bool ShouldBeFighting()
        {
            var methodName = "ShouldBeFighting";
            LogTrace(methodName);

            bool shouldBeFighting = (Core.Metatron.Attackers.ThreatEntities.Count > 0 || _isCombatTargetLocked) && !Core.Metatron.Defense.IsFleeing;
            LogTrace(methodName, shouldBeFighting.ToString());
            return shouldBeFighting;
        }

        private void Kill(IEntityWrapper target)
        {
            var methodName = "Kill";
            LogTrace(methodName);

            //If the active target is out of warp range, dequeue it.
            if (target.Distance >= (int)Ranges.Warp * 0.95)
            {
                Core.Metatron.TargetQueue.DequeueTarget(target.ID);
                return;
            }

            if (_turretTargetId >= 0 && !Core.Metatron.EntityProvider.EntityWrappersById.ContainsKey(_turretTargetId) ||
                !Core.Metatron.Targeting.QueueTargetsByEntityId.ContainsKey(_turretTargetId) || !Core.Metatron.EntityProvider.EntityWrappersById[_turretTargetId].IsLockedTarget)
                _turretTargetId = -1;

            if (_launcherTargetId >= 0 && !Core.Metatron.EntityProvider.EntityWrappersById.ContainsKey(_launcherTargetId) ||
                !Core.Metatron.Targeting.QueueTargetsByEntityId.ContainsKey(_launcherTargetId) || !Core.Metatron.EntityProvider.EntityWrappersById[_launcherTargetId].IsLockedTarget)
                _launcherTargetId = -1;

            //Make use of any target painters, tracking computers, etc.
            UseOtherModules(target);

            var targetDamageProfile = Core.Metatron.Attackers.GetDamageProfileFromNpc(target.GroupID);

            var launchersInUse = UseLaunchers(target, targetDamageProfile);

            var turretsInUse = UseTurrets(target, targetDamageProfile);

            var dronesInUse = false;
            if (Core.Metatron.Drones.CanAttackEntity(target))
            {
                //Do drone use after turret/launcher use because they're lower priority than the others.

                if (Core.Metatron.MeCache.ActiveTargetId != target.ID &&
                    Core.Metatron.Drones.DroneTargetEntityId != target.ID)
                {
                    if (Core.Metatron.Targeting.CanChangeTarget && Core.Metatron.MeCache.Targets.Contains(target))
                    {
                        dronesInUse = true;
                        LogMessage(methodName, LogSeverityTypes.Standard, "Changing target to {0} ({1}) for drone attack.",
                            target.Name, target.ID);
                        ChangeTarget(target);
                    }
                }
                else
                {
                    dronesInUse = UseDrones(target);
                }

            }

            UseTargetPainters(target);

            if (Core.Metatron.Targeting.CanChangeTarget && _entitiesToKill.Count > 1 && _unusedWeapons > 0)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "We have more threats and have all possible weapons on current, switching to next.");

                foreach (var entity in _entitiesToKill)
                {
                    if (!Core.Metatron.Targeting.QueueTargetsByEntityId.ContainsKey(entity.ID)) continue;
                    if (entity.ID == target.ID) continue;
                    if (!Core.Metatron.MeCache.Targets.Contains(entity)) continue;

                    //Firstly check for drones.
                    if (!dronesInUse)
                    {
                        //Try to use drones on the new target
                        dronesInUse = UseDrones(entity);

                        if (dronesInUse || ChangeTargetForDrones(entity)) break;
                    }

                    var entityDamageProfile = Core.Metatron.Attackers.GetDamageProfileFromNpc(entity.GroupID);
                    //Now check turrets.
                    //Keep track of last type ID so I can skip re-checking a duplicate turret, minor optimization)
                    if (!turretsInUse && !_activatedTurrets)
                    {
                        turretsInUse = UseTurrets(entity, entityDamageProfile);
                    }

                    //Last, missiles.
                    //Again, minor optimization to avoid recheckign duplicate modules
                    if (!launchersInUse && !_activatedLaunchers)
                    {
                        launchersInUse = UseLaunchers(entity, entityDamageProfile);
                    }
                }
            }

            //Don't bother approaching or orbiting threats if we're not in a combat mode or are fleeing. Barges chasing rats? NAH BRO.
            if (Core.Metatron.ModuleManager.IsNonCombatMode || Core.Metatron.Defense.IsFleeing)
                return;

            //If we're moving towards the wrong target, change it
            if (Core.Metatron.Movement.IsMoving)
            {
                var destination = Core.Metatron.Movement.DestinationQueue.First();

                if (destination.IsObstacleAvoidanceMovement) return;

                if (destination.EntityId != target.ID)
                {
                    Core.Metatron.Movement.ClearDestinations(false);
                }
            }

            //If nothing was in range, approach this target
            MoveForTarget(target);
        }

        private void MoveForTarget(IEntityWrapper target)
        {
            var methodName = "MoveForTarget";
            LogTrace(methodName, "target: {0} ({1})", target.Name, target.ID);

            if (Core.Metatron.Config.MovementConfig.OrbitCollidableNotTarget && target.Distance <= _ship.GetMaximumWeaponRange() && !Core.Metatron.Movement.IsMoving)
            {
                var collidableToOrbit = _entityProvider.EntityWrappers.Where(e => Core.Metatron.Movement.IsEntityCollidable(e)).FirstOrDefault();
                if (collidableToOrbit != null)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, $"Orbiting collidable entity {collidableToOrbit.Name}");
                    Core.Metatron.Movement.QueueDestination(new Destination(DestinationTypes.Entity,
                                                            collidableToOrbit.ID)
                    {
                        Distance = Core.Metatron.Config.MovementConfig.UseCustomOrbitDistance ? Core.Metatron.Config.MovementConfig.CustomOrbitDistance : 15000,
                        ApproachType = ApproachTypes.Orbit
                    });
                    return;
                }
                else
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Collidable orbit was requested but no collidables were found - switching to target orbit");
                }
            }

            if (!Core.Metatron.Attackers.IsRatTarget(target))
            {
                if (!Core.Metatron.Movement.IsMoving)
                {
                    var distance = Core.Metatron.Ship.GetOptimalWeaponRange();
                    if (distance > Core.Metatron.Ship.MaxTargetRange)
                    {
                        distance = Core.Metatron.Ship.MaxTargetRange;
                    }

                    if (target.Distance > distance)
                    {
                        Approach(target, distance);
                    }
                }
            }
            else
            {
                if (Core.Metatron.Config.MovementConfig.UseKeepAtRangeInsteadOfOrbit)
                    KeepAtRange(target);
                else
                    Orbit(target);
            }
        }

        /// <summary>
        /// Attempt to use other types of offensive modules on the given target.
        /// </summary>
        /// <param name="target"></param>
        private void UseOtherModules(IEntityWrapper target)
        {
            var methodName = "UseOtherModules";
            LogTrace(methodName, "Target: {0}", target.ID);

            //Don't try to use modules if the target isn't locked
            if (!target.IsLockedTarget) return;

            Core.Metatron.Ship.ActivateModuleList(Core.Metatron.Ship.TrackingComputerModules, true);

            //First try to use any stasis webbers
            foreach (var module in Core.Metatron.Ship.StasisWebifierModules)
            {
                TryUseModule(module, target, true);
            }

            //Next, nosferatus
            foreach (var module in Core.Metatron.Ship.NosferatuModules.Where(module => target.Distance <= module.TransferRange.GetValueOrDefault(0)))
            {
                TryUseModule(module, target, false);
            }
        }

        private void TryUseModule(EVE.ISXEVE.Interfaces.IModule module, IEntityWrapper target, bool canDeactivateDueToTargetMismatch)
        {
            var methodName = "TryUseModule";
            LogTrace(methodName, "Module: {0}, Target: {1}, CanDeactivate: {2}",
                module.ID, target.ID, canDeactivateDueToTargetMismatch);

            if (!module.IsValid || module.IsDeactivating)
                return;

            //If the module is on the wrong target and we're allowed to switch for this module, turn the module off
            if (canDeactivateDueToTargetMismatch && Core.Metatron.EntityProvider.EntityWrappersById.ContainsKey(module.TargetID))
            {
                var entity = Core.Metatron.EntityProvider.EntityWrappersById[module.TargetID];

                if (module.TargetID != target.ID && module.IsActive)
                {
                    //if (target.Distance >= _ship.GetMaximumWeaponRange()) return;
                    LogMessage(methodName, LogSeverityTypes.Debug,
                                "Deactivating module \'{0}\' with target \'{1}\' ({2}, {3}) due to active target mismatch.",
                                module.ToItem.Name, entity.Name, module.TargetID, entity.Distance);
                    module.Deactivate();
                    return;
                }
            }

            //Make sure I've got cap to run the module and the target is in range
            if (Core.Metatron.MeCache.Ship.Capacitor > module.ActivationCost.GetValueOrDefault(0))
            {
                double optimalRange = 0;
                if (module.AccuracyFalloff != null && module.AccuracyFalloff > 0) optimalRange = module.AccuracyFalloff.Value;
                else optimalRange = module.OptimalRange.Value;

                if (optimalRange == 0 || optimalRange >= target.Distance)
                    module.Activate(target.ID);
            }
        }

        private void Approach(IEntityWrapper activeTarget, double distance)
        {
            var methodName = "Approach";
            LogTrace(methodName, "ActiveTarget: {0}", activeTarget.ID);

            LogMessage(methodName, LogSeverityTypes.Debug, "Approaching entity \'{0}\' ({1}, {2}) to kill it.",
                activeTarget.Name, activeTarget.ID, activeTarget.Distance);

            Core.Metatron.Movement.QueueDestination(new Destination(DestinationTypes.Entity,
                activeTarget.ID)
            { Distance = distance });
        }

        private void KeepAtRange(IEntityWrapper activeTarget)
        {
            var methodName = "KeepAtRange";
            LogTrace(methodName, "ActiveTarget: {0}", activeTarget.ID);
            if (_ship.GangLinkModules.Count > 0)
            {
                //LogMessage(methodName, LogSeverityTypes.Debug, "Skipping movement because command module is on");
                return;
            }

            var distance = GetOrbitDistance();

            var existingKeepAtRangeDestination = Core.Metatron.Movement.DestinationQueue.FirstOrDefault(
                destination => destination.ApproachType == ApproachTypes.KeepAtRange);

            if (existingKeepAtRangeDestination != null) return;

            LogMessage(methodName, LogSeverityTypes.Debug, "Orbiting entity \'{0}\' ({1}, {2}) at {3}m to kill it.",
                       activeTarget.Name, activeTarget.ID, activeTarget.Distance, distance);
            Core.Metatron.Movement.QueueDestination(
                new Destination(DestinationTypes.Entity, activeTarget.ID)
                {
                    Distance = distance,
                    ApproachType = ApproachTypes.KeepAtRange
                });
        }

        private void Orbit(IEntityWrapper activeTarget)
        {
            var methodName = "Orbit";
            LogTrace(methodName, "ActiveTarget: {0}", activeTarget.ID);

            if (_ship.GangLinkModules.Count > 0)
            {
                //LogMessage(methodName, LogSeverityTypes.Debug, "Skipping movement because command module is on");
                return;
            }

            var distance = GetOrbitDistance();

            var acceptibleTargetRange = (int)(Core.Metatron.Ship.MaxTargetRange * 0.9);
            if (distance > acceptibleTargetRange)
                distance = acceptibleTargetRange;

            var existingOrbitDestination = Core.Metatron.Movement.DestinationQueue.FirstOrDefault(
                destination => destination.ApproachType == ApproachTypes.Orbit);

            if (existingOrbitDestination != null)
            {
                if (distance != (int)existingOrbitDestination.Distance)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Adjusting orbit on entity \'{0}\' ({1}, {2}) to {3}m.",
                        activeTarget.Name, activeTarget.ID, activeTarget.Distance, distance);
                    existingOrbitDestination.Distance = distance;   //Update the existing orbit entry
                }
            }
            else
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Orbiting entity \'{0}\' ({1}, {2}) at {3}m.",
                    activeTarget.Name, activeTarget.ID, activeTarget.Distance, distance);
                Core.Metatron.Movement.QueueDestination(new Destination(DestinationTypes.Entity,
                    activeTarget.ID)
                { Distance = distance, ApproachType = ApproachTypes.Orbit });
            }
        }

        private static int GetOrbitDistance()
        {
            int distance;
            if (Core.Metatron.Config.MovementConfig.UseCustomOrbitDistance)
            {
                distance = Core.Metatron.Config.MovementConfig.CustomOrbitDistance;
            }
            else
            {
                distance = (int)Core.Metatron.Ship.GetOptimalWeaponRange();
            }
            return distance;
        }

        /// <summary>
        /// Attempt to use launchers on the given target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="targetDamageProfile"></param>
        /// <returns></returns>
        private bool UseLaunchers(IEntityWrapper target, DamageProfile targetDamageProfile)
        {
            var methodName = "UseLaunchers";
            LogTrace(methodName, "ActiveTarget {0}", target.ID);

            var somethingInRange = false;
            _usedPainters = false;

            var moduleHadValidCharge = false;

            var canChangeAmmo = true;
            foreach (var module in Core.Metatron.Ship.LauncherModules)
            {
                if (Core.Metatron.Ship.DidModuleRecentlyChangeAmmo(module.ID))
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Module \"{0}\" ({1}) recently changed ammo.",
                        module.ToItem.Name, module.ID);

                    _unusedWeapons--;
                    continue;
                }

                if (LavishScriptObject.IsNullOrInvalid(module.Charge))
                {
                    _unusedWeapons--;

                    if (module.IsActive) continue;

                    if (LoadBestChargeForModule(target, module, targetDamageProfile))
                    {
                        canChangeAmmo = false;

                        LogMessage(methodName, LogSeverityTypes.Debug, "Module \"{0}\" ({1}) has invalid charge, rearming it.",
                            module.ToItem.Name, module.ID);
                        continue;
                    }

                    LogMessage(methodName, LogSeverityTypes.Debug, "Module \"{0}\" ({1}) has invalid charge, skipping it.",
                               module.ToItem.Name, module.ID);
                    continue;
                }

                var currentChargeTypeId = LavishScriptObject.IsNullOrInvalid(module.Charge) ? -1 : module.Charge.TypeId;
                var bestChargeTypeId = GetBestCharge(target, module, targetDamageProfile, currentChargeTypeId);

                if (currentChargeTypeId != bestChargeTypeId)
                {
                    if (!module.IsActive)
                    {
                        //todo: If I have multiple types of missiles with me (for multiple damage types),
                        //make sure I've got the optimal type loaded before activating.
                        if (canChangeAmmo && LoadBestChargeForModule(target, module, targetDamageProfile))
                        {
                            canChangeAmmo = false;
                        }
                    }
                    else if (!module.IsDeactivating)
                    {
                        module.Deactivate();
                    }

                    _unusedWeapons--;
                    continue;
                }

                moduleHadValidCharge = true;

                var maximumMissileRange = Core.Metatron.Ship.MaximumMissileRange(module);
                if (maximumMissileRange >= target.Distance)
                {
                    somethingInRange = true;
                    //If the module's not active or reloading or changing ammo, activate it
                    if (!module.IsActive)
                    {
                        //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                        //methodName, String.Format("Activating module \'{0}\' on active target \'{1}\' ({2}, {3}).",
                        //m.ToItem.Name, target.Name, target.ID, target.Distance)));
                        _unusedWeapons--;
                        _launcherTargetId = target.ID;
                        _activatedLaunchers = true;

                        if (Core.Metatron.MeCache.Targets.Contains(target))
                            module.Activate(target.ID);
                    }
                    else
                    {
                        //If the module is active...
                        //if it's on the wrong target, turn it off.
                        if (module.TargetID != target.ID)
                        {
                            if (!module.IsDeactivating)
                            {
                                LogMessage(methodName, LogSeverityTypes.Debug, "Deactivating module \"{0}\" due to target mismatch.",
                                    module.ToItem.Name);

                                module.Deactivate();

                                if (!_usedPainters)
                                {
                                    _usedPainters = true;
                                    Core.Metatron.Ship.DeactivateModuleList(Core.Metatron.Ship.TargetPainterModules, true);
                                }
                            }
                        }
                    }
                }
                else
                {
                    //If the module's active and not deactivating, deactivate it - we're out of range
                    //actually, first check distance tothe module's actual target
                    if (module.IsActive && !module.IsDeactivating)
                    {
                        if (Core.Metatron.EntityProvider.EntityWrappersById.ContainsKey(module.TargetID))
                        {
                            var moduleTarget = Core.Metatron.EntityProvider.EntityWrappersById[module.TargetID];

                            if (maximumMissileRange > 0 && moduleTarget.Distance > maximumMissileRange)
                            {
                                LogMessage(methodName, LogSeverityTypes.Debug, "Deactivating module \'{0}\' with target \'{1}\' ({2}, {3}) - out of range {4}.",
                                    module.ToItem.Name, target.Name, target.ID, target.Distance, maximumMissileRange);
                                module.Deactivate();

                                if (!_usedPainters)
                                {
                                    _usedPainters = true;
                                    Core.Metatron.Ship.DeactivateModuleList(Core.Metatron.Ship.TargetPainterModules, true);
                                }
                            }
                        }
                    }
                }
            }

            //Consider us in range of something if we didn't have any modules with a valid target
            if (!moduleHadValidCharge)
                somethingInRange = true;

            //This really needs to be re-thought and reworked in the morning.

            return somethingInRange;
        }

        private bool LoadBestChargeForModule(IEntityWrapper target, EVE.ISXEVE.Interfaces.IModule module, DamageProfile targetDamageProfile)
        {
            var methodName = "LoadBestChargeForModule";
            LogTrace(methodName);

            var availableAmmo = module.GetAvailableAmmo();
            if (availableAmmo == null)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Error: Module.GetAvailableAmmo() returned null.");
                return false;
            }

            var moduleAmmoByTypeId = new Dictionary<int, Item>();
            foreach (var item in availableAmmo)
            {
                var typeId = item.TypeID;
                if (!moduleAmmoByTypeId.ContainsKey(typeId))
                    moduleAmmoByTypeId.Add(typeId, item);
            }

            var currentChargeTypeId = LavishScriptObject.IsNullOrInvalid(module.Charge) ? -1 : module.Charge.TypeId;
            var bestChargeForCurrentTarget = Core.Metatron.Ship.GetBestMatchTypeId(targetDamageProfile, moduleAmmoByTypeId.Values, currentChargeTypeId);
            if (currentChargeTypeId < 0 || bestChargeForCurrentTarget != currentChargeTypeId)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Current charge type: {0}, best charge: {1} - changing charge.", currentChargeTypeId, bestChargeForCurrentTarget);
                Core.Metatron.Ship.ChangeTurretAmmo(module, bestChargeForCurrentTarget);
                return true;
            }
            LogMessage(methodName, LogSeverityTypes.Debug, "Current charge type: {0}, best charge: {1} - not changing charge.", currentChargeTypeId, bestChargeForCurrentTarget);
            return false;
        }

        private int GetBestCharge(IEntityWrapper target, EVE.ISXEVE.Interfaces.IModule module, DamageProfile targetDamageProfile, int currentChargeTypeId)
        {
            var methodName = "GetBestCharge";
            LogTrace(methodName, "{0}, {1}, {2}", target.ID, module.ID, targetDamageProfile);

            var moduleAmmoByTypeId = new Dictionary<int, Item>();
            foreach (var item in module.GetAvailableAmmo())
            {
                var typeId = item.TypeID;
                if (!moduleAmmoByTypeId.ContainsKey(typeId))
                    moduleAmmoByTypeId.Add(typeId, item);
            }

            var bestChargeForCurrentTarget = Core.Metatron.Ship.GetBestMatchTypeId(targetDamageProfile, moduleAmmoByTypeId.Values, currentChargeTypeId);
            if (currentChargeTypeId < 0 || bestChargeForCurrentTarget != currentChargeTypeId)
            {
                return bestChargeForCurrentTarget;
            }
            return currentChargeTypeId;
        }

        /// <summary>
        /// Attempt to use drones on the given target.
        /// </summary>
        /// <param name="target"></param>
        /// <returns>True if drones were can be sent after the given target, otherwise false</returns>
        private bool UseDrones(IEntityWrapper target)
        {
            var methodName = "UseDrones";
            LogTrace(methodName, "Target: {0}", target.ID);

            if (target.ID != Core.Metatron.MeCache.ActiveTargetId)
                return false;

            //If I have no drones in bay or space, just return.
            if (Core.Metatron.Drones.TotalDrones <= 0 || Core.Metatron.Config.MiningConfig.UseMiningDrones)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "No drones in bay or configured to mine using drones");
                return false;
            }
            var somethingInRange = false;
            LogTrace(methodName, "DroneControlDistance: {0}", Core.Metatron.MeCache.DroneControlDistance);
            if (target.Distance < Core.Metatron.MeCache.DroneControlDistance)
            {
                somethingInRange = true;
                _unusedWeapons--;

                //If drones were recalled for damage, just dont' touch them.
                LogMessage(methodName, LogSeverityTypes.Debug, "DronesRecalled: {0}, HasFullAggro: {1}, IsUnderDangerousEwarAttack: {2}, CanLaunchDrones: {3}",
                    Core.Metatron.Drones.DronesRecalled, Core.Metatron.Attackers.HasFullAggro, Core.Metatron.Attackers.IsUnderDangerousEwarAttack, Core.Metatron.Drones.CanLaunchDrones());
                if (!Core.Metatron.Drones.DronesRecalled)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, $"Kill: {Core.Metatron.Drones.DronesInBay} drones in bay and {Core.Metatron.Drones.DronesInSpace} in space, max {Core.Metatron.Drones.MaxDronesInSpace}.");
                    //We want to send drones if we have full aggro, we're under dangerous ewar attack, or our target is of
                    // at least ECM priority (meaning it's at least a possible ewar target)
                    var queueTarget = Core.Metatron.Targeting.QueueTargetsByEntityId[target.ID];
                    if (Core.Metatron.Attackers.HaveHostilesRecentlySpawned && !Core.Metatron.Attackers.IsUnderDangerousEwarAttack && queueTarget.Priority > (int)TargetPriorities.Kill_OtherElectronicWarfare)
                    {
                        //LogMessage(methodName, LogSeverityTypes.Debug, "We don't have full aggro. If there are any drones in space, pull them.");
                        if (Core.Metatron.Drones.DronesInSpace > 0)
                        {
                            LogMessage(methodName, LogSeverityTypes.Debug, "Normally we would've recalled all drones here but I disabled that cause it's dumb");
                            //Core.Metatron.Drones.RecallAllDrones(true);
                        }
                    }
                    else
                    {
                        //If we have drones in bay and can launch more...
                        if (Core.Metatron.Drones.DronesInBay > 0 &&
                            Core.Metatron.Drones.DronesInSpace < Core.Metatron.Drones.MaxDronesInSpace)
                        {
                            LogMessage(methodName, LogSeverityTypes.Debug, $"Kill: {Core.Metatron.Drones.DronesInBay} drones in bay and {Core.Metatron.Drones.DronesInSpace} in space, max {Core.Metatron.Drones.MaxDronesInSpace}, launching.");
                            //Block target change and launch drones
                            if (Core.Metatron.Drones.CanLaunchDrones())
                            {
                                LogMessage(methodName, LogSeverityTypes.Debug, "We can launch drones. Attempting to launch.");
                                Core.Metatron.Targeting.BlockTargetChangeNextFrame();
                                Core.Metatron.Drones.LaunchAllDrones();
                            }
                        }
                        //If we have some drones in space...
                        else if (Core.Metatron.Drones.DronesInSpace > 0)
                        {
                            //Send the drone after our target
                            Core.Metatron.Drones.SendAllDrones();
                        }
                    }
                }
            }
            else
            {
                //if our drones are on another target which is in range, report something is in range.
                //The check will pass and they'll be sent to our main target as soon as it's in range, if it ever is.
                if (Core.Metatron.Drones.DroneTargetIsValid)
                {
                    somethingInRange = true;
                    _unusedWeapons--;
                }
            }

            return somethingInRange;
        }

        /// <summary>
        /// Attempt to use drones on the given target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="targetDamageProfile"></param>
        /// <returns>True if turrets were or can be activated on the given target, otherwise false</returns>
        private bool UseTurrets(IEntityWrapper target, DamageProfile targetDamageProfile)
        {
            var methodName = "UseTurrets";
            LogTrace(methodName, "Target: {0}", target.ID);

            var somethingInRange = false;

            //Turrets are a bit of adifferent breed. We want to try to first make sure we're in the optimal range band for
            //our hardest hitting ammo and try to hit a target in that band before shifting to a better ammo.
            if (Core.Metatron.Ship.TurretModules.Count > 0)
            {
                CalculateAmmoByModuleTables(targetDamageProfile);
            }

            //Track turrets available

            var anyModuleHasValidCharge = false;
            var canChangeAmmo = true;
            foreach (var module in Core.Metatron.Ship.TurretModules)
            {
                //If the module is reloading, just continue.
                if (Core.Metatron.Ship.DidModuleRecentlyChangeAmmo(module.ID))
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Ignoring module \"{0}\" while changing ammo...",
                        module.ToItem.Name);
                    somethingInRange = true;
                    _unusedWeapons--;
                    continue;
                }

                var ammoTypeIDsByModuleTypeId = _ammoTypeIDsByModuleTypeIDsByTargets[target.ID];
                var moduleItem = module.ToItem;
                var ammoTypeId = ammoTypeIDsByModuleTypeId != null ? ammoTypeIDsByModuleTypeId[moduleItem.TypeID] : -1;
                LogTrace(methodName, $"Ammo TypeID: {ammoTypeId}");

                //If the charge is null (no charge) just continue
                if (LavishScriptObject.IsNullOrInvalid(module.Charge))
                {
                    _unusedWeapons--;

                    if (module.IsActive) continue;

                    if (ammoTypeId != -1)
                    {
                        canChangeAmmo = false;
                        Core.Metatron.Ship.ChangeTurretAmmo(module, ammoTypeId);
                        continue;
                    }

                    LogMessage(methodName, LogSeverityTypes.Debug, "Ignoring module \"{0}\" due to invalid charge...",
                        module.ToItem.Name);
                    somethingInRange = false;
                    continue;
                }

                var chargeTypeId = module.Charge.TypeId;
                LogTrace(methodName, $"Charge TypeID: {chargeTypeId}");

                anyModuleHasValidCharge = true;

                //If the module's not active...
                if (!module.IsActive)
                {
                    if (ammoTypeId > -1)
                    {
                        _unusedWeapons--;
                        somethingInRange = true;

                        //If our target is in the current optimal range band of this module...
                        if (chargeTypeId == ammoTypeId)
                        {
                            LogTrace(methodName, "Charge typeid matches desired ammo");
                            //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                            //methodName, String.Format("Activating module \'{0}\' on active target \'{1}\' ({2}, {3}).",
                            //moduleItem.Name, _turretTargetEntity.Name, _turretTargetID, _turretTargetEntity.Distance)));
                            _turretTargetId = target.ID;
                            _activatedTurrets = true;

                            if (Core.Metatron.MeCache.Targets.Contains(target))
                                module.Activate(target.ID);
                        }
                        else if (canChangeAmmo)
                        {
                            canChangeAmmo = false;
                            //Targets are sorted by priority so the active one needs to die if it at all can.
                            //This target isn't in my current optimal range band, so see if I can change ammo to a fitting band.
                            //
                            LogTrace(methodName, "Changing ammo");
                            Core.Metatron.Ship.ChangeTurretAmmo(module, ammoTypeId);
                            //Return so we don't change ammo on all modules at once
                            //If it couldn't change ammo it'll just need to change target after turrent shit is done.
                        }
                    }
                }
                else if (!module.IsDeactivating && Core.Metatron.EntityProvider.EntityWrappersById.ContainsKey(module.TargetID))
                {
                    //Find the module's target. IF it's outside the current optimal range band, need to deactivate.

                    var moduleTarget = Core.Metatron.EntityProvider.EntityWrappersById[module.TargetID];

                    //If the module isn't on our high priority target deactivate.
                    if (moduleTarget.ID != target.ID)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug,
                                   "Deactivating module \'{0}\' with target \'{1}\' ({2}, {3}) due to active target mismatch.",
                                   moduleItem.Name, moduleTarget.Name, moduleTarget.ID, moduleTarget.Distance);

                        if (!_usedPainters)
                        {
                            _usedPainters = true;
                            Core.Metatron.Ship.DeactivateModuleList(Core.Metatron.Ship.TargetPainterModules, true);
                        }
                        module.Deactivate();
                        _turretTargetId = -1;
                        continue;
                    }

                    //if (_targetInModuleOptimalRangeBand(m, moduleTarget.Distance))
                    if (ammoTypeId == -1 || ammoTypeId != chargeTypeId)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug,
                                   "Deactivating module \'{0}\' with target \'{1}\' ({2}, {3}) due to range.",
                                   moduleItem.Name, moduleTarget.Name, moduleTarget.ID, moduleTarget.Distance);
                        module.Deactivate();
                    }
                }
            }

            //If all guns have invalid charges, consider something in range so we don't approach to 0.
            //This is a hack and needs a fundamental rework.
            if (!anyModuleHasValidCharge)
                somethingInRange = true;

            return somethingInRange;
        }

        /// <summary>
        /// Attempt to use target painters on the given target.
        /// </summary>
        /// <param name="target"></param>
        private void UseTargetPainters(IEntityWrapper target)
        {
            var methodName = "UseTargetPainters";
            LogTrace(methodName);

            if (!Core.Metatron.MeCache.Targets.Contains(target))
                return;

            long painterTarget;
            if (_launcherTargetId > -1)
                painterTarget = _launcherTargetId;
            else if (_turretTargetId > -1)
                painterTarget = _turretTargetId;
            else
                painterTarget = target.ID;

            foreach (var module in Core.Metatron.Ship.TargetPainterModules)
            {
                if (!module.IsActive && painterTarget >= 0)
                {
                    module.Activate(painterTarget);
                    LogMessage(methodName, LogSeverityTypes.Debug, string.Format("Activating painter \"{0}\" on target {1} ({2}).",
                        module.ToItem.Name, target.Name, target.ID));
                }
                else if (module.TargetID != painterTarget && !module.IsDeactivating)
                {
                    module.Deactivate();
                    LogMessage(methodName, LogSeverityTypes.Debug, string.Format("Deactivating painter \"{0}\" due to target mismatch.",
                        module.ToItem.Name));
                }
            }
        }

        private void CalculateAmmoByModuleTables(DamageProfile damageProfile)
        {
            var methodName = "CalculateAmmoByModuleTables";
            LogTrace(methodName);

            _ammoTypeIDsByModuleTypeIDsByTargets.Clear();
            foreach (var entity in _entitiesToKill)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Calculating ammo table for entity \"{0}\" ({1}, {2})...",
                    entity.Name, entity.ID, entity.Distance);
                _ammoTypeIDsByModuleTypeIDsByTargets.Add(entity.ID, Core.Metatron.Ship.GetAmmoTypeIdsByModuleTypeId(entity.Distance, damageProfile));
            }
        }

        private int CalculateTotalUnusedWeapons()
        {
            var methodName = "CalculateTotalUnusedWeapons";
            LogTrace(methodName);

            var totalUnusedGroups = 0;
            if (!Core.Metatron.Config.MiningConfig.UseMiningDrones &&
                Core.Metatron.Drones.TotalDrones > 0 &&
                !Core.Metatron.Drones.DroneTargetIsValid)
            {
                totalUnusedGroups++;
            }

            totalUnusedGroups += Core.Metatron.Ship.TurretModules.Count(module => !module.IsActive && !LavishScriptObject.IsNullOrInvalid(module.Charge));
            totalUnusedGroups += Core.Metatron.Ship.LauncherModules.Count(module => !module.IsActive && !LavishScriptObject.IsNullOrInvalid(module.Charge));
            return totalUnusedGroups;
        }

        private void ChangeTarget(IEntityWrapper intendedTarget)
        {
            var methodName = "ChangeTarget";
            LogTrace(methodName, "ActiveTarget: {0}", intendedTarget.ID);

            //Make sure we can change target
            if (!Core.Metatron.Targeting.CanChangeTarget)
                return;

            LogMessage(methodName, LogSeverityTypes.Standard, "Changing target to \'{0}\' ({1}, {2}) because of priority.",
                        intendedTarget.Name, intendedTarget.ID, intendedTarget.Distance);
            Core.Metatron.Targeting.ChangeTargetTo(intendedTarget, true);
        }

        private bool ChangeTargetForDrones(IEntityWrapper entity)
        {
            var methodName = "ChangeTargetForDrones";

            if (!Core.Metatron.MeCache.Targets.Contains(entity))
                return false;

            if (Core.Metatron.Drones.TotalDrones > 0 &&
                (Core.Metatron.Drones.DronesInSpace > 0 || Core.Metatron.Attackers.HasFullAggro) &&
                entity.Distance < Core.Metatron.MeCache.DroneControlDistance)
            {
                //Make sure drones aren't already being used.
                if (!Core.Metatron.Drones.DroneTargetIsValid ||
                    entity.ID != Core.Metatron.Drones.DroneTargetEntityId)
                {
                    //Change target for drones and return
                    LogMessage(methodName, LogSeverityTypes.Standard, "Changing target to \'{0}\' ({1}, {2}) for drones.",
                               entity.Name, entity.ID, entity.Distance);
                    Core.Metatron.Targeting.ChangeTargetTo(entity, true);
                    Core.Metatron.Drones.SendAllDrones();
                    return true;
                }
            }
            return false;
        }

        private bool ChangeTargetForLaunchers(IEntityWrapper entity)
        {
            var methodName = "ChangeTargetForLaunchers";
            LogTrace(methodName, "Target: {0}", entity.ID);

            var lastLauncherTypeId = 0;
            foreach (var module in Core.Metatron.Ship.LauncherModules)
            {
                //If the module's already active, has an invalid charge, or is changing ammo, don't check it.
                if (module.IsActive || module.IsDeactivating || Core.Metatron.Ship.DidModuleRecentlyChangeAmmo(module.ID) ||
                    LavishScriptObject.IsNullOrInvalid(module.Charge))
                    continue;

                var moduleItem = module.ToItem;
                if (lastLauncherTypeId == 0)
                {
                    lastLauncherTypeId = moduleItem.TypeID;
                }
                else if (lastLauncherTypeId == moduleItem.TypeID)
                {
                    //Don't check an identical turret twice
                    continue;
                }

                //This one's easy. If the target's within the calculated max missile range, change target.
                if (Core.Metatron.Ship.MaximumMissileRange(module) >= entity.Distance)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Changing target to \'{0}\' ({1}, {2}) for module \'{3}\'.",
                               entity.Name, entity.ID, entity.Distance, moduleItem.Name);
                    Core.Metatron.Targeting.ChangeTargetTo(entity, true);
                    return true;
                }
            }

            return false;
        }

        private bool ChangeTargetForTurrets(IEntityWrapper entity)
        {
            var methodName = "ChangeTargetForTurrets";
            LogTrace(methodName, "Target: {0}", entity.ID);

            var lastTurretTypeId = 0;
            foreach (var module in Core.Metatron.Ship.TurretModules)
            {
                //If the module's already active, has an invalid charge, or is changing ammo, don't check it.
                if (module.IsActive || module.IsDeactivating || Core.Metatron.Ship.DidModuleRecentlyChangeAmmo(module.ID) ||
                    LavishScriptObject.IsNullOrInvalid(module.Charge))
                    continue;

                var moduleItem = module.ToItem;
                if (lastTurretTypeId == 0)
                {
                    lastTurretTypeId = moduleItem.TypeID;
                }
                else if (lastTurretTypeId == moduleItem.TypeID)
                    //Don't check an identical turret twice
                    continue;

                //Alright, we've previously calculated ammo tables for targets.
                //Grab the one matching this entity.
                var ammoTable = _ammoTypeIDsByModuleTypeIDsByTargets[entity.ID];

                var ammoTypeId = ammoTable[moduleItem.TypeID];

                //If I have a valid ammo for this module and target, we're set. Change target and return.
                if (ammoTypeId == -1)
                    continue;

                LogMessage(methodName, LogSeverityTypes.Standard, "Changing target to \'{0}\' ({1}, {2}) for turret \'{3}\'.",
                           entity.Name, entity.ID, entity.Distance, moduleItem.Name);
                Core.Metatron.Targeting.ChangeTargetTo(entity, true);
                return true;
            }

            return false;
        }
    }
    // ReSharper restore CompareOfFloatsByEqualityOperator
    // ReSharper restore ConvertToConstant.Local
    // ReSharper restore UnusedParameter.Local
}
