using System;
using System.Collections.Generic;
using System.Linq;
using EVE.ISXEVE;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    internal sealed class Drones : ModuleBase, IDrones
    {
		//Track how many drones we can have in space
        private int _maxDronesInSpace = -1;

		//Track the last known target of drones
        public Int64 DroneTargetEntityId { get; private set; }

		//Easy property for accessing last known target of drones
		public IEntityWrapper DroneTarget
		{
			get
			{
				if (Metatron.EntityProvider.EntityWrappersById.ContainsKey(DroneTargetEntityId))
				{
					return Metatron.EntityProvider.EntityWrappersById[DroneTargetEntityId];
				}
				return null;
			}
		}

		//Track the last known values of drone shield and armor
    	private readonly Dictionary<Int64, double> _droneLastShieldValues = new Dictionary<Int64, double>();
    	private readonly Dictionary<Int64, double> _droneLastArmorValues = new Dictionary<Int64, double>();

		//Keep cached versions of active drones
    	private readonly List<CachedDrone> _cachedDrones = new List<CachedDrone>();
    	private readonly Dictionary<Int64, CachedDrone> _cachedDronesById = new Dictionary<long, CachedDrone>();

		//Track the time we can next launch drones
		private DateTime _nextDroneLaunch = DateTime.Now, _nextAllowDroneStatusUpdate = DateTime.Now;
    	private int DroneDamageRelaunchDelaySeconds = 5;
		private bool _dronesRecalledForTank;

        private readonly IIsxeveProvider _isxeveProvider;

        public Drones(IIsxeveProvider isxeveProvider)
        {
            _isxeveProvider = isxeveProvider;

            ModuleManager.ModulesToPulse.Add(this);
            DroneTargetEntityId = -1;
			ModuleName = "Drones";
        }

        public override void Pulse()
        {
            var methodName = "Pulse";
			LogTrace(methodName);

        	if (!ShouldPulse())
				return;

            if (!Metatron.MeCache.InSpace)
                return;

            StartPulseProfiling();

            if (Metatron.MeCache.ActiveDrones.Count > 0)
            {
                foreach (var activeDrone in Metatron.MeCache.ActiveDrones)
                {
                    if (LavishScriptObject.IsNullOrInvalid(activeDrone) || activeDrone.ID <= 0)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Got invalid drone - skipping.");
                        continue; //Yay for invalid drones.
                    }

                    var cachedDrone = new CachedDrone(activeDrone);

                    _cachedDrones.Add(cachedDrone);
                    _cachedDronesById.Add(cachedDrone.Id, cachedDrone);
                }
            }
			//else
			//{
			//    LogMessage(methodName, LogSeverityTypes.Debug, "No active drones were found.");
			//}

            //if I needed to recall drones...
        	if (_dronesRecalledForTank)
        	{
        		//Update the timer.
        		_nextDroneLaunch = DateTime.Now.AddSeconds(DroneDamageRelaunchDelaySeconds);

				_dronesRecalledForTank = false;

        		//If all drones are in, unset the flag.
        		//if (DronesInSpace == 0)
        		//	_dronesRecalledForTank = false;
        		//else
        		//	RecallAllDrones(true);
        	}

            UpdateDroneStatus();

        	//If there are any drones in space, do the HP check
        	if (DronesInSpace > 0)
        	{
        		CheckDroneHitPoints();
        	}

        	//If there is a current drone target
        	if (DroneTargetEntityId > 0)
        	{
        		//Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
        		//"Pulse", String.Format("Target valid: {0}, IsQueued(target): {1}, Drones in Space: {2}, 
        		//and it's either invalid, not queued, or there are no drones in space
        		if (!DroneTargetIsValid || !Metatron.TargetQueue.IsQueued(DroneTargetEntityId) ||
        		    DronesInSpace == 0/* ||
						(DronesInSpace > 0 || Core.Metatron._Me.ActiveDrones[0].State != (int)DroneStates.Fighting)*/)
        		{
        			//reset current drone target
        			DroneTargetEntityId = -1;
        		}
        	}
        	EndPulseProfiling();
        }

		public override void InFrameCleanup()
		{
			//Clear old drone cache entries
			_cachedDronesById.Clear();
			_cachedDrones.Clear();
		}

        public bool RecentlyLaunchedDrones
        {
            get { return DateTime.Now.CompareTo(_nextAllowDroneStatusUpdate) < 0; }
        }

        private void UpdateDroneStatus()
        {
            if (RecentlyLaunchedDrones)
                return;

            DronesInBay = Metatron.MeCache.Ship.Drones.Count();
            DronesInSpace = Metatron.MeCache.ActiveDrones.Count;
        }

        public bool DroneTargetIsValid
        {
            get
            {
				return Metatron.EntityProvider.EntityWrappersById.ContainsKey(DroneTargetEntityId);
            }
        }

        public int MaxDronesInSpace
        {
            get
            {
				return (int)Metatron.MeCache.MaxActiveDrones;
            }
        }

		public bool DronesRecalled
		{
			get
			{
				return _dronesRecalledForTank;
			}
		}

        public void LaunchAllDrones()
        {
            var methodName = "LaunchAllDrones";
			LogTrace(methodName);

            if (!CanLaunchDrones())
                return;

			LogMessage(methodName, LogSeverityTypes.Debug, "Launching all drones.");
            Metatron.MeCache.Ship.Ship.LaunchAllDrones();

            _nextDroneLaunch = DateTime.Now.AddSeconds(5);
            _nextAllowDroneStatusUpdate = DateTime.Now.AddSeconds(5);
        }

        public bool CanLaunchDrones()
        {
            var methodName = "CanLaunchDrones";
            LogTrace(methodName);

            return (DateTime.Now.CompareTo(_nextDroneLaunch) >= 0);
        }

		private void CheckDroneHitPoints()
		{
            var methodName = "CheckDroneHitPoints";
			LogTrace(methodName);

			var needRecall = false;

			//Iterate all cached drones...
			foreach (var drone in _cachedDrones)
			{
				//If it's a known drone for shield values...
				if (_droneLastShieldValues.ContainsKey(drone.Id))
				{
					//If its shield value has decreased since last check...
					if (drone.ShieldPct < _droneLastShieldValues[drone.Id])
					{
						needRecall = true;
					}
					//update its last known value
					_droneLastShieldValues[drone.Id] = drone.ShieldPct;
				}
				else
				{
					//Add the drone and its current value to the list
					_droneLastShieldValues.Add(drone.Id, drone.ShieldPct);
				}

				//If it's a known drone for armor values...
				if (_droneLastArmorValues.ContainsKey(drone.Id))
				{
					//and its armor has decreased since last check...
					if (drone.ArmorPct < _droneLastArmorValues[drone.Id])
					{
						needRecall = true;
					}
					//update its current value
					_droneLastArmorValues[drone.Id] = drone.ArmorPct;
				}
				else
				{
					//Add the new drone to the list with its current value
					_droneLastArmorValues.Add(drone.Id, drone.ArmorPct);
				}

				if (!needRecall) continue;

				LogMessage(methodName, LogSeverityTypes.Debug, "Recalling drone {0} ({1}) because it's taking damage.",
					drone.ToActiveDrone.ToEntity.Name, drone.Id);
				drone.ReturnToDroneBay();
				_dronesRecalledForTank = true;
			}
		}

        public bool SendAllDrones()
        {
            var methodName = "SendAllDrones";
			LogTrace(methodName);

			//Keep track of whether or not we sent drones
			var dronesSent = false;
			
			//Keep a list of any drones to send
			var droneIdsToSend = new List<long>();

			//If we have sent drones after a different target than the active target or we have idle drones, send drones.
			if (DroneTargetEntityId != Metatron.MeCache.ActiveTargetId)
			{
				//Set the drone target ID to the current active target
				DroneTargetEntityId = Metatron.MeCache.ActiveTargetId;

				//Populate the list of drones to send with any drones not recalled due to damage
				droneIdsToSend.AddRange(_cachedDrones.Select(drone => drone.Id));

				LogMessage(methodName, LogSeverityTypes.Standard, "Sending all drones after active target.");
			}

			//If we haven't sent any droens yet due to different ID, check those which may be idle
			if (droneIdsToSend.Count == 0)
			{
				//Add any idle drones
				foreach (var cachedDrone in _cachedDrones)
				{
					if (cachedDrone.State == (int)DroneStates.Idle ||
                        cachedDrone.TargetEntityId != DroneTargetEntityId)
					{
						droneIdsToSend.Add(cachedDrone.Id);
					}
				}

				//If we added anything...
				if (droneIdsToSend.Count > 0)
				{
					LogMessage(methodName, LogSeverityTypes.Debug, "Sending idle and mistargeted drones after active target.");
				}
			}

			//If the list of drones to send contains something, send 'em.
			if (droneIdsToSend.Count > 0)
			{
				dronesSent = true;
				if (!Metatron.Config.MiningConfig.UseMiningDrones)
				{
					DronesEngageMyTarget(droneIdsToSend);
				}
				else
				{
					DronesMineRepeatedly(droneIdsToSend);
				}
			}

			return dronesSent;
        }

        public void RecallAllDrones(bool toBay)
        {
            var methodName = "RecallAllDrones";
        	LogTrace(methodName, "ToBay: {0}", toBay);

			//Reset the drone active target
			DroneTargetEntityId = -1;

			//Keep track of whether or not all drones are returning
			var allDronesReturning = true;
			
			//Iterate all of my drones
            foreach (var drone in _cachedDrones)
            {
				//If the drone didnt' have a valid entity ID, just set the flag true to force recall and continue.
				//Even if it's already returning.
				var doContinue = false;
				if (!drone.IsEntityValid)
				{
					LogMessage(methodName, LogSeverityTypes.Debug, "ActiveDrone {0}, {1} has no valid entity.",
					           drone.Id, drone.State);
					doContinue = true;
				}
				else if (LavishScriptObject.IsNullOrInvalid(drone.ToActiveDrone))
				{
					LogMessage(methodName, LogSeverityTypes.Debug, "ActiveDrone {0}, {1} is invalid.",
					           drone.Id, drone.State);
					doContinue = true;
				}
				else if (LavishScriptObject.IsNullOrInvalid(drone.ToActiveDrone.ToEntity))
				{
					LogMessage(methodName, LogSeverityTypes.Debug, "ActiveDrone {0}, {1} is invalid.",
					           drone.ToActiveDrone.ID, drone.ToActiveDrone.State);
					doContinue = true;
				}

				if (doContinue)
				{
					allDronesReturning = true;
					continue;
				}

				//If a drone isn't currently in the returning state
                if (drone.State == (int)DroneStates.Returning)
                {
                    //LogMessage(methodName, LogSeverityTypes.Debug, "Drone {0}, {1} is already returning.",
                    //    drone.ToActiveDrone.ID, drone.ToActiveDrone.State);
                    continue;
                }

                if (drone.HasDroneBeenCommanded)
            	{
                    //LogMessage(methodName, LogSeverityTypes.Standard, "Drone \"{0}\" ({1}) has been commanded this frame; skipping.",
                    //    drone.ToActiveDrone.ToEntity.Name, drone.ToActiveDrone.ToEntity.ID);
            		continue;
            	}

            	//If we're pulling drones to bay...
            	if (toBay)
            	{
            		//and it's in scoop range and hasn't been commanded, scoop it
            		if (drone.Distance <= (int)Ranges.LootActivate * 0.95)
            		{
						LogMessage(methodName, LogSeverityTypes.Standard, "Scooping drone \"{0}\" ({1}) instead of issuing recall command.",
							drone.ToActiveDrone.ToEntity.Name, drone.ToActiveDrone.ToEntity.ID);
            			drone.ScoopToDroneBay();
            			continue;
            		}
            	}

            	//Clearly we need to return a drone
            	allDronesReturning = false;
            }

			//If any drones aren't returning...
            if (allDronesReturning)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Already recalling all drones.");
                return;
            }

            //If pulling to bay...
			if (toBay)
			{
				LogMessage(methodName, LogSeverityTypes.Debug, "Returning all drones to bay.");
				//Pull 'em to bay!
                _isxeveProvider.Eve.Execute(ExecuteCommand.CmdDronesReturnToBay);
			}
			else
			{
				LogMessage(methodName, LogSeverityTypes.Debug, "Returning all drones to orbit.");
				//Pull 'em all to orbit!
                _isxeveProvider.Eve.Execute(ExecuteCommand.CmdDronesReturnAndOrbit);
			}

			//Make sure they're all marked as having been commanded this frame.
			foreach (var cachedDrone in _cachedDrones)
			{
				cachedDrone.HasDroneBeenCommanded = true;
			}
        }

		#region quantities
        public int DronesInSpace { get; private set; }

        public int DronesInBay { get; private set; }

        public int TotalDrones
        {
            get
            {
                return DronesInSpace + DronesInBay;
            }
        }

        #endregion

        public bool IsAnyDroneIdle
        {
            get { return _cachedDrones.Any(cachedDrone => cachedDrone.State == (int)DroneStates.Idle); }
        }

		private void DronesEngageMyTarget(List<Int64> droneIDs)
		{
			var methodName = "DronesEngageMyTarget";
			LogTrace(methodName);

			//Mark any involved drone as having been commanded
			foreach (var id in droneIDs.Where(id => _cachedDronesById.ContainsKey(id)))
			{
				_cachedDronesById[id].HasDroneBeenCommanded = true;
			}

			//Tell EVE to send the damn drones
            _isxeveProvider.Eve.Execute(ExecuteCommand.CmdDronesEngage);
		}

		private void DronesMineRepeatedly(List<Int64> droneIDs)
		{
			var methodName = "DronesMineRepeatedly";
			LogTrace(methodName);

			//Mark any involved drone as having been commanded
			foreach (var id in droneIDs.Where(id => _cachedDronesById.ContainsKey(id)))
			{
				_cachedDronesById[id].HasDroneBeenCommanded = true;
			}

			//Send teh drones!
            _isxeveProvider.Eve.DronesMineRepeatedly(droneIDs);
		}

		public bool CanAttackEntity(IEntityWrapper entity)
		{
			var methodName = "CanAttackEntity";
			LogTrace(methodName, "entityID: {0}", entity.ID);

			if (TotalDrones <= 0 || Metatron.Config.MiningConfig.UseMiningDrones) return false;

			return entity.Distance < Metatron.MeCache.DroneControlDistance;
		}
    }
}
