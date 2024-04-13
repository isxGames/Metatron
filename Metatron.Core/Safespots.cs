using System;
using System.Collections.Generic;
using System.Linq;
using EVE.ISXEVE.Interfaces;
using Metatron.Core.Config;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    /// <summary>
    /// Provide utility for determining if we're at a safespot and getting the best safespot destination.
    /// </summary>
    public class Safespots : ISafespots
    {
        private readonly IMeCache _meCache;
        private readonly IBookMarkCache _bookMarkCache;
        private readonly IMovementConfiguration _movementConfiguration;
        private readonly IMeToEntityCache _meToEntityCache;
        private readonly IEntityProvider _entityProvider;
        private readonly IIsxeveProvider _isxeveProvider;
        private readonly IShip _ship;
        private readonly ISocial _social;
        private readonly ILogging _logging;
        private readonly MathUtility _mathUtility;
        private static readonly string ClassName = "Safespots";

        public Safespots(IMeCache meCache, IBookMarkCache bookMarkCache, IMovementConfiguration movementConfiguration, IMeToEntityCache meToEntityCache, IEntityProvider entityProvider, IIsxeveProvider isxeveProvider,
            IShip ship, ISocial social, MathUtility mathUtility, ILogging logging)
        {
            _meCache = meCache;
            _bookMarkCache = bookMarkCache;
            _movementConfiguration = movementConfiguration;
            _meToEntityCache = meToEntityCache;
            _entityProvider = entityProvider;
            _isxeveProvider = isxeveProvider;
            _ship = ship;
            _social = social;
            _mathUtility = mathUtility;
            _logging = logging;
        }

        /// <summary>
        /// Determine if we are currently safe from harm
        /// </summary>
        /// <returns></returns>
        public bool IsSafe()
        {
            var methodName = "IsSafe";

            //If we're in a station, we are safe.
            if (_meCache.InStation) return true;

            //If we're in a tower bubble, we're safe.
            var controlTowers = _entityProvider.EntityWrappers.Where(e => e.GroupID == (int)GroupIDs.ControlTower); // Max tower shield range
            foreach (var controlTower in controlTowers)
            {
                var itemInfo = _isxeveProvider.Eve.ItemInfo(controlTower.TypeID);

                _logging.LogMessage(ClassName, methodName, LogSeverityTypes.Debug, "ShieldRadius: {0}, Tower distance: {1}",
                    itemInfo.ShieldRadius, controlTower.Distance);

                if (itemInfo.ShieldRadius > 0 && controlTower.Distance <= itemInfo.ShieldRadius)
                {
                    _logging.LogMessage(ClassName, methodName, LogSeverityTypes.Debug, "We are within shield radius {0} to control tower {1} ({2}).",
                        itemInfo.ShieldRadius, controlTower.Name, controlTower.ID);
                    return true;
                }
            }

            //Alright, we're in space. Are we within warp range of a safe bookmark?
            var safeBookMarksInSystem = GetSafeBookMarksInSystem();

            //If I have safe bookmarks, I should never ever be able to consider a planet safe. I should be moving at a safe bookmark.
            //So - if I have bookmarks { if I'm at one, I'm safe, otherwise I'm not safe } 
            var isAtSafeBookMark = safeBookMarksInSystem.Any(bm => _mathUtility.Distance(bm.X, bm.Y, bm.Z, _meToEntityCache.X, _meToEntityCache.Y, _meToEntityCache.Z) < (int)Ranges.Warp);

            var localIsSafeOrICanCloak = _social.IsLocalSafe || (_ship.CloakingDeviceModules.Any() && !_entityProvider.EntityWrappers.Any(e => e.IsTargetingMe));
            if (isAtSafeBookMark && localIsSafeOrICanCloak)
            {
                _logging.LogMessage(ClassName, methodName, LogSeverityTypes.Debug, "We are within warp range of a safe bookmark and local is safe or we can cloak.");
                return true;
            }

            //Only consider planets safe spots if there are no safe bookmarks or stations
            var stationEntities = _entityProvider.EntityWrappers.Where(e => e.GroupID == (int)GroupIDs.Station
                                                                        ||  e.GroupID == (int)GroupIDs.Refinery
                                                                        ||  e.GroupID == (int)GroupIDs.EngineeringComplex
                                                                        ||  e.GroupID == (int)GroupIDs.Citadel);
            if (!safeBookMarksInSystem.Any() && !stationEntities.Any())
            {
                //If there were no safe bookmarks in this system, we can consider ourselves at a safe-spot at a planet warp-in
                var planetWithinPlanetWarpRange = _entityProvider.EntityWrappers
                    .FirstOrDefault(e => e.GroupID == (int)GroupIDs.Planet && e.Distance <= (long)Ranges.PlanetWarpIn);

                if (planetWithinPlanetWarpRange != null && localIsSafeOrICanCloak)
                {
                    _logging.LogMessage(ClassName, methodName, LogSeverityTypes.Debug, "We are within planet warpin range of planet \"{0}\" ({1}, {2}) and local is safe or we can cloak.",
                        planetWithinPlanetWarpRange.Name, planetWithinPlanetWarpRange.ID, planetWithinPlanetWarpRange.Distance);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get the best safe spot to move to, excluding any spot we're currently at.
        /// </summary>
        /// <returns></returns>
        public Destination GetSafeSpot()
        {
            var methodName = "GetSafeSpot";
            var safeBookMarksInSystem = GetSafeBookMarksInSystem();

            // Filter out any bookmarks we're already at, preferring space bookmarks (itemID < 0)
            safeBookMarksInSystem = safeBookMarksInSystem.Where(bm => _mathUtility.Distance(bm.X, bm.Y, bm.Z, _meToEntityCache.X, _meToEntityCache.Y, _meToEntityCache.Z) > (int)Ranges.Warp)
                .OrderBy(bm => bm.ItemID);

            var safeBookMark = safeBookMarksInSystem.FirstOrDefault();
            if (safeBookMark != null)
            {
                //Return a new destination for this safe bookmark
                Destination safeBookMarkDestination;
                //If it's a space bookmark, vs station...
                if (safeBookMark.ItemID < 0)
                {
                    safeBookMarkDestination = new Destination(DestinationTypes.BookMark, safeBookMark.ID);
                    _logging.LogMessage(ClassName, methodName, LogSeverityTypes.Debug, "Got safe bookmark labeled \"{0}\".", safeBookMark.Label);
                }
                else
                {
                    safeBookMarkDestination = new Destination(DestinationTypes.Entity, safeBookMark.ItemID)
                        {
                            Distance = (int)Ranges.Dock,
                            Dock = true
                        };
                    _logging.LogMessage(ClassName, methodName, LogSeverityTypes.Debug, "Got safe station bookmark labeled \"{0}\".", safeBookMark.Label);
                }
                return safeBookMarkDestination;
            }
            
            //See if we can find our home station
            var stationEntities = _entityProvider.EntityWrappers.Where(e => e.GroupID == (int) GroupIDs.Station ||
                                                                            e.GroupID == (int) GroupIDs.Refinery ||
                                                                            e.GroupID == (int) GroupIDs.EngineeringComplex ||
                                                                            e.GroupID == (int) GroupIDs.Citadel);

            if (stationEntities.Any())
            {
                var stationEntity = stationEntities.FirstOrDefault(e => e.Name.Equals(_movementConfiguration.HomeStation, StringComparison.InvariantCultureIgnoreCase));

                //If we couldn't find our home station, just grab ANY station
                if (stationEntity == null)
                    stationEntity = stationEntities.First();

                var stationDestination = new Destination(DestinationTypes.Entity, stationEntity.ID)
                    {
                        Distance = (int) Ranges.Dock,
                        Dock = true
                    };
                _logging.LogMessage(ClassName, methodName, LogSeverityTypes.Debug, "Got safe station named \"{0}\".", stationEntity.Name);
                return stationDestination;
            }

            //Sweet. Couldn't use a station. Grab a planet.
            var planetEntities = _entityProvider.EntityWrappers.Where(e => e.GroupID == (int) GroupIDs.Planet && e.Distance > (long)Ranges.PlanetWarpIn);
            var planetEntity = planetEntities.FirstOrDefault();
            if (planetEntity != null)
            {
                var planetDestination = new Destination(DestinationTypes.Entity, planetEntity.ID)
                    {
                        Distance = (long) Ranges.PlanetWarpIn
                    };
                _logging.LogMessage(ClassName, methodName, LogSeverityTypes.Debug, "Got safe planet named \"{0}\".", planetEntity.Name);
                return planetDestination;
            }

            //No safe spots available :(
            return null;
        }

        /// <summary>
        /// Get any safe bookmarks in our system.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IBookMark> GetSafeBookMarksInSystem()
        {
            var safeBookMarksInSystem = _bookMarkCache.BookMarks
                .Where(bm => bm.SolarSystemID == _meCache.SolarSystemId && bm.Label.StartsWith(_movementConfiguration.SafeBookmarkPrefix, StringComparison.InvariantCultureIgnoreCase));

            return safeBookMarksInSystem;
        }
    }
}
