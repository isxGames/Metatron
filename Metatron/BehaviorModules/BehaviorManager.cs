using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Metatron.Core;

namespace Metatron.BehaviorModules
{
    /// <summary>
    /// This class will pulse any Behavior classes necessary and handle any automatic mode switching / 
    /// cleanup as necessary.
    /// </summary>
    public sealed class BehaviorManager : ModuleBase
    {
        public static Dictionary<BotModes, BehaviorBase> BehaviorsToPulse = new Dictionary<BotModes,BehaviorBase>();

        public BehaviorManager()
        {
            //Set the object name
            ModuleName = "BehaviorManager";
            //Make sure the object is enabled
            IsEnabled = true;
            //Pulse frequency of 1
            PulseFrequency = 1;
            //Add this module to the pulse list
            ModuleManager.ModulesToPulse.Add(this);
        }

        public override void Pulse()
        {
            var methodName = "Pulse";
			LogTrace(methodName);

            if (BehaviorsToPulse.ContainsKey(Core.Metatron.Config.MainConfig.ActiveBehavior))
            {
                TryPulseModule(BehaviorsToPulse[Core.Metatron.Config.MainConfig.ActiveBehavior]);
            }
        }

        private void TryPulseModule(BehaviorBase module)
        {
            var methodName = "TryPulseModule";
			LogTrace(methodName, "Module: {0}", module.ModuleName);

            //Try/Catch block for any unhandled exceptions in the Module
            try
            {
                module.Pulse();
            }
            catch (Exception e)
            {
				LogException(e, methodName, "Caught exception while pulsing {0}:", module.ModuleName);
            }
        }
    }
}
