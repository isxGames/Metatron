using EVE.ISXEVE;
using ProtoBuf;

namespace Metatron.Core
{
    [ProtoContract]
    public sealed class CachedStanding
    {
        [ProtoMember(1)]
        public int AllianceToAlliance;

        [ProtoMember(2)]
        public int CorpToAlliance;

        [ProtoMember(3)]
        public int CorpToCorp;

        [ProtoMember(4)]
        public int CorpToPilot;

        [ProtoMember(5)]
        public int MeToCorp;

        [ProtoMember(6)]
        public int MeToPilot;

        public CachedStanding() { }

        public CachedStanding(Standing standing)
        {
            AllianceToAlliance = standing.AllianceToAlliance;
            CorpToAlliance = standing.CorpToAlliance;
            CorpToCorp = standing.CorpToCorp;
            CorpToPilot = standing.CorpToPilot;
            MeToCorp = standing.MeToCorp;
            MeToPilot = standing.MeToPilot;
        }

        public static bool Equals(CachedStanding first, CachedStanding second)
        {
            return (first == null && second == null) ||
                   first.AllianceToAlliance == second.AllianceToAlliance &&
                   first.CorpToAlliance == second.CorpToAlliance &&
                   first.CorpToCorp == second.CorpToCorp &&
                   first.CorpToPilot == second.CorpToPilot &&
                   first.MeToCorp == second.MeToCorp &&
                   first.MeToPilot == second.MeToPilot;
        }
    }
}
