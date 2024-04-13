using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EVE.ISXEVE;
using Metatron.ActionModules;
using Metatron.Core.Extensions;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    public class ShipHub : ModuleBase
    {

        public ShipHub()
        {
            IsEnabled = false;
            ModuleManager.Modules.Add(this);
        }

        public bool IsAtShipHub
        {
            get
            {
                var methodName = "IsAtShipHub";
                LogTrace(methodName);

                if (string.IsNullOrEmpty(Metatron.Config.ShipHubConfig.HubLocation.BookmarkLabel))
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Error: ShipHub BookmarkLabel is not set.");
                    return false;
                }

                var hubBookmark = GetShipHubBookmark();

                if (hubBookmark == null)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Error: Could not find bookmark for ShipHub BookmarkLabel \"{0}\".",
                        Metatron.Config.ShipHubConfig.HubLocation.BookmarkLabel);
                    return false;
                }

                return Metatron.Bookmarks.IsAtBookmark(hubBookmark);
            }
        }

        public void MoveToShipHub()
        {
            var methodName = "MoveToShipHub";
            LogTrace(methodName);

            var shipHubBookmark = GetShipHubBookmark();

            if (shipHubBookmark == null)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Error: Could not find bookmark for ShipHub BookmarkLabel \"{0}\".",
                    Metatron.Config.ShipHubConfig.HubLocation.BookmarkLabel);
                return;
            }

            var destination = new Destination(DestinationTypes.BookMark, shipHubBookmark.Id);
            Metatron.Movement.QueueDestination(destination);
        }

        public void ChangeShip(string shipName)
        {
            var methodName = "ChangeShip";
            LogTrace(methodName);

            var matchingShip = Metatron.MeCache.HangarShips.FirstOrDefault(ship => ship.GivenName.Contains(shipName, StringComparison.InvariantCultureIgnoreCase));

            if (matchingShip == null)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Error: Could not find a ship with a given name of \"{0}\".", shipName);
                return;
            }

            LogMessage(methodName, LogSeverityTypes.Standard, "Making ship \"{0}\" active.", matchingShip.GivenName);
            matchingShip.MakeActive();

            Metatron.ModuleManager.DelayPulseByTicks(5);
        }

        private CachedBookMark GetShipHubBookmark()
        {
            var methodName = "GetShipHubBookmark";
            LogTrace(methodName);

            return Metatron.BookMarkCache.FirstBookMarkMatching(Metatron.Config.ShipHubConfig.HubLocation.BookmarkLabel, false);
        }
    }
}
