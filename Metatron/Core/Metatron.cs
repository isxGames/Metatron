using System;
using System.IO;
using LavishScriptAPI;
using LavishVMAPI;
using EVE.ISXEVE;
using System.Threading;
using System.Diagnostics;
using Metatron.ActionModules;
using Metatron.BehaviorModules;
using Metatron.BehaviorModules.PartialBehaviors;
using Metatron.Core.Config;
using Metatron.Core.EventCommunication;
using Metatron.Core.Interfaces;
using Metatron.Core.Analytics;

namespace Metatron.Core
{
    internal sealed class Metatron : ModuleBase, IDisposable
    {
        //Timer for runtime
        public Stopwatch RunTime = new Stopwatch();

        //Events for onframe and such
        private EventHandler<LSEventArgs> OnEVEFrame;
        internal static event EventHandler<LSEventArgs> OnPulse;

    	private bool _areEventsAttached = false;

        public static bool JustLoadConfig = true;

        private static bool _getSafe;
        public static bool GetSafe
        {
            get { return _getSafe; }
            set { lock (Instance) { _getSafe = value; } }
        }

        //Exit event
		internal static event EventHandler SaveAndExit;

        #region Core object references

        private MathUtility _mathUtility;

        //ISXEVE object providers
        private static IIsxeveProvider _isxeveProvider;
        private static IEveWindowProvider _eveWindowProvider;
        public static IEveWindowProvider EveWindowProvider
        {
            get
            {
                return _eveWindowProvider;
            }
            set
            {
                _eveWindowProvider = value;
            }
        }

        //Me cache. Contains the other caches.
        internal static IMeCache MeCache;
        internal static IBookMarkCache BookMarkCache;

        //entity populator object
        internal static EntityProviderBase EntityProvider;

        //Instances of the Core classes
        internal static IModuleManager ModuleManager;
		internal static IStatistics Statistics;
        internal static IShip Ship;
        internal static IJettisonContainer JetCan;
        internal static ILogging Logging;
        internal static ISocial Social;
        internal static IStation Station;
        internal static IBookmarks Bookmarks;
        internal static IAsteroidBelts AsteroidBelts;
        internal static IFileManager FileManager;
        internal static IConfigurationManager ConfigurationManager;
		internal static IConfiguration Config;
        internal static IDrones Drones;
		internal static EventCommunications EventCommunications;
		internal static IPilotCache PilotCache;
		internal static IFleet Fleet;
        internal static IAlerts Alerts;
		internal static MissionProcessor MissionProcessor;
        internal static IMissionCache MissionCache;
        internal static IAgentCache AgentCache;
		internal static INpcBountyCache NpcBountyCache;
        internal static ITargetQueue TargetQueue;
        private IAnomalyProvider _anomalyProvider;
        private IAnomalyClaimTracker _anomalyClaimTracker;
        private ISafespots _safespots;

        //EVEDB modules
        internal static IAllianceCache AllianceCache;
        internal static ICorporationCache CorporationCache;
        internal static IEntityCache EntityCache;
        internal static IDogmaAttributeCache DogmaAttributeCache;
        internal static IDogmaEffectCache DogmaEffectCache;

		//Databases
		internal static IMissionDatabase MissionDatabase;
    	internal static IPossibleEwarNpcs PossibleEwarNpcs;

		internal static IAttackers Attackers;
        #endregion

        #region ActionModule object references
        //Action classes
		internal static Defense Defense;
        internal static IMovement Movement;
        internal static ITargeting Targeting;
        internal static Offensive Offensive;
        internal static NonOffensive NonOffensive;
        #endregion

        #region BehaviorModule object references
        //Partial Behaviors
        private MoveToDropOffLocationPartialBehavior _moveToDropOffLocationPartialBehavior;
        private DropOffCargoPartialBehavior _dropOffCargoPartialBehavior;

        //Behavior classes
        internal static BehaviorManager BehaviorManager;
        internal static Mining Mining;
		internal static Hauler Hauler;
		internal static BoostCanOrca BoostCanOrca;
		internal static BoostOrca BoostOrca;
        internal static Freighter Freighter;
        internal static MissionRunner MissionRunner;
        internal static JumpStabilityTest JumpStabilityTest;
		internal static Ratting Ratting;
        internal static CombatAssist CombatAssist;
        internal static Salvaging Salvaging;
        internal static CallbackDelegate LogCallback;
        #endregion

