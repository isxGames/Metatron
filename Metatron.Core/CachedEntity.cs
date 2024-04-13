using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Metatron.Core
{
    [ProtoContract]
    public class CachedEntity
    {
        [ProtoMember(1)]
        public long TypeID { get; set; }
        [ProtoMember(2)]
        public long GroupID { get; set; }
        [ProtoMember(3)]
        public string Name { get; set; }
        [ProtoMember(4)]
        public string Description { get; set; }
        [ProtoMember(5)]
        public float? Mass { get; set; }
        [ProtoMember(6)]
        public float? Radius { get; set; }
        [ProtoMember(7)]
        public List<DogmaAttribute> DogmaAttributes { get; set; }
        public DateTime LastUpdated { get; set; }

        public class DogmaAttribute
        {
            [ProtoMember(1)]
            public int Id { get; set; }
            [ProtoMember(2)]
            public float Value { get; set; }
        }
    }
}
