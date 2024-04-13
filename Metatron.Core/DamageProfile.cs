using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Metatron.Core.Extensions;

namespace Metatron.Core
{
    /// <summary>
    /// Percentages should be treated as % of damage when used for a charge damage profile and % resist when used for a rat resist profile
    /// </summary>
    public class DamageProfile
    {
        public double PercentKinetic, PercentThermal, PercentExplosive, PercentEM;

        public static DamageProfile Default = new DamageProfile(25, 25, 25, 25);
        public static DamageProfile SerpentisResistances = new DamageProfile(49,60,79,69);
        public static DamageProfile AngelResistances = new DamageProfile(50, 60, 40, 70);
        public static DamageProfile BloodResistances = new DamageProfile(66, 57, 76, 46);
        public static DamageProfile GuristasResistances = new DamageProfile(47, 58, 67, 77);
        public static DamageProfile DroneResistances = new DamageProfile(67, 58, 77, 47);
        public static DamageProfile SanshaResistances = new DamageProfile(70, 60, 80, 50);

        public DamageProfile(double percentKinetic, double percentThermal, double percentExplosive, double percentEM)
        {
            PercentKinetic = percentKinetic;
            PercentThermal = percentThermal;
            PercentExplosive = percentExplosive;
            PercentEM = percentEM;
        }

        public double GetPercentMatchAgainstResistances(DamageProfile resistances)
        {
            var kineticMatch = PercentKinetic*(1-resistances.PercentKinetic / 100);
            var thermalMatch = PercentThermal*(1-resistances.PercentThermal / 100);
            var explosiveMatch = PercentExplosive*(1-resistances.PercentExplosive / 100);
            var emMatch = PercentEM*(1-resistances.PercentEM / 100);

            return kineticMatch + thermalMatch + explosiveMatch + emMatch;
        }

        public static DamageProfile GetNpcDamageProfile(string npcFaction)
        {
            if (npcFaction.Contains("Guristas", StringComparison.InvariantCultureIgnoreCase) ||
                npcFaction.Contains("Caldari", StringComparison.InvariantCultureIgnoreCase) ||
                npcFaction.Contains("Mordus", StringComparison.InvariantCultureIgnoreCase))
                return GuristasResistances;
            if (npcFaction.Contains("Serpentis", StringComparison.InvariantCultureIgnoreCase) ||
                npcFaction.Contains("Gallente", StringComparison.InvariantCultureIgnoreCase))
                return SerpentisResistances;
            if (npcFaction.Contains("Angel", StringComparison.InvariantCultureIgnoreCase) ||
                npcFaction.Contains("Minmatar", StringComparison.InvariantCultureIgnoreCase))
                return AngelResistances;
            if (npcFaction.Contains("Sansha", StringComparison.InvariantCultureIgnoreCase) ||
                npcFaction.Contains("Amarr", StringComparison.InvariantCultureIgnoreCase))
                return SanshaResistances;
            if (npcFaction.Contains("Blood", StringComparison.InvariantCultureIgnoreCase) ||
                npcFaction.Contains("Khanid", StringComparison.InvariantCultureIgnoreCase))
                return BloodResistances;
            if (npcFaction.Contains("Drones", StringComparison.InvariantCultureIgnoreCase))
                return DroneResistances;
            if (npcFaction.Contains("Mercenary", StringComparison.InvariantCultureIgnoreCase) ||
                npcFaction.Contains("Concord", StringComparison.InvariantCultureIgnoreCase))
                return Default;
            return Default;
        }
    }
}
