using System;
using System.Collections.Generic;
using System.Linq;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    public sealed class ModuleManager : ModuleBase, IModuleManager
    {
        public bool IsConfigLoaded;

        private string _uplinkName = string.Empty;
        public string UplinkName
        {
            get { return _uplinkName; }
        }

        //Cached result of Is3DDisplayOn and IsUIDisplayOn
        private bool _is3DDisplayOn = true, _isUiDisplayOn = true, _isTextureLoadingOn = true;

		//Keep track of the highest requested pulse delay
		private int _pulseDelayHighestSeconds, _pulseDelayTotalSeconds;
		//And the next pulse
		private DateTime _timeOfNextPulse = DateTime.Now;

        private readonly IIsxeveProvider _isxeveProvider;
        private readonly IEveWindowProvider _eveWindowProvider;

		#region Module Lists
		//Module lists for iteration during Pulse
		public static List<ModuleBase> Modules = new List<ModuleBase>();
        public static List<ModuleBase> CachesToPulse = new List<ModuleBase>();
        public static List<ModuleBase> EntityCachesToPulse = new List<ModuleBase>();
        public static List<ModuleBase> ModulesToPulse = new List<ModuleBase>();

        public static List<IDisposable> ModulesToDispose = new List<IDisposable>();
		public static List<LavishScriptObject> HomelessLsosToDispose = new List<LavishScriptObject>();

		public static List<ICriticallyBlocking> CriticallyBlockingModules = new List<ICriticallyBlocking>();
		#endregion

		internal ModuleManager(IIsxeveProvider isxeveProvider, IEveWindowProvider eveWindowProvider)
        {
		    _isxeveProvider = isxeveProvider;
		    _eveWindowProvider = eveWindowProvider;

		    //Set the object name
            ModuleName = "ModuleManager";
            //Set the pulse frequency, default to fastest until otherwise set
            PulseFrequency = 1;
            //make sure it's enabled
            IsEnabled = true;
            //Don't need to add this to a list of modules to pulse; it's pulsed from Metatron.
        }

        ~ModuleManager()
        {
            Dispose(false);
        }

		public override bool ShouldPulse()
		{
			if (IsEnabled)
			{
				if (PulseCounter-- <= 0)
				{
					PulseCounter = PulseFrequency;
					if (DateTime.Now.CompareTo(_timeOfNextPulse) >= 0)
					{
						return true;
					}
				}
			}
			return false;
		}

        #region IDisposable
		private bool _isDisposed;

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
        	if (_isDisposed)
				return;

        	_isDisposed = true;

        	//If not finalizing...
            if (!disposing) return;

            //re-enable 3D display if necessary
            if (!_is3DDisplayOn)
            {
                _isxeveProvider.Eve.Toggle3DDisplay();
            }

            //Do the same for UI display
            if (!_isUiDisplayOn)
            {
                _isxeveProvider.Eve.ToggleUIDisplay();
            }

            if (!_isTextureLoadingOn)
            {
                _isxeveProvider.Eve.ToggleTextureLoading();
            }

            //Dispose any IDisposable classes
            foreach (var disposable in ModulesToDispose)
            {
                disposable.Dispose();
            }

            GC.SuppressFinalize(this);
        }
        #endregion

        public override bool Initialize()
        {
            var methodName = "Initialize";
			LogTrace(methodName);

            var allModulesInitialized = true;
            foreach (var module in Modules)
            {
                try
                {
                    if (module is Metatron ||
                        module is ModuleManager)
                    {
                        continue;
                    }

                    if (!module.Initialize())
                    {
                    	LogMessage(methodName, LogSeverityTypes.Debug, module.ModuleName);
                        allModulesInitialized = false;
                    }
                }
                catch (Exception e)
                {
					LogException(e, methodName, "Caught exception while initializing {0}:", module.ModuleName);
                }
            }
            if (allModulesInitialized)
            {
				LogMessage(methodName, LogSeverityTypes.Standard, "Done initializing!");
            }
            else
            {
				LogMessage(methodName, LogSeverityTypes.Standard, "Still initializing modules...");
            }
            return allModulesInitialized;
        }

        public override bool OutOfFrameCleanup()
        {
            var methodName = "OutOfFrameCleanup";
			LogTrace(methodName);

            var allModulesCleanedUp = true;
            foreach (var module in Modules)
            {
                try
                {
                    if (module is Metatron ||
                        module is ModuleManager)
                    {
                        continue;
                    }

                    if (!module.OutOfFrameCleanup())
                    {
						LogMessage(methodName, LogSeverityTypes.Debug, "Module \"{0}\" not done cleaning up.",
							module.ModuleName);
                        allModulesCleanedUp = false;
                    }
                }
                catch (Exception e)
                {
					LogException(e, methodName, "Caught exception while cleaning up {0}:", module.ModuleName);
                }
            }

            if (allModulesCleanedUp)
            {
				LogMessage(methodName, LogSeverityTypes.Standard, "Done cleaning up!");
            }
            else
            {
				LogMessage(methodName, LogSeverityTypes.Standard, "Still cleaning up modules...");
            }

            return allModulesCleanedUp;
        }

		public override void InFrameCleanup()
		{
			var methodName = "InFrameCleanup";
			LogTrace(methodName);

			foreach (var module in Modules)
			{
				try
				{
					if (module is Metatron || module is ModuleManager)
					{
						continue;
					}

					module.InFrameCleanup();
				}
				catch (Exception e)
				{
					LogException(e, methodName, "Caught exception while cleaning up {0}:", module.ModuleName);
				}
			}

			try
			{
				foreach (var lavishScriptObject in HomelessLsosToDispose)
				{
					lavishScriptObject.Invalidate();
				}
				HomelessLsosToDispose.Clear();
			}
			catch (Exception e)
			{
				LogException(e, methodName, "Caught exception while disposing homeless LSOs:");
			}
		}

        public override void Pulse()
        {
            var methodName = "Pulse";
			LogTrace(methodName);

            //Make sure we should be pulsing...
        	if (!ShouldPulse()) 
				return;

        	HandlePulse();

        	//Do all in-frame cleanup. Dispose references etc.
        	InFrameCleanup();

        	EndPulse();
        }

		private void HandlePulse()
		{
			var methodName = "HandlePulse";
			LogTrace(methodName);

			//Do any critical pulses
			foreach (var module in Modules)
			{
				module.CriticalPulse();
			}

			LavishScript.DataParse("${SettingXML[InnerSpace.XML].Set[Remote].GetString[Name].Escape}", ref _uplinkName);

			//Process critically-blocking modules
			var isAnyModuleBlocking = CriticallyBlockingModules.Any(blockingModule => blockingModule.CriticallyBlock());

			if (isAnyModuleBlocking)
				return;

			//First, pulse the Cache modules
			foreach (var module in CachesToPulse)
			{
				//StartMethodProfiling(string.Format("Pulse {0}", module.ObjectName));
				TryPulseModule(module);
				//EndMethodProfiling();
			}

			//Return if EVE, MyShip are invalid and not InSpace but not InStation
			if (!Metatron.MeCache.InSpace && !Metatron.MeCache.InStation)
			{
				LogMessage(methodName, LogSeverityTypes.Debug, "Ending pulse due to some form of loading or break (not InSpace and not InStation).");
				return;
			}

			//Do any important actions, i.e. toggling UI/3d display or closing chats
			//StartMethodProfiling("Important Pulse Actions");
			HandleImportantPulseActions();
			//EndMethodProfiling();

			//If we're just supposed to load a config and not actually pulse things, return.
			if (Metatron.JustLoadConfig)
			{
				return;
			}

			//If we're not initialized yet, initialize
			if (!IsInitialized)
			{
				IsInitialized = Initialize();
				return;
			}

			//Now pulse Entity caches
			foreach (var module in EntityCachesToPulse)
			{
				//StartMethodProfiling(string.Format("Pulse {0}", module.ObjectName));
				TryPulseModule(module);
				//EndMethodProfiling();
			}

			//up next, actual core modules
			foreach (var module in ModulesToPulse)
			{
				//StartMethodProfiling(string.Format("Pulse {0}", module.ObjectName));
				TryPulseModule(module);
				//EndMethodProfiling();
			}

			//Separately pulse the SbUiCommunication module
			//StartMethodProfiling("SbUiCommunication");
			//TryPulseModule(Metatron.SbUiCommunication);
			//EndMethodProfiling();
		}

		private void EndPulse()
		{
			//Next pulse should be one second after the pulse ends
			_timeOfNextPulse = DateTime.Now.AddSeconds(PulseFrequency);
			//Now apply the pulse delay
			_timeOfNextPulse = _timeOfNextPulse.AddSeconds(_pulseDelayHighestSeconds);
			//Clear the tracking variable
			_pulseDelayHighestSeconds = 0;
			//Also add the "total" time delay
			_timeOfNextPulse = _timeOfNextPulse.AddSeconds(_pulseDelayTotalSeconds);
			_pulseDelayTotalSeconds = 0;
		}

        #region Important Pulse Action methods
        private void HandleImportantPulseActions()
        {
            //Load pilot cache, configuration files, and set logfile prefix
            LoadConfigAndCache();

            //Close windows
            CloseEveWindows();

            //Toggle render states as necessary
            ToggleRender();
        }

        private void LoadConfigAndCache()
        {
			var methodName = "LoadConfigAndCache";
			LogTrace(methodName);

            //If we've got a valid Me and our name is readable...
        	if (!Metatron.MeCache.IsValid || Metatron.MeCache.Name.Length <= 0) 
				return;

        	//Load a config if we haven't already loaded one
        	if (IsConfigLoaded) 
				return;

        	//Go ahead and load config
        	Metatron.ConfigurationManager.LoadConfigurationFiles();

// ReSharper disable RedundantAssignment
        	var versionString = "Stable";
// ReSharper restore RedundantAssignment
#if DEBUG
        	versionString = "Testing";
#endif
			LogMessage(methodName, LogSeverityTypes.Standard, "Version: {0} {1}", 
				System.Windows.Forms.Application.ProductVersion, versionString);

        	//Also load missions
        	//EVEBotMissions.Cache.LoadCache(_Me.Name);

        	if (!Metatron.Config.DefenseConfig.DisableStandingsChecks)
        	{
        		_isxeveProvider.Eve.RefreshStandings();
        	}

        	IsConfigLoaded = true;
        }

        private void CloseEveWindows()
        {
        	var methodName = "CloseEveWindows";
			LogTrace(methodName);

            //First check for any "MessageBox" template windows
            using (var infoWindow = _eveWindowProvider.GetWindowByName("MessageBox"))
			{
			    if (infoWindow.IsValid)
                    infoWindow.Close();
			}

            using (var modalWindow = _eveWindowProvider.GetWindowByName("modal"))
            {
                if (modalWindow.IsValid)
                {
                    //If it's a mission warning, click 'yes'
                    if (modalWindow.Text.StartsWith("This mission involves objectives requiring a total capacity of ",
                                                   StringComparison.InvariantCultureIgnoreCase))
                    {
                        modalWindow.ClickButtonYes();
                    }
                    //Fleet invitations are message boxes. Close it if we're not currently invited to a fleet
                    else if (!Metatron.MeCache.Me.Fleet.Invited || Metatron.MeCache.Me.Fleet.InvitationText == "NULL")
                    {
                        modalWindow.ClickButtonClose();
                    }
                }
            }
        }

        private void ToggleRender()
        {
			//If I'm in station don't do anything
			if (Metatron.MeCache.InStation)
				return;

            //first, toggle 3D rendering as necessary
            _is3DDisplayOn = _isxeveProvider.Eve.Is3DDisplayOn;
            if (Metatron.Config.MainConfig.Disable3DRender)
            {
                if (_is3DDisplayOn)
                    _isxeveProvider.Eve.Toggle3DDisplay();
            }
            else
            {
                if (!_is3DDisplayOn)
                    _isxeveProvider.Eve.Toggle3DDisplay();
            }

            //next, toggle UI rendering as necessary
            _isUiDisplayOn = _isxeveProvider.Eve.IsUIDisplayOn;
            if (Metatron.Config.MainConfig.DisableUiRender)
            {
                if (_isUiDisplayOn)
                    _isxeveProvider.Eve.ToggleUIDisplay();
            }
            else
            {
                if (!_isUiDisplayOn)
                    _isxeveProvider.Eve.ToggleUIDisplay();
            }

            _isTextureLoadingOn = _isxeveProvider.Eve.IsTextureLoadingOn;
			if (Metatron.Config.MainConfig.DisableTextureLoading)
			{
				if (_isTextureLoadingOn)
                    _isxeveProvider.Eve.ToggleTextureLoading();
			}
			else
			{
				if (!_isTextureLoadingOn)
                    _isxeveProvider.Eve.ToggleTextureLoading();
			}
        }
        #endregion

        public bool IsNonCombatMode
        {
            get
            {
                var botMode = Metatron.Config.MainConfig.ActiveBehavior;
                switch (botMode)
                {
                    case BotModes.Ratting:
                        return false;
                    case BotModes.Missioning:
                        return Metatron.MissionRunner.ActiveMission == null ||
                               Metatron.MissionRunner.ActiveMission.Type.Contains("Mining");
                    case BotModes.CombatAssist:
                        return false;
                    default:
                        return true;
                }
            }
        }

		public void DelayPulseByTicks(int pulses)
		{
			PulseCounter += pulses;
		}

		public void DelayPulseByHighestTime(int seconds)
		{
			if (seconds > _pulseDelayHighestSeconds)
				_pulseDelayHighestSeconds = seconds;
		}

		public void DelayPulseByTotalTime(int seconds)
		{
			_pulseDelayTotalSeconds += seconds;
		}

        private void TryPulseModule(ModuleBase module)
        {
            var methodName = "TryPulseModule";
			LogTrace(methodName, "Module: {0}", module.ModuleName);

            //Try/Catch block for any unhandled exceptions in the odule
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
