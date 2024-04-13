using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Metatron.Core
{
    [ProtoContract]
    public class CachedMission
    {
        /// <summary>
        /// ID of the agent this mission is for
        /// </summary>
        [ProtoMember(1)]
        public int AgentId;
        /// <summary>
        /// Name of the mission
        /// </summary>
        [ProtoMember(2)]
        public string Name;
        /// <summary>
        /// FactionID of the faction this mission will be "killing".
        /// </summary>
        [ProtoMember(3)]
        public int FactionId;
        /// <summary>
        /// TypeID of the item required by the mission, if any.
        /// </summary>
        [ProtoMember(4)]
        public int TypeId;
        /// <summary>
        /// Total volume of the items required by the mission, if any.
        /// </summary>
        [ProtoMember(5)]
        public double CargoVolume;
        /// <summary>
        /// Whether or not this mission involves low-security systems.
        /// </summary>
        [ProtoMember(6)]
        public bool IsLowSecurity;
        /// <summary>
        /// Whether or not we have a valid autopilot route to the mission.
        /// </summary>
        [ProtoMember(7)]
        public bool HaveRoute;
        /// <summary>
        /// MissionState of the mission.
        /// </summary>
        [ProtoMember(8)]
        public int State;
        /// <summary>
        /// Type of mission, i.e. Courier, Important - Encounter, etc.
        /// </summary>
        [ProtoMember(9)]
        public string Type;
        /// <summary>
        /// When this mission expires in Int64 format
        /// </summary>
        [ProtoMember(10)]
        public Int64 ExpirationTime;
        /// <summary>
        /// The subtype of this mining mission - Ore, Ice, or Gas
        /// </summary>
        [ProtoMember(11)]
        public MiningMissionTypes MiningMissionType;

        [ProtoMember(12)]
        public bool Expires;

        [ProtoMember(13)]
        private List<string> _parsedRatTypes;
        [ProtoMember(14)]
        public bool DeclineTimerExempt = false;
        public List<string> ParsedRatTypes
        {
            get { return _parsedRatTypes ?? (_parsedRatTypes = new List<string>()); }
            set { _parsedRatTypes = value; }
        }

        public CachedMission() { }

        internal CachedMission(string name, int agentId)
        {
            AgentId = agentId;
            Name = name;
        }
    }
}
