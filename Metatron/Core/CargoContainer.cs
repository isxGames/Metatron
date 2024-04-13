using System.Collections.Generic;
using System.Linq;
using EVE.ISXEVE;
using EVE.ISXEVE.Interfaces;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    public abstract class CargoContainer : ModuleBase, ICargoContainer
    {
// ReSharper disable InconsistentNaming
        protected readonly IEveWindowProvider _eveWindowProvider;
// ReSharper restore InconsistentNaming

    	protected CargoContainer(IEveWindowProvider eveWindowProvider)
    	{
    	    _eveWindowProvider = eveWindowProvider;

    	    ModuleName = "CargoContainer";
    	}

        public static List<IItem> GetOreFromList(List<IItem> items)
        {
        	var retList = items
				.Where(item => item.GroupID != (int) GroupIDs.MiningCrystal && item.GroupID != (int) GroupIDs.MercoxitMiningCrystal)
				.ToList();
        	return retList;
        }

    	public abstract IEntityWrapper CurrentContainer { get; }

    	public virtual bool IsCurrentContainerWindowOpen
    	{
    		get
    		{
				var childWindow = GetChildWindow();

    			return !LavishScriptObject.IsNullOrInvalid(childWindow);
    		}
    	}

    	public virtual bool IsCurrentContainerWindowActive
    	{
    		get
    		{
                var inventoryWindow = _eveWindowProvider.GetInventoryWindow();
    			var activeChildWindow = inventoryWindow.ActiveChild;

				if (LavishScriptObject.IsNullOrInvalid(activeChildWindow))
				{
					LogMessage("IsCurrentContainerWindowActive", LogSeverityTypes.Debug, "Error: EveInvWindow.ActiveChild is null or invalid.");
					return false;
				}

    			return activeChildWindow.ItemId == CurrentContainer.ID;
    		}
    	}

    	public virtual void MakeCurrentContainerWindowActive()
    	{
    		var methodName = "MakeCurrentContainerWindowActive";
    		LogTrace(methodName);

    		var childWindow = GetChildWindow();

    		if (LavishScriptObject.IsNullOrInvalid(childWindow))
    		{
				LogMessage(methodName, LogSeverityTypes.Debug, "Error: Could not get child window for container ID {0}.", CurrentContainer.ID);
    			return;
    		}

    		childWindow.MakeActive();
    	}

    	public IEveInvChildWindow GetChildWindow()
    	{
    		var methodName = "GetChildWindow";
    		LogTrace(methodName);

            var inventoryWindow = _eveWindowProvider.GetInventoryWindow();

			if (LavishScriptObject.IsNullOrInvalid(inventoryWindow))
			{
				LogMessage(methodName, LogSeverityTypes.Debug, "Error: Inventory window was null.");
				return null;
			}

    	    var eveInvChildWindow = inventoryWindow.GetChildWindow(CurrentContainer.ID);

            if (eveInvChildWindow == null)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Error: Child inventory window for ID {0} was null.", CurrentContainer.ID);
                return null;
            }

    	    return eveInvChildWindow;
    	}

    	public virtual double Capacity
    	{
    		get
    		{
    			var childWindow = GetChildWindow();

				if (childWindow == null) return -1;

    			return childWindow.Capacity;
    		}
    	}

    	public virtual double UsedCapacity
    	{
			get
			{
				var childWindow = GetChildWindow();

				if (childWindow == null) return -1;

				return childWindow.UsedCapacity;
			}
    	}
    }
}
