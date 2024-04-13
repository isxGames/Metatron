using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EVE.ISXEVE;
using EVE.ISXEVE.Interfaces;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    internal class Station : ModuleBase, IStation
    {
        private readonly List<int> _stationTypeIds; 
        public ReadOnlyCollection<int> StationTypeIDs
        {
            get { return _stationTypeIds.AsReadOnly(); }
        }

        private readonly IIsxeveProvider _isxeveProvider;
        private readonly IEveWindowProvider _eveWindowProvider;

        internal Station(IIsxeveProvider isxeveProvider, IEveWindowProvider eveWindowProvider)
        {
            _isxeveProvider = isxeveProvider;
            _eveWindowProvider = eveWindowProvider;

            IsEnabled = false;
            ModuleName = "Station";

            _stationTypeIds = new List<int>
                {
                    (int) TypeIDs.Concord_Starbase,
                    (int) TypeIDs.Minmatar_Trade_Post,
                    (int) TypeIDs.Amarr_Trade_Post,
                    (int) TypeIDs.Minmatar_Hub
                };
        }

        public bool IsDockedAtStation(long stationId)
        {
            var methodName = "IsDockedAtStation";
			LogTrace(methodName, "StationID: {0}", stationId);
            
			return !Metatron.MeCache.InSpace && Metatron.MeCache.InStation && Metatron.MeCache.StationId == stationId;
        }

        /// <summary>
        /// Undock from the current station.
        /// </summary>
        public void Undock()
        {
            var methodName = "Undock";
			LogTrace(methodName);

            //Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
            //    "Undock", String.Format("Undocking from station {0}", Core.MeCache.Me.Station.Name)));
            Metatron.Config.MovementConfig.HomeStation = Metatron.MeCache.Me.Station.Name;
            _isxeveProvider.Eve.Execute(ExecuteCommand.CmdExitStation);
        }

        public bool IsStationHangarActive
        {
            get
            {
				if (!Metatron.Ship.IsInventoryOpen) return false;

                var inventoryWindow = _eveWindowProvider.GetInventoryWindow();
				if (LavishScriptObject.IsNullOrInvalid(inventoryWindow)) return false;

            	var activeChild = inventoryWindow.ActiveChild;
				if (LavishScriptObject.IsNullOrInvalid(activeChild)) return false;

				return activeChild.ItemId == Metatron.MeCache.StationId;
            }
        }

		public void MakeStationHangarActive()
		{
			var methodName = "MakeStationHangarActive";
			LogTrace(methodName);

			if (!Metatron.Ship.IsInventoryOpen) return;

			var childWindow = GetStationItemsChildWindow();
			if (LavishScriptObject.IsNullOrInvalid(childWindow)) return;

			childWindow.MakeActive();
		}

    	private IEveInvChildWindow GetStationItemsChildWindow()
    	{
    		var methodName = "GetStationItemsChildWindow";
    		LogTrace(methodName);

            var inventoryWindow = _eveWindowProvider.GetInventoryWindow();
    		if (LavishScriptObject.IsNullOrInvalid(inventoryWindow)) return null;

            IEveInvChildWindow childWindow; 
           
            if (IsCurrentStationAStructure())
            {
                childWindow = inventoryWindow.GetChildWindow(Metatron.MeCache.StationId, "StructureItemHangar");
                if (LavishScriptObject.IsNullOrInvalid(childWindow))
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Error: Child window for ID {0}, Name StructureItemHangar was null or invalid.", Metatron.MeCache.StationId);
                    return null;
                }
            }
            else
            {
                childWindow = inventoryWindow.GetChildWindow(Metatron.MeCache.StationId, "StationItems");
                if (LavishScriptObject.IsNullOrInvalid(childWindow))
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Error: Child window for ID {0}, Name StationItems was null or invalid.", Metatron.MeCache.StationId);
                    return null;
                }

            }
            return childWindow;
    	}

        private bool IsCurrentStationAStructure()
        {
            var methodName = "IsCurrentStationAStructure";
            bool returnVal;
            //Check if stationID is an Int to see if we're in a station or a structure.
            if (Metatron.MeCache.StationId <= int.MaxValue && Metatron.MeCache.StationId >= int.MinValue)
            {
                //ID is an INT that means it's an NPC station
                returnVal = false;
            }
            else
            {
                returnVal = true;
            }
            LogTrace(methodName, returnVal.ToString());
            return returnVal;
        }

        private IEveInvChildWindow GetStationCorpHangarChildWindow()
		{
			var methodName = "GetStationCorpHangarChildWindow";
			LogTrace(methodName);

            var inventoryWindow = _eveWindowProvider.GetInventoryWindow();
			if (LavishScriptObject.IsNullOrInvalid(inventoryWindow)) return null;

			var folder = String.Format("Folder{0}", Metatron.Config.CargoConfig.DropoffLocation.HangarDivision);
			var childWindow = inventoryWindow.GetChildWindow("StationCorpHangar", folder);
			if (LavishScriptObject.IsNullOrInvalid(childWindow))
			{
				LogMessage(methodName, LogSeverityTypes.Debug, "Error: Child window for Name StationCorpHangar, Location {0} was null or invalid.", folder);
				return null;
			}

			return childWindow;
		}

		private IEveInvChildWindow GetStationCorpHangarsContainerChildWindow()
		{
			var methodName = "GetStationCorpHangarContainerChildWindow";
			LogTrace(methodName);

            var inventoryWindow = _eveWindowProvider.GetInventoryWindow();
			if (LavishScriptObject.IsNullOrInvalid(inventoryWindow)) return null;

			var childWindow = inventoryWindow.GetChildWindow("StationCorpHangars");
			if (LavishScriptObject.IsNullOrInvalid(childWindow))
			{
				LogMessage(methodName, LogSeverityTypes.Debug, "Error: Child window for station Name StationCorpHangars was null.");
			}

			return childWindow;
		}

    	public bool IsStationCorpHangarActive
		{
			get
			{
                var inventoryWindow = _eveWindowProvider.GetInventoryWindow();
				if (LavishScriptObject.IsNullOrInvalid(inventoryWindow)) return false;

				var activeChild = inventoryWindow.ActiveChild;
				if (LavishScriptObject.IsNullOrInvalid(activeChild)) return false;

				return activeChild.ItemId == Metatron.MeCache.StationId && activeChild.Name == "StationCorpHangar";
			}
		}

    	public void MakeStationCorpHangarActive()
    	{
    		var childWindow = GetStationCorpHangarChildWindow();

			if (LavishScriptObject.IsNullOrInvalid(childWindow)) return;

    		childWindow.MakeActive();
    	}

		public void InitializeStationCorpHangars()
		{
			var childWindow = GetStationCorpHangarsContainerChildWindow();

			if (LavishScriptObject.IsNullOrInvalid(childWindow)) return;

			childWindow.MakeActive();
		}
    }
}
