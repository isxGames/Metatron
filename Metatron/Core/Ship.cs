using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EVE.ISXEVE;
using EVE.ISXEVE.Interfaces;
using LavishScriptAPI;
using Metatron.Core.Config;
using Metatron.Core.CustomEventArgs;
using Metatron.Core.Interfaces;
using System.Windows.Forms;

namespace Metatron.Core
{
    //using IModule = EVE.ISXEVE.Interfaces.IModule;

    internal sealed class Ship : ModuleBase, IShip
    {
        // ReSharper disable CompareOfFloatsByEqualityOperator
        private EventHandler<SessionChangedEventArgs> _sessionChanging;

        //Module lists
        #region Module Lists
        private List<EVE.ISXEVE.Interfaces.IModule> _allModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> AllModules { get { return _allModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _armorRepairerModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> ArmorRepairerModules { get { return _armorRepairerModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _shieldBoosterModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> ShieldBoosterModules { get { return _shieldBoosterModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _activeHardenerModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> ActiveHardenerModules { get { return _activeHardenerModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _passiveShieldTankModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> PassiveShieldTankModules { get { return _passiveShieldTankModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _allWeaponModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> WeaponModules { get { return _allWeaponModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _turretModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> TurretModules { get { return _turretModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _launcherModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> LauncherModules { get { return _launcherModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _precursorModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> PrecursorModules { get { return _precursorModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _cloakingDeviceModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> CloakingDeviceModules { get { return _cloakingDeviceModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _covertOpsCloakingDeviceModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> CovertOpsCloakingDeviceModules { get { return _covertOpsCloakingDeviceModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _otherCloakingDeviceModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> OtherCloakingDeviceModules { get { return _otherCloakingDeviceModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _damageControlModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> DamageControlModules { get { return _damageControlModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _eccmModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> EccmModules { get { return _eccmModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _miningLaserModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> MiningLaserModules { get { return _miningLaserModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _tractorBeamModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> TractorBeamModules { get { return _tractorBeamModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _salvagerModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> SalvagerModules { get { return _salvagerModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _afterBurnerModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> AfterBurnerModules { get { return _afterBurnerModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _gangLinkModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> GangLinkModules { get { return _gangLinkModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _surveyScannerModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> SurveyScannerModules { get { return _surveyScannerModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _targetPainterModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> TargetPainterModules { get { return _targetPainterModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _trackingComputerModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> TrackingComputerModules { get { return _trackingComputerModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _stasisWebifierModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> StasisWebifierModules { get { return _stasisWebifierModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _nosferatuModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> NosferatuModules { get { return _nosferatuModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _sensorBoosterModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> SensorBoosterModules { get { return _sensorBoosterModules.AsReadOnly(); } }

        private readonly List<EVE.ISXEVE.Interfaces.IModule> _seigeModules = new List<EVE.ISXEVE.Interfaces.IModule>();
        public ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> SeigeModules { get { return _seigeModules.AsReadOnly(); } }
        #endregion
        #region Ammo-related variables
        private Dictionary<string, double> _hybridRangeModsByName = new Dictionary<string, double>();
        private Dictionary<string, double> _frequencyRangeModsByName = new Dictionary<string, double>();
        private Dictionary<string, double> _projectileRangeModsByName = new Dictionary<string, double>();
        private Dictionary<string, double> _missileRangeModsByName = new Dictionary<string, double>();
        private Dictionary<string, double> _precursorRangeModsByName = new Dictionary<string, double>();

        private Dictionary<string, DamageProfile> _projectileDamageProfilesByChargeName = new Dictionary<string, DamageProfile>();
        private Dictionary<string, DamageProfile> _missileDamageProfilesByChargeName = new Dictionary<string, DamageProfile>();

        internal Dictionary<string, List<Dictionary<int, double>>> DictionariesByModuleType = new Dictionary<string, List<Dictionary<int, double>>>();
        private bool _dictionariesByModuleNameBuilt = false;

        internal Dictionary<int, double> ModuleBaseOptimaRanges = new Dictionary<int, double>();

        //track the next ammo change, per module
        private Dictionary<Int64, DateTime> _nextAmmoChangeByModuleID = new Dictionary<long, DateTime>();
        //Constant for time between ammo changes
        private readonly int SECONDS_BETWEEN_STANDARD_AMMO_CHANGES = 12,
            SECONDS_BETWEEN_CRYSTAL_AMMO_CHANGES = 2;

        private RearmStates _rearmState = RearmStates.Idle;
        #endregion

        public List<int> ChargeTypesUsed = new List<int>();
        public List<int> ChargeQuantities = new List<int>();

        private readonly Dictionary<int, int> _chargeTypeIdToGroupIdMap = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _loadedChargeCountByTypeId = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _loadedChargeCountByGroupId = new Dictionary<int, int>();
        private readonly Dictionary<int, string> _chargeNamesByTypeId = new Dictionary<int, string>();
        private readonly Dictionary<int, double> _chargeVolumesByGroupId = new Dictionary<int, double>();
        private readonly Dictionary<int, ItemStub> _loadedChargesByTypeId = new Dictionary<int, ItemStub>();
        private readonly Dictionary<int, int> _moduleFullLoadQuantityByGroupId = new Dictionary<int, int>();
        private readonly List<ItemStub> _allowedCharges = new List<ItemStub>();
        private readonly List<ItemStub> _fittedWeapons = new List<ItemStub>();
        public bool AbleToSwapAmmoLoadout { get; private set; }

        private readonly Dictionary<long, int> _lastChargeTypeIdByWeaponId = new Dictionary<long, int>();

        private double _totalWeaponChargeVolumeCapacity = 0;

        private int _turretCount;

        //Cache a few things that go nuts now and then
        private double _maxMiningRange, _maxWeaponRange = -1;

        private bool _wasInventoryOpenedThisPulse;
        private DateTime _openInventoryDelay = DateTime.Now;

        private readonly List<int> _crystalExclusionList = new List<int>
                                                               {
                                                                   (int) GroupIDs.MiningCrystal,
                                                                   (int) GroupIDs.MercoxitMiningCrystal
                                                               };

        private readonly IIsxeveProvider _isxeveProvider;
        private readonly IEveWindowProvider _eveWindowProvider;
        private readonly IMeCache _meCache;
        private readonly IShipCache _shipCache;
        private readonly ICargoConfiguration _cargoConfiguration;
        private readonly IStatistics _statistics;
        private readonly IMovementConfiguration _movementConfiguration;

        internal Ship(IIsxeveProvider isxeveProvider, IEveWindowProvider eveWindowProvider, IMeCache meCache, IShipCache shipCache, ICargoConfiguration cargoConfiguration, IStatistics statistics,
            IMovementConfiguration movementConfiguration)
        {
            _isxeveProvider = isxeveProvider;
            _eveWindowProvider = eveWindowProvider;
            _meCache = meCache;
            _shipCache = shipCache;
            _cargoConfiguration = cargoConfiguration;
            _statistics = statistics;
            _movementConfiguration = movementConfiguration;

            ModuleManager.ModulesToPulse.Add(this);
            ModuleName = "Ship";
            PulseFrequency = 1;

            #region populate charge name/modifier pairs
            _hybridRangeModsByName.Add("Spike", 1.8);
            _hybridRangeModsByName.Add("Iron", 1.6);
            _hybridRangeModsByName.Add("Tungsten", 1.4);
            _hybridRangeModsByName.Add("Null", 1.25);
            _hybridRangeModsByName.Add("Iridium", 1.2);
            _hybridRangeModsByName.Add("Lead", 1.0);
            _hybridRangeModsByName.Add("Thorium", 0.875);
            _hybridRangeModsByName.Add("Uranium", 0.75);
            _hybridRangeModsByName.Add("Void", 0.75);
            _hybridRangeModsByName.Add("Plutonium", 0.625);
            _hybridRangeModsByName.Add("Antimatter", 0.5);
            _hybridRangeModsByName.Add("Javelin", 0.25);

            _precursorRangeModsByName.Add("Baryon", 1.1);
            _precursorRangeModsByName.Add("Meson", 1.8);
            _precursorRangeModsByName.Add("Tetryon", 0.6);
            _precursorRangeModsByName.Add("Mystic", 1.5);
            _precursorRangeModsByName.Add("Occult", 0.6);

            _projectileRangeModsByName.Add("Tremor", 1.8);
            _projectileDamageProfilesByChargeName.Add("Tremor", new DamageProfile(37.5, 0, 62.5, 0));
            _projectileRangeModsByName.Add("Carbonized Lead", 1.6);
            _projectileDamageProfilesByChargeName.Add("Carbonized Lead", new DamageProfile(80, 0, 20, 0));
            _projectileRangeModsByName.Add("Nuclear", 1.6);
            _projectileDamageProfilesByChargeName.Add("Nuclear", new DamageProfile(20, 0, 80, 0));
            _projectileRangeModsByName.Add("Proton", 1.6);
            _projectileDamageProfilesByChargeName.Add("Proton", new DamageProfile(34, 0, 0, 66));
            _projectileRangeModsByName.Add("Depleted Uranium", 1.0);
            _projectileDamageProfilesByChargeName.Add("Depleted Uranium", new DamageProfile(25, 37.5, 37.5, 0));
            _projectileRangeModsByName.Add("Barrage", 1.0);
            _projectileDamageProfilesByChargeName.Add("Barrage", new DamageProfile(45, 0, 55, 0));
            _projectileRangeModsByName.Add("Titanium Sabot", 1.0);
            _projectileDamageProfilesByChargeName.Add("Titanium Sabot", new DamageProfile(75, 0, 25, 0));
            _projectileRangeModsByName.Add("Phased Plasma", 0.5);
            _projectileDamageProfilesByChargeName.Add("Phased Plasma", new DamageProfile(17, 83, 0, 0));
            _projectileRangeModsByName.Add("Fusion", 0.5);
            _projectileDamageProfilesByChargeName.Add("Fusion", new DamageProfile(17, 0, 83, 0));
            _projectileRangeModsByName.Add("EMP", 0.5);
            _projectileDamageProfilesByChargeName.Add("EMP", new DamageProfile(8, 0, 17, 75));
            _projectileRangeModsByName.Add("Hail", 0.5);
            _projectileDamageProfilesByChargeName.Add("Hail", new DamageProfile(21, 0, 79, 0));
            _projectileRangeModsByName.Add("Quake", 0.25);
            _projectileDamageProfilesByChargeName.Add("Quake", new DamageProfile(36, 0, 64, 0));

            _frequencyRangeModsByName.Add("Aurora", 1.8);
            _frequencyRangeModsByName.Add("Radio", 1.6);
            _frequencyRangeModsByName.Add("Scorch", 1.5);
            _frequencyRangeModsByName.Add("Microwave", 1.4);
            _frequencyRangeModsByName.Add("Infrared", 1.2);
            _frequencyRangeModsByName.Add("Standard", 1.0);
            _frequencyRangeModsByName.Add("Ultraviolet", 0.875);
            _frequencyRangeModsByName.Add("Xray", 0.75);
            _frequencyRangeModsByName.Add("Gamma", 0.625);
            _frequencyRangeModsByName.Add("Multifrequency", 0.5);
            _frequencyRangeModsByName.Add("Conflagration", 0.5);
            _frequencyRangeModsByName.Add("Gleam", 0.25);

            _missileRangeModsByName.Add("Rage", 0.88);
            _missileRangeModsByName.Add("Javelin", 1.5);
            _missileRangeModsByName.Add("Precision", 0.5);
            _missileRangeModsByName.Add("Fury", 0.53);

            _missileDamageProfilesByChargeName.Add("Scourge", new DamageProfile(100, 0, 0, 0));
            _missileDamageProfilesByChargeName.Add("Inferno", new DamageProfile(0, 100, 0, 0));
            _missileDamageProfilesByChargeName.Add("Nova", new DamageProfile(0, 0, 100, 0));
            _missileDamageProfilesByChargeName.Add("Mjolnir", new DamageProfile(0, 0, 0, 100));
            #endregion
        }

        #region Activate/Deactivate Module methods
        //Activate tank repair modules
        public bool ActivateTankRepairModules()
        {
            var methodName = "Activate_TankRepairModules";
            LogTrace(methodName);

            //If I have shield boosters, kick 'em on.
            if (_shieldBoosterModules.Count > 0)
            {
                foreach (var module in
                    _shieldBoosterModules.Where(module => !module.IsActive && _meCache.Ship.Capacitor > module.ActivationCost))
                {
                    module.Click();
                    return false;
                }
            }
            //Same for armor reppers
            if (_armorRepairerModules.Count > 0)
            {
                foreach (var module in
                    _armorRepairerModules.Where(module => !module.IsActive && _meCache.Ship.Capacitor > module.ActivationCost))
                {
                    module.Click();
                    return false;
                }
            }
            return true;
        }

        public bool DeactivateTankRepairModules()
        {
            var methodName = "Deactivate_TankRepairModules";
            LogTrace(methodName);

            //If I have shield boosters, kick 'em on.
            if (_shieldBoosterModules.Count > 0)
            {
                foreach (var module in _shieldBoosterModules.Where(module => module.IsActive))
                {
                    module.Click();
                    return false;
                }
            }
            //Same for armor reppers
            if (_armorRepairerModules.Count > 0)
            {
                foreach (var module in _armorRepairerModules.Where(module => module.IsActive))
                {
                    module.Click();
                    return false;
                }
            }
            return true;
        }

        public bool ActivateCovertOpsCloak()
        {
            var methodName = "Activate_CovertOpsCloak";
            LogTrace(methodName);

            if (_covertOpsCloakingDeviceModules.Any())
            {
                var module = _covertOpsCloakingDeviceModules.First();
                if (!module.IsActive)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Activating Covert Ops cloak.");
                    module.Click();
                }
            }

            return true;
        }

        public bool DeactivateCovertOpsCloak()
        {
            var methodName = "Deactivate_CovertOpsCloak";
            LogTrace(methodName);

            if (_covertOpsCloakingDeviceModules.Any())
            {
                var module = _covertOpsCloakingDeviceModules.First();
                if (module.IsActive)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Deactivating Covert Ops cloak.");
                    module.Click();
                }
            }

            return true;
        }

        public bool ActivateModuleList(IEnumerable<EVE.ISXEVE.Interfaces.IModule> modules, bool fastActivate)
        {
            return ActivateModuleList(modules, fastActivate, -1);
        }

        public bool ActivateModuleList(IEnumerable<EVE.ISXEVE.Interfaces.IModule> modules, bool fastActivate, long targetEntityId)
        {
            var methodName = "Activate_ModuleList";
            LogTrace(methodName, "FastActivate: {0}, TargetID: {1}", fastActivate, targetEntityId);

            if (modules.Any())
            {
                foreach (var module in
                    modules.Where(module => !module.IsActive && (_shipCache.Capacitor > module.ActivationCost && !module.IsDeactivating || module.ToItem.GroupID == (int)GroupIDs.IndustrialCore)))
                {
                    LogTrace(methodName, "Module: " + module.ToItem.Name);
                    if (targetEntityId >= 0)
                    {
                        module.Activate(targetEntityId);
                    }
                    else
                    {
                        module.Activate();
                    }
                    if (!fastActivate)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool DeactivateModuleList(IEnumerable<EVE.ISXEVE.Interfaces.IModule> modules, bool fastDeactivate)
        {
            var methodName = "Deactivate_ModuleList";
            LogTrace(methodName, "FastDeactivate: {0}", fastDeactivate);

            if (modules.Any())
            {
                foreach (var module in modules.Where(module => module.IsActive && !module.IsDeactivating))
                {
                    module.Deactivate();
                    if (!fastDeactivate)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        #endregion

        public override void Pulse()
        {
            var methodName = "Pulse";
            LogTrace(methodName);

            if (!ShouldPulse())
                return;

            if (!IsInventoryOpen)
            {
                if (DateTime.Now.CompareTo(_openInventoryDelay) < 0)
                    return;

                LogMessage(methodName, LogSeverityTypes.Standard, "Opening ship cargo.");
                OpenInventory();
            }

            StartPulseProfiling();
            //Update module list
            //Don't do this if in station!
            if (!_meCache.InStation && _meCache.InSpace)
            {
                //StartMethodProfiling("UpdateModuleLists");
                UpdateModuleLists();
                //EndMethodProfiling();

                if (Metatron.Config.MainConfig.ActiveBehavior != BotModes.Mining)
                    DisableWeaponAutoReload();
                else
                    EnableWeaponAutoReload();

                if (!_dictionariesByModuleNameBuilt)
                {
                    StartMethodProfiling("CalculateTurretOptimalRangeBands");
                    CalculateTurretOptimalRangeBands();
                    EndMethodProfiling();
                }

                UpdateLoadedChargeTypeIds();

                //If I've got mining lasers, calculate our mining optimal range. Otherwise leave it at 0.
                if (_miningLaserModules.Count > 0)
                {
                    _maxMiningRange = (double)_miningLaserModules[0].OptimalRange * 0.975;
                    _maxMiningRange = (double)_miningLaserModules[0].OptimalRange * 0.975;
                }
                else
                {
                    _maxMiningRange = 0;
                }

                //if I have ammo and need to reload, do so
                var isAmmoAvailable = IsAmmoAvailable;
                if (_meCache.ToEntity.Mode == (int)Modes.Warp && isAmmoAvailable)
                {
                    ReloadAllModules();
                }
                else if (isAmmoAvailable && WeaponModules.Any() && WeaponModules.All(mod => mod.Charge == null || !mod.Charge.IsValid) && !Metatron.TargetQueue.Targets.Any())
                {
                    ReloadAllModules();
                }
            }

            if (Metatron.Movement.IsCriticalMoving || !IsInventoryReady || !IsInventoryOpen)
            {
                EndPulseProfiling();
                return;
            }

            //Cargo should ALWAYS be open when not critically moving (i.e. undocking)
            //If I have an ore hold and I'm NOT an orca, default the cargo capacity to ore hold. Orcas should still use cargohold.
            var typeId = _meCache.InSpace
                             ? _meCache.ToEntity.TypeId
                             : _meCache.Ship.Ship.ToItem.TypeID;

            var cargoCapacity = true && typeId != (int)TypeIDs.Orca
                                    ? OreHoldCapacity
                                    : _meCache.Ship.CargoCapacity;

            if (cargoCapacity == 0 && _meCache.Ship.Ship.ToItem.TypeID != (int)TypeIDs.Capsule)
            {
                LogMessage("Pulse", LogSeverityTypes.Debug, "A cargo capacity of 0 was detected. Making the ship cargo hold active to prime CargoCapacity...");
                MakeCargoHoldActive();
                return;
            }

            //TODO: Find a way to re-enable.
            //If the cargofullthreshold hasn't been set or is higher than our current capacity, re-set it.
            //if (cargoCapacity > 0 &&
            //    (_cargoConfiguration.CargoFullThreshold == 0 || _cargoConfiguration.CargoFullThreshold > cargoCapacity))
            //{
            //    //Set the threshold to 85% of cargo capacity
            //    LogMessage("Pulse", LogSeverityTypes.Standard, "Warning: Cargo full theshold of {0} was zero or above the cargo capacity of {1}. HasOreHold: <{2}>, IsOrca: <{3}>, ContainerReady: <{4}>",
            //        _cargoConfiguration.CargoFullThreshold, cargoCapacity, true, typeId == (int) TypeIDs.Orca, cargoCapacity == 5);
            //    _cargoConfiguration.CargoFullThreshold = (int) (cargoCapacity*0.85);
            //    Metatron.ConfigurationManager.OnConfigLoaded();
            //}

            CacheChargeAndModuleInfoForFrame();
            EndPulseProfiling();
        }

        public bool HasOreHold()
        {
            var invWindow = _eveWindowProvider.GetInventoryWindow();
            IEveInvChildWindow shipOreHold = invWindow.GetChildWindow(_meCache.ShipId, "ShipGeneralMiningHold");
            return shipOreHold.IsValid;
        }

        private void UpdateLoadedChargeTypeIds()
        {
            var methodName = "UpdateLoadedChargeTypeIds";
            LogTrace(methodName);

            foreach (var weapon in WeaponModules)
            {
                if (LavishScriptObject.IsNullOrInvalid(weapon.Charge)) continue;

                if (!_lastChargeTypeIdByWeaponId.ContainsKey(weapon.ID))
                    _lastChargeTypeIdByWeaponId.Add(weapon.ID, weapon.Charge.TypeId);
                else
                    _lastChargeTypeIdByWeaponId[weapon.ID] = weapon.Charge.TypeId;
            }
        }

        private void DisableWeaponAutoReload()
        {
            foreach (var module in _turretModules.Union(_launcherModules).Where(module => module.IsAutoReloadOn))
            {
                module.SetAutoReloadOff();
            }
        }

        private void EnableWeaponAutoReload()
        {
            foreach (var module in _turretModules.Union(_launcherModules).Where(module => !module.IsAutoReloadOn))
            {
                module.SetAutoReloadOn();
            }
        }

        private void ClearModuleLists()
        {
            foreach (Module module in _allModules)
            {
                module.Invalidate();
            }
            _allModules.Clear();

            _allWeaponModules.Clear();
            _armorRepairerModules.Clear();
            _passiveShieldTankModules.Clear();
            _shieldBoosterModules.Clear();
            _launcherModules.Clear();
            _precursorModules.Clear();

            _turretModules.Clear();
            _cloakingDeviceModules.Clear();
            _covertOpsCloakingDeviceModules.Clear();
            _otherCloakingDeviceModules.Clear();
            _activeHardenerModules.Clear();
            _damageControlModules.Clear();
            _eccmModules.Clear();
            _miningLaserModules.Clear();
            _tractorBeamModules.Clear();
            _salvagerModules.Clear();
            _afterBurnerModules.Clear();
            _gangLinkModules.Clear();
            _surveyScannerModules.Clear();
            _targetPainterModules.Clear();
            _trackingComputerModules.Clear();
            _stasisWebifierModules.Clear();
            _nosferatuModules.Clear();
            _sensorBoosterModules.Clear();
        }

        public override void InFrameCleanup()
        {
            ClearModuleLists();

            _wasInventoryOpenedThisPulse = false;

            //reset the max weapon range
            _maxWeaponRange = -1;
        }

        public override bool Initialize()
        {
            if (!IsInitialized)
            {
                _sessionChanging = SessionChanging;
                Metatron.Movement.PreSessionChanged += _sessionChanging;

                IsInitialized = true;
            }
            return IsInitialized;
        }

        public override bool OutOfFrameCleanup()
        {
            if (!IsCleanedUpOutOfFrame)
            {
                Metatron.Movement.PreSessionChanged -= _sessionChanging;

                if (_meCache.InSpace)
                {
                    EnableWeaponAutoReload();
                }

                IsCleanedUpOutOfFrame = true;
            }
            return IsCleanedUpOutOfFrame;
        }

        private void SessionChanging(object sender, SessionChangedEventArgs e)
        {
            var methodName = "SessionChanging";
            LogTrace(methodName);

            if (!e.InSpace)
            {
                EnableWeaponAutoReload();
                CacheAllChargeAndModuleInfo();
            }
            else
            {
                _dictionariesByModuleNameBuilt = false;
            }

            if (_meCache.IsInventoryOpen)
                _meCache.GetInventoryWindow().Close();
        }

        internal void UpdateModuleLists()
        {
            _allModules = new List<EVE.ISXEVE.Interfaces.IModule>(_meCache.Ship.Modules);
            //Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
            //    string.Format("Found {0} Ship Modules.", Modules_All.Count)));
            //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
            //	"UpdateModuleLists", String.Format("Updating module lists on pulse {0}",
            //	Core.Metatron.Pulses)));

            var allModulesOffline = _allModules.Count > 0 && _allModules.All(module => !module.IsOnline);

            foreach (var mod in _allModules)
            {
                //Ignore offline modules to avoid EVE warning spam
                //Hack: ISXEVE's Module.IsOnline is broken for some people and constantly returning FALSE.
                //Only check if a module's online if they're not one of the broken users.
                if (!allModulesOffline && !mod.IsOnline) continue;

                switch (mod.ToItem.GroupID)
                {
                    //Turrets
                    case (int)GroupIDs.EnergyWeapon:
                        goto case (int)GroupIDs.ProjectileWeapon;
                    case (int)GroupIDs.HybridWeapon:
                        goto case (int)GroupIDs.ProjectileWeapon;
                    case (int)GroupIDs.PrecursorWeapon:
                        _allWeaponModules.Add(mod);
                        _turretModules.Add(mod);
                        break;
                    case (int)GroupIDs.ProjectileWeapon:
                        _allWeaponModules.Add(mod);
                        _turretModules.Add(mod);
                        break;
                    //Lotta fuckin' launchers
                    case (int)GroupIDs.MissileLauncher:
                        _allWeaponModules.Add(mod);
                        _launcherModules.Add(mod);
                        break;
                    case (int)GroupIDs.MissileLauncher_Assault:
                        goto case (int)GroupIDs.MissileLauncher;
                    case (int)GroupIDs.MissileLauncher_Bomb:
                        goto case (int)GroupIDs.MissileLauncher;
                    case (int)GroupIDs.MissileLauncher_Cruise:
                        goto case (int)GroupIDs.MissileLauncher;
                    case (int)GroupIDs.MissileLauncher_Defender:
                        goto case (int)GroupIDs.MissileLauncher;
                    case (int)GroupIDs.MissileLauncher_Heavy:
                        goto case (int)GroupIDs.MissileLauncher;
                    case (int)GroupIDs.MissileLauncher_HeavyAssault:
                        goto case (int)GroupIDs.MissileLauncher;
                    case (int)GroupIDs.MissileLauncher_Rocket:
                        goto case (int)GroupIDs.MissileLauncher;
                    case (int)GroupIDs.MissileLauncher_Siege:
                        goto case (int)GroupIDs.MissileLauncher;
                    case (int)GroupIDs.MissileLauncher_Standard:
                        goto case (int)GroupIDs.MissileLauncher;
                    case (int)GroupIDs.ShieldRecharger:
                        _passiveShieldTankModules.Add(mod);
                        break;
                    case (int)GroupIDs.ShieldBooster:
                        _shieldBoosterModules.Add(mod);
                        break;
                    case (int)GroupIDs.ArmorRepairer:
                        _armorRepairerModules.Add(mod);
                        break;
                    case (int)GroupIDs.CloakingDevice:
                        _cloakingDeviceModules.Add(mod);

                        if (mod.ToItem.TypeID == (int)TypeIDs.CovertOpsCloakingDevice)
                        {
                            _covertOpsCloakingDeviceModules.Add(mod);
                        }
                        else
                        {
                            _otherCloakingDeviceModules.Add(mod);
                        }
                        break;
                    case (int)GroupIDs.ArmorHardener:
                        _activeHardenerModules.Add(mod);
                        break;
                    case (int)GroupIDs.ArmorResistanceHardener:
                        _activeHardenerModules.Add(mod);
                        break;
                    case (int)GroupIDs.ShieldHardener:
                        goto case (int)GroupIDs.ArmorHardener;
                    case (int)GroupIDs.DamageControl:
                        _damageControlModules.Add(mod);
                        break;
                    case (int)GroupIDs.ECCM:
                        _eccmModules.Add(mod);
                        break;
                    case (int)GroupIDs.Salvager:
                        _salvagerModules.Add(mod);
                        break;
                    case (int)GroupIDs.StripMiner:
                        goto case (int)GroupIDs.MiningLaser;
                    case (int)GroupIDs.MiningLaser:
                        _miningLaserModules.Add(mod);
                        break;
                    case (int)GroupIDs.FrequencyMiningLaser:
                        goto case (int)GroupIDs.MiningLaser;
                    case (int)GroupIDs.TractorBeam:
                        _tractorBeamModules.Add(mod);
                        break;
                    case (int)GroupIDs.DataMiner:
                        switch (mod.ToItem.TypeID)
                        {
                            case (int)TypeIDs.Salvager:
                                _salvagerModules.Add(mod);
                                break;
                        }
                        break;
                    case (int)GroupIDs.AfterBurner:
                        _afterBurnerModules.Add(mod);
                        break;
                    case (int)GroupIDs.GangLink:
                        _gangLinkModules.Add(mod);
                        break;
                    case (int)GroupIDs.IndustrialCore:
                        goto case (int)GroupIDs.GangLink;
                    case (int)GroupIDs.SurveyScanner:
                        _surveyScannerModules.Add(mod);
                        break;
                    case (int)GroupIDs.TargetPainter:
                        _targetPainterModules.Add(mod);
                        break;
                    case (int)GroupIDs.TrackingComputer:
                        _trackingComputerModules.Add(mod);
                        break;
                    case (int)GroupIDs.EnergyVampire:
                        _nosferatuModules.Add(mod);
                        break;
                    case (int)GroupIDs.StasisWebifier:
                    case (int)GroupIDs.StasisGrappler:
                        _stasisWebifierModules.Add(mod);
                        break;
                    case (int)GroupIDs.SensorBooster:
                        _sensorBoosterModules.Add(mod);
                        break;
                    case (int)GroupIDs.DroneTrackingModules:
                        _trackingComputerModules.Add(mod);
                        break;
                }
            }
        }

        public bool IsInventoryOpen
        {
            get
            {
                var inventoryWindow = _eveWindowProvider.GetInventoryWindow();

                return !LavishScriptObject.IsNullOrInvalid(inventoryWindow);
            }
        }

        public void OpenInventory()
        {
            if (_wasInventoryOpenedThisPulse) return;

            //Delay next check due to lag with EVE's generation of the window
            _openInventoryDelay = DateTime.Now.AddSeconds(3);
            _wasInventoryOpenedThisPulse = true;

            _isxeveProvider.Eve.Execute(ExecuteCommand.OpenInventory);
        }

        public bool IsOreHoldActive
        {
            get
            {
                var inventoryWindow = _eveWindowProvider.GetInventoryWindow();
                if (inventoryWindow == null) return false;

                var activeChild = inventoryWindow.ActiveChild;
                if (activeChild == null) return false;

                return activeChild.ItemId == _meCache.Ship.Id && activeChild.Name == "ShipGeneralMiningHold";
            }
        }

        public void MakeOreHoldActive()
        {
            if (!Metatron.Ship.IsInventoryOpen) return;

            var childWindow = GetOreHoldChildWindow();
            if (childWindow == null) return;

            childWindow.MakeActive();
        }

        public IEveInvChildWindow GetOreHoldChildWindow()
        {
            var methodName = "GetOreHoldChildWindow";
            LogTrace(methodName);

            var inventoryWindow = _eveWindowProvider.GetInventoryWindow();
            if (inventoryWindow == null) return null;

            var childWindow = inventoryWindow.GetChildWindow(_meCache.Ship.Id, "ShipGeneralMiningHold");
            if (childWindow == null)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Error: Child window for ItemId {0}, location ShipGeneralMiningHold was null.", _meCache.Ship.Id);
            }

            return childWindow;
        }

        public bool IsCargoHoldActive
        {
            get
            {
                var inventoryWindow = _meCache.GetInventoryWindow();
                if (inventoryWindow == null) return false;

                var activeChild = inventoryWindow.ActiveChild;

                return activeChild.ItemId == _meCache.Ship.Id && activeChild.Name == "ShipCargo";
            }
        }

        public bool IsRepairWindowOpen
        {
            get
            {
                var repairWindow = Core.Metatron.EveWindowProvider.GetRepairShopWindow();
                if (repairWindow == null || !repairWindow.IsValid) return false;
                else return true;
            }
        }

        public bool IsInventoryReady
        {
            get
            {
                if (_wasInventoryOpenedThisPulse) return false;

                var cargoWindow = GetCargoHoldChildWindow();
                if (cargoWindow == null) return false;

                if (HasCargoHold())
                {
                    var oreHoldWindow = GetOreHoldChildWindow();
                    if (oreHoldWindow == null) return false;
                }

                return true;
            }
        }

        private bool HasCargoHold()
        {
            var methodName = "HasCargoHold";
            LogTrace(methodName);

            var inventoryWindow = _eveWindowProvider.GetInventoryWindow();
            if (inventoryWindow == null) return false;

            var childWindow = inventoryWindow.GetChildWindow(_meCache.Ship.Id, "ShipGeneralMiningHold");
            if (childWindow == null)
            {
                return false;
            }

            return true;

        }

        public IEveInvChildWindow GetCargoHoldChildWindow()
        {
            var methodName = "GetCargoHoldChildWindow";
            LogTrace(methodName);

            var inventoryWindow = _eveWindowProvider.GetInventoryWindow();
            if (inventoryWindow == null) return null;

            var childWindow = inventoryWindow.GetChildWindow(_meCache.Ship.Id, "ShipCargo");
            if (childWindow == null)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Warning: Child window for ItemId {0}, location ShipCargo was null.", _meCache.Ship.Id);
            }

            return childWindow;
        }

        #region Cargo transfer functions
        public bool HangarContainsItem(int itemTypeId)
        {
            var methodName = "HangarContainsItem";
            LogTrace(methodName, "ItemTypeID: {0}", itemTypeId);

            return _meCache.HangarItems.Any(item => item.TypeID == itemTypeId);
        }

        public bool CargoContainsItem(int itemTypeId)
        {
            var methodName = "CargoContainsItem";
            LogTrace(methodName, "ItemTypeID: {0}", itemTypeId);

            return _meCache.Ship.Cargo.Any(item => item.TypeID == itemTypeId);
        }

        public bool TransferItemFromHangarToCargo(int itemTypeId, out bool allItemsFit)
        {
            var methodName = "TransferItemFromHangarToCargo";
            LogTrace(methodName, "ItemTypeID: {0}", itemTypeId);

            //Iterate hangar items and look for the matching item
            foreach (var item in _meCache.HangarItems)
            {
                if (item.TypeID != itemTypeId)
                    continue;

                //Move as many as we can. If we've got the space to move 'em all, do so.
                var freeCargoCapacity = _meCache.Ship.CargoCapacity - _meCache.Ship.UsedCargoCapacity;
                if (Math.Abs(item.Quantity) * item.Volume > freeCargoCapacity)
                {
                    var quantityToMove = (int)Math.Floor(freeCargoCapacity / item.Volume);
                    item.MoveTo(ToLocationNames.MyShip.ToString(), ToDestinationNames.CargoHold.ToString(), quantityToMove);
                    allItemsFit = false;
                }
                else
                {
                    item.MoveTo(ToLocationNames.MyShip.ToString(), ToDestinationNames.CargoHold.ToString());
                    allItemsFit = true;
                }
                return true;
            }
            allItemsFit = false;
            return false;
        }

        public bool TransferStationHangarToShipHangar()
        {
            var methodName = "TransferStationHangarToShipHangar";
            LogTrace(methodName);

            var movedEverything = true;
            var items = _meCache.Me.GetHangarItems();
            var freeCargoSpace = _meCache.Ship.CargoCapacity - _meCache.Ship.UsedCargoCapacity;

            //This dictionary will store the ItemIDs and items
            var itemsToMove = new Dictionary<Item, int>();

            foreach (var item in items)
            {
                if (freeCargoSpace > item.Quantity * item.Volume)
                {
                    itemsToMove.Add(item, item.Quantity);
                    freeCargoSpace -= item.Quantity * item.Volume;
                    LogMessage(methodName, LogSeverityTypes.Debug, "Preparing to move {0} of item {1} ({2})",
                        item.Quantity, item.Name, item.ID);
                }
                else if (freeCargoSpace > item.Volume)
                {
                    var numToMove = (int)(freeCargoSpace / item.Volume);

                    if (numToMove >= 1)
                    {
                        itemsToMove.Add(item, numToMove);
                        freeCargoSpace -= numToMove * item.Volume;
                        LogMessage(methodName, LogSeverityTypes.Debug, "Preparing to move {0} of item {1} ({2}).",
                            numToMove, item.Name, item.ID);
                    }
                }
                else
                {
                    movedEverything = false;
                    LogMessage(methodName, LogSeverityTypes.Debug, "No more free space; done preparing to move items.");
                    break;
                }
            }

            foreach (var item in itemsToMove.Keys.ToList())
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Moving {0} of item {1} ({2}).",
                    itemsToMove[item], item.Name, item.ID);
                item.MoveTo(ToLocationNames.MyShip.ToString(), ToDestinationNames.CargoHold.ToString(), itemsToMove[item]);
            }
            return movedEverything;
        }

        public bool TransferStationCorpHangarToShipHangar()
        {
            var methodName = "TransferStationCorpHangarToShipHangar";
            LogTrace(methodName);

            var movedEverything = true;
            var items = _meCache.Me.GetCorpHangarItems();
            var itemsInDivision = items.Where(i => GetHangarSlotFromSlotId(i.SlotID) == _cargoConfiguration.PickupLocation.HangarDivision);
            var freeCargoSpace = _meCache.Ship.CargoCapacity - _meCache.Ship.UsedCargoCapacity;

            //This dictionary will store the ItemIDs and items
            var itemsToMove = new Dictionary<Item, int>();

            foreach (var item in itemsInDivision)
            {
                if (freeCargoSpace > item.Quantity * item.Volume)
                {
                    itemsToMove.Add(item, item.Quantity);
                    freeCargoSpace -= item.Quantity * item.Volume;
                    LogMessage(methodName, LogSeverityTypes.Debug, "Preparing to move {0} of item {1} ({2}).",
                               item.Quantity, item.Name, item.ID);
                }
                else if (freeCargoSpace > item.Volume)
                {
                    var numToMove = (int)Math.Floor(freeCargoSpace / item.Volume);

                    if (numToMove >= 1)
                    {
                        itemsToMove.Add(item, numToMove);
                        freeCargoSpace -= numToMove * item.Volume;
                        LogMessage(methodName, LogSeverityTypes.Debug, "Preparing to move {0} of item {1} ({2}).",
                            numToMove, item.Name, item.ID);
                    }
                }
                else
                {
                    movedEverything = false;
                    LogMessage(methodName, LogSeverityTypes.Debug, "No more free space; done preparing to move items.");
                    break;
                }
            }

            foreach (var item in itemsToMove.Keys.ToList())
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Moving {0} of item {1} ({2}).",
                    itemsToMove[item], item.Name, item.ID);
                item.MoveTo(ToLocationNames.MyShip.ToString(), ToDestinationNames.CargoHold.ToString(), itemsToMove[item]);
            }
            return movedEverything;
        }

        public bool TransferCorporateHangarArrayToShipHangar(ICorporateHangarArray corporateHangarArray)
        {
            var methodName = "TransferCorpHangarArrayToShipHangar";
            LogTrace(methodName, "CorporateHangarArray: {0}", corporateHangarArray.CurrentContainer.ID);

            if (!corporateHangarArray.IsCurrentContainerWindowActive)
            {
                throw new InvalidOperationException("Cannot move cargo from an inactive corporate hangar array.");
            }

            var movedEverything = true;

            var items = corporateHangarArray.CurrentContainer.ToEntity.GetCargo();
            var itemsInDivision = items.Where(i => GetHangarSlotFromSlotId(i.SlotID) == _cargoConfiguration.PickupLocation.HangarDivision);
            var freeCargoSpace = _meCache.Ship.CargoCapacity - _meCache.Ship.UsedCargoCapacity;

            //This dictionary will store the ItemIDs and items
            var itemsToMove = new Dictionary<IItem, int>();

            foreach (var item in itemsInDivision)
            {
                if (freeCargoSpace > item.Quantity * item.Volume)
                {
                    itemsToMove.Add(item, item.Quantity);
                    freeCargoSpace -= item.Quantity * item.Volume;
                    LogMessage(methodName, LogSeverityTypes.Debug, "Preparing to move {0} of item {1} ({2}).",
                        item.Quantity, item.Name, item.ID);
                }
                else if (freeCargoSpace > item.Volume)
                {
                    var numToMove = (int)(freeCargoSpace / item.Volume);

                    if (numToMove >= 1)
                    {
                        itemsToMove.Add(item, numToMove);
                        freeCargoSpace -= numToMove * item.Volume;
                        LogMessage(methodName, LogSeverityTypes.Debug, "Preparing to move {0} of item {1} ({2}).",
                            numToMove, item.Name, item.ID);
                    }
                }
                else
                {
                    movedEverything = false;
                    LogMessage(methodName, LogSeverityTypes.Debug, "No more free space; done preparing to move items.");
                    break;
                }
            }

            foreach (var item in itemsToMove.Keys.ToList())
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Moving {0} of item {1} ({2}).",
                    itemsToMove[item], item.Name, item.ID);
                item.MoveTo(_meCache.ToEntity.Id, ToDestinationNames.CargoHold.ToString(), itemsToMove[item]);
            }
            return movedEverything;
        }

        private int GetHangarSlotFromSlotId(int slotId)
        {
            var methodName = "GetHangarSlotFromSlotId";
            LogTrace(methodName, "slotId: {0}", slotId);

            switch (slotId)
            {
                case 4:
                    return 1;
                case 116:
                case 117:
                case 118:
                case 119:
                case 120:
                case 121:
                    //116 - 114 = 2, 117 - 114 = 3, so on up through 7.
                    return slotId - 114;
                default:
                    LogMessage(methodName, LogSeverityTypes.Debug, "Cannot handle item with SlotID {0}.", slotId);
                    throw new NotImplementedException(string.Format("Cannot handle item with SlotID {0}.", slotId));
            }
        }

        #region -> Jetcan
        public void TransferCargoHoldToJetCan(Int64 jetCanEntityId)
        {
            var methodName = "TransferCargoHoldToJetCan";
            LogTrace(methodName, "jetCanEntityId: {0}", jetCanEntityId);

            var childWindow = Metatron.JetCan.GetChildWindow();
            if (childWindow == null) return;

            var cargoToMove = new List<IItem>(_meCache.Ship.Cargo);

            var freeVolume = childWindow.Capacity - childWindow.UsedCapacity;
            var quantitiesByItem = new Dictionary<IItem, int>();
            DetermineItemsToMoveByFreeVolume(cargoToMove, quantitiesByItem, freeVolume);

            TransferCargo(quantitiesByItem, MoveItemToJetCan, null);
        }

        public void TransferOreInCargoHoldToJetCan(Int64 jetCanEntityId)
        {
            var methodName = "TransferOreInCargoHoldToJetCan";
            LogTrace(methodName, "jetCanEntityId: {0}", jetCanEntityId);

            var childWindow = Metatron.JetCan.GetChildWindow();
            if (childWindow == null) return;

            var items = new List<IItem>();
            var nonCrystalItems = _meCache.Ship.Cargo.
                Where(item => !_crystalExclusionList.Contains(item.GroupID));
            items.AddRange(nonCrystalItems);

            var freeVolume = childWindow.Capacity - childWindow.UsedCapacity;
            var quantitiesByItem = new Dictionary<IItem, int>();
            DetermineItemsToMoveByFreeVolume(items, quantitiesByItem, freeVolume);

            TransferCargo(quantitiesByItem, MoveItemToJetCan, _crystalExclusionList);
        }

        public void TransferOreHoldToJetCan(Int64 jetCanEntityId)
        {
            var methodName = "TransferOreHoldToJetCan";
            LogTrace(methodName, "jetCanEntityId: {0}", jetCanEntityId);

            var childWindow = Metatron.JetCan.GetChildWindow();
            if (childWindow == null) return;

            var cargoToMove = _meCache.Ship.Ship.GetOreHoldCargo();

            var freeVolume = childWindow.Capacity - childWindow.UsedCapacity;
            var quantitiesByItem = new Dictionary<IItem, int>();
            DetermineItemsToMoveByFreeVolume(cargoToMove, quantitiesByItem, freeVolume);

            TransferCargo(quantitiesByItem, MoveItemToJetCan, _crystalExclusionList);
        }
        #endregion

        #region -> Corporate Hangar Array
        public bool TransferCargoHoldToCorporateHangarArray(ICorporateHangarArray corporateHangarArray)
        {
            var methodName = "TransferCargoHoldToCorporateHangarArray";
            LogTrace(methodName);

            var freeCargoVolume = corporateHangarArray.Capacity - corporateHangarArray.UsedCapacity;

            var items = new List<IItem>(_meCache.Ship.Cargo);
            var quantitiesByItem = new Dictionary<IItem, int>();
            var movingEverything = DetermineItemsToMoveByFreeVolume(items, quantitiesByItem, freeCargoVolume);

            TransferCargo(quantitiesByItem, (item, quantity) => MoveItemToCorporateHangarArray(corporateHangarArray, item, quantity), null);

            return movingEverything;
        }

        public bool TransferOreInCargoHoldToCorporateHangarArray(ICorporateHangarArray corporateHangarArray)
        {
            var methodName = "TransferCargoHoldToCorporateHangarArray";
            LogTrace(methodName);

            var freeCargoVolume = corporateHangarArray.Capacity - corporateHangarArray.UsedCapacity;

            var items = new List<IItem>();
            var nonCrystalItems = _meCache.Ship.Cargo.
                Where(item => !_crystalExclusionList.Contains(item.GroupID));
            items.AddRange(nonCrystalItems);

            var quantitiesByItem = new Dictionary<IItem, int>();
            var movingEverything = DetermineItemsToMoveByFreeVolume(items, quantitiesByItem, freeCargoVolume);

            TransferCargo(quantitiesByItem, (item, quantity) => MoveItemToCorporateHangarArray(corporateHangarArray, item, quantity), _crystalExclusionList);

            return movingEverything;
        }

        public bool TransferOreHoldToCorporateHangarArray(ICorporateHangarArray corporateHangarArray)
        {
            var methodName = "TransferCargoHoldToCorporateHangarArray";
            LogTrace(methodName);

            var freeCargoVolume = corporateHangarArray.Capacity - corporateHangarArray.UsedCapacity;

            var items = _meCache.Ship.Ship.GetOreHoldCargo();
            var quantitiesByItem = new Dictionary<IItem, int>();
            var movingEverything = DetermineItemsToMoveByFreeVolume(items, quantitiesByItem, freeCargoVolume);

            TransferCargo(quantitiesByItem, (item, quantity) => MoveItemToCorporateHangarArray(corporateHangarArray, item, quantity), null);

            return movingEverything;
        }
        #endregion

        #region -> Station Corporate Hangars
        public bool TransferCargoHoldToStationCorporateHangars()
        {
            var methodName = "TransferCargoHoldToStationCorporateHangars";
            LogTrace(methodName);

            var items = new List<IItem>(_meCache.Ship.Cargo);

            TransferCargo(items, MoveItemToStationCorporateHangar, null, null);
            return true;
        }

        public bool TransferOreInCargoHoldToStationCorporateHangars()
        {
            var methodName = "TransferCargoHoldToStationCorporateHangars";
            LogTrace(methodName);

            var items = new List<IItem>();
            var nonCrystalItems = _meCache.Ship.Cargo.
                Where(item => !_crystalExclusionList.Contains(item.GroupID));
            items.AddRange(nonCrystalItems);

            TransferCargo(items, MoveItemToStationCorporateHangar, _crystalExclusionList, null);
            return true;
        }

        public bool TransferOreHoldToStationCorporateHangars()
        {
            var methodName = "TransferOreHoldToStationCorporateHangars";
            LogTrace(methodName);

            var items = _meCache.Ship.Ship.GetOreHoldCargo();

            TransferCargo(items, MoveItemToStationCorporateHangar, null, null);
            return true;
        }
        #endregion

        #region -> Station Hangar
        public void TransferCargoHoldToStationHangar()
        {
            var methodName = "TransferCargoHoldToStationHangar";
            LogTrace(methodName);

            var cargoToMove = new List<IItem>(_meCache.Ship.Cargo);

            TransferCargo(cargoToMove, MoveItemToStationHangar, null, null);
        }

        public void TransferCargoHoldToStationExcludingCategoryIds(params int[] categoryIds)
        {
            var methodName = "TransferCargoHoldToStationExcludingCategoryIds";
            LogTrace(methodName);

            var cargoToMove = _meCache.Ship.Cargo;

            TransferCargo(cargoToMove, MoveItemToStationHangar, null, categoryIds);
        }

        public void TransferOreInCargoHoldToStationHangar()
        {
            var methodName = "TransferOreInCargoHoldToStationHangar";
            LogTrace(methodName);

            var items = new List<IItem>();
            var nonCrystalItems = _meCache.Ship.Cargo.
                Where(item => !_crystalExclusionList.Contains(item.GroupID));
            items.AddRange(nonCrystalItems);


            TransferCargo(items, MoveItemToStationHangar, _crystalExclusionList, null);
        }

        public void TransferOreHoldToStationHangar()
        {
            var methodName = "TransferOreHoldToStationHangar";
            LogTrace(methodName);

            var cargoToMove = _meCache.Ship.Ship.GetOreHoldCargo();

            TransferCargo(cargoToMove, MoveItemToStationHangar, null, null);
        }
        #endregion

        private bool DetermineItemsToMoveByFreeVolume(IEnumerable<IItem> items, IDictionary<IItem, int> quantitiesByItem, double freeVolumeInDestination)
        {
            var methodName = "DetermineItemsToMoveByFreeVolume";
            LogTrace(methodName);

            var movingEverything = true;

            foreach (var item in items)
            {
                var itemStackVolumne = item.Quantity * item.Volume;
                if (freeVolumeInDestination > itemStackVolumne)
                {
                    quantitiesByItem.Add(item, item.Quantity);
                    freeVolumeInDestination -= itemStackVolumne;
                    LogMessage(methodName, LogSeverityTypes.Debug, "Preparing to move {0} of item {1} ({2}).",
                               item.Quantity, item.Name, item.ID);
                }
                else if (freeVolumeInDestination > item.Volume)
                {
                    var quantityToMove = (int)(freeVolumeInDestination / item.Volume);

                    quantitiesByItem.Add(item, quantityToMove);
                    freeVolumeInDestination -= quantityToMove * item.Volume;
                    LogMessage(methodName, LogSeverityTypes.Debug, "Preparing to move {0} of item {1} ({2}).",
                                quantityToMove, item.Name, item.ID);
                }
                else
                {
                    movingEverything = false;
                    LogMessage(methodName, LogSeverityTypes.Debug, "No more free space; done preparing to move items.");
                    break;
                }
            }
            return movingEverything;
        }

        /// <summary>
        /// Transfers items using the given Item movement method, excluding items matching the given GroupIDs.
        /// </summary>
        /// <param name="items">Items to be moved</param>
        /// <param name="itemMoveMethod">Method to be used for moving items</param>
        /// <param name="groupIdExclusions"></param>
        /// <param name="categoryIdExclusions"></param>
        private void TransferCargo(IEnumerable<IItem> items, Action<IItem> itemMoveMethod, IEnumerable<int> groupIdExclusions, IEnumerable<int> categoryIdExclusions)
        {
            var methodName = "TransferCargo";
            LogTrace(methodName);
            LogMessage(methodName, LogSeverityTypes.Debug, $"Transferring {items.Count()} items.");

            foreach (var item in items)
            {
                if (categoryIdExclusions != null && categoryIdExclusions.Contains(item.CategoryID)) continue;

                if (groupIdExclusions != null && groupIdExclusions.Contains(item.GroupID)) continue;

                _statistics.TrackIceOrOreMined(item.Type, item.Quantity);

                itemMoveMethod(item);
            }
        }

        /// <summary>
        /// Transfers a given quantity of items using the given Item movement method, excluding items matching the given GroupIDs.
        /// </summary>
        /// <param name="quantitiesByItem">Items to be moved, by quantity</param>
        /// <param name="itemMoveMethod">Method to be used for moving items</param>
        /// <param name="exceptionGroupIds">GroupIDs of items to exclude from movement</param>
        private void TransferCargo(IDictionary<IItem, int> quantitiesByItem, Action<IItem, int> itemMoveMethod, IEnumerable<int> exceptionGroupIds)
        {
            var methodName = "TransferCargo";
            LogTrace(methodName);

            foreach (var item in quantitiesByItem.Keys)
            {
                if (exceptionGroupIds != null && exceptionGroupIds.Contains(item.GroupID))
                    continue;

                _statistics.TrackIceOrOreMined(item.Type, quantitiesByItem[item]);

                itemMoveMethod(item, quantitiesByItem[item]);
            }
        }

        #region Item Movement Methods
        public void MoveItemToStationHangar(IItem item)
        {
            var methodName = "MoveItemToStationHangar";
            LogTrace(methodName);

            LogMessage(methodName, LogSeverityTypes.Debug, "Moving item \"{0}\" to station hangar.", item.Name);
            item.MoveTo(ToLocationNames.MyStationHangar.ToString(), ToDestinationNames.Hangar.ToString());
        }

        public void MoveItemToStationCorporateHangar(IItem item)
        {
            var methodName = "MoveItemToStationCorporateHangar";
            LogTrace(methodName);

            LogMessage(methodName, LogSeverityTypes.Debug, "Moving {0} of item \"{1}\" to station corporate hangar folder {2}.",
                item.Quantity, item.Name, _cargoConfiguration.DropoffLocation.HangarDivision);
            item.MoveTo(ToLocationNames.MyStationCorporateHangar.ToString(),
                ToDestinationNames.StationCorporateHangar.ToString(), item.Quantity, _cargoConfiguration.DropoffLocation.HangarDivision);
        }

        public void MoveItemToCorporateHangarArray(ICorporateHangarArray corporateHangarArray, IItem item, int quantity)
        {
            var methodName = "MoveItemToCorporateHangarArray";
            LogTrace(methodName);

            LogMessage(methodName, LogSeverityTypes.Debug, "Moving {0} of item \"{1}\" to corporate hangar array entity {2}, folder {3}.",
                quantity, item.Name, _cargoConfiguration.DropoffLocation.EntityID,
                _cargoConfiguration.DropoffLocation.HangarDivision);
            item.MoveTo(corporateHangarArray.CurrentContainer.ID, ToDestinationNames.CorpHangars.ToString(), quantity,
                _cargoConfiguration.DropoffLocation.HangarDivision);
        }

        public void MoveItemToJetCan(IItem item, int quantity)
        {
            var methodName = "MoveItemToJetCan";
            LogTrace(methodName);

            LogMessage(methodName, LogSeverityTypes.Debug, "Moving {0} of item \"{1}\" to jetcan entity {2}.",
                quantity, item.Name, Metatron.JetCan.CurrentContainerId);
            item.MoveTo(Metatron.JetCan.CurrentContainerId, ToDestinationNames.CargoHold.ToString(), quantity);
        }
        #endregion
        #endregion

        #region Mining Ammo functions
        public Item GetBestMiningCrystal(IEntityWrapper target, EVE.ISXEVE.Interfaces.IModule currentModule)
        {
            var methodName = "GetBestMiningCrystal";
            LogTrace(methodName, "Target: {0}, CurrentModule: {1}",
                target.ID, currentModule.ID);

            var type = Metatron.AsteroidBelts.AsteroidGroupsByGroupId[target.GroupID];

            var availableCharges = currentModule.GetAvailableAmmo()
                .Where(availableCharge => (availableCharge.GroupID == (int)GroupIDs.MiningCrystal || availableCharge.GroupID == (int)GroupIDs.MercoxitMiningCrystal) &&
                    availableCharge.Name.Contains(type))
                .OrderByDescending(availableCharge => BoolToInt(availableCharge.Name.EndsWith("II")))
                .ToList();

            //If there are results...
            if (availableCharges.Count > 0)
            {
                //Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                //methodName, String.Format("Returning best mining crystal \"{0}\".", cargo[0].Name)));
                var returnItem = availableCharges[0];
                availableCharges.Clear();
                return returnItem;
            }

            LogMessage(methodName, LogSeverityTypes.Debug, "No mining crystals matching \"{0}\" found; returning null.", type);
            //No good crystal, return null
            return null;
        }

        private void CacheChargeAndModuleInfoForFrame()
        {
            if (_meCache.InSpace)
            {
                _chargeVolumesByGroupId.Clear();
                _chargeTypeIdToGroupIdMap.Clear();
                _chargeNamesByTypeId.Clear();
                _fittedWeapons.Clear();

                var turretsLaunchersAndLasers = _turretModules.Union(_launcherModules).Union(_miningLaserModules);

                foreach (var module in turretsLaunchersAndLasers)
                {
                    var charge = module.Charge;
                    var moduleGroupId = module.ToItem.GroupID;

                    var itemStub = new ItemStub(moduleGroupId, (int)module.ChargeSize.GetValueOrDefault(0),
                                                module.ToItem.Name);
                    _fittedWeapons.Add(itemStub);

                    if (!LavishScriptObject.IsNullOrInvalid(charge))
                        CacheModuleCharge(charge);
                }
            }

            var allCharges = _meCache.Ship.Cargo.Union(_meCache.HangarItems).Where(item => item.CategoryID == (int)CategoryIDs.Charge);
            foreach (var charge in allCharges)
            {
                CacheChargeItem(charge);
            }
        }

        private void CacheChargeItem(IItem charge)
        {
            var typeId = charge.TypeID;
            var groupId = charge.GroupID;

            if (!_chargeNamesByTypeId.ContainsKey(typeId))
                _chargeNamesByTypeId.Add(typeId, charge.Name);

            if (!_chargeTypeIdToGroupIdMap.ContainsKey(typeId))
                _chargeTypeIdToGroupIdMap.Add(typeId, groupId);

            if (!_chargeVolumesByGroupId.ContainsKey(groupId))
                _chargeVolumesByGroupId.Add(groupId, charge.Volume);
        }

        private void CacheModuleCharge(ModuleCharge charge)
        {
            var typeId = charge.TypeId;
            var groupId = charge.GroupId;

            if (!_chargeNamesByTypeId.ContainsKey(typeId))
                _chargeNamesByTypeId.Add(typeId, charge.Type);

            if (!_chargeTypeIdToGroupIdMap.ContainsKey(typeId))
                _chargeTypeIdToGroupIdMap.Add(typeId, groupId);

            if (!_chargeVolumesByGroupId.ContainsKey(groupId))
                _chargeVolumesByGroupId.Add(groupId, charge.Volume);
        }

        private void CacheAllChargeAndModuleInfo()
        {
            var methodName = "CacheAllChargeAndModuleInfo";
            LogTrace(methodName);

            AbleToSwapAmmoLoadout = true;

            _turretCount = _turretModules.Count;

            ChargeQuantities.Clear();
            ChargeTypesUsed.Clear();

            _loadedChargeCountByGroupId.Clear();
            _loadedChargeCountByTypeId.Clear();
            _loadedChargesByTypeId.Clear();

            _totalWeaponChargeVolumeCapacity = 0;

            var allCharges = GetChargesInCargo();

            var validTurrets = from module in _turretModules
                               where !LavishScriptObject.IsNullOrInvalid(module.Charge)
                               select module;

            CacheModuleInfoAndCharges(_turretModules);

            var validLaunchers = from module in _launcherModules
                                 where !LavishScriptObject.IsNullOrInvalid(module.Charge)
                                 select module;

            CacheModuleInfoAndCharges(_launcherModules);

            var loadedCharges = _miningLaserModules.Where(item => !LavishScriptObject.IsNullOrInvalid(item.Charge)).Select(item => item.Charge).ToList();

            loadedCharges.AddRange(validTurrets.Select(module => module.Charge));

            loadedCharges.AddRange(validLaunchers.Select(module => module.Charge));

            foreach (var loadedCharge in loadedCharges)
            {
                var typeId = loadedCharge.TypeId;

                if (!_loadedChargesByTypeId.ContainsKey(typeId))
                    _loadedChargesByTypeId.Add(typeId, new ItemStub(loadedCharge.GroupId, loadedCharge.ChargeSize, loadedCharge.Type) { TypeId = typeId });
            }

            _totalWeaponChargeVolumeCapacity += validTurrets.Sum(module => module.MaxCharges * module.Charge.Volume);
            _totalWeaponChargeVolumeCapacity += validLaunchers.Sum(module => module.MaxCharges * module.Charge.Volume);

            foreach (var item in loadedCharges)
            {
                var typeId = item.TypeId;
                var groupId = item.GroupId;

                if (!_loadedChargeCountByTypeId.ContainsKey(typeId))
                    _loadedChargeCountByTypeId.Add(typeId, item.Quantity);
                else
                    _loadedChargeCountByTypeId[typeId] += item.Quantity;

                if (!_loadedChargeCountByGroupId.ContainsKey(groupId))
                    _loadedChargeCountByGroupId.Add(groupId, item.Quantity);
                else
                    _loadedChargeCountByGroupId[groupId] += item.Quantity;
            }

            CacheModuleChargeTypesAndQuantities(loadedCharges);

            CacheChargeTypesAndQuantities(allCharges);
        }

        private void CacheModuleInfoAndCharges(IEnumerable<EVE.ISXEVE.Interfaces.IModule> modules)
        {
            foreach (var module in modules)
            {
                var groupId = module.ToItem.GroupID;

                if (!_moduleFullLoadQuantityByGroupId.ContainsKey(groupId))
                    _moduleFullLoadQuantityByGroupId.Add(groupId, module.MaxCharges);
                else
                    _moduleFullLoadQuantityByGroupId[groupId] += module.MaxCharges;

                AddAllowedChargesForModule(module);
            }
        }

        private void CacheChargeTypesAndQuantities(IEnumerable<IItem> charges)
        {
            var methodName = "CacheChargeTypesAndQuantities";
            LogTrace(methodName);

            foreach (var item in charges)
            {
                var quantity = Math.Abs(item.Quantity);

                var typeId = item.TypeID;

                if (!ChargeTypesUsed.Contains(typeId))
                {
                    ChargeTypesUsed.Add(typeId);
                    ChargeQuantities.Add(quantity);
                }
                else
                {
                    ChargeQuantities[ChargeTypesUsed.IndexOf(typeId)] += quantity;
                }
            }
        }

        private void CacheModuleChargeTypesAndQuantities(IEnumerable<ModuleCharge> charges)
        {
            var methodName = "CacheModuleChargeTypesAndQuantities";
            LogTrace(methodName);

            foreach (var item in charges)
            {
                var quantity = Math.Abs(item.Quantity);

                var typeId = item.TypeId;

                if (!ChargeTypesUsed.Contains(typeId))
                {
                    ChargeTypesUsed.Add(typeId);
                    ChargeQuantities.Add(quantity);
                }
                else
                {
                    ChargeQuantities[ChargeTypesUsed.IndexOf(typeId)] += quantity;
                }
            }
        }

        private void AddAllowedChargesForModule(EVE.ISXEVE.Interfaces.IModule module)
        {
            foreach (var availableCharge in module.GetAvailableAmmo())
            {
                var newAllowedCharge = new ItemStub(availableCharge.GroupID, availableCharge.ChargeSize, availableCharge.Name) { ModuleGroupId = module.ToItem.GroupID };

                var matchFound = _allowedCharges
                    .Any(allowedCharge => allowedCharge.GroupId == newAllowedCharge.GroupId && allowedCharge.ChargeSize == newAllowedCharge.ChargeSize);

                if (!matchFound)
                    _allowedCharges.Add(newAllowedCharge);
            }
        }

        private bool _chargesCachedThisRearm;

        /// <summary>
        /// Attempt to rearm mining crystals. This is a multi-invocation process.
        /// </summary>
        /// <param name="corporateHangarArray"></param>
        /// <returns>true when complete; false otherwise</returns>
        public bool RearmMiningCrystals(ICorporateHangarArray corporateHangarArray)
        {
            var methodName = "RearmMiningCrystals";
            LogTrace(methodName, "UseStation: {0}", corporateHangarArray == null);

            if (corporateHangarArray != null && !_chargesCachedThisRearm)
            {
                if (!IsCargoHoldActive)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard,
                                "Making ship cargo hold active to determine how many crystals we're carrying.");
                    MakeCargoHoldActive();
                    return false;
                }

                CacheAllChargeAndModuleInfo();

                _chargesCachedThisRearm = true;
            }

            if (ChargeTypesUsed.Count == 0)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "We have no used charge types.");
                _chargesCachedThisRearm = false;
                return true;
            }

            List<IItem> itemSource = null;
            if (corporateHangarArray == null)
            {
                if (!Metatron.Station.IsStationHangarActive)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Making the station hangar active before loading crystals.");
                    Metatron.Station.MakeStationHangarActive();
                    return false;
                }

                itemSource = _meCache.HangarItems.ToList();
            }
            else
            {
                if (corporateHangarArray.IsCurrentContainerWindowActive)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Making the corporate hangar array window active before loading crystals.");
                    corporateHangarArray.MakeCurrentContainerWindowActive();
                    return false;
                }

                itemSource = corporateHangarArray.CurrentContainer.ToEntity.GetCargo();
            }

            var targetCrystals = Metatron.Config.MiningConfig.NumCrystalsToCarry;

            //We know what we have, now tell us what we need
            var loadedAllNeededCrystals = true; //Default to true; set to false if we end up loading a partial stack
            var neededCrystals = false;
            foreach (var item in itemSource)
            {
                if (!ChargeTypesUsed.Contains(item.TypeID))
                    continue;

                //Ok, we have a surplus of this crystal. Compare how many we have to the target crystal #
                //to figure out how many we need.
                var neededQuantity = targetCrystals - ChargeQuantities[ChargeTypesUsed.IndexOf(item.TypeID)];

                //Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                //methodName, String.Format("Needed crystals of type {0}: {1} ({2} - {3})",
                //i.TypeID, needed, targetCrystals, ChargeQuantities)));

                if (neededQuantity <= 0)
                    continue;

                neededCrystals = true;
                var crystalQuantity = Math.Abs(item.Quantity);
                if (neededQuantity > crystalQuantity)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Grabbing {0} of crystal {1} ({2})",
                        crystalQuantity, item.Name, item.TypeID);
                    ChargeQuantities[ChargeTypesUsed.IndexOf(item.TypeID)] += crystalQuantity;
                    _statistics.TrackCrystalUsed(item.Type, crystalQuantity);
                    item.MoveTo(ToLocationNames.MyShip.ToString(), ToDestinationNames.CargoHold.ToString(), crystalQuantity);
                    loadedAllNeededCrystals = false;
                }
                else
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Grabbing {0} of crystal {1} ({2})",
                        neededQuantity, item.Name, item.TypeID);
                    ChargeQuantities[ChargeTypesUsed.IndexOf(item.TypeID)] += neededQuantity;
                    _statistics.TrackCrystalUsed(item.Type, neededQuantity);
                    item.MoveTo(ToLocationNames.MyShip.ToString(), ToDestinationNames.CargoHold.ToString(), neededQuantity);
                }
                break;
            }

            if (neededCrystals && !loadedAllNeededCrystals)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Warning: Could not find all needed mining crystals.");
            }

            _chargesCachedThisRearm = false;
            return true;
        }
        #endregion

        public double OreHoldCapacity
        {
            get
            {
                var childWindow = GetOreHoldChildWindow();
                if (childWindow == null) return -1;

                return childWindow.Capacity;
            }
        }

        public double UsedOreHoldCapacity
        {
            get
            {
                var childWindow = GetOreHoldChildWindow();
                if (childWindow == null) return -1;

                return childWindow.UsedCapacity;
            }
        }

        public void MakeCargoHoldActive()
        {
            //Only open cargo if it isn't already open
            if (IsCargoHoldActive) return;

            var childWindow = GetCargoHoldChildWindow();
            if (childWindow == null) return;

            childWindow.MakeActive();
        }

        public void StackInventory()
        {
            var inventoryWindow = _eveWindowProvider.GetInventoryWindow();
            inventoryWindow.StackAll();
        }

        public double MaxTargetRange
        {
            get
            {
                return _meCache.Ship.MaxTargetRange;
            }
        }

        public double MaxLockedTargets
        {
            get
            {
                return _meCache.MaxLockedTargets < _meCache.Ship.MaxLockedTargets ?
                    _meCache.MaxLockedTargets : _meCache.Ship.MaxLockedTargets;
            }
        }

        public double MaximumMiningRange
        {
            get
            {
                //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                //    String.Format("MaximumMiningRange: Mining Lasers - {0}, Is Valid - {1}, OptimalRange - {2}",
                //    Modules_MiningLasers.Count, Modules_MiningLasers[0].IsValid, Modules_MiningLasers[0].OptimalRange)));
                return _maxMiningRange;
            }
        }

        public double MaxSlowboatDistance
        {
            get
            {
                return (_movementConfiguration.MaxSlowboatTime * _meCache.ToEntity.MaxVelocity) +
                MaximumMiningRange * 0.9;
            }
        }

        public PrimaryWeaponPlatforms PrimaryWeaponPlatform
        {
            get
            {
                if (_turretModules.Count == 0 && _launcherModules.Count == 0 && _precursorModules.Count == 0)
                    return PrimaryWeaponPlatforms.Drones;
                else if (_precursorModules.Count > 0) return PrimaryWeaponPlatforms.Precursor;
                else return _turretModules.Count > 0 ? PrimaryWeaponPlatforms.Turrets : PrimaryWeaponPlatforms.Missiles;
            }
        }

        #region Ammo-related members and methods

        public bool IsAmmoAvailable
        {
            get
            {
                //If we have no weapons, it's hard to have ammo. Retun true in such a case.
                var haveAmmo = true;

                var quantitiesByTypeId = new Dictionary<int, int>();

                //Iterate all weapons, pouplate the typeID_quantity dict
                foreach (var typeId in _allWeaponModules.Select(module => module.ToItem.TypeID))
                {
                    if (quantitiesByTypeId.ContainsKey(typeId))
                    {
                        quantitiesByTypeId[typeId]++;
                    }
                    else
                    {
                        quantitiesByTypeId.Add(typeId, 1);
                    }
                }

                //Again iterate all weapons, checking available charges for a weapon against all modules
                //of that type, making sure I have enough for a full reload.
                foreach (var module in _allWeaponModules)
                {
                    var toItem = module.ToItem;
                    var typeId = toItem.TypeID;

                    //If this is an energy weapon with a Tech 1 crystal, don't worry about it.
                    if (toItem.GroupID == (int)GroupIDs.EnergyWeapon &&
                        !LavishScriptObject.IsNullOrInvalid(module.Charge) && module.Charge.GroupId == (int)GroupIDs.FrequencyCrystal)
                        continue;

                    if (!quantitiesByTypeId.ContainsKey(typeId))
                        continue;

                    var requiredQuantity = module.MaxCharges * quantitiesByTypeId[typeId];
                    var availableAmmo = module.GetAvailableAmmo();

                    if (availableAmmo == null)
                    {
                        LogMessage("IsAmmoAvailable", LogSeverityTypes.Debug, "Error: Module {0} ({1}) GetAvailableAmmo() returned null.",
                            toItem.Name, module.ID);
                        continue;
                    }

                    var enoughAmmoFound = availableAmmo.Any(item => Math.Abs(item.Quantity) >= requiredQuantity);

                    if (!enoughAmmoFound)
                    {
                        haveAmmo = false;
                        break;
                    }

                    quantitiesByTypeId.Remove(typeId);
                }

                return haveAmmo;
            }
        }

        /*
         * I'm going to want to only calculate maximums, minimums, and optimals once.
         * I'll want to keep track of what range modifier gives me a certain range.
         * That info should be stored in a lookup table.
         */

        /// <summary>
        /// Iterate all turret modules on the current ship and calculate the optimal
        /// range bands for turret and for each ammo type available for the turret.
        /// </summary>
        /// <returns></returns>
        public void CalculateTurretOptimalRangeBands()
        {
            var methodName = "CalculateTurretOptimalRangeBands";
            LogTrace(methodName);

            DictionariesByModuleType.Clear();

            var successfullyBuiltTables = true;

            //for each UNIQUE turret
            var lastModuleTypeId = 0;
            foreach (var module in _turretModules)
            {
                var moduleItem = module.ToItem;
                var moduleType = moduleItem.Type;
                var moduleTypeId = moduleItem.TypeID;

                var availableCharges = GetChargesAvailableForModule(module);
                if (availableCharges.Count == 0)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Module has no available charges. ModuleItem: {0}, ModuleType: {1}, ModuleTypeId: {2}",
                        moduleItem, moduleType, moduleTypeId);
                    successfullyBuiltTables = false;
                    continue;
                }

                if (lastModuleTypeId == 0)
                {
                    lastModuleTypeId = moduleTypeId;
                }
                else if (lastModuleTypeId == moduleTypeId)
                {
                    //Dont' need to do this again for a turret we already have
                    continue;
                }

                //A secondary check for duplicate entries
                if (DictionariesByModuleType.ContainsKey(moduleType))
                {
                    continue;
                }

                //Get the base optimal for this module
                var baseOptimal = GetBaseOptimalRangeFromModule(module);
                var accuracyFalloff = module.AccuracyFalloff.GetValueOrDefault(0);

                if (GangLinkModules.Count > 0)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Bastion detected - all ranges are up by 25%");
                    baseOptimal *= 1.25;
                    accuracyFalloff *= 1.25;
                }

                //Add all relevant info.
                var ammoMinimumRanges = new Dictionary<int, double>();
                var ammoMaximumRanges = new Dictionary<int, double>();

                foreach (var charge in availableCharges)
                {
                    //If the key has already been added, skip
                    if (ammoMaximumRanges.ContainsKey(charge.TypeID) ||
                        ammoMinimumRanges.ContainsKey(charge.TypeID))
                    {
                        continue;
                    }

                    //If we're checking the currently loaded charge get it from the module
                    var rangeModifier = charge.ID == module.Charge.Id ?
                        GetRangeModifierFromModule(module) : GetRangeModifierFromCharge(charge);

                    double minimumRange = baseOptimal * rangeModifier * 0.75;
                    double maximumRange = baseOptimal * rangeModifier + module.AccuracyFalloff.GetValueOrDefault(0);

                    LogMessage(methodName, LogSeverityTypes.Debug, "Calculated min range of {0} for charge {1} ({2} * {3} * 0.5)",
                        minimumRange, charge.Name, baseOptimal, rangeModifier);
                    LogMessage(methodName, LogSeverityTypes.Debug, "Calculated max range of {0} for charge {1} ({2} * {3} + {4})",
                        maximumRange, charge.Name, baseOptimal, rangeModifier, accuracyFalloff);

                    ammoMinimumRanges.Add(charge.TypeID, minimumRange);
                    ammoMaximumRanges.Add(charge.TypeID, maximumRange);
                }

                var dictionaries = new List<Dictionary<int, double>> { ammoMinimumRanges, ammoMaximumRanges };
                DictionariesByModuleType.Add(moduleType, dictionaries);
            }

            if (successfullyBuiltTables)
                _dictionariesByModuleNameBuilt = true;
        }

        /// <summary>
        /// Lookup the base optimal range for a given module, calculating it first if necessary and storing the result.
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public double GetBaseOptimalRangeFromModule(EVE.ISXEVE.Interfaces.IModule module)
        {
            var methodName = "GetBaseOptimalRangeFromModule";
            LogTrace(methodName, "Module: {0} ({1})", module.ToItem.Name, module.ToItem.TypeID);

            var typeId = module.ToItem.TypeID;

            //If it's already defined just return the defined result
            if (ModuleBaseOptimaRanges.ContainsKey(typeId))
                return ModuleBaseOptimaRanges[typeId];

            var currentOptimalRange = module.OptimalRange.GetValueOrDefault(0); ;
            //if there's a loaded charge...
            if (!LavishScriptObject.IsNullOrInvalid(module.Charge))
            {
                //First get the charge range modifier
                var chargeRangeMod = GetRangeModifierFromModule(module);

                //Use it to find the real base optimal range
                var baseOptimalRange = currentOptimalRange / chargeRangeMod;
                LogMessage(methodName, LogSeverityTypes.Debug, "Calculated base optimal range for module \"{0}\" ({1}) to be {2} ({3} / {4}).",
                    module.ToItem.Name, typeId, baseOptimalRange, currentOptimalRange, chargeRangeMod);
                ModuleBaseOptimaRanges.Add(typeId, baseOptimalRange);
                return baseOptimalRange;
            }

            //Just add/return the current optimal
            LogMessage(methodName, LogSeverityTypes.Debug, "Calculated base optimal range for module \"{0}\" ({1}) to be {2}.",
                module.ToItem.Name, typeId, currentOptimalRange);
            ModuleBaseOptimaRanges.Add(typeId, currentOptimalRange);
            return currentOptimalRange;
        }

        public double GetRangeModifierFromCharge(IItem charge)
        {
            return GetRangeModifierFromCharge(charge.Name, charge.GroupID);
        }

        /// <summary>
        /// Look up the given charge in the various dictionaries and find the appropriate range modifier.
        /// </summary>
        public double GetRangeModifierFromCharge(string chargeName, int chargeGroupId)
        {
            var methodName = "GetRangeModifierFromCharge";
            LogTrace(methodName, "{0}, {1}", chargeName, chargeGroupId);

            //Use the charge's type to determine the dictionary to look up from
            switch (chargeGroupId)
            {

                //Projectiles
                case (int)GroupIDs.ProjectileCharge:
                case (int)GroupIDs.AdvancedArtilleryCharge:
                case (int)GroupIDs.AdvancedAutoCannonCharge:
                    return GetRangeModByChargeName(_projectileRangeModsByName, chargeName);
                //Precursor Plasma
                case (int)GroupIDs.AdvancedExoticPlasmaCharge:
                case (int)GroupIDs.ExoticPlasmaCharge:
                    return GetRangeModByChargeName(_precursorRangeModsByName, chargeName);
                //Hybrids
                case (int)GroupIDs.HybridCharge:
                case (int)GroupIDs.AdvancedBlasterCharge:
                case (int)GroupIDs.AdvancedRailGunCharge:
                    return GetRangeModByChargeName(_hybridRangeModsByName, chargeName);
                //Lasers
                case (int)GroupIDs.FrequencyCrystal:
                case (int)GroupIDs.AdvancedPulseLaserCrystal:
                case (int)GroupIDs.AdvancedBeamLaserCrystal:
                    return GetRangeModByChargeName(_frequencyRangeModsByName, chargeName);
                case (int)GroupIDs.CruiseMissile:
                case (int)GroupIDs.Torpedo:
                case (int)GroupIDs.LightMissile:
                case (int)GroupIDs.HeavyMissile:
                case (int)GroupIDs.AdvancedTorpedo:
                case (int)GroupIDs.AdvancedCruiseMissile:
                    return GetRangeModByChargeName(_missileRangeModsByName, chargeName);
            }
            return -1;      //error
        }

        /// <summary>
        /// Look up the correct dictionary for the given module.
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public double GetRangeModifierFromModule(EVE.ISXEVE.Interfaces.IModule module)
        {
            return GetRangeModByChargeName(GetChargeModDictionaryFromModuleGroup(module), module.Charge.Type);
        }

        /// <summary>
        /// Get the charge dictionary from a module's group.
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public Dictionary<string, double> GetChargeModDictionaryFromModuleGroup(EVE.ISXEVE.Interfaces.IModule module)
        {
            var methodName = "GetChargeModDictionaryFromModuleGroup";
            LogTrace(methodName, "{0} ({1})", module.ToItem.Name, module.ToItem.GroupID);

            switch (module.ToItem.GroupID)
            {
                case (int)GroupIDs.ProjectileWeapon:
                    return _projectileRangeModsByName;
                case (int)GroupIDs.EnergyWeapon:
                    return _frequencyRangeModsByName;
                case (int)GroupIDs.HybridWeapon:
                    return _hybridRangeModsByName;
                case (int)GroupIDs.PrecursorWeapon:
                    return _precursorRangeModsByName;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Search the given charge dictionary for a match for the given charge name.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="fullKey"></param>
        /// <returns></returns>
        public double GetRangeModByChargeName(Dictionary<string, double> dictionary, string fullKey)
        {
            var methodName = "GetRangeModByChargeName";
            LogTrace(methodName, "{0}", fullKey);

            foreach (var partialKey in dictionary.Keys.ToList().Where(partialKey => fullKey.Contains(partialKey)))
            {
                return dictionary[partialKey];
            }
            LogMessage(methodName, LogSeverityTypes.Debug, "Error: Could not find match for charge \"{0}\" in dictionary.", fullKey);
            return 1.0;
        }

        /// <summary>
        /// Check the optimal range bands for a given turret to see if the given distance matches,
        /// accounting for available ammo.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="distance"></param>
        /// <param name="damageProfile"></param>
        /// <returns>-1 if no bands match, otherwise typeID of matching ammo</returns>
        public int CheckTurretOptimalRangeBands(EVE.ISXEVE.Interfaces.IModule module, double distance, DamageProfile damageProfile)
        {
            var methodName = "CheckTurretOptimalRangeBands";
            LogTrace(methodName, "Module: {0}, Distance: {1}", module.ID, distance);

            var moduleName = module.ToItem.Name;
            var ammoMinimumRanges = DictionariesByModuleType[moduleName][0];
            var ammoMaximumRanges = DictionariesByModuleType[moduleName][1];

            var availableCharges = GetChargesAvailableForModule(module);
            RemoveChargesWithoutAtLeastOneFullLoad(availableCharges, module.MaxCharges);

            //Track the typeID of the best match for the range bands and the
            //highest range type in case there is no best match found, meaning target's out of range
            var bestMatchingChargeTypeId = -1;
            var bestMatchingChargePctDamageMatch = 0d;
            var highestRangeTypeId = -1;

            //Base ranges are up by 25% if we have a bastion
            var moduleBaseOptimal = GangLinkModules.Count > 0 ? GetBaseOptimalRangeFromModule(module) * 1.25 : GetBaseOptimalRangeFromModule(module);
            var closestOptimal = -1d;
            foreach (var item in availableCharges)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Available charge: {0} ({1} - {2})",
                    item.Name, ammoMinimumRanges.ContainsKey(item.TypeID) ? ammoMinimumRanges[item.TypeID] : -1,
                    ammoMaximumRanges.ContainsKey(item.TypeID) ? ammoMaximumRanges[item.TypeID] : -1);
                var typeId = item.TypeID;
                if (!ammoMinimumRanges.ContainsKey(typeId) || ammoMaximumRanges[typeId] < distance)
                    continue;

                //Target -must- be in or below range band.
                //So, the max range must be >= the given distance because it's in range until past that point.
                //IF this is true, set the charge if...
                //current best charge isn't set OR
                //delta between this charge's minimum range and given distance smaller than delta of best charge

                var currentChargeOptimal = GetRangeModifierFromCharge(item) * moduleBaseOptimal;
                var currentChargePctDamageMatch = GetChargeDamageProfileFromTypeID(typeId).GetPercentMatchAgainstResistances(damageProfile);

                if (bestMatchingChargeTypeId == -1 || Math.Abs(currentChargeOptimal - distance) < Math.Abs(closestOptimal - distance))
                {
                    //Update the best match
                    bestMatchingChargeTypeId = typeId;
                    bestMatchingChargePctDamageMatch = currentChargePctDamageMatch;
                    closestOptimal = currentChargeOptimal;

                    LogMessage(methodName, LogSeverityTypes.Debug, "Current best range match typeID {0} is {1}...",
                        distance, bestMatchingChargeTypeId);
                }
                else if (Math.Abs(currentChargeOptimal - distance) == Math.Abs(closestOptimal - distance) &&
                    (bestMatchingChargePctDamageMatch == 0 || currentChargePctDamageMatch > bestMatchingChargePctDamageMatch))
                {
                    //Update the best match
                    bestMatchingChargeTypeId = typeId;
                    bestMatchingChargePctDamageMatch = currentChargePctDamageMatch;
                    closestOptimal = currentChargeOptimal;

                    LogMessage(methodName, LogSeverityTypes.Debug, "Current best damagetype match typeID {0} is {1}...",
                        distance, bestMatchingChargeTypeId);
                }

                //I should also track the current highest range incase there is no best (meaning target out of range)
                if (highestRangeTypeId != -1 && ammoMaximumRanges[typeId] <= ammoMaximumRanges[highestRangeTypeId])
                    continue;

                highestRangeTypeId = typeId;
                LogMessage(methodName, LogSeverityTypes.Debug, "Current highest range typeID is {0}...",
                    highestRangeTypeId);
            }

            //If there's a best match, return it. Otherwise, return highest range.
            return bestMatchingChargeTypeId >= 0 ? bestMatchingChargeTypeId : highestRangeTypeId;
        }

        /// <summary>
        /// Change a given turret's charge to the given type
        /// </summary>
        /// <param name="module"></param>
        /// <param name="typeId"></param>
        public void ChangeTurretAmmo(EVE.ISXEVE.Interfaces.IModule module, int typeId)
        {
            var methodName = "ChangeTurretAmmo";
            LogTrace(methodName, "Module: {0}, TypeID: {1}", module.ID, typeId);

            var availableCharges = module.GetAvailableAmmo();

            foreach (var item in availableCharges.Where(item => item.TypeID == typeId))
            {
                var quantity = item.Quantity > module.MaxCharges ? module.MaxCharges : item.Quantity;
                LogMessage(methodName, LogSeverityTypes.Debug, "Changing module \'{0}\' ammo to \'{1}\' ({2} charges).",
                    module.ToItem.Name, item.Name, quantity);
                module.ChangeAmmo(item.ID, quantity);

                //Set the time of ammo change
                SetModuleLastAmmoChange(module);
                break;
            }
        }

        /// <summary>
        /// Determine if a given module has recently changed ammo.
        /// </summary>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        public bool DidModuleRecentlyChangeAmmo(Int64 moduleId)
        {
            var methodName = "DidModuleRecentlyChangeAmmo";
            LogTrace(methodName, "Module: {0}", moduleId);

            if (_nextAmmoChangeByModuleID.ContainsKey(moduleId))
            {
                return DateTime.Now.CompareTo(_nextAmmoChangeByModuleID[moduleId]) < 0;
            }
            return false;
        }

        /// <summary>
        /// Set the time of the last ammo change for a given module.
        /// </summary>
        private void SetModuleLastAmmoChange(EVE.ISXEVE.Interfaces.IModule module)
        {
            var methodName = "SetModuleLastAmmoChange";
            LogTrace(methodName, "Module: {0}", module.ID);

            DateTime timeOfNextAmmoChange;
            //If it's a laser of some type, just delay by one second. Otherwise, delay the full delay.
            var groupID = module.ToItem.GroupID;

            if (groupID == (int)GroupIDs.FrequencyMiningLaser || groupID == (int)GroupIDs.EnergyWeapon)
            {
                timeOfNextAmmoChange = DateTime.Now.AddSeconds(SECONDS_BETWEEN_CRYSTAL_AMMO_CHANGES);
            }
            else
            {
                timeOfNextAmmoChange = DateTime.Now.AddSeconds(SECONDS_BETWEEN_STANDARD_AMMO_CHANGES);
            }

            if (_nextAmmoChangeByModuleID.ContainsKey(module.ID))
            {
                _nextAmmoChangeByModuleID[module.ID] = timeOfNextAmmoChange;
            }
            else
            {
                _nextAmmoChangeByModuleID.Add(module.ID, timeOfNextAmmoChange);
            }
        }

        /// <summary>
        /// Reload all modules needing reloaded.
        /// Note: Should only be called when we're in a spot nothing else will be using turrets, like while warping.
        /// </summary>
        public void ReloadAllModules()
        {
            var methodName = "ReloadAllModules";
            LogTrace(methodName);

            foreach (var module in _allWeaponModules.Where(
                module => !module.IsActive && !DidModuleRecentlyChangeAmmo(module.ID) &&
                    (module.Charge == null || module.Charge.Quantity < module.MaxCharges)))
            {
                Int64 chargeItemId;

                var availableAmmo = module.GetAvailableAmmo();
                if (availableAmmo == null)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Error: Module.GetAvailableAmmo() returned null.");
                    return; //If one module returns null, they'll likely all return null
                }

                if (availableAmmo.Count == 0)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Warning: Module {0} ({1}) has no available ammo.",
                        module.ToItem.Name, module.ID);
                    continue;
                }

                if (module.Charge == null)
                {
                    var charge = availableAmmo.First();

                    chargeItemId = charge.ID;
                }
                else
                {
                    var matchingItem = availableAmmo.FirstOrDefault(item => item.TypeID == module.Charge.TypeId);

                    if (matchingItem == null)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Warning: Module {0} ({1}) has no more of the currently loaded charge.",
                            module.ToItem.Name, module.ID);

                        matchingItem = availableAmmo.First();
                    }

                    chargeItemId = matchingItem.ID;
                }

                module.ChangeAmmo(chargeItemId);
                SetModuleLastAmmoChange(module);
            }
        }

        /// <summary>
        /// Get all charges available for a given module including any loaded charge.
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public List<IItem> GetChargesAvailableForModule(EVE.ISXEVE.Interfaces.IModule module)
        {
            var methodName = "GetChargesAvailableForModule";
            LogTrace(methodName, "Module: {0}, ModuleName: {1}", module.ID, module.ToItem.Name);

            var items = new List<IItem>();

            var availableAmmo = module.GetAvailableAmmo();

            if (availableAmmo != null)
                items.AddRange(availableAmmo);

            if (items.Count == 0)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "No charges available for module.");
            }

            //Commented out due to changes in ISXEVE .0622 - shouldn't be necessary anyway

            //if (LavishScriptObject.IsNullOrInvalid(module.Charge))
            //    return items;

            //var currentChargeTypeId = module.Charge.TypeId;
            //var foundCurrentCharge = items.Any(item => item.TypeID == currentChargeTypeId);

            //if (!foundCurrentCharge)
            //{
            //    items.Add(module.Charge);
            //}

            return items;
        }

        public Dictionary<int, int> GetChargesAvailableForModuleByTypeId(Module module)
        {
            var availableCharges = GetChargesAvailableForModule(module);

            var availableChargesByTypeId = new Dictionary<int, int>();

            foreach (var availableCharge in availableCharges)
            {
                var typeId = availableCharge.TypeID;

                if (!availableChargesByTypeId.ContainsKey(typeId))
                    availableChargesByTypeId.Add(typeId, 0);

                availableChargesByTypeId[typeId] += availableCharge.Quantity;
            }

            return availableChargesByTypeId;
        }

        public void RemoveChargesWithoutAtLeastOneFullLoad(List<IItem> availableCharges, int fullLoadQuantity)
        {
            var methodName = "RemoveChargesWithoutAtLeastOneFullLoad";
            LogTrace(methodName, "AvailableChargeCount: {0}, FullLoadQuantity: {1}", availableCharges.Count, fullLoadQuantity);

            for (var index = 0; index < availableCharges.Count; index++)
            {
                var availableCharge = availableCharges[index];

                var sum = availableCharges.Where(charge => charge.TypeID == availableCharge.TypeID)
                    .Sum(charge => charge.Quantity);

                if (sum >= fullLoadQuantity) continue;

                LogMessage(methodName, LogSeverityTypes.Debug, "Removing charge \"{0}\" from available charges. There are only {1} of it available and we need {2}.",
                    availableCharge.Name, sum, fullLoadQuantity);
                availableCharges.RemoveAt(index);
                index--;
            }
        }

        /// <summary>
        /// Todo: Move this to Ship and call it from there. MissionProcessor will use it to calculate whether
        /// or not closest target is in turret range if we're using turrets.
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="damageProfile"></param>
        /// <returns></returns>
        public Dictionary<int, int> GetAmmoTypeIdsByModuleTypeId(double distance, DamageProfile damageProfile)
        {
            var methodName = "GetAmmoTypeIdsByModuleTypeID";
            LogTrace(methodName, "Distance: {0}", distance);

            var returnValue = new Dictionary<int, int>();

            foreach (var module in _turretModules)
            {
                if (returnValue.ContainsKey(module.ToItem.TypeID))
                    continue;

                var bestTypeId = CheckTurretOptimalRangeBands(module, distance, damageProfile);
                LogMessage(methodName, LogSeverityTypes.Debug, "Determined best charge typeID for the given target range of {0} is {1}.",
                    distance, bestTypeId);
                returnValue.Add(module.ToItem.TypeID, bestTypeId);
            }

            return returnValue;
        }

        /// <summary>
        /// Return the estimated maximum range for missiles.
        /// </summary>
        public double MaximumMissileRange(EVE.ISXEVE.Interfaces.IModule launcher)
        {
            var methodName = "MaximumMissileRange";
            LogTrace(methodName, "Module: {0}, ModuleName: {1}", launcher.ID, launcher.ToItem.Name);

            if (launcher.IsValid)
            {
                return GetMaximumRangeForMissileModuleCharge(launcher.Charge);
            }

            LogMessage(methodName, LogSeverityTypes.Debug, "Module invalid or null!");
            return 0;
        }

        public double GetMaximumRangeForMissileModuleCharge(ModuleCharge charge)
        {
            var methodName = "GetMaximumRangeForMissileModuleCharge";
            LogTrace(methodName);

            if (LavishScriptObject.IsNullOrInvalid(charge))
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Module charge invalid or null!");
                return 0;
            }

            double maxFlightTime = charge.MaxFlightTime,
                maxVelocity = charge.MaxVelocity;

            if (maxFlightTime <= 0)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "MaxFlightTime returning {0}!",
                    maxFlightTime);
            }
            if (maxVelocity <= 0)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "MaxVelocity returning {0}!",
                    maxVelocity);
            }

            if (maxFlightTime > 0 && maxVelocity > 0)
            {
                return maxFlightTime * maxVelocity * 0.975;
            }
            return 0;
        }

        public double GetMaximumRangeForMissileItem(IItem charge)
        {
            var methodName = "GetMaximumRangeForMissileItem";
            LogTrace(methodName);

            if (!charge.IsValid)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Module charge invalid!");
                return 0;
            }

            double maxFlightTime = charge.MaxFlightTime,
                maxVelocity = charge.MaxVelocity;

            if (maxFlightTime <= 0)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "MaxFlightTime returning {0}!",
                    maxFlightTime);
            }
            if (maxVelocity <= 0)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "MaxVelocity returning {0}!",
                    maxVelocity);
            }

            if (maxFlightTime > 0 && maxVelocity > 0)
            {
                return maxFlightTime * maxVelocity * 0.975;
            }
            return 0;
        }

        /// <summary>
        /// Determine the maximum effective range given my current arsenal.
        /// </summary>
        /// <returns></returns>
        public double GetMaximumWeaponRange()
        {
            var methodName = "GetMaximumWeaponRange";
            LogTrace(methodName);

            //If it's <= 0, set it
            if (_maxWeaponRange <= 0)
            {
                //Check for drones
                if (_meCache.Ship.Drones.Count > 0 &&
                    Metatron.Drones.TotalDrones > 0)
                {
                    if (_meCache.DroneControlDistance > _maxWeaponRange)
                        _maxWeaponRange = _meCache.DroneControlDistance;
                }

                //Check against LAUNCHERS
                if (_launcherModules.Count > 0)
                {
                    var launcher = _launcherModules.OrderBy(m => LavishScriptObject.IsNullOrInvalid(m.Charge)).First();
                    var launcherRange = 0d;
                    if (!LavishScriptObject.IsNullOrInvalid(launcher.Charge) && (launcher.Charge.Quantity > 0 || DidModuleRecentlyChangeAmmo(launcher.ID)))
                    {
                        launcherRange = GetMaximumRangeForMissileModuleCharge(launcher.Charge);
                    }
                    else
                    {
                        var charges = launcher.GetAvailableAmmo();
                        if (charges != null)
                        {
                            var launcherWithKnownType = _launcherModules.FirstOrDefault(lm => _lastChargeTypeIdByWeaponId.ContainsKey(lm.ID));

                            if (launcherWithKnownType != null)
                            {
                                var knownTypeId = _lastChargeTypeIdByWeaponId[launcherWithKnownType.ID];
                                var matchingCharge = charges.FirstOrDefault(charge => charge.TypeID == knownTypeId);
                                launcherRange = GetMaximumRangeForMissileItem(matchingCharge);
                            }
                            else
                            {
                                launcherRange = GetMaximumRangeForMissileItem(charges.First());
                            }
                        }
                        if (charges != null && launcherRange <= 0)
                        {
                            ReloadAllModules();
                        }
                    }
                    if (launcherRange > _maxWeaponRange)
                        _maxWeaponRange = launcherRange;
                }

                //Check for TURRETS
                if (_turretModules.Count > 0)
                {
                    var turret = _turretModules.FirstOrDefault(innerTurret => !LavishScriptObject.IsNullOrInvalid(innerTurret.Charge) && innerTurret?.Charge?.Quantity > 0);

                    var moduleName = turret.ToItem.Name;
                    var ammoMinimumRanges = DictionariesByModuleType[moduleName][0];
                    var ammoMaximumRanges = DictionariesByModuleType[moduleName][1];

                    var longestRangeCharge = ammoMaximumRanges.Values.Max();

                    longestRangeCharge *= 0.95;

                    if (longestRangeCharge > _maxWeaponRange)
                        _maxWeaponRange = longestRangeCharge;

                }
            }
            LogTrace(methodName, $"Range: {_maxWeaponRange}");
            return _maxWeaponRange;
        }

        /// <summary>
        /// Returns the optimal damage range given our ship's weapon platforms.
        /// </summary>
        /// <returns></returns>
        public double GetOptimalWeaponRange()
        {
            var methodName = "GetOptimalWeaponRange";
            LogTrace(methodName);

            double distance = -1;
            switch (PrimaryWeaponPlatform)
            {
                case PrimaryWeaponPlatforms.Drones:
                    distance = _meCache.DroneControlDistance * 0.9;
                    break;
                case PrimaryWeaponPlatforms.Missiles:
                    var launcher = _launcherModules
                        .OrderBy(module => BoolToInt(LavishScriptObject.IsNullOrInvalid(module.Charge)))
                        .First();

                    if (!LavishScriptObject.IsNullOrInvalid(launcher.Charge) &&
                        (launcher.Charge.Quantity > 0 || DidModuleRecentlyChangeAmmo(launcher.ID)))
                    {
                        distance = GetMaximumRangeForMissileModuleCharge(launcher.Charge);
                        break;
                    }

                    var charges = launcher.GetAvailableAmmo();
                    if (charges == null)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Error: Module.GetAvailableAmmo returned null.");
                        break;
                    }

                    var lowestRangeMissile = charges
                        .OrderBy(charge => charge.MaxFlightTime * charge.MaxVelocity)
                        .First();

                    distance = GetMaximumRangeForMissileItem(lowestRangeMissile);
                    break;
                case PrimaryWeaponPlatforms.Turrets:
                    var turret = _turretModules
                        .OrderBy(module => BoolToInt(LavishScriptObject.IsNullOrInvalid(module.Charge)))
                        .First();

                    var listOfDictsForTurret = DictionariesByModuleType[turret.ToItem.Type];
                    var minimumRangesDict = listOfDictsForTurret[0];

                    //If the minimum charge is the current loaded charge, return optimal range. Otherwise, return the pre-calculated minimum range.
                    var lowestRangeCharge = minimumRangesDict.OrderBy(kvp => kvp.Value).First();

                    distance = !LavishScriptObject.IsNullOrInvalid(turret.Charge) && lowestRangeCharge.Key == turret.Charge.TypeId
                                       ? turret.OptimalRange.Value
                                       : lowestRangeCharge.Value;

                    break;
            }

            //If distance is invalid...
            if (distance == -1)
                return distance;

            //Adjust for drones
            if (PrimaryWeaponPlatform != PrimaryWeaponPlatforms.Drones &&
                Metatron.Drones.TotalDrones > 0 && !Metatron.Config.MiningConfig.UseMiningDrones)
            {
                var droneDistance = _meCache.DroneControlDistance;

                if (distance > droneDistance)
                    distance = droneDistance;
            }

            return distance != -1 ? distance * 0.9 : distance;
        }

        public bool RearmCurrentAmmoFromStation()
        {
            var methodName = "RearmCurrentAmmo";
            LogTrace(methodName);

            var chargesInCargo = GetChargesInCargo();

            //1) Determine how many of what charges I have, factoring in loaded charges
            var chargesOnShipByTypeID = GetChargesOnShipByTypeId(chargesInCargo);

            //2) DEtermine the volume alloted for each type of charge
            var allowedVolumePerCharge = GetAllowedVolumePerCharge(chargesOnShipByTypeID.Keys);

            //3) From that, determine how many I need (or need to unload)
            var chargesNeededByTypeID = GetChargesNeededByTypeId(chargesOnShipByTypeID, allowedVolumePerCharge);

            return LoadCharges(chargesNeededByTypeID);
        }

        private bool UnloadCharges(IEnumerable<IItem> chargesInCargo, IDictionary<int, int> chargesNeededByTypeId)
        {
            var methodName = "UnloadCharges";
            LogTrace(methodName);

            var movedCharges = false;
            var chargesInCargoByTypeId = new Dictionary<int, List<IItem>>();

            foreach (var item in chargesInCargo)
            {
                var typeId = item.TypeID;
                if (!chargesInCargoByTypeId.ContainsKey(typeId))
                {
                    chargesInCargoByTypeId.Add(typeId, new List<IItem> { item });
                }
                else
                {
                    chargesInCargoByTypeId[typeId].Add(item);
                }
            }

            //If we don't have a replacement for this charge, don't fucking unload it.
            foreach (var entry in chargesInCargoByTypeId)
            {
                if (!chargesNeededByTypeId.ContainsKey(entry.Key))
                {
                    foreach (var charge in entry.Value)
                    {
                        LogMessage(methodName, LogSeverityTypes.Standard, "Unloading unnecessary charge \"{0}\".", charge.Name);
                        charge.MoveTo(ToLocationNames.MyStationHangar.ToString(), ToDestinationNames.Hangar.ToString());
                        movedCharges = true;
                    }
                    continue;
                }

                //The value in chargesNeededByTypeId will be negative initially if we need to unload a charge. Make it positive.
                var items = entry.Value;
                var quantityToMove = chargesNeededByTypeId[entry.Key] * -1;

                if (quantityToMove <= 0)
                    continue;

                foreach (var item in items)
                {
                    var itemQuantity = item.Quantity;
                    if (itemQuantity >= quantityToMove)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Unloading {0} of {1}.", quantityToMove, item.Name);

                        item.MoveTo(ToLocationNames.MyStationHangar.ToString(), ToDestinationNames.Hangar.ToString(), quantityToMove);
                        quantityToMove = 0;
                        chargesNeededByTypeId.Remove(entry.Key);
                    }
                    else
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Unloading {0} of {1}.", itemQuantity, item.Name);

                        item.MoveTo(ToLocationNames.MyStationHangar.ToString(), ToDestinationNames.Hangar.ToString());
                        quantityToMove -= itemQuantity;
                    }
                    movedCharges = true;
                }
            }

            return movedCharges;
        }

        private bool LoadCharges(Dictionary<int, int> chargesNeededByTypeId)
        {
            var methodName = "LoadCharges";
            LogTrace(methodName);

            var movedCharges = false;

            foreach (var typeId in chargesNeededByTypeId.Keys)
            {
                var quantityToMove = chargesNeededByTypeId[typeId];

                if (quantityToMove <= 0) continue;

                //Find a matching item in the hangar.
                var matchingItems = _meCache.HangarItems.Where(hangarItem => hangarItem.TypeID == typeId);

                foreach (var hangarItem in matchingItems)
                {
                    var groupId = _chargeTypeIdToGroupIdMap[typeId];
                    if (hangarItem.Quantity >= quantityToMove)
                    {
                        LogMessage(methodName, LogSeverityTypes.Standard,
                                   "Moving {0} of charge \"{1}\" to our ship.", quantityToMove, hangarItem.Name);
                        hangarItem.MoveTo(ToLocationNames.MyShip.ToString(), ToDestinationNames.CargoHold.ToString(), quantityToMove);
                        movedCharges = true;
                        if (groupId == (int)GroupIDs.MiningCrystal ||
                            groupId == (int)GroupIDs.MercoxitMiningCrystal)
                            _statistics.TrackCrystalUsed(hangarItem.Type, quantityToMove);
                        break;
                    }

                    LogMessage(methodName, LogSeverityTypes.Standard,
                               "Moving {0} of charge \"{1}\" to our ship - still need more charges.",
                               hangarItem.Quantity, hangarItem.Name);
                    quantityToMove -= hangarItem.Quantity;
                    hangarItem.MoveTo(ToLocationNames.MyShip.ToString(), ToDestinationNames.CargoHold.ToString());
                    movedCharges = true;
                    if (groupId == (int)GroupIDs.MiningCrystal ||
                        groupId == (int)GroupIDs.MercoxitMiningCrystal)
                        _statistics.TrackCrystalUsed(hangarItem.Type, hangarItem.Quantity);
                }
            }
            return movedCharges;
        }

        private Dictionary<int, int> GetChargesNeededByTypeId(Dictionary<int, int> chargesOnShipByTypeId, int allowedVolumePerCharge)
        {
            var allowedVolumesByTypeId = chargesOnShipByTypeId.Keys.ToDictionary(typeId => typeId, typeID => (double)allowedVolumePerCharge);
            return GetChargesNeededByTypeId(chargesOnShipByTypeId, allowedVolumesByTypeId);
        }

        private Dictionary<int, int> GetChargesNeededByTypeId(IDictionary<int, int> chargesOnShipByTypeId, Dictionary<int, double> allowedVolumesByTypeId)
        {
            var methodName = "GetChargesNeededByTypeId";
            LogTrace(methodName);

            var chargesNeededByTypeId = new Dictionary<int, int>();
            //For all charges we need...
            foreach (var typeId in allowedVolumesByTypeId.Keys)
            {
                var groupId = _chargeTypeIdToGroupIdMap[typeId];
                var quantityOfChargeOnShip = chargesOnShipByTypeId.ContainsKey(typeId) ? chargesOnShipByTypeId[typeId] : 0;

                //3a) For frequency crystals, only need 1 full load of each type
                if (groupId == (int)GroupIDs.FrequencyCrystal)
                {
                    //Save the delta. It will unload for - values, load for + values, do nada for 0 values
                    chargesNeededByTypeId.Add(typeId, _turretCount - quantityOfChargeOnShip);
                }
                //3b) For mining crystals, I need to respect the config setting
                else if (groupId == (int)GroupIDs.MiningCrystal || groupId == (int)GroupIDs.MercoxitMiningCrystal)
                {
                    chargesNeededByTypeId.Add(typeId, Metatron.Config.MiningConfig.NumCrystalsToCarry - quantityOfChargeOnShip);
                }
                //3c) For all other charges, fill the cargo hold respecting reserved volume
                else
                {
                    var itemVolume = _chargeVolumesByGroupId[groupId];
                    var volumeTakenByCharge = itemVolume * quantityOfChargeOnShip;
                    var volumeOfChargeNeeded = Math.Round(allowedVolumesByTypeId[typeId], 3) - volumeTakenByCharge;
                    var quantityOfChargeNeeded = (int)(volumeOfChargeNeeded / itemVolume);

                    LogMessage(methodName, LogSeverityTypes.Debug, "Item volume: {0}, Volume taken by charge: {1}, Volume of charge needed: {2}, Qty of charge needed: {3}",
                        itemVolume, volumeTakenByCharge, volumeOfChargeNeeded, quantityOfChargeNeeded);

                    if (quantityOfChargeNeeded == 0) continue;

                    chargesNeededByTypeId.Add(typeId, quantityOfChargeNeeded);
                }
            }
            return chargesNeededByTypeId;
        }

        private int GetAllowedVolumePerCharge(IEnumerable<int> typeIDsOfDesiredCharges)
        {
            var volumeToFill = GetTotalUsableVolume();

            var countOfRealCharges = typeIDsOfDesiredCharges.Select(typeID => _chargeTypeIdToGroupIdMap[typeID])
                .Count(groupID => groupID != (int)GroupIDs.FrequencyCrystal && groupID != (int)GroupIDs.MiningCrystal && groupID != (int)GroupIDs.MercoxitMiningCrystal);

            return countOfRealCharges > 0 ? volumeToFill / countOfRealCharges : -1;
        }

        private int GetTotalUsableVolume()
        {
            var methodName = "GetTotalUsableVolume";
            LogTrace(methodName);

            var reservedVolume = GetReservedVolume();

            var cargoUsedByNonCharges = 0D;
            var nonChargeItems = _meCache.Ship.Cargo.Where(item => item.CategoryID != (int)CategoryIDs.Charge);

            if (nonChargeItems.Any())
                cargoUsedByNonCharges = nonChargeItems.Sum(item => item.Volume * item.Quantity);

            LogMessage(methodName, LogSeverityTypes.Debug, "Cargo used by non-charges: {0}m^3", cargoUsedByNonCharges);
            reservedVolume += cargoUsedByNonCharges;
            //Total volume for charges = cargo capacity - volume reserved for charges or mission items)
            return (int)(_meCache.Ship.CargoCapacity - reservedVolume);
        }

        private double GetReservedVolume()
        {
            var methodName = "GetReservedVolume";
            LogTrace(methodName, "totalWeaponChargeVolumeCapacity: {0}", _totalWeaponChargeVolumeCapacity);

            var reservedVolume = _totalWeaponChargeVolumeCapacity;

            if (Metatron.MissionRunner.ActiveMission != null)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Reserving {0} m^3 for mission objective items.", Metatron.MissionRunner.ActiveMission.CargoVolume);
                reservedVolume += Metatron.MissionRunner.ActiveMission.CargoVolume;
            }

            return reservedVolume;
        }

        private Dictionary<int, int> GetChargesOnShipByTypeId(IEnumerable<IItem> chargesInCargo)
        {
            var chargesOnShipByTypeId = new Dictionary<int, int>();

            //First account for loaded charges
            foreach (var itemId in _loadedChargeCountByTypeId.Keys)
            {
                chargesOnShipByTypeId.Add(itemId, _loadedChargeCountByTypeId[itemId]);
            }

            //Now account for cargo charges
            foreach (var item in chargesInCargo)
            {
                var typeId = item.TypeID;

                if (!chargesOnShipByTypeId.ContainsKey(typeId))
                    chargesOnShipByTypeId.Add(typeId, item.Quantity);
                else
                    chargesOnShipByTypeId[typeId] += item.Quantity;
            }
            return chargesOnShipByTypeId;
        }

        public bool RearmShip(List<DamageProfile> damageProfiles)
        {
            var methodName = "RearmShip";
            LogTrace(methodName, "RearmState: {0}, DamageProfiles count: {1}", _rearmState, damageProfiles.Count);

            if (_fittedWeapons.Count == 0)
                return true;

            List<IItem> chargesInCargo;
            bool movedCharges;
            switch (_rearmState)
            {
                case RearmStates.Idle:
                    if (_fittedWeapons.Select(fittedWeapon => fittedWeapon.GroupId)
                        .Any(groupId => groupId == (int)GroupIDs.HybridWeapon || groupId == (int)GroupIDs.EnergyWeapon || groupId == (int)GroupIDs.FrequencyMiningLaser))
                    {
                        RearmCurrentAmmoFromStation();
                        _rearmState = RearmStates.RepairShip;
                    }

                    _rearmState = RearmStates.UnloadUnnecessaryCharges;
                    goto case RearmStates.UnloadUnnecessaryCharges;
                case RearmStates.UnloadUnnecessaryCharges:
                    if (!IsCargoHoldActive)
                    {
                        MakeCargoHoldActive();
                        return false;
                    }

                    chargesInCargo = GetChargesInCargo();
                    var neededChargesByTypeId = GetNeededChargesByTypeId(chargesInCargo, damageProfiles);

                    movedCharges = UnloadCharges(chargesInCargo, neededChargesByTypeId);
                    _rearmState = RearmStates.LoadNecessaryCharges;

                    if (!movedCharges)
                        goto case RearmStates.LoadNecessaryCharges;
                    break;
                case RearmStates.LoadNecessaryCharges:
                    if (!Metatron.Station.IsStationHangarActive)
                    {
                        Metatron.Station.MakeStationHangarActive();
                        return false;
                    }

                    chargesInCargo = GetChargesInCargo();
                    var chargesNeededByTypeId = GetNeededChargesByTypeId(chargesInCargo, damageProfiles);

                    movedCharges = LoadCharges(chargesNeededByTypeId);

                    _rearmState = RearmStates.LoadNecessaryDrones;

                    if (!movedCharges)
                        goto case RearmStates.LoadNecessaryDrones;
                    break;
                case RearmStates.LoadNecessaryDrones:
                    _rearmState = RearmStates.StackStationCargo;
                    goto case RearmStates.StackStationCargo;
                case RearmStates.StackStationCargo:
                    if (!Metatron.Station.IsStationHangarActive)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Making the station hangar active before stacking cargo.");
                        Metatron.Station.MakeStationHangarActive();
                        return false;
                    }

                    StackInventory();

                    _rearmState = RearmStates.StackShipCargo;
                    break;
                case RearmStates.StackShipCargo:
                    if (!IsCargoHoldActive)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Making ship cargo hold active before stacking cargo.");
                        MakeCargoHoldActive();
                        return false;
                    }

                    StackInventory();

                    _rearmState = RearmStates.RepairShip;
                    break;
                case RearmStates.RepairShip:
                    if (!IsRepairWindowOpen)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Opening repair window before repairing ship");
                        _shipCache.Ship.ToItem.GetRepairQuote();
                        return false;
                    }
                    if (_eveWindowProvider.GetRepairShopWindow().TotalCost == "0")
                    {
                        _rearmState = RearmStates.Idle;
                        return true;
                    }
                    if (_eveWindowProvider.GetRepairItemsModalWindow() != null)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Repairing these items");
                        _eveWindowProvider.GetRepairItemsModalWindow().ClickButtonYes();
                        return false;
                    }
                    if (_eveWindowProvider.GetQuantityWindow().IsValid)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Setting quantity");
                        _eveWindowProvider.GetQuantityWindow().ClickButtonOK();
                        return false;
                    }
                    if (_eveWindowProvider.GetRepairShopWindow().IsValid)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Repairing all");
                        _eveWindowProvider.GetRepairShopWindow().RepairAll();
                        return false;
                    }
                    return false;
            }
            return false;
        }

        public bool RearmSpecificAmmoFromStation(List<DamageProfile> damageProfiles, List<IItem> chargesInCargo)
        {
            var methodName = "RearmSpecificAmmoFromStation";
            LogTrace(methodName);

            //1) IF I'm using hybrids or lasers, just call ReloadCurrentAmmo.
            //2) Determine which RANGES of ammo I have.
            //3) Determine replacement charges of the same range.
            //4) Replace current charges with the proper replacements.
            //4) Replace current charges with the proper replacements.

            var neededChargesByTypeID = GetNeededChargesByTypeId(chargesInCargo, damageProfiles);

            var movedCharges = LoadCharges(neededChargesByTypeID);
            return movedCharges;
        }

        private Dictionary<int, int> GetNeededChargesByTypeId(List<IItem> chargesInCargo, List<DamageProfile> damageProfiles)
        {
            var chargeRanges = GetRangesOfChargesOnShip(chargesInCargo);

            //3) Determine which charges we NEED
            // For each weapon range...
            //Iterate all hangar items matching that range's groupID and weapon size (if any)
            //Find the best match of the available items
            //If we don't have enough charges for a full load of ammo, it's not a good match

            //Filter out any disallowed items or items not matching charge range
            var hangarItems = GetAllowedChargesInHangar();

            var chargeSharesByTypeId = GetBestMatchingChargeSharesByTypeId(hangarItems, chargeRanges, damageProfiles);

            //Determine how many charges of each type we need given the shares per charge
            var allowedVolumesByTypeId = GetNeededChargeVolumeByTypeId(chargeSharesByTypeId);

            var chargesOnShipByTypeId = GetChargesOnShipByTypeId(chargesInCargo);
            return GetChargesNeededByTypeId(chargesOnShipByTypeId, allowedVolumesByTypeId);
        }

        private Dictionary<int, double> GetNeededChargeVolumeByTypeId(Dictionary<int, int> chargeSharesByTypeId)
        {
            var methodName = "GetChargeNeededVolumeByTypeID";
            LogTrace(methodName);

            var allowedVolumesByTypeId = new Dictionary<int, double>();
            var totalUsableVolume = GetTotalUsableVolume();
            LogMessage(methodName, LogSeverityTypes.Debug, "{0}m^3 of {1}m^3 is usable.", totalUsableVolume, _meCache.Ship.CargoCapacity);
            //Assuming all charges are the same size here
            //Use 90% of our usable cargo volume
            var volumePerShare = (totalUsableVolume * 0.9) / (chargeSharesByTypeId.Values.Sum());
            foreach (var typeId in chargeSharesByTypeId.Keys)
            {
                var shares = chargeSharesByTypeId[typeId];
                var volumeForCharge = shares * volumePerShare;

                LogMessage(methodName, LogSeverityTypes.Debug, "Need {0}m^3 of charge {1}.", volumeForCharge, typeId);
                allowedVolumesByTypeId.Add(typeId, volumeForCharge);
            }
            return allowedVolumesByTypeId;
        }

        private List<IItem> GetAllowedChargesInHangar()
        {
            var methodName = "GetAllowedChargesInHangar";
            LogTrace(methodName);

            var hangarItems = _meCache.HangarItems.Where(item => item.CategoryID == (int)CategoryIDs.Charge).ToList();
            LogMessage(methodName, LogSeverityTypes.Debug, "Found {0} stacks of charges.", hangarItems.Count);

            for (var index = 0; index < hangarItems.Count(); index++)
            {
                var hangarItem = hangarItems[index];

                if (!_allowedCharges.Any(allowedCharge => allowedCharge.GroupId == hangarItem.GroupID && allowedCharge.ChargeSize == hangarItem.ChargeSize))
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Hangar item \"{0}\" ({1}, {2}) does not match the allowed charges.",
                        hangarItem.Name, hangarItem.ChargeSize, hangarItem.GroupID);
                    hangarItems.RemoveAt(index);
                    index--;
                    continue;
                }

                var matchingFittedModules = _fittedWeapons.Where(fittedModule => fittedModule.GroupId == GetWeaponGroupIdFromChargeGroupId(hangarItem.GroupID));
                if (!matchingFittedModules.Any())
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Could not find any weapons capable of taking charge \"{0}\" ({1}).",
                        hangarItem.Name, hangarItem.GroupID);
                    hangarItems.RemoveAt(index);
                    index--;
                    continue;
                }


                var quantityForFullLoad = matchingFittedModules.Count() * matchingFittedModules.First().MaxCharges;
                if (hangarItem.Quantity < quantityForFullLoad)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Hangar item \"{0}\" has too few items in stack for a full load. ({1}, {2})",
                        hangarItem.Name, hangarItem.Quantity, quantityForFullLoad);
                    hangarItems.RemoveAt(index);
                    index--;
                }

