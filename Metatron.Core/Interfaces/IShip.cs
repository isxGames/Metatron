using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EVE.ISXEVE;
using EVE.ISXEVE.Interfaces;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
	public interface IShip : IModule
	{
		bool CargoContainsItem(int itemTypeID);
        Item GetBestMiningCrystal(IEntityWrapper target, EVE.ISXEVE.Interfaces.IModule currentModule);
		bool HangarContainsItem(int itemTypeID);
		double MaximumMiningRange { get; }
		double MaxLockedTargets { get; }
		double MaxTargetRange { get; }
	    bool IsInventoryOpen { get; }
	    bool IsOreHoldActive { get; }
	    bool IsCargoHoldActive { get; }
	    bool IsInventoryReady { get; }
	    double OreHoldCapacity { get; }
	    double UsedOreHoldCapacity { get; }
	    double MaxSlowboatDistance { get; }
	    PrimaryWeaponPlatforms PrimaryWeaponPlatform { get; }
	    bool IsAmmoAvailable { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> AllModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> ArmorRepairerModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> ShieldBoosterModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> ActiveHardenerModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> PassiveShieldTankModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> WeaponModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> LauncherModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> CloakingDeviceModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> CovertOpsCloakingDeviceModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> OtherCloakingDeviceModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> DamageControlModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> EccmModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> MiningLaserModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> TractorBeamModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> AfterBurnerModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> GangLinkModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> SurveyScannerModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> TargetPainterModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> StasisWebifierModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> NosferatuModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> SensorBoosterModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> SalvagerModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> TrackingComputerModules { get; }
	    ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> TurretModules { get; }
        ReadOnlyCollection<EVE.ISXEVE.Interfaces.IModule> SeigeModules { get; }

        bool AbleToSwapAmmoLoadout { get; }
	    void OpenInventory();
	    void MakeOreHoldActive();
	    IEveInvChildWindow GetOreHoldChildWindow();
	    IEveInvChildWindow GetCargoHoldChildWindow();
	    bool TransferItemFromHangarToCargo(int itemTypeId, out bool allItemsFit);
	    bool TransferStationHangarToShipHangar();
	    bool TransferStationCorpHangarToShipHangar();
	    bool TransferCorporateHangarArrayToShipHangar(ICorporateHangarArray corporateHangarArray);
	    void TransferCargoHoldToJetCan(Int64 jetCanEntityId);
	    void TransferOreInCargoHoldToJetCan(Int64 jetCanEntityId);
	    void TransferOreHoldToJetCan(Int64 jetCanEntityId);
	    bool TransferCargoHoldToCorporateHangarArray(ICorporateHangarArray corporateHangarArray);
	    bool TransferOreInCargoHoldToCorporateHangarArray(ICorporateHangarArray corporateHangarArray);
	    bool TransferOreHoldToCorporateHangarArray(ICorporateHangarArray corporateHangarArray);
	    bool TransferCargoHoldToStationCorporateHangars();
	    bool TransferOreInCargoHoldToStationCorporateHangars();
	    bool TransferOreHoldToStationCorporateHangars();
	    void TransferCargoHoldToStationHangar();
	    void TransferOreInCargoHoldToStationHangar();
	    void TransferOreHoldToStationHangar();
		bool HasOreHold();
        void MoveItemToStationHangar(IItem item);
        void MoveItemToStationCorporateHangar(IItem item);
        void MoveItemToCorporateHangarArray(ICorporateHangarArray corporateHangarArray, IItem item, int quantity);
        void MoveItemToJetCan(IItem item, int quantity);

	    /// <summary>
	    /// Attempt to rearm mining crystals. This is a multi-invocation process.
	    /// </summary>
	    /// <param name="corporateHangarArray"></param>
	    /// <returns>true when complete; false otherwise</returns>
	    bool RearmMiningCrystals(ICorporateHangarArray corporateHangarArray);

	    void MakeCargoHoldActive();
	    void StackInventory();

	    /// <summary>
	    /// Iterate all turret modules on the current ship and calculate the optimal
	    /// range bands for turret and for each ammo type available for the turret.
	    /// </summary>
	    /// <returns></returns>
	    void CalculateTurretOptimalRangeBands();

	    /// <summary>
	    /// Lookup the base optimal range for a given module, calculating it first if necessary and storing the result.
	    /// </summary>
	    /// <param name="module"></param>
	    /// <returns></returns>
        double GetBaseOptimalRangeFromModule(EVE.ISXEVE.Interfaces.IModule module);

	    double GetRangeModifierFromCharge(IItem charge);

	    /// <summary>
	    /// Look up the given charge in the various dictionaries and find the appropriate range modifier.
	    /// </summary>
	    double GetRangeModifierFromCharge(string chargeName, int chargeGroupId);

	    /// <summary>
	    /// Look up the correct dictionary for the given module.
	    /// </summary>
	    /// <param name="module"></param>
	    /// <returns></returns>
        double GetRangeModifierFromModule(EVE.ISXEVE.Interfaces.IModule module);

	    /// <summary>
	    /// Get the charge dictionary from a module's group.
	    /// </summary>
	    /// <param name="module"></param>
	    /// <returns></returns>
        Dictionary<string, double> GetChargeModDictionaryFromModuleGroup(EVE.ISXEVE.Interfaces.IModule module);

	    /// <summary>
	    /// Search the given charge dictionary for a match for the given charge name.
	    /// </summary>
	    /// <param name="dictionary"></param>
	    /// <param name="fullKey"></param>
	    /// <returns></returns>
	    double GetRangeModByChargeName(Dictionary<string, double> dictionary, string fullKey);

	    /// <summary>
	    /// Check the optimal range bands for a given turret to see if the given distance matches,
	    /// accounting for available ammo.
	    /// </summary>
	    /// <param name="module"></param>
	    /// <param name="distance"></param>
	    /// <param name="damageProfile"></param>
	    /// <returns>-1 if no bands match, otherwise typeID of matching ammo</returns>
        int CheckTurretOptimalRangeBands(EVE.ISXEVE.Interfaces.IModule module, double distance, DamageProfile damageProfile);

	    /// <summary>
	    /// Change a given turret's charge to the given type
	    /// </summary>
	    /// <param name="module"></param>
	    /// <param name="typeId"></param>
        void ChangeTurretAmmo(EVE.ISXEVE.Interfaces.IModule module, int typeId);

	    /// <summary>
	    /// Determine if a given module has recently changed ammo.
	    /// </summary>
	    /// <param name="moduleId"></param>
	    /// <returns></returns>
	    bool DidModuleRecentlyChangeAmmo(Int64 moduleId);

	    /// <summary>
	    /// Reload all modules needing reloaded.
	    /// Note: Should only be called when we're in a spot nothing else will be using turrets, like while warping.
	    /// </summary>
	    void ReloadAllModules();

	    /// <summary>
	    /// Get all charges available for a given module including any loaded charge.
	    /// </summary>
	    /// <param name="module"></param>
	    /// <returns></returns>
	    List<IItem> GetChargesAvailableForModule(EVE.ISXEVE.Interfaces.IModule module);

	    Dictionary<int, int> GetChargesAvailableForModuleByTypeId(Module module);
        void RemoveChargesWithoutAtLeastOneFullLoad(List<IItem> availableCharges, int fullLoadQuantity);

	    /// <summary>
	    /// Todo: Move this to Ship and call it from there. MissionProcessor will use it to calculate whether
	    /// or not closest target is in turret range if we're using turrets.
	    /// </summary>
	    /// <param name="distance"></param>
	    /// <param name="damageProfile"></param>
	    /// <returns></returns>
	    Dictionary<int, int> GetAmmoTypeIdsByModuleTypeId(double distance, DamageProfile damageProfile);

	    /// <summary>
	    /// Return the estimated maximum range for missiles.
	    /// </summary>
        double MaximumMissileRange(EVE.ISXEVE.Interfaces.IModule launcher);

	    double GetMaximumRangeForMissileModuleCharge(ModuleCharge charge);
        double GetMaximumRangeForMissileItem(IItem charge);

	    /// <summary>
	    /// Determine the maximum effective range given my current arsenal.
	    /// </summary>
	    /// <returns></returns>
	    double GetMaximumWeaponRange();

	    /// <summary>
	    /// Returns the optimal damage range given our ship's weapon platforms.
	    /// </summary>
	    /// <returns></returns>
	    double GetOptimalWeaponRange();

	    bool RearmCurrentAmmoFromStation();
	    bool RearmShip(List<DamageProfile> damageProfiles);
        bool RearmSpecificAmmoFromStation(List<DamageProfile> damageProfiles, List<IItem> chargesInCargo);
        int GetBestMatchTypeId(DamageProfile damageProfile, IEnumerable<IItem> availableCharges, int currentChargeTypeId);
	    DamageProfile GetChargeDamageProfileFromTypeID(int typeId);
	    int GetWeaponGroupIdFromChargeGroupId(int groupId);
        List<EVE.ISXEVE.Interfaces.IModule> GetInactiveTurrets();
        List<EVE.ISXEVE.Interfaces.IModule> GetInactiveLaunchers();
	    bool DeactivateTankRepairModules();
	    bool ActivateTankRepairModules();
	    bool ActivateCovertOpsCloak();
	    bool DeactivateCovertOpsCloak();
        bool ActivateModuleList(IEnumerable<EVE.ISXEVE.Interfaces.IModule> modules, bool fastActivate);
        bool ActivateModuleList(IEnumerable<EVE.ISXEVE.Interfaces.IModule> modules, bool fastActivate, long targetEntityId);
        bool DeactivateModuleList(IEnumerable<EVE.ISXEVE.Interfaces.IModule> modules, bool fastDeactivate);
	    void TransferCargoHoldToStationExcludingCategoryIds(params int[] categoryIds);
	}
}
