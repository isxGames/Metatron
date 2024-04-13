using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using EVE.ISXEVE;
using EVE.ISXEVE.Interfaces;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    public class InventoryProvider : IInventoryProvider
    {
        private readonly ILogging _logging;
        private readonly IEveWindowProvider _eveWindowProvider;
        private readonly IShipCache _shipCache;
        private readonly IEve _eve;

        public static readonly string ShipCargoName = "ShipCargo";
        public static readonly string ShipOreHoldName = "ShipGeneralMiningHold";

        public InventoryProvider(ILogging logging, IEveWindowProvider eveWindowProvider, IShipCache shipCache, IEve eve)
        {
            _logging = logging;
            _eveWindowProvider = eveWindowProvider;
            _shipCache = shipCache;
            _eve = eve;
        }

        public IEveInvWindow GetInventoryWindow()
        {
            return _eveWindowProvider.GetInventoryWindow();
        }

        public bool IsInventoryOpen
        {
            get
            {
                var inventoryWindow = GetInventoryWindow();
                return inventoryWindow != null && inventoryWindow.IsValid;
            }
        }

        public void OpenInventory()
        {
            if (IsInventoryOpen) return;

            _eve.Execute(ExecuteCommand.OpenInventory);
        }

        #region Cargo Hold

        public bool IsCargoHoldActive
        {
            get
            {
                var inventoryWindow = GetInventoryWindow();
                if (inventoryWindow == null || !inventoryWindow.IsValid) return false;

                var activeChild = inventoryWindow.ActiveChild;
                if (activeChild == null || !activeChild.IsValid) return false;

                return activeChild.ItemId == _shipCache.Id && activeChild.Name == ShipCargoName;
            }
        }

        public double CargoCapacity 
        {
            get { return _shipCache.CargoCapacity; }
        }

        public double UsedCargoCapacity
        {
            get { return _shipCache.UsedCargoCapacity; }
        }

        public ReadOnlyCollection<IItem> Cargo
        {
            get { return _shipCache.Cargo; }
        }

        public IEveInvChildWindow GetCargoHoldWindow()
        {
            var methodName = "GetCargoHoldWindow";
            _logging.LogTrace(this, methodName);

            var inventoryWindow = _eveWindowProvider.GetInventoryWindow();
            if (inventoryWindow == null || !inventoryWindow.IsValid) return null;

            var childWindow = inventoryWindow.GetChildWindow(_shipCache.Id, ShipCargoName);
            return childWindow;
        }

        public void MakeCargoHoldActive()
        {
            var methodName = "MakeCargoHoldActive";
            _logging.LogTrace(this, methodName);
            
            var cargoHoldWindow = GetCargoHoldWindow();

            if (cargoHoldWindow == null || !cargoHoldWindow.IsValid)
            {
                _logging.LogMessage(this, methodName, LogSeverityTypes.Debug, "Error: Cargo hold window was null or invalid.");
                return;
            }

            cargoHoldWindow.MakeActive();
        }

        #endregion

        #region Ore Hold

        public bool IsOreHoldActive
        {
            get
            {
                var inventoryWindow = GetInventoryWindow();
                if (inventoryWindow == null || !inventoryWindow.IsValid) return false;

                var activeChild = inventoryWindow.ActiveChild;
                if (activeChild == null || !activeChild.IsValid) return false;

                return activeChild.ItemId == _shipCache.Id && activeChild.Name == ShipOreHoldName;
            }
        }

        public double OreHoldCapacity
        {
            get
            {
                var oreHoldWindow = GetOreHoldWindow();

                if (oreHoldWindow == null || !oreHoldWindow.IsValid) return -1;

                return oreHoldWindow.Capacity;
            }
        }

        public double OreHoldUsedCapacity
        {
            get
            {
                var oreHoldWindow = GetOreHoldWindow();

                if (oreHoldWindow == null || !oreHoldWindow.IsValid) return -1;

                return oreHoldWindow.UsedCapacity;
            }
        }

        public ReadOnlyCollection<IItem> OreHoldCargo
        {
            get { return _shipCache.OreHoldCargo; }
        }

        public IEveInvChildWindow GetOreHoldWindow()
        {
            var methodName = "GetOreHoldWindow";
            _logging.LogTrace(this, methodName);

            var inventoryWindow = _eveWindowProvider.GetInventoryWindow();
            if (inventoryWindow == null || !inventoryWindow.IsValid) return null;

            var childWindow = inventoryWindow.GetChildWindow(_shipCache.Id, ShipOreHoldName);
            return childWindow;
        }

        public void MakeOreHoldActive()
        {
            var methodName = "MakeOreHoldActive";
            _logging.LogTrace(this, methodName);

            var oreHoldWindow = GetOreHoldWindow();

            if (oreHoldWindow == null || !oreHoldWindow.IsValid)
            {
                _logging.LogMessage(this, methodName, LogSeverityTypes.Debug, "Error: Ore hold window was null or invalid.");
                return;
            }

            oreHoldWindow.MakeActive();
        }

        #endregion
    }
}
