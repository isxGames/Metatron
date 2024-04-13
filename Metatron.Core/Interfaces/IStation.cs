using System.Collections.ObjectModel;

namespace Metatron.Core.Interfaces
{
    public interface IStation
    {
        ReadOnlyCollection<int> StationTypeIDs { get; }
        bool IsStationHangarActive { get; }
        bool IsStationCorpHangarActive { get; }
        bool IsDockedAtStation(long stationId);

        /// <summary>
        /// Undock from the current station.
        /// </summary>
        void Undock();

        void MakeStationHangarActive();
        void MakeStationCorpHangarActive();
        void InitializeStationCorpHangars();
    }
}
