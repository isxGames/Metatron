using ProtoBuf;

namespace Metatron.Core
{
    [ProtoContract]
    public class PlayerShip
    {
        [ProtoMember(0)] public string ShipName;

        [ProtoMember(1)] private int _shipRole;

        public ShipRoles ShipRole
        {
            get { return (ShipRoles) _shipRole; }
            set { _shipRole = (int) value; }
        }

        [ProtoMember(2)] private int _tankTypes;

        public DamageTypes TankTypes
        {
            get { return (DamageTypes) _tankTypes; }
            set { _tankTypes = (int) value; }
        }
    }
}
