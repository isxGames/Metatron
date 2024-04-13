using System;

namespace Metatron.Core
{
	public interface IModule
	{
	    int PulseCounter { get; set; }
	    bool IsInitialized { get; }
	    bool IsCleanedUpOutOfFrame { get; }
	    string ModuleName { get; }

	    void Pulse();
		bool Initialize();
		bool OutOfFrameCleanup();
		void InFrameCleanup();
		void CriticalPulse();
		bool ShouldPulse();
	}
}
