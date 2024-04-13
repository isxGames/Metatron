using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Metatron.Core;

namespace Metatron.BehaviorModules
{
    public abstract class BehaviorBase : ModuleBase
    {
		//For delaying pulse by time period
		public DateTime TimeOfNextPulse = DateTime.Now;
        public bool CanSendCombatAssistanceRequests { get; protected set; }

        public BehaviorBase() : base()
        {

        }

        //SetState and ProcessState methods for pulse
        protected abstract void SetPulseState();
        protected abstract void ProcessPulseState();

        //SetState and ProcessState methods for cleaing up
        protected abstract void _setCleanupState();
        protected abstract void _processCleanupState();

		//Override default shouldPulse to account for timer
		public override bool ShouldPulse()
		{
			if (IsEnabled)
			{
				PulseCounter--;
				if (PulseCounter <= 0)
				{
					PulseCounter = PulseFrequency;

					//TimeOfNextPulse must also be met or passed
					if (Core.Metatron.TimeOfPulse.CompareTo(TimeOfNextPulse) >= 0)
					{
						return true;
					}
				}
			}
			return false;
		}
		public void DelayNextPulseBySeconds(int seconds)
		{
			TimeOfNextPulse = Core.Metatron.TimeOfPulse.AddSeconds(seconds);
		}
    }
}
