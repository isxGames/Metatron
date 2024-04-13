using System;
using System.Collections.Generic;
using ProtoBuf;
using Metatron.Core.Collections.Generic;

namespace Metatron.Core.Config
{
    //Notes on ProtoBuf-Net and deserailization
    //So far, it appears that with bools, a bool member initialized to 'true' will not be overwritten by a 'false' saved bool.
    //For instance, if UseRandomWaits is false when saved, and is default initialized to true, protobuf-net does not 'false' it.
    //To get around this, since C# defaults uninitialized bools to false, just don't initialize it 'true' unless making a new config.

    //Note on tags - the # used must be unique across ProtoInclude and ProtoMembers in the type and any subtypes,
    //otherwise Undefined Behavior (tm) can happen.
    [ProtoContract]
    [ProtoInclude(501, typeof(ConfigProperty<int>))]
    [ProtoInclude(502, typeof(ConfigProperty<string>))]
    [ProtoInclude(503, typeof(ConfigProperty<double>))]
    [ProtoInclude(504, typeof(ConfigProperty<Dictionary<string, bool>>))]
    [ProtoInclude(505, typeof(ConfigProperty<List<string>>))]
    [ProtoInclude(506, typeof(ConfigProperty<long>))]
    [ProtoInclude(507, typeof(ConfigProperty<Location>))]
    [ProtoInclude(508, typeof(ConfigProperty<BotModes>))]
    [ProtoInclude(509, typeof(ConfigProperty<HaulerModes>))]
    [ProtoInclude(510, typeof(ConfigProperty<List<long>>))]
    [ProtoInclude(511, typeof(ConfigProperty<BeltSubsetModes>))]
    [ProtoInclude(512, typeof(ConfigProperty<bool>))]
    [ProtoInclude(513, typeof(ConfigProperty<List<Pair<string,bool>>>))]
    public class ConfigProperty
    {
        [ProtoMember(1, IsRequired = true)] public string Name;

        public virtual object UntypedValue { get; set; }

        public static ConfigProperty<T> Create<T>(string name, T value)
        {
            return new ConfigProperty<T> {Name = name, Value = value};
        }

        //public static ConfigProperty CreateDynamic(string name, object value)
        //{
        //    var type = value.GetType();

        //    switch (Type.GetTypeCode(value.GetType()))
        //    {
        //        default:
        //            var configProperty = (ConfigProperty)Activator.CreateInstance(typeof(ConfigProperty<>).MakeGenericType(type));
        //            configProperty.UntypedValue = value;
        //            configProperty.Name = name;
        //            return configProperty;
        //    }
        //}
    }

    [ProtoContract]
    public class ConfigProperty<T> : ConfigProperty
    {
        [ProtoMember(2, IsRequired=true)]
        public T Value { get; set; }

        public override object UntypedValue
        {
            get { return Value; }
            set { Value = (T) value;  }
        }

        public ConfigProperty() { }

        public ConfigProperty(string name, T value)
        {
            Name = name;
            Value = value;
        }
    }
}