        #region Singleton
        //Instance of this class
        private static Metatron _instance;
        //Accessor, used for returning the instance or nulling it for disposal
        internal static Metatron Instance
        {
            get
            {
				if (_instance == null)
				{
					_instance = new Metatron();

					_instance.Initialize();
					_instance.AttachEvents();
				}

                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }
        #endregion

        #region Pulse-related objects
        //DateTime representing the time the pulse started firing
        internal static DateTime TimeOfPulse = new DateTime();
        //DateTime representing the time of the next pulse
        public DateTime NextPulse = DateTime.Now;
        //DateTime representing the time Metatron was instantiated
        internal static DateTime TimeOfStart = DateTime.Now;
        //Number of total pulses
        internal static uint Pulses = 0;
        //ManualResetEvent used for preventing exit mid-pulse
        private static ManualResetEvent _exitResetEvent = new ManualResetEvent(true);
		private static ManualResetEvent _InFrameCleanupResetEvent = new ManualResetEvent(false);
		private static bool _isDoingInFrameCleanup = false;
        //Constant for the pulsefrequency of HandleEveFrame, thus everything else
        private static readonly int PULSE_FREQUENCY = 1;
        #endregion

        #region IDisposable members
        static bool _isDisposed = false;
        #endregion

        #region MetatronUI Interop
        //MetatronUI interop class
        //internal static SbUiCommunication SbUiCommunication;

        //DataGridView references from UI;
        public System.Windows.Forms.DataGridView ItemsMined_Moved = null, Ammo_CrystalsUsed = null;
        #endregion

        //Class that will hold static instances of all helper objects.
        private Metatron()
        {
            IsEnabled = false;
            PulseFrequency = 1;
            ModuleName = "Metatron";
        }

		public override bool Initialize()
        {
			IsCleanedUpOutOfFrame = false;

            _isxeveProvider = new IsxeveProvider();
            _eveWindowProvider = new EveWindowProvider();

            #region Core object construction
            //Background stuff
            Logging = new Logging();
            LogCallback = LogIsxeveMessage;
            Tracing.AddCallback(LogCallback);

		    _mathUtility = new MathUtility();
            Statistics = new Statistics();
            EventCommunications = new EventCommunications(Logging);
            FileManager = new FileManager();

            //moduleManager and BehaviorManager
            ModuleManager = new ModuleManager(_isxeveProvider, _eveWindowProvider);

            //Me cache. Contains the other caches.
            MeCache = new MeCache(_isxeveProvider, _eveWindowProvider);

            //ConfigurationManager
            Config = new Configuration();
            ConfigurationManager = new ConfigurationManager(Config, MeCache);

            Ship = new Ship(_isxeveProvider, _eveWindowProvider, MeCache, MeCache.Ship, Config.CargoConfig, Statistics, Config.MovementConfig);
            
            //Entity Populator object
            EntityProvider = new EntityProvider(_isxeveProvider);

            //Other Cache and Data classes
            MissionCache = new MissionCache(_isxeveProvider, _eveWindowProvider);
            AgentCache = new AgentCache(FileManager, MeCache, Config, _eveWindowProvider, _isxeveProvider);

		    BookMarkCache = new BookMarkCache(MeCache, EntityProvider, Logging, _isxeveProvider);
            NpcBountyCache = new NpcBountyCache();
            //EVEDB modules
            PilotCache = new PilotCache();
            AllianceCache = new AllianceCache();
            CorporationCache = new CorporationCache();
            EntityCache = new EntityCache();
            DogmaAttributeCache = new DogmaAttributeCache();
            DogmaEffectCache = new DogmaEffectCache();
            MissionDatabase = new MissionDatabase();
            PossibleEwarNpcs = new PossibleEwarNpcs();

            //Core Functionality Providers, not necessarily caches
		    TargetQueue = new TargetQueue(MeCache, EntityProvider, Config.MiningConfig, Config.MainConfig);
			JetCan = new JettisonContainer(_eveWindowProvider);
			Social = new Social(_isxeveProvider);
            Station = new Station(_isxeveProvider, _eveWindowProvider);
            AsteroidBelts = new AsteroidBelts(Config, MeCache, EntityProvider, BookMarkCache, Ship);
			Bookmarks = new Bookmarks(MeCache, Station, Config, BookMarkCache, AsteroidBelts, _isxeveProvider);
			Drones = new Drones(_isxeveProvider);
			Fleet = new Fleet();
            Alerts = new Alerts(Config, MeCache, Logging);
            _anomalyProvider = new AnomalyProvider(MeCache.Ship);
		    _anomalyClaimTracker = new AnomalyClaimTracker(Logging, EventCommunications, MeCache);
            _safespots = new Safespots(MeCache, BookMarkCache, Config.MovementConfig, MeCache.ToEntity, EntityProvider, _isxeveProvider, Ship, Social, _mathUtility, Logging);

			Attackers = new Attackers(MeCache, Config, Ship, Drones, EntityProvider, Alerts, AsteroidBelts, PossibleEwarNpcs, TargetQueue, ModuleManager);
            #endregion

            #region ActionModule construction
            //Action classes
			Movement = new Movement(_isxeveProvider, EntityProvider, MeCache, _anomalyProvider, TargetQueue, Ship, Drones);
			Targeting = new Targeting(Logging, Config.MaxRuntimeConfig, MeCache, Ship, Drones, Alerts, ModuleManager, TargetQueue, EntityProvider, Movement);
			Offensive = new Offensive(Logging, TargetQueue, EntityProvider, Ship);
			NonOffensive = new NonOffensive(MeCache, Config.MiningConfig, Config.DefenseConfig, EntityProvider, TargetQueue, Ship, Drones, Targeting, Movement);
            Defense = new Defense(_isxeveProvider, EntityProvider, Ship, MeCache, Config.DefenseConfig, Social, Drones, Alerts, _safespots, Movement, TargetQueue);
            #endregion

            #region Processor construction
            MissionProcessor = new MissionProcessor(_eveWindowProvider, Movement);
            #endregion

            #region BehaviorModule construction
            _moveToDropOffLocationPartialBehavior = new MoveToDropOffLocationPartialBehavior(Movement, EntityProvider, Config.CargoConfig, BookMarkCache, Bookmarks,
                Config.MovementConfig, MeCache, _mathUtility, _isxeveProvider);
            _dropOffCargoPartialBehavior = new DropOffCargoPartialBehavior(_eveWindowProvider, Config.CargoConfig, Config.MainConfig, Config.MiningConfig,
                MeCache, Ship, Station, JetCan, EntityProvider, EventCommunications);

            //Behavior classes
            BehaviorManager = new BehaviorManager();
			Mining = new Mining(Config.CargoConfig, Config.MainConfig, MeCache, Ship, EntityProvider,
                _safespots, Movement, Social, Config.MovementConfig, AsteroidBelts, _moveToDropOffLocationPartialBehavior, _dropOffCargoPartialBehavior, Config.MiningConfig,
                _isxeveProvider, BookMarkCache, TargetQueue);
            Hauler = new Hauler(_eveWindowProvider, Config.CargoConfig, Config.MainConfig, Config.MiningConfig, MeCache, Ship, Station, JetCan, EntityProvider, EventCommunications,
                _safespots, Movement, BookMarkCache, _moveToDropOffLocationPartialBehavior);
			BoostCanOrca = new BoostCanOrca();
			BoostOrca = new BoostOrca(BookMarkCache, Config.MiningConfig, Bookmarks, _safespots, Movement, Config.MainConfig, Ship, MeCache);
            Freighter = new Freighter(_eveWindowProvider, Config.CargoConfig, Config.MainConfig,
                Config.MiningConfig, MeCache, Ship, Station, JetCan, EntityProvider, EventCommunications, _moveToDropOffLocationPartialBehavior, _dropOffCargoPartialBehavior, Movement);
            MissionRunner = new MissionRunner(Config, MissionCache, AgentCache, _eveWindowProvider);
            JumpStabilityTest = new JumpStabilityTest();
		    Ratting = new Ratting(Social, MeCache, Bookmarks, Config.SalvageConfig, Config.RattingConfig, _anomalyProvider, EntityProvider, _anomalyClaimTracker, _safespots,
                Movement, Ship, AsteroidBelts, Config.MovementConfig, Alerts, TargetQueue, Attackers);
            CombatAssist = new CombatAssist(Social, MeCache, Bookmarks, Config.SalvageConfig, Config.RattingConfig, _anomalyProvider, EntityProvider, _anomalyClaimTracker, _safespots,
                Movement, Ship, AsteroidBelts, Config.MovementConfig, Alerts, TargetQueue, Attackers);
            Salvaging = new Salvaging(Social, MeCache, Bookmarks, Config.SalvageConfig, EntityProvider, _safespots, Movement, Ship, Config.MovementConfig, Alerts, TargetQueue, Attackers, 
                new InventoryProvider(Logging, _eveWindowProvider, MeCache.Ship, _isxeveProvider.Eve), _isxeveProvider);


            #endregion

            //MetatronUI Interop construction
            //SbUiCommunication = new SbUiCommunication();

#if DEBUG
            LavishScript.ExecuteCommand("ISXEVE:Debug_SetHighPerfLogging[1]");
#endif

            return true;
		}

        public override bool OutOfFrameCleanup()
        {
            IsEnabled = false;

            //RemoveBookmarkAndCacheEntry the logging callback
            Tracing.RemoveCallback();

			//Set the inframe cleanup flag and unset enabled
			Instance.IsEnabled = false;
			_isDoingInFrameCleanup = true;

			//Wait on the event
            if (!_isDisposed)
			    _InFrameCleanupResetEvent.WaitOne();

            //Block dispose until we can safely exit
            if (!_isDisposed)
                _exitResetEvent.WaitOne();

            //Dispose any disposables
            if (ModuleManager != null)
            {
                var timeout = 450;
                while (!ModuleManager.OutOfFrameCleanup() && timeout-- > 0)
                {
					Thread.Sleep(10);
                }
                
                ModuleManager.Dispose();
            }

            //Null the instance so there are no more references to this object.
            Instance = null;
			return true;
        }

        #region IDisposable implementors
		private void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			_isDisposed = true;

			if (disposing)
			{
                DetachEvents();

                OutOfFrameCleanup();

				//Suppress finalization
				GC.SuppressFinalize(this);
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}
        #endregion

        /// <summary>
        /// EventHandler method for the ISXEVE_onFrame LS event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void HandleEveFrame(object sender, LSEventArgs e)
        {
            using (new FrameLock(true))
            {
                //Instance.LogMessage("HandleEveFrame", LogSeverityTypes.Debug, "Beginning frame handler.");

                //Reset the exitResetEvent. We should not attempt to dispose right now.
                _exitResetEvent.Reset();

                //If paused, stop the runtime timer.
                if (!IsEnabled)
                {
                    RunTime.Stop();
                }

                //If doing cleanup go ahead and and do cleanup.
                if (_isDoingInFrameCleanup)
                {
                    //Have ModuleManager do inframe cleanup and set the event
                    //ModuleManager.InFrameCleanup();

                    _isDoingInFrameCleanup = false;
                    _InFrameCleanupResetEvent.Set();
                }

                //If not paused and it's been enough time to pulse again...
                if (Instance.IsEnabled && DateTime.Now.CompareTo(NextPulse) >= 0)
                {
                    //Make sure the runtime timer is running.
                    RunTime.Start();

                    //Now, if it's not been enough pulses to fire the pulse...
                    if (!Instance.ShouldPulse())
                    {
                        //Set the reset event and return.
                        EndPulse();
                        return;
                    }

                    var methodName = "HandleEveFrame";

                    //Set the time of this pulse
                    TimeOfPulse = DateTime.Now;
                    //Increment the pulse count
                    Pulses++;
                    //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                    //	"HandleEveFrame", String.Format("Pulse check: {0}", Pulses)));

                    #region ISXEVE IsSafe Check

                    //If ISXEve is reporting unsafe, just abort and return. It means
                    //it's not safe to continue logic checks with ISXEVE.
                    if (!_isxeveProvider.Isxeve.IsSafe)
                    {
                        //Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                        //methodName, "Error: ISXEVE reporting not safe. Aborting pulse."));
                        EndPulse();
                        return;
                    }

                    #endregion

                    #region ISXEVE Detection

                    if (!_isxeveProvider.Isxeve.IsReady)
                    {
                        Instance.LogMessage(methodName, LogSeverityTypes.Debug, "ISXEVE is not ready, pausing.");
                        Instance.IsEnabled = false;
                        EndPulse();
                        return;
                    }

                    #endregion

                    #region Me Validity Check

                    //Another check - ME validity
                    if (!MeCache.CheckValidity())
                    {
                        //Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                        //methodName, "Error: InSpace and InStation both false or Me invalid."));

                        if (LavishScriptObject.IsNullOrInvalid(MeCache.Me))
                        {
                            #region Login Screen Detection

                            var login = new Login();
                            if (!LavishScriptObject.IsNullOrInvalid(login))
                            {
                                if (ConfigurationManager.ActiveConfigProfile != null)
                                {
                                    Instance.LogMessage(methodName, LogSeverityTypes.Standard,
                                                        "Login screen detected. Exiting, relaunching if enabled.");
                                    Instance.ExitAndRelaunch();
                                }
                                else
                                {
                                    Instance.LogMessage(methodName, LogSeverityTypes.Standard,
                                                        "Login screen detected. Please close Metatron, restart EVE, and start Metatron while fully logged in.");
                                }
                                Instance.IsEnabled = false;
                            }

                            #endregion

                            #region CharSelect Screen Detection

                            var charSelect = new CharSelect();
                            if (!LavishScriptObject.IsNullOrInvalid(charSelect))
                            {
                                Instance.LogMessage(methodName, LogSeverityTypes.Standard,
                                                    "Character Select screen detected. Please close Metatron, restart EVE, and start Metatron while fully logged in.");
                                Instance.IsEnabled = false;
                            }

                            #endregion
                        }

                        _exitResetEvent.Set();
                        Logging.LogMessage("Metatron", methodName, LogSeverityTypes.Debug, "Aborting pulse due to invalidity.");
                        NextPulse = DateTime.Now.AddSeconds(PULSE_FREQUENCY);
                        return;
                    }

                    #endregion

                    //Pulse the ModuleManager.
                    ModuleManager.Pulse();

                    //Fire off the OnPulse event to let the UI know of any changes
                    try
                    {
                        //Instance.LogMessage("HandleEveFrame", LogSeverityTypes.Debug, "Beginning OnPulse.");
                        //Instance.StartMethodProfiling("OnPulse");
                        OnPulse(sender, e);
                        //Instance.EndMethodProfiling();
                        //Instance.LogMessage("HandleEveFrame", LogSeverityTypes.Debug, "Ending OnPulse.");
                    }
                    catch (Exception ex)
                    {
                        LogException(ex, methodName, "Caught excpetion while firing OnPulse event:");
                    }

                    EndPulse();
                }

                //We're done; set the reset event so we can dispose if necessary.
                _exitResetEvent.Set();
                //Instance.LogMessage("HandleEveFrame", LogSeverityTypes.Debug, "Ending EVE Frame.");
            }
        }

		private void EndPulse()
		{
			_exitResetEvent.Set();
			NextPulse = DateTime.Now.AddSeconds(PULSE_FREQUENCY);
			//Instance.StartMethodProfiling("GC.Collect");
			//GC.Collect();
			//Instance.EndMethodProfiling();
		}

		private void AttachEvents()
		{
			if (_areEventsAttached)
				return;

		    OnEVEFrame += HandleEveFrame;
			LavishScript.Events.AttachEventTarget("ISXEVE_onFrame", OnEVEFrame);
			
			_areEventsAttached = true;
		}

    	private void DetachEvents()
		{
			if (!_areEventsAttached)
				return;

			LavishScript.Events.DetachEventTarget("ISXEVE_onFrame", OnEVEFrame);
    	    OnEVEFrame -= HandleEveFrame;

			_areEventsAttached = false;
		}

        /// <summary>
        /// Determine if this build was compiled with the DEBUG token. Returns true if so, otherwise false.
        /// </summary>
        internal static bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

    	internal static MetatronBuilds Build
    	{
    		get
    		{
#if DEBUG
    			return MetatronBuilds.Testing;
#else
				return MetatronBuilds.Stable;
#endif

			}
    	}

        private static string _directory;
        internal static string Directory
        {
            get
            {
                return _directory ?? (_directory = Path.Combine(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "Metatron"));
            }
        }

    	private static string _dataDirectory;
    	internal static string DataDirectory
    	{
    		get
    		{
				return _dataDirectory ?? (_dataDirectory = Path.Combine(Directory, "Data"));
    		}
    	}

        private static string _configDirectory;
        internal static string ConfigDirectory
        {
            get { return _configDirectory ?? (_configDirectory = Path.Combine(Directory, "Config")); }
        }

        /// <summary>
        /// Call the WaitOne 
        /// method of the exitreset event and return its result.
        /// </summary>
        internal static bool CanExit
        {
            get
            {
                return _exitResetEvent.WaitOne();
            }
        }

        /// <summary>
        /// Fire the SaveAndExit event to inform everything it's time to save config and exit Metatron.
        /// </summary>
        /// <param name="sender"></param>
		internal static void OnSaveAndExit(object sender)
		{
			if (SaveAndExit != null)
			{
				SaveAndExit(sender, new EventArgs());
			}
		}

        private void LogIsxeveMessage(string method, string args)
        {
			Logging.LogMessage("ISXEVEWrapper", method, LogSeverityTypes.Trace, args);
        }
    }
}
