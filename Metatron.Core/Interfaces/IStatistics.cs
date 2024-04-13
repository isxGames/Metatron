using System;
using System.Collections.Generic;
using Metatron.Core.CustomEventArgs;

namespace Metatron.Core.Interfaces
{
    public interface IStatistics
    {
        event EventHandler<PairEventArgs<string, int>> OnAddIceOreMined;
        event EventHandler<TimeSpanEventArgs> OnDropoff;
        event EventHandler<PairEventArgs<string, int>> OnCrystalsUsed;
        event EventHandler<WalletStatisticsUpdatedEventArgs> WalletStatisticsUpdated;
        Dictionary<string, int> MinedItemsMovedByItemName { get; }
        Dictionary<string, int> QuantityChargesUsedByName { get; }
        void TrackIceOrOreMined(string type, int quantity);
        void TrackDropoff(TimeSpan timeTaken);
        void TrackCrystalUsed(string type, int quantity);
    }
}
