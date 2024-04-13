using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EVE.ISXEVE;
using EVE.ISXEVE.Interfaces;

namespace Metatron.Core.Interfaces
{
    public interface IMeCache
    {
        Me Me { get; }
        string Name { get; }
        string CorporationTicker { get; }
        string Corporation { get; }
        string AllianceTicker { get; }
        string Alliance { get; }
        long StationId { get; }
        int SolarSystemId { get; }
        int AllianceId { get; }
        Int64 CharId { get; }
        Int64 ShipId { get; }
        DateTime ActiveTargetActivatedAt { get; }
        Int64 ActiveTargetId { get; }
        Int64 CorporationId { get; }
        double DroneControlDistance { get; }
        double MaxLockedTargets { get; }
        double MaxActiveDrones { get; }
        double WalletBalance { get; }
        bool InSpace { get; }
        bool InStation { get; }
        bool AutoPilotEnabled { get; }
        bool IsFleetInvited { get; }
        IEntityWrapper ActiveTarget { get; }
        EVETime EveTime { get; }
        ReadOnlyCollection<IEntityWrapper> TargetedBy { get; }
        bool IsValid { get; }
        bool IsInventoryOpen { get; }
        ReadOnlyCollection<FleetMember> FleetMembers { get; }
        Dictionary<Int64, FleetMember> FleetMembersById { get; }
        ReadOnlyCollection<Attacker> Attackers { get; }
        Dictionary<Int64, Attacker> AttackersById { get; }
        ReadOnlyCollection<AgentMission> AgentMissions { get; }
        ReadOnlyCollection<Being> Buddies { get; }
        ReadOnlyCollection<ActiveDrone> ActiveDrones { get; }
        ReadOnlyCollection<IItem> HangarItems { get; }
        ReadOnlyCollection<IItem> HangarShips { get; }
        int GameHour { get; }
        int GameMinute { get; }
        bool IsDowntimeNear { get; }
        bool IsDowntimeImminent { get; }
        IMeToEntityCache ToEntity { get; }
        IShipCache Ship { get; }
        ReadOnlyCollection<IEntityWrapper> Targets { get; }
        bool CheckValidity();
        IEveInvWindow GetInventoryWindow();
        bool IsAtBookMark(CachedBookMark bookMark);
    }
}
