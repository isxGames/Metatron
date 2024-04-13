using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Metatron.Core
{
    [ProtoContract]
    public class Location
    {
        [ProtoMember(1)]
        public string BookmarkLabel;

        [ProtoMember(2)]
        public LocationTypes LocationType;

        [ProtoMember(3)]
        public Int64 EntityID;

        [ProtoMember(4)]
        public int HangarDivision;

        public static Location CreateDefault()
        {
            var location = new Location
                               {
                                   BookmarkLabel = string.Empty,
                                   EntityID = -1,
                                   HangarDivision = 1,
                                   LocationType = LocationTypes.Station
                               };

            return location;
        }
    }
}
