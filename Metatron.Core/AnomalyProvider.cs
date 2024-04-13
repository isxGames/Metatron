using System.Collections.Generic;
using EVE.ISXEVE;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    /// <summary>
    /// Encapsulate access to system anomalies.
    /// </summary>
    public class AnomalyProvider : IAnomalyProvider
    {
        private readonly IShipCache _shipCache;

        public AnomalyProvider(IShipCache shipCache)
        {
            _shipCache = shipCache;
        }

        public IList<SystemAnomaly> GetAnomalies()
        {
            var scanner = _shipCache.Ship.Scanners.System;
            return scanner.GetAnomalies();
        }
    }
}
