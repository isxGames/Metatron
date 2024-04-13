using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Metatron.Core.CachedDogma
{
    [ProtoContract]
    public class Attribute
    {
        [ProtoMember(1)]
        public int AttributeId { get; set; }
        [ProtoMember(2)]
        public float DefaultValue { get; set; }
        [ProtoMember(3)]
        public string Description { get; set; }
        [ProtoMember(4)]
        public string Name { get; set; }
        [ProtoMember(5)]
        public bool HighIsGood { get; set; }
        [ProtoMember(6)]
        public int IconId { get; set; }
        [ProtoMember(7)]
        public string DisplayName { get; set; }
        [ProtoMember(8)]
        public bool Stackable { get; set; }
        [ProtoMember(9)]
        public int UnitId { get; set; }
        [ProtoMember(10)]
        public DateTime LastUpdated { get; set; }
    }

    [ProtoContract]
    public class Effect
    {
        [ProtoMember(1)]
        public string Description { get; set; }
        [ProtoMember(2)]
        public bool DisallowAutoRepeat { get; set; }
        [ProtoMember(3)]
        public int DischargeAttributeId { get; set; }
        [ProtoMember(4)]
        public string DisplayName { get; set; }
        [ProtoMember(5)]
        public int DurationAttributeId { get; set; }
        [ProtoMember(6)]
        public int EffectCategory { get; set; }
        [ProtoMember(7)]
        public int EffectId { get; set; }
        [ProtoMember(8)]
        public bool ElectronicChange { get; set; }
        [ProtoMember(9)]
        public int FalloffAttributeId { get; set; }
        [ProtoMember(10)]
        public int IconId { get; set; }
        [ProtoMember(11)]
        public bool IsAssistance { get; set; }
        [ProtoMember(12)]
        public bool IsOffensive { get; set; }
        [ProtoMember(13)]
        public bool IsWarpSafe { get; set; }
        [ProtoMember(14)]
        public string Name { get; set; }
        [ProtoMember(15)]
        public int PostExpression { get; set; }
        [ProtoMember(16)]
        public int PreExpression { get; set; }
        [ProtoMember(17)]
        public int RangeAttributeId { get; set; }
        [ProtoMember(18)]
        public bool RangeChance { get; set; }
        [ProtoMember(19)]
        public int TrackingSpeedAttributeId { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
