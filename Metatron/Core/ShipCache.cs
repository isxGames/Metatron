using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EVE.ISXEVE.Interfaces;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    public sealed class ShipCache : ModuleBase, IShipCache
    {
        public Int64 Id { get; private set; }
        public string Name { get; private set; }
        public double ArmorPct { get; private set; }
        public double StructurePct { get; private set; }
        public double ShieldPct { get; private set; }
        public double CapacitorPct { get; private set; }
        public double CargoCapacity { get; private set; }
        public double UsedCargoCapacity { get; private set; }
        public double MaxTargetRange { get; private set; }
        public double MaxLockedTargets { get; private set; }
        public double Capacitor { get; private set; }
        public double Armor { get; private set; }
        public double Shield { get; private set; }
        public double MaxArmor { get; private set; }
        public double MaxShield { get; private set; }
        public double MaxCapacitor { get; private set; }
        public double DronebayCapacity { get; private set; }

        private EVE.ISXEVE.MyShip _ship;
        public EVE.ISXEVE.MyShip Ship
        {
            get
            {
                if (LavishScriptObject.IsNullOrInvalid(_ship))
                {
                    _ship = new EVE.ISXEVE.MyShip();

                    if (LavishScriptObject.IsNullOrInvalid(_ship))
                    {
                        LogMessage("Ship_get", LogSeverityTypes.Debug, "Error: Ship reference null or invalid.");
                    }
                }

                return _ship;
            }
        }

        private readonly List<IItem> _cargo = new List<IItem>();
        public ReadOnlyCollection<IItem> Cargo { get { return _cargo.AsReadOnly(); } }

        private readonly List<IItem> _oreHoldCargo = new List<IItem>();
        public ReadOnlyCollection<IItem> OreHoldCargo { get { return _oreHoldCargo.AsReadOnly(); } } 

        private readonly List<IItem> _drones = new List<IItem>();
        public ReadOnlyCollection<IItem> Drones { get { return _drones.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _modules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> Modules { get { return _modules.AsReadOnly(); } }

        internal ShipCache()
        {
            PulseFrequency = 1;
            ModuleManager.CachesToPulse.Add(this);
            base.ModuleName = "ShipCache";
        }

        public override void Pulse()
        {
            var methodName = "Pulse";
            LogTrace(methodName);

            if (!ShouldPulse()) return;

            StartPulseProfiling();

            StartMethodProfiling("Pulse - Both");

            Id = Ship.ID;
            Name = Ship.Name;

            //Do this both instation and inspace
            //StartMethodProfiling("GetCargoCapacity");
            if (Ship.CargoCapacity >= 0)
                CargoCapacity = Ship.CargoCapacity;

            if (Ship.DronebayCapacity >= 0)
                DronebayCapacity = Ship.DronebayCapacity;

            //EndMethodProfiling();
            //StartMethodProfiling("GetCargo");
            if (Ship.UsedCargoCapacity >= 0)
                UsedCargoCapacity = Ship.UsedCargoCapacity;

            if (Metatron.Ship.IsInventoryOpen && Metatron.Ship.IsInventoryReady)
            {
                //LogMessage(methodName, LogSeverityTypes.Debug, "Ship Inventory is ready. Getting cargo.");
                var cargo = Ship.GetCargo();
                if (cargo == null)
                {
                    //LogMessage(methodName, LogSeverityTypes.Debug, "Ship.GetCargo returned a null list.");
                }
                else
                {
                    //LogMessage(methodName, LogSeverityTypes.Debug, $"Ship.GetCargo returned {cargo.Count} items.");
                    _cargo.AddRange(cargo);
                }

                var oreHoldCargo = Ship.GetOreHoldCargo();
                if (oreHoldCargo == null)
                {
                    //LogMessage(methodName, LogSeverityTypes.Debug, "Ship.GetOreHoldCargo returned a null list.");
                }
                else
                {
                    //LogMessage(methodName, LogSeverityTypes.Debug, $"Ship.GetOreHoldCargo returned {oreHoldCargo.Count} items.");
                    _oreHoldCargo.AddRange(oreHoldCargo);
                }
            }
            //EndMethodProfiling();
            //StartMethodProfiling("InSpaceCheck");
            //Do the station-only stuff and return
            if (!Metatron.MeCache.Me.InSpace && Metatron.MeCache.Me.InStation)
            {
                //Only get drones in space
                if (DronebayCapacity > 0 && Metatron.Ship.IsCargoHoldActive)
                {
                    GetDrones();
                }
                EndMethodProfiling();
                EndPulseProfiling();

                return;
            }
            //EndMethodProfiling();
            EndMethodProfiling();

            //***** EVERYTHING BELOW THIS LINE ONLY HAPPENS IN SPACE *****

            StartMethodProfiling("Pulse - GetModules");
            var modules = Ship.GetModules();
            if (modules == null)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Ship.GetModules returned a null list.");
            }
            else
            {
                _modules.AddRange(modules);
            }
            EndMethodProfiling();

            StartMethodProfiling("Pulse - GetDrones");
            if (DronebayCapacity > 0)
                GetDrones();
            EndMethodProfiling();

            StartMethodProfiling("Pulse - Stats");
            ArmorPct = Ship.ArmorPct;
            ShieldPct = Ship.ShieldPct;
            CapacitorPct = Ship.CapacitorPct;
            StructurePct = Ship.StructurePct;
            MaxLockedTargets = Ship.MaxLockedTargets;
            MaxTargetRange = Ship.MaxTargetRange;
            Capacitor = Ship.Capacitor;
            Armor = Ship.Armor;
            MaxArmor = Ship.MaxArmor;
            Shield = Ship.Shield;
            MaxShield = Ship.MaxShield;
            MaxCapacitor = Ship.MaxCapacitor;
            EndMethodProfiling();

            EndPulseProfiling();
        }

        private void GetDrones()
        {
            var methodName = "GetDrones";
            LogTrace(methodName);

            var drones = Ship.GetDrones();
            if (drones == null)
            {
                //LogMessage(methodName, LogSeverityTypes.Debug, "Ship.GetDrones returned a null list.");
            }
            else
            {
                //LogMessage(methodName, LogSeverityTypes.Debug, $"Ship.GetDrones returned {drones.Count} drones");
                _drones.AddRange(drones);
            }
        }

        public override void InFrameCleanup()
        {
            foreach (var item in _cargo)
            {
                item.Dispose();
            }
            _cargo.Clear();

            _oreHoldCargo.Clear();

            foreach (var item in _drones)
            {
                item.Dispose();
            }
            _drones.Clear();

            foreach (var module in _modules)
            {
                module.Dispose();
            }
            _modules.Clear();

            if (_ship != null)
            {
                _ship.Dispose();
                _ship = null;
            }
        }
    }
}