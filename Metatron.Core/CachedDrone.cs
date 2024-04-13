using System;
using EVE.ISXEVE;
using LavishScriptAPI;

namespace Metatron.Core
{
    public sealed class CachedDrone
    {
        readonly ActiveDrone _activeDrone;

        public bool HasDroneBeenCommanded, IsEntityValid;
        public int State = -1;
        public Int64 Id = -1, TargetEntityId = -1;
        public double Distance = -1, ShieldPct = -1, ArmorPct = -1;

        public CachedDrone(ActiveDrone activeDrone)
        {
            _activeDrone = activeDrone;
            State = activeDrone.State;
            Id = activeDrone.ID;

            var activeDroneEntity = activeDrone.ToEntity;
            if (LavishScriptObject.IsNullOrInvalid(activeDroneEntity) || activeDroneEntity.ID <= 0)
            {
                Distance = -1;
                ShieldPct = -1;
                ArmorPct = -1;
                IsEntityValid = false;
            }
            else
            {
                Distance = activeDroneEntity.Distance;
                ShieldPct = activeDroneEntity.ShieldPct;
                ArmorPct = activeDroneEntity.ArmorPct;
                TargetEntityId = LavishScriptObject.IsNullOrInvalid(activeDrone.Target) ? -1 : activeDrone.Target.ID;
                IsEntityValid = true;
                //Metatron.Logging.LogMessage("CachedDrone", "CachedDrone", LogSeverityTypes.Debug, "ActiveDrone dump: ID {0}, Name \"{1}\", State {2}, Distance {3}",
                //activeDrone.ID, activeDrone.ToEntity.Name, activeDrone.State, activeDrone.ToEntity.Distance);
            }

            //TargetEntityID = activeDrone.Target.ID;
        }

        public void ScoopToDroneBay()
        {
            if (HasDroneBeenCommanded)
                return;

            _activeDrone.ToEntity.ScoopToDroneBay();
            HasDroneBeenCommanded = true;
        }

        public void ReturnToDroneBay()
        {
            if (HasDroneBeenCommanded)
                return;

            _activeDrone.ToEntity.ReturnToDroneBay();
            HasDroneBeenCommanded = true;
        }

        public ActiveDrone ToActiveDrone
        {
            get { return _activeDrone; }
        }
    }
}
