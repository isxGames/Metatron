using System;
using ProtoBuf;

namespace Metatron.Core
{
    [ProtoContract]
    public class CachedCorporation
    {
        [ProtoMember(1)]
        public long CorporationId;
        [ProtoMember(2)]
        public long MemberOfAlliance;
        [ProtoMember(3)]
        public string Name;
        [ProtoMember(4)]
        public string Ticker;
        [ProtoMember(5)]
        public DateTime LastUpdated;
    }
}
