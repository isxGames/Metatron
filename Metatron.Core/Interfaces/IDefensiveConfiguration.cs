using Metatron.Core.Config;

namespace Metatron.Core.Interfaces
{
    public interface IDefensiveConfiguration : IConfigurationBase
    {
        int MinimumShieldPct { get; set; }
        int MinimumArmorPct { get; set; }
        int MinimumCapPct { get; set; }
        int ResumeShieldPct { get; set; }
        int ResumeCapPct { get; set; }
        bool RunOnNonWhitelistedPilot { get; set; }
        bool RunOnBlacklistedPilot { get; set; }
        bool RunOnLowTank { get; set; }
        bool RunOnLowCap { get; set; }
        bool RunIfTargetJammed { get; set; }
        bool RunOnLowAmmo { get; set; }
        bool PreferStationsOverSafespots { get; set; }
        bool AlwaysShieldBoost { get; set; }
        bool AlwaysRunTank { get; set; }
        bool RunOnMeToPilot { get; set; }
        bool RunOnCorpToPilot { get; set; }
        bool RunOnMeToCorp { get; set; }
        bool RunOnCorpToCorp { get; set; }
        bool RunOnCorpToAlliance { get; set; }
        bool RunOnAllianceToAlliance { get; set; }
        bool RunOnLowDrones { get; set; }
        int MinimumNumDrones { get; set; }
        bool DisableStandingsChecks { get; set; }
        bool WaitAfterFleeing { get; set; }
        int MinutesToWait { get; set; }
    }
}