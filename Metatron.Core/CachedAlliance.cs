using ProtoBuf;
using System;

namespace Metatron.Core
{
    [ProtoContract]
    public class CachedAlliance
    {
        [ProtoMember(1)]
        public long AllianceId;
        [ProtoMember(2)]
        public string Name;
        [ProtoMember(3)]
        public string Ticker;
        public DateTime LastUpdated;
    }
}
