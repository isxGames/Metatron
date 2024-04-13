using System.Collections.Generic;

namespace Metatron.BehaviorModules
{
    public interface IBehaviorManager
    {
        Dictionary<BotModes, BehaviorBase> Behaviors { get; }
    }
}