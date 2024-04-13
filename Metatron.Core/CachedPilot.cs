using System;
using EVE.ISXEVE;
using ProtoBuf;

namespace Metatron.Core
{
    [ProtoContract]
    public sealed class CachedPilot
    {
        // ReSharper disable InconsistentNaming
        [ProtoMember(1)]
        public string Name;

        [ProtoMember(2)]
        public string Corp;

        [ProtoMember(3)]
        public string Alliance;

        [ProtoMember(4)]
        public Int64 CharID;

        [ProtoMember(5)]
        public Int64 CorpID;

        [ProtoMember(6)]
        public int AllianceID;

        [ProtoMember(7)]
        public CachedStanding Standing;

        public CachedPilot()
        {

        }

        public CachedPilot(Pilot pilot, string corporationName, string allianceName)
        {
            Name = pilot.Name;
            CharID = pilot.CharID;
            CorpID = pilot.Corp.ID;
            Corp = CorpID >= 0 ? corporationName : string.Empty;

            AllianceID = pilot.AllianceID;
            Alliance = AllianceID >= 0 ? allianceName : string.Empty;

            Standing = new CachedStanding();
        }
        // ReSharper restore InconsistentNaming
    }
}
