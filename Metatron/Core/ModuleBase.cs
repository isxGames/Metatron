using System;
using System.Collections.Generic;
using System.Linq;
using LavishScriptAPI;
using System.Diagnostics;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    //Contain the pulse shit that every class will use
    public abstract class ModuleBase : IModule
    {
        public string ModuleName { get; protected set; }

        //Number of StelathBot pulses per pulse of this module
        internal int PulseFrequency = 1;
        //Counter to keep track of when this module can pulse
		public int PulseCounter { get; set; }
        //Whether or not this module is enabled for pulsing
        internal bool IsEnabled;
 
        //Profiling-related variables
    	readonly Stopwatch _pulseProfilingTimer = new Stopwatch();
    	readonly Stopwatch _methodProfilingTimer = new Stopwatch();
		string _methodProfilingName = string.Empty;

		static DateTime _startOfRunSession = DateTime.Now, _startOfNextSession = DateTime.Now;
		static bool _maxRuntimeLogged;

		protected static Dictionary<string, object> _cachedResourcesByKeys = new Dictionary<string, object>();

        //Keep track of whether or not the module is initialized and cleaned up
        public bool IsInitialized { get; protected set; }

        public bool IsCleanedUpOutOfFrame { get; protected set; }

        //Keep track of whether or not we are initializing or cleaning up
        protected bool _isInitializing, _isCleaningUp;

        protected readonly ILogging _logging;

        internal ModuleBase()
        {
            _logging = Metatron.Logging;

            PulseCounter = -1;

            //Default to "enabled"
            IsEnabled = true;
            //Add this to the list of modules 
            ModuleManager.Modules.Add(this);
			//Default "IsCleanedUpOutOfFrame" to true. It will be set false in Initialize.
			IsCleanedUpOutOfFrame = true;
        }

        internal ModuleBase(ILogging logging)
        {
            _logging = logging;

            PulseCounter = -1;

            //Default to "enabled"
            IsEnabled = true;
            //Add this to the list of modules 
            ModuleManager.Modules.Add(this);
            //Default "IsCleanedUpOutOfFrame" to true. It will be set false in Initialize.
            IsCleanedUpOutOfFrame = true;
        }

        public virtual void Pulse()
        {

        }

		public virtual bool Initialize()
        {
			IsCleanedUpOutOfFrame = false;
            IsInitialized = true;
            return true;
        }

        public virtual bool OutOfFrameCleanup()
        {
            IsCleanedUpOutOfFrame = true;
            return true;
        }

		public virtual void InFrameCleanup()
		{
			
		}

		public virtual void CriticalPulse()
		{

		}

		public virtual bool ShouldPulse()
		{
			if (IsEnabled)
			{
				PulseCounter--;
				if (PulseCounter <= 0)
				{
					PulseCounter = PulseFrequency;
					return true;
				}
			}
			return false;
		}

        internal double Distance(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return Math.Sqrt(
                Math.Pow(x1 - x2, 2) +
                Math.Pow(y1 - y2, 2) +
                Math.Pow(z1 - z2, 2)
                );
        }

        internal double DistanceTo(double x, double y, double z)
        {
            var methodName = "DistanceTo";
            var returnVal = Math.Sqrt(
                Math.Pow(x - Metatron.MeCache.ToEntity.X, 2) +
                Math.Pow(y - Metatron.MeCache.ToEntity.Y, 2) +
                Math.Pow(z - Metatron.MeCache.ToEntity.Z, 2)
                ) - Metatron.MeCache.ToEntity.Radius;
            LogTrace(methodName, returnVal.ToString());
            return returnVal;
        }

		#region logging
		protected void LogMessage(string sendingMethod, LogSeverityTypes logSeverity, string messageFormat, params object[] messageParameters)
		{
			_logging.LogMessage(ModuleName, sendingMethod, logSeverity, messageFormat, messageParameters);
		}

		protected void LogException(Exception e, string methodName, string message, params object[] args)
		{
		    if (_logging != null)
		    {
		        _logging.LogException(ModuleName, e, methodName, message, args);
		    }
		    else
		    {
		        InnerSpaceAPI.InnerSpace.Echo(string.Format("Object {0} logging reference null.", ModuleName));

                var exceptionText = string.Format("Message: {0}\nStack Trace: {1}\nInner Exception: {2}",
                    e.Message, e.StackTrace, e.InnerException);
                InnerSpaceAPI.InnerSpace.Echo(exceptionText);
		    }
		}

		protected void LogTrace(string sendingMethod, string message = null)
		{
			_logging.LogTrace(this, sendingMethod, message);
		}

		protected void LogTrace(string sendingMethod, string messageFormat, params object[] messageParameters)
		{
			_logging.LogTrace(this, sendingMethod, messageFormat, messageParameters);
		}
		#endregion

		#region Profiling
		internal void StartPulseProfiling()
        {
            _pulseProfilingTimer.Start();
        }

        internal void EndPulseProfiling()
        {
            _pulseProfilingTimer.Stop();
			LogMessage("EndPulseProfiling", LogSeverityTypes.Profiling,
				"Ending pulse profiling for: {0}, Elapsed: {1}", ModuleName, _pulseProfilingTimer.Elapsed);
            _pulseProfilingTimer.Reset();
        }

		internal void StartMethodProfiling(string methodName)
		{
			_methodProfilingTimer.Start();
			_methodProfilingName = methodName;
		}

		internal void EndMethodProfiling()
		{
			_methodProfilingTimer.Stop();
			LogMessage("EndMethodProfiling", LogSeverityTypes.Profiling,
				"Ending method profiling for: {0}, Elapsed: {1}", _methodProfilingName, _methodProfilingTimer.Elapsed);
			_methodProfilingTimer.Reset();
			_methodProfilingName = string.Empty;
		}
        #endregion

        #region Exiting
        protected void ExitAndRelaunch()
		{
			Metatron.Instance.IsEnabled = false;
			if (Metatron.Config.MaxRuntimeConfig.UseRelaunching)
			{
				LavishScript.ExecuteCommand(
					String.Format("relay {0} \"TimedCommand 600 run isboxer -launch \"{1}\"\"",
					"${SettingXML[InnerSpace.XML].Set[Remote].GetString[Name].Escape}",
                    Metatron.Config.MaxRuntimeConfig.CharacterSetToRelaunch));
			}

			//This code might not be correct, but the idea is. We want to exit after 5 seconds so we don't undock forever, which is going to get things blown up in low/nullsec.
			LavishScript.ExecuteCommand("uplink TimedCommand 200 kill ${Session}");
			Metatron.OnSaveAndExit(this);
		}

		protected void DowntimeExitAndRelaunch()
		{
			Metatron.Instance.IsEnabled = false;
            if (Metatron.Config.MaxRuntimeConfig.RelaunchAfterDowntime)
			{
				LavishScript.ExecuteCommand(
					String.Format("relay {0} \"TimedCommand 54000 run isboxer -launch \"{1}\"\"",
					"${SettingXML[InnerSpace.XML].Set[Remote].GetString[Name].Escape}",
                    Metatron.Config.MaxRuntimeConfig.CharacterSetToRelaunch));
			}
			LavishScript.ExecuteCommand(
				"uplink TimedCommand 200 kill ${Session}");
			Metatron.OnSaveAndExit(this);
		}
        #endregion

		#region Utility
		protected static void Echo(string messageFormat, params object[] messageParameters)
		{
			InnerSpaceAPI.InnerSpace.Echo(string.Format(messageFormat, messageParameters));
		}

        protected bool HasMaxRuntimeExpired()
		{
            var methodName = "HasMaxRuntimeExpired";
        	_logging.LogTrace("ModuleBase", methodName);

			//Check should return true if we're not using MaxRuntime););
            if (!Metatron.Config.MaxRuntimeConfig.UseMaxRuntime)
                return false;

            if (_startOfRunSession == DateTime.MinValue)
                _startOfRunSession = DateTime.Now;

            var pastMaxRuntime = false;

			//If we're using max runtime and have exceeded the max runtime, switch to session wait and return false.
			//If we're still good on time, return true.
            var minutesBetweenStartAndNow = Metatron.TimeOfPulse.Subtract(_startOfRunSession).TotalMinutes;
            if (minutesBetweenStartAndNow >= Metatron.Config.MaxRuntimeConfig.MaxRuntimeMinutes)
			{
                if (!_maxRuntimeLogged)
				{
                    _logging.LogMessage("ModuleBase", methodName, LogSeverityTypes.Standard,
                        "Max runtime of {0} minutes exceeded at {1}", Metatron.Config.MaxRuntimeConfig.MaxRuntimeMinutes, DateTime.Now.ToString("hh:mm tt"));
				    _maxRuntimeLogged = true;
				}

				if (Metatron.Config.MaxRuntimeConfig.ResumeAfterWaiting)
				{
                    if (_startOfNextSession == DateTime.MinValue)
                    {
                        _logging.LogMessage("ModuleBase", methodName, LogSeverityTypes.Standard,
                            "Resuming running in {0} minutes", Metatron.Config.MaxRuntimeConfig.ResumeWaitMinutes);
                        _startOfNextSession = DateTime.Now.AddMinutes(Metatron.Config.MaxRuntimeConfig.ResumeWaitMinutes);
                    }

                    //If we've waited long enough switch back to run session timer and return true
                    if (Metatron.TimeOfPulse.CompareTo(_startOfNextSession) >= 0)
                    {
                        _logging.LogMessage("ModuleBase", methodName, LogSeverityTypes.Standard, "Done waiting, resuming run.");

                        //Reset variables
                        _startOfRunSession = DateTime.Now;
                        _startOfNextSession = DateTime.MinValue;
                        _maxRuntimeLogged = false;
                    }
                    else
                    {
                        //Haven't waited long enough
                        pastMaxRuntime = true;
                    }
				}
				else
				{
                    pastMaxRuntime = true;
				}
			}
        	
            return pastMaxRuntime;
		}

        /// <summary>
        /// Convert a boolean to an integer value. 1 for true, 0 for false.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected int BoolToInt(bool value)
        {
        	return value ? 1 : 0;
        }

    	#endregion
	}
}
