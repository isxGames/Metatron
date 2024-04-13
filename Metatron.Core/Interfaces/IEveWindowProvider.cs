using System;
using EVE.ISXEVE;
using EVE.ISXEVE.Interfaces;

namespace Metatron.Core.Interfaces
{
    public interface IEveWindowProvider
    {
        IEveInvWindow GetInventoryWindow();
        EVEWindow GetWindowByItemId(Int64 itemId);
        EVEWindow GetWindowByName(string name);
        EVEWindow GetWindowByCaption(string caption);
        EveRepairShopWindow GetRepairShopWindow();
        EVEWindow GetRepairItemsModalWindow();
        EVEWindow GetQuantityWindow();
    }
}
