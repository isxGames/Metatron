using System.Collections.Generic;
using System.Linq;

namespace Metatron.Core
{
    public class Mission
    {
        public string Name { get; private set; }
        public string NameMatchType = "Equals";
        public bool IsEmpireKill, IsPirateKill;
        public List<string> RatTypes = new List<string>();

        public List<ActionSet> ActionSets = new List<ActionSet>();

        public Mission(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Get an actionset from the mission matching the level, if it exists.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public ActionSet GetActionSetByLevel(int level)
        {
            return ActionSets.FirstOrDefault(actionSet => actionSet.Level == level);
        }
    }
}
