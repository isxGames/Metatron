using System;
using EVE.ISXEVE;
using ProtoBuf;

namespace Metatron.Core
{
    /// <summary>
    /// Cached representation of an ISXEVE Agent object.
    /// </summary>
    [ProtoContract]
    public sealed class CachedAgent
    {
        #region members
        [ProtoMember(1)]
        public int Id;
        [ProtoMember(2)]
        public int AgentTypeId;
        [ProtoMember(3)]
        public int DivisionId;
        [ProtoMember(5)]
        public long StationId;
        [ProtoMember(6)]
        public int Level;
        [ProtoMember(7)]
        public int CorporationId;
        [ProtoMember(8)]
        public int FactionId;
        [ProtoMember(9)]
        public int SolarSystemId;
        [ProtoMember(10)]
        public string Station;
        [ProtoMember(11)]
        public string SolarSystem;
        [ProtoMember(12)]
        public string Division;
        [ProtoMember(13)]
        public string Name;
        [ProtoMember(14)]
        public DateTime NextMissionDeclinable;
        [ProtoMember(15)]
        public DateTime NextResearchMissionAvailable;
        #endregion

        /// <summary>
        /// Constructor used for deserialization. Do not use.
        /// </summary>
        public CachedAgent() { }

        public CachedAgent(Agent agent)
        {
            Id = agent.ID;
            AgentTypeId = agent.AgentTypeID;
            DivisionId = agent.DivisionID;
            StationId = agent.StationID;
            Level = agent.Level;
            CorporationId = agent.CorporationID;
            FactionId = agent.FactionID;
            Station = agent.Station;
            SolarSystem = agent.Solarsystem.Name;
            SolarSystemId = agent.Solarsystem.ID;
            Division = agent.Division;
            Name = agent.Name;
            NextMissionDeclinable = DateTime.Now;
            NextResearchMissionAvailable = DateTime.Now;
        }
    }
}