                LogMessage(methodName, LogSeverityTypes.Debug, "Hangar item \"{0}\" ({1}) is an acceptible charge.", hangarItem.Name, hangarItem.GroupID);
            }
            return hangarItems;
        }

        private Dictionary<int, int> GetBestMatchingChargeSharesByTypeId(List<IItem> hangarItems, IEnumerable<ChargeRange> chargeRanges, List<DamageProfile> damageProfiles)
        {
            var chargeSharesByTypeId = new Dictionary<int, int>();
            foreach (var chargeRange in chargeRanges)
            {
                //Grab all hangar items matching this charge range
                var hangarItemsMatchingChargeRange = hangarItems
                    .Where(item => item.GroupID == chargeRange.GroupID &&
                        chargeRange.Range == GetRangeModifierFromCharge(item.Name, item.GroupID) && chargeRange.ChargeSize == item.ChargeSize);

                //Determine what charges we need for this charge range based on the passed in damage types.);)
                foreach (var damageProfile in damageProfiles)
                {
                    //Initialize this to the current charge if possible
                    var bestMatchTypeId = GetBestMatchTypeId(damageProfile, hangarItemsMatchingChargeRange, chargeRange.CurrentChargeTypeId);
                    if (!chargeSharesByTypeId.ContainsKey(bestMatchTypeId))
                        chargeSharesByTypeId.Add(bestMatchTypeId, 1);
                    else
                        chargeSharesByTypeId[bestMatchTypeId]++;
                }
            }
            return chargeSharesByTypeId;
        }

        public int GetBestMatchTypeId(DamageProfile damageProfile, IEnumerable<IItem> availableCharges, int currentChargeTypeId)
        {
            var methodName = "GetBestMatchTypeId";
            LogTrace(methodName, "{0}, {1}, {2}", damageProfile, availableCharges.Count(), currentChargeTypeId);

            IItem bestMatch = null;
            var bestMatchingPercent = currentChargeTypeId >= 0 ? GetPercentDamagetypeMatch(damageProfile, currentChargeTypeId) : 0d;
            //LogMessage(methodName, LogSeverityTypes.Debug, "Loaded charge {0} is a {1}% match.", currentChargeTypeID, bestMatchingPercent);

            foreach (var chargeItem in availableCharges)
            {
                if (chargeItem == null)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Error: Null charge item found in index {0} of available charges.");
                    continue;
                }

                var weaponGroupId = GetWeaponGroupIdFromChargeGroupId(chargeItem.GroupID);

                var totalChargesRequired = 0;

                //Hack for Assault / Standard missiles, since they're two different weapon groups sharing the same charge type
                if (weaponGroupId == (int)GroupIDs.MissileLauncher || weaponGroupId == (int)GroupIDs.MissileLauncher_Assault)
                {
                    totalChargesRequired = 500;
                }
                else
                {
                    var weaponItemStub = _fittedWeapons.FirstOrDefault(itemStub => itemStub.GroupId == weaponGroupId);

                    if (weaponItemStub == null)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Error: Could not find itemstub for groupID {0}.",
                                   weaponGroupId);
                        continue;
                    }

                    totalChargesRequired = weaponItemStub.MaxCharges *
                                           _fittedWeapons.Count(fittedWeapon => fittedWeapon.GroupId == weaponGroupId);
                }

                var matchPercent = GetPercentDamagetypeMatch(damageProfile, chargeItem.TypeID);
                //LogMessage(methodName, LogSeverityTypes.Debug, "Charge {0} is a {1}% match.", charge.Name, matchPercent);
                if ((bestMatchingPercent == 0d || matchPercent > bestMatchingPercent) && chargeItem.Quantity >= totalChargesRequired)
                {
                    bestMatchingPercent = matchPercent;
                    bestMatch = chargeItem;
                }
            }

            //LogMessage(methodName, LogSeverityTypes.Debug, "Best match: {0} at {1}%.", bestMatch == null ? currentChargeTypeID.ToString() : bestMatch.Name, bestMatchingPercent);
            return bestMatch == null ? currentChargeTypeId : bestMatch.TypeID;
        }

        private double GetPercentDamagetypeMatch(DamageProfile damageProfile, int typeId)
        {
            var methodName = "GetPercentDamagetypeMatch";
            LogTrace(methodName, "{0}, {1}", damageProfile, typeId);

            var chargeDamageProfile = GetChargeDamageProfileFromTypeID(typeId);

            if (chargeDamageProfile == null)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Error: Could not find a charge damage profile for charge with type ID {0}.", typeId);
                return 0;
            }

            return chargeDamageProfile.GetPercentMatchAgainstResistances(damageProfile);
        }

        private List<ChargeRange> GetRangesOfChargesOnShip(List<IItem> chargesInCargo)
        {
            var chargeRanges = new List<ChargeRange>();
            foreach (var loadedCharge in _loadedChargesByTypeId.Values)
            {
                var chargeRangeModifier = GetRangeModifierFromCharge(loadedCharge.Name, loadedCharge.GroupId);
                var newChargeRange = new ChargeRange(loadedCharge.GroupId, chargeRangeModifier, loadedCharge.ChargeSize) { CurrentChargeTypeId = loadedCharge.TypeId };

                if (!chargeRanges.Any(chargeRange => chargeRange == newChargeRange))
                {
                    chargeRanges.Add(newChargeRange);
                }
            }

            foreach (var chargeInCargo in chargesInCargo)
            {
                var chargeRangeModifier = GetRangeModifierFromCharge(chargeInCargo.Name, chargeInCargo.GroupID);
                var newChargeRange = new ChargeRange(chargeInCargo.GroupID, chargeRangeModifier, chargeInCargo.ChargeSize) { CurrentChargeTypeId = chargeInCargo.TypeID };

                if (!chargeRanges.Any(chargeRange => chargeRange == newChargeRange))
                {
                    chargeRanges.Add(newChargeRange);
                }
            }
            return chargeRanges;
        }

        public DamageProfile GetChargeDamageProfileFromTypeID(int typeId)
        {
            var methodName = "GetChargeDamageProfileFromTypeID";
            LogTrace(methodName, "{0}", typeId);

            var chargeName = _chargeNamesByTypeId[typeId];
            var groupId = _chargeTypeIdToGroupIdMap[typeId];

            switch (groupId)
            {
                //Projectiles
                case (int)GroupIDs.ProjectileCharge:
                case (int)GroupIDs.AdvancedArtilleryCharge:
                case (int)GroupIDs.AdvancedAutoCannonCharge:
                    return GetDamageProfileByChargeName(_projectileDamageProfilesByChargeName, chargeName);
                //Hybrids
                case (int)GroupIDs.HybridCharge:
                case (int)GroupIDs.AdvancedBlasterCharge:
                case (int)GroupIDs.AdvancedRailGunCharge:
                    return new DamageProfile(40, 60, 0, 0);
                //Precursor
                case (int)GroupIDs.ExoticPlasmaCharge:
                case (int)GroupIDs.AdvancedExoticPlasmaCharge:
                    return new DamageProfile(0, 60, 40, 0);
                //Lasers
                case (int)GroupIDs.FrequencyCrystal:
                case (int)GroupIDs.AdvancedPulseLaserCrystal:
                case (int)GroupIDs.AdvancedBeamLaserCrystal:
                    return new DamageProfile(0, 18, 0, 82);
                case (int)GroupIDs.CruiseMissile:
                case (int)GroupIDs.Torpedo:
                case (int)GroupIDs.AdvancedLightMissile:
                case (int)GroupIDs.LightMissile:
                case (int)GroupIDs.AdvancedHeavyMissile:
                case (int)GroupIDs.HeavyMissile:
                case (int)GroupIDs.AdvancedTorpedo:
                case (int)GroupIDs.AdvancedCruiseMissile:
                case (int)GroupIDs.AssaultMissile:
                case (int)GroupIDs.AdvancedAssaultMissile:
                    return GetDamageProfileByChargeName(_missileDamageProfilesByChargeName, chargeName);
            }
            return DamageProfile.Default;
        }

        public int GetWeaponGroupIdFromChargeGroupId(int groupId)
        {
            switch (groupId)
            {
                case (int)GroupIDs.ProjectileCharge:
                case (int)GroupIDs.AdvancedArtilleryCharge:
                case (int)GroupIDs.AdvancedAutoCannonCharge:
                    return (int)GroupIDs.ProjectileWeapon;
                //Hybrids
                case (int)GroupIDs.HybridCharge:
                case (int)GroupIDs.AdvancedBlasterCharge:
                case (int)GroupIDs.AdvancedRailGunCharge:
                    return (int)GroupIDs.HybridWeapon;
                //Precursor
                case (int)GroupIDs.ExoticPlasmaCharge:
                case (int)GroupIDs.AdvancedExoticPlasmaCharge:
                    return (int)GroupIDs.PrecursorWeapon;
                //Lasers
                case (int)GroupIDs.FrequencyCrystal:
                case (int)GroupIDs.AdvancedPulseLaserCrystal:
                case (int)GroupIDs.AdvancedBeamLaserCrystal:
                    return (int)GroupIDs.EnergyWeapon;

                case (int)GroupIDs.CruiseMissile:
                case (int)GroupIDs.AdvancedCruiseMissile:
                    return (int)GroupIDs.MissileLauncher_Cruise;

                case (int)GroupIDs.AdvancedTorpedo:
                case (int)GroupIDs.Torpedo:
                    return (int)GroupIDs.MissileLauncher_Siege;

                //Problem: Both Assault launchers and Standard launchers use light missiles
                case (int)GroupIDs.AdvancedLightMissile:
                case (int)GroupIDs.LightMissile:
                    return (int)GroupIDs.MissileLauncher;

                case (int)GroupIDs.AdvancedHeavyMissile:
                case (int)GroupIDs.HeavyMissile:
                    return (int)GroupIDs.MissileLauncher_Heavy;

                case (int)GroupIDs.AdvancedAssaultMissile:
                case (int)GroupIDs.AssaultMissile:
                    return (int)GroupIDs.MissileLauncher_HeavyAssault;
                default:
                    LogMessage("GetWeaponGroupIdFromChargeGroupId", LogSeverityTypes.Debug, "Error: Could not determine weapon group for charge group ID {0}.",
                        groupId);
                    return -1;
            }
        }

        private DamageProfile GetDamageProfileByChargeName(Dictionary<string, DamageProfile> dictionary, string chargeName)
        {
            var methodName = "GetDamageProfileByChargeName";
            LogTrace(methodName, "ChargeName: {0}", chargeName);

            foreach (var key in dictionary.Keys)
            {
                if (chargeName.Contains(key))
                    return dictionary[key];
            }

            return DamageProfile.Default;
        }

        private List<IItem> GetChargesInCargo()
        {
            var methodName = "GetChargesInCargo";
            LogTrace(methodName);

            return _meCache.Ship.Cargo.Where(item => item.CategoryID == (int)CategoryIDs.Charge).ToList();
        }
        #endregion

        public List<EVE.ISXEVE.Interfaces.IModule> GetInactiveTurrets()
        {
            var methodName = "GetInactiveTurrets";
            LogTrace(methodName);

            return TurretModules.Where(module => module.IsValid && !module.IsActive).ToList();
        }

        public List<EVE.ISXEVE.Interfaces.IModule> GetInactiveLaunchers()
        {
            return _launcherModules.Where(module => module.IsValid && !module.IsActive).ToList();
        }

        private class ChargeRange
        {
            public readonly int GroupID;
            public readonly double Range;
            public readonly int ChargeSize;

            public int CurrentChargeTypeId;

            public ChargeRange(int groupId, double range, int chargeSize)
            {
                GroupID = groupId;
                Range = range;
                ChargeSize = chargeSize;
            }

            public override int GetHashCode()
            {
                return GroupID.GetHashCode() ^
                    Range.GetHashCode() ^
                    ChargeSize.GetHashCode() ^
                    CurrentChargeTypeId.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(obj, null)) return false;

                return obj.GetHashCode() == GetHashCode();
            }

            public static bool operator ==(ChargeRange first, ChargeRange second)
            {
                return (first.GroupID == second.GroupID &&
                        first.Range == second.Range &&
                        first.ChargeSize == second.ChargeSize);
            }

            public static bool operator !=(ChargeRange first, ChargeRange second)
            {
                return !(first == second);
            }
        }

        private class ItemStub
        {
            public readonly int GroupId;
            public readonly int ChargeSize;

            public readonly string Name;

            public int ModuleGroupId;
            public int MaxCharges;
            public int TypeId;

            public ItemStub(int groupID, int chargeSize, string name)
            {
                GroupId = groupID;
                ChargeSize = chargeSize;
                Name = name;
            }

            public override int GetHashCode()
            {
                return GroupId.GetHashCode() ^
                       ChargeSize.GetHashCode() ^
                       Name.GetHashCode() ^
                       ModuleGroupId.GetHashCode() ^
                       MaxCharges.GetHashCode() ^
                       TypeId.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(obj, null)) return false;

                return obj.GetHashCode() == GetHashCode();
            }

            public static bool operator ==(ItemStub first, ItemStub second)
            {
                if ((object)first == null || (object)second == null)
                    return false;

                return first.GroupId == second.GroupId && first.ChargeSize == second.ChargeSize &&
                       first.Name == second.Name && first.ModuleGroupId == second.ModuleGroupId &&
                       first.MaxCharges == second.MaxCharges;
            }

            public static bool operator !=(ItemStub first, ItemStub second)
            {
                return !(first == second);
            }
        }
    }

    public enum RearmStates
    {
        Idle,
        UnloadUnnecessaryCharges,
        LoadNecessaryCharges,
        LoadNecessaryDrones,
        StackStationCargo,
        StackShipCargo,
        RepairShip
    }

    // ReSharper restore CompareOfFloatsByEqualityOperator
}
