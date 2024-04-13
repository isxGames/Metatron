using System;

namespace Metatron.Core
{
    public class Destination
    {
        public DestinationTypes Type;
        public int SolarSystemId, WarpToDistance, MissionAgentId;
        public Int64 EntityId, FleetMemberId, FleetMemberEntityId, BookMarkId;
        public double Distance, MinimumDistance = 0;
        public string FleetMemberName, BookMarkTypeTag;
        public bool UseGate, Dock;
        public ApproachTypes ApproachType = ApproachTypes.Approach;
        
        public bool IsObstacleAvoidanceMovement { get; set; }

        /// <summary>
        /// ID of the entity this obstacle avoidance movement is for, if any.
        /// </summary>
        public Int64 GoalEntityId { get; set; }

        public Destination(DestinationTypes destinationType, params object[] args)
        {
            Type = destinationType;

            GoalEntityId = Int64.MinValue;

            switch (destinationType)
            {
                case DestinationTypes.BookMark:
                    BookMarkId = (Int64)args[0];
                    if (args.Length > 1)
                    {
                        WarpToDistance = (int)args[1];
                    }
                    else
                    {
                        WarpToDistance = 0;
                    }
                    break;
                case DestinationTypes.FleetMember:
                    break;
                case DestinationTypes.SolarSystem:
                    SolarSystemId = (int)args[0];
                    break;
                case DestinationTypes.Entity:
                    EntityId = (Int64)args[0];
                    if (args.Length > 1)
                    {
                        Distance = Convert.ToDouble(args[1]);
                        WarpToDistance = Convert.ToInt32(args[1]);
                    }
                    if (args.Length > 2)
                    {
                        UseGate = (bool)args[2];
                    }
                    break;
            }
        }

        public bool HasGoalEntity { get { return GoalEntityId != Int64.MinValue; }}

        public Int64 SystemAnomalyId { get; set; }

        public static bool operator ==(Destination a, Destination b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if ((object)a == null || (object)b == null)
            {
                return false;
            }

            if (a.BookMarkId == b.BookMarkId &&
                a.Distance == b.Distance &&
                a.Dock == b.Dock &&
                a.EntityId == b.EntityId &&
                a.FleetMemberEntityId == b.FleetMemberEntityId &&
                a.FleetMemberId == b.FleetMemberId &&
                a.FleetMemberName == b.FleetMemberName &&
                a.SolarSystemId == b.SolarSystemId &&
                a.Type == b.Type &&
                a.UseGate == b.UseGate &&
                a.WarpToDistance == b.WarpToDistance)
            {
                return true;
            }
            return false;
        }

        public static bool operator !=(Destination a, Destination b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj is Destination)
            {
                if (this == (Destination)obj)
                {
                    return true;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode() ^ SolarSystemId.GetHashCode() ^ EntityId.GetHashCode() ^
                   WarpToDistance.GetHashCode() ^ FleetMemberId.GetHashCode() ^ FleetMemberEntityId.GetHashCode() ^
                   MissionAgentId.GetHashCode() ^ BookMarkId.GetHashCode() ^ Distance.GetHashCode() ^
                   FleetMemberName.GetHashCode() ^ BookMarkTypeTag.GetHashCode() ^
                   UseGate.GetHashCode() ^ Dock.GetHashCode();
        }
    }
}