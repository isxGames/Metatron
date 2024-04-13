namespace Metatron.Core.Interfaces
{
    public interface ISafespots
    {
        /// <summary>
        /// Determine if we are currently safe from harm
        /// </summary>
        /// <returns></returns>
        bool IsSafe();

        /// <summary>
        /// Get the best safe spot to move to, excluding any spot we're currently at.
        /// </summary>
        /// <returns></returns>
        Destination GetSafeSpot();
    }
}