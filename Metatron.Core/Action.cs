using System;
using System.Collections.Generic;

namespace Metatron.Core
{
    public class Action
    {
        public string Name = string.Empty;

        //Used for specific actions
        public string GateName = string.Empty, NearWreck = string.Empty, TargetName = string.Empty, ItemName = string.Empty;
        public int GroupId, Distance = 2250, TimeoutSeconds, TypeId;
        public bool BreakOnTargeted, BreakOnSpawn;
        public bool PreventSalvageBookmark;
        public List<String> PossibleTriggerNames = new List<string>(), DoNotKillNames = new List<string>();

        public Action(string name)
        {
            Name = name;
        }
    }
}
