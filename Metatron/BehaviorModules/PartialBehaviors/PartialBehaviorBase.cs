using System;
using Metatron.Core;

namespace Metatron.BehaviorModules.PartialBehaviors
{
    public interface IPartialBehaviorBase
    {
        BehaviorExecutionResults Execute();
        void Reset();
    }

    public abstract class PartialBehaviorBase : ModuleBase, IPartialBehaviorBase
    {
		public abstract BehaviorExecutionResults Execute();
		
		public virtual void Reset()
		{
			throw new NotImplementedException();
		}
	}

	public enum BehaviorExecutionResults
	{
		Incomplete,
		Complete,
		Error
	}
}
