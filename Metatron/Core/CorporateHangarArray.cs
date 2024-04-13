using System;
using EVE.ISXEVE;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
	public sealed class CorporateHangarArray : CargoContainer, ICorporateHangarArray
	{
		private readonly IEntityWrapper _corporateHangarArrayEntity;

		public CorporateHangarArray(IEntityWrapper corporateHangarArrayEntity, IEveWindowProvider eveWindowProvider)
            : base(eveWindowProvider)
		{
			ModuleName = "CorporateHangarArray";

			if (corporateHangarArrayEntity == null)
				throw new ArgumentNullException("corporateHangarArrayEntity");

			_corporateHangarArrayEntity = corporateHangarArrayEntity;
		}

		public override IEntityWrapper CurrentContainer
		{
			get { return _corporateHangarArrayEntity; }
		}

		public bool IsActiveContainerFull
		{
			get
			{
				return (UsedCapacity >= Capacity * 0.95);
			}
		}

		public bool IsActiveContainerHalfFull
		{
			get
			{
				LogMessage("IsActiveContainerHalfFull", LogSeverityTypes.Debug, "Used: {0}, Half Capacity: {1}, IsOrca: {2}",
				           UsedCapacity, Capacity * 0.5, CurrentContainer.TypeID == (int)TypeIDs.Orca);
				return (UsedCapacity >= Capacity * 0.50);
			}
		}

		public override bool IsCurrentContainerWindowActive
		{
			get
			{
				if (!base.IsCurrentContainerWindowActive)
					return false;

                var inventoryWindow = _eveWindowProvider.GetInventoryWindow();
				var activeChild = inventoryWindow.ActiveChild;

				if (LavishScriptObject.IsNullOrInvalid(activeChild))
				{
					LogMessage("IsCurrentContainerWindowActive", LogSeverityTypes.Debug, "Error: EVEInvWindow.ActiveChild is null or invalid.");
					return false;
				}

				if (CurrentContainer.CategoryID == (int) CategoryIDs.Ship)
					return activeChild.Name == "ShipFleetHangar";

				return activeChild.Name == "POSCorpHangar";
			}
		}

		public override void MakeCurrentContainerWindowActive()
		{
			var folderText = string.Format("Folder{0}", Metatron.Config.CargoConfig.DropoffLocation.HangarDivision);

            var inventoryWindow = _eveWindowProvider.GetInventoryWindow();
			var childWindow = inventoryWindow.GetChildWindow(CurrentContainer.ID, "POSCorpHangar", folderText);

			if (LavishScriptObject.IsNullOrInvalid(childWindow))
			{
				LogMessage("MakeCurrentContainerWindowActive", LogSeverityTypes.Debug, "Error: No child window found for ID {0}, Name {1}, Location {2}.",
				           CurrentContainer.ID, "POSCorpHangar", folderText);
				return;
			}

			childWindow.MakeActive();
		}

		public void InitializeCorporateHangars()
		{
            var inventoryWindow = _eveWindowProvider.GetInventoryWindow();
			var childWindow = inventoryWindow.GetChildWindow(CurrentContainer.ID, "POSCorpHangars");

			if (LavishScriptObject.IsNullOrInvalid(childWindow))
			{
				LogMessage("InitializeCorporateHangars", LogSeverityTypes.Debug, "Error: No child window found for ID {0}, Name {1}.", CurrentContainer.ID, "POSCorpHangars");
				return;
			}

			childWindow.MakeActive();
		}
	}
}