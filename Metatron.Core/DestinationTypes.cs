namespace Metatron.Core
{
    /* Possible destinations:
     * SolarSystem: For inter-system travel.
     * Celstial:    Warping within system to stargates, asteroid belts, or stations.
     * FleetMember: For warping within system to a fleet member.
     * BookMark:    For warping within system to a bookmark.
     * Entity:      Non-warp, approach-based travel, i.e. approaching cans, rats, etc.
     * Undock:      Just undock.
     * CosmicAnomaly: In-system-movement only; otherwise similar to a bookmark.
     */

    public enum DestinationTypes
    {
        SolarSystem,
        Celestial,
        FleetMember,
        BookMark,
        Entity,
        Undock,
        MissionBookmark,
        CosmicAnomaly
    }
}