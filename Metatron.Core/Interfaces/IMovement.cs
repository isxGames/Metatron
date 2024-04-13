using System;
using System.Collections.ObjectModel;
using Metatron.Core.CustomEventArgs;

namespace Metatron.Core.Interfaces
{
    public interface IMovement
    {
        void QueueDestination(Destination destination);
        bool IsMoving { get; }
        bool IsCriticalMoving { get; }
        MovementTypes MovementType { get; }
        ReadOnlyCollection<Destination> DestinationQueue { get; }
        void ClearDestinations(bool deactivatePropulsionMods);
        bool IsEntityCollidable(IEntityWrapper entity);
        int GetEveWarpToDistance(int warpToDistance);
        void StopCurrentMovement(bool dequeueDestination);
        void RemoveDestination(Destination destination);
        event EventHandler<SessionChangedEventArgs> PreSessionChanged;
        event EventHandler<SessionChangedEventArgs> SessionChanged;

        /// <summary>
        /// Indicates whether or not nearby collidable avoidance is enabled.
        /// </summary>
        bool IsNearbyCollidableAvoidanceEnabled { get; }

        /// <summary>
        /// Disable nearby collidable avoidance.
        /// </summary>
        void DisableNearbyCollidableAvoidance();

        /// <summary>
        /// Enable nearby collidable avoidance.
        /// </summary>
        void EnableNearbyCollidableAvoidance();
    }
}