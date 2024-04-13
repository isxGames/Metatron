using System;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Metatron.Core.Collections.Generic
{
    [ProtoContract]
    public class Pair<K, V> : IEquatable<Pair<K,V>>, IEquatable<object>
    {
        public Pair()
        {
            
        }

        public Pair(K first, V second)
        {
            First = first;
            Second = second;
        }

        [ProtoMember(1, IsRequired = true)]
        public K First { get; set; }
        [ProtoMember(2, IsRequired=true)]
        public V Second { get; set; }

        public bool Equals(Pair<K, V> other)
        {
            if (other == null) return false;

            if (ReferenceEquals(this, other)) return true;

            return First.Equals(other.First) && Second.Equals(other.Second);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            var pair = obj as Pair<K, V>;

            if (pair == null) return false;

            return Equals(pair);
        }

        public override int GetHashCode()
        {
            return First.GetHashCode() ^ Second.GetHashCode();
        }
    }
}
