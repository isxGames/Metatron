using System;
using System.Security.Principal;
using EVE.ISXEVE;
using EVE.ISXEVE.Interfaces;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    public class EveWindowProvider : IEveWindowProvider
    {
        public IEveInvWindow GetInventoryWindow()
        {
            return EVEWindow.GetInventoryWindow();
        }

        public EveRepairShopWindow GetRepairShopWindow()
        {
            return EVEWindow.GetRepairShopWindow();
        }

        public EVEWindow GetWindowByItemId(Int64 itemId)
        {
            return EVEWindow.GetWindowByItemId(itemId);
        }
        
        public EVEWindow GetWindowByName(string name)
        {
            return EVEWindow.GetWindowByName(name);
        }

        public EVEWindow GetWindowByCaption(string caption)
        {
            return EVEWindow.GetWindowByCaption(caption);
        }

        public EVEWindow GetRepairItemsModalWindow()
        {
            var window = EVEWindow.GetWindowByName("modal");
            if (window == null) return null;
            else if (!string.IsNullOrEmpty(window.Text) && window.Text.Contains("Repairing these items"))
            {
                return window;
            }
            else return null;
        }

        public EVEWindow GetQuantityWindow()
        {
            return EVEWindow.GetWindowByName("Set Quantity");
        }
    }
}
