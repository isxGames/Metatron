using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Metatron
{
    public enum Ranges
    {
        Warp = 150000,
        Dock = 200,
        LootActivate = 2500,
		PlanetWarpIn = 100000000		//100 million meters
    }

    public enum TypeIDs
    {
        SolarSystem = 5,
		Plagioclase = 18,
		Spodumain = 19,
		Kernite = 20,
		Hedbergite = 21,
		Arkonor = 22,
        Capsule = 670,
		Bistot = 1223,
		Pyroxeres = 1224,
		Crokite = 1225,
		Jaspet = 1226,
		Omber = 1227,
		Scordite = 1228,
		Gneiss = 1229,
		Veldspar = 1230,
		Hemorphite = 1231,
		Dark_Ochre = 1232,
        PrototypeCloakingDevice = 11370,
		Mercoxit = 11396,
        CovertOpsCloakingDevice = 11578,
		Clear_Icicle = 16262,
		Glacial_Mass = 16263,
		Blue_Ice = 16264,
		White_Glaze = 16265,
		Glare_Crust = 16266,
		Dark_Glitter = 16267,
		Gelidus = 16268,
		Krystallos = 16269,
		Crimson_Arkonor = 17425,
		Prime_Arkonor = 17426,
		Triclinic_Bistot = 17428,
		Monoclinic_Bistot = 17429,
		Sharp_Crokite = 17432,
		Crystalline_Crokite = 17433,
		Onyx_Ochre = 17436,
		Obsidian_Ochre = 17437,
		Vitric_Hedbergite = 17440,
		Glazed_Hedbergite = 17441,
		Vivid_Hemorphite = 17444,
		Radiant_Hemorphite = 17445,
		Pure_Jaspet = 17448,
		Pristine_Jaspet = 17449,
		Luminous_Kernite = 17452,
		Fiery_Kernite = 17453,
		Azure_Plagioclase = 17455,
		Rich_Plagioclase = 17456,
		Solid_Pyroxeres = 17459,
		Viscous_Pyroxeres = 17460,
		Condensed_Scordite = 17463,
		Massive_Scordite = 17464,
		Bright_Spodumain = 17466,
		Gleaming_Spodumain = 17467,
		Concentrated_Veldspar = 17470,
		Dense_Veldspar = 17471,
        Covetor = 17476,
        Retriever = 17478,
        Procurer = 17480,
		CorporateHangarArray = 17621,
		Iridescent_Gneiss = 17865,
		Prismatic_Gneiss = 17866,
		Silvery_Omber = 17867,
		Golden_Omber = 17868,
		Magma_Mercoxit = 17869,
		Vitreous_Mercoxit = 17870,
		Thick_Blue_Ice = 17975,
		Pristine_White_Glaze = 17976,
		Smooth_Glacial_Mass = 17977,
		Enriched_Clear_Icicle = 17978,
        Hulk = 22544,
        Skiff = 22546,
        Mackinaw = 22548,
		MiningForemanLink_HarvesterCapacitorEfficiency = 22553,
		MiningForemanLink_MiningLaserFieldEnhancement = 22555,
		MiningForemanLink_LaserOptimization = 22557,
        IndustrialCore = 28583,
        Salvager = 25861,
		Orca = 28606
    }

    public enum CategoryIDs
    {
        Station = 3,
        Ship = 6,
        Entity = 11,
        Asteroid = 25
    }

    public enum GroupIDs
    {
		Planet = 7,
        AsteroidBelt = 9,
        Stargate = 10,
        CargoContainer = 12,
        Station = 15,
        ShieldRecharger = 39,
        ShieldBooster = 40,
		AfterBurner = 46,
        EnergyWeapon = 53,
        MiningLaser = 54,
        ProjectileWeapon = 55,
        MissileLauncher = 56,
        DamageControl = 60,
        ArmorRepairer = 62,
        HybridWeapon = 74,
        ShieldHardener = 77,
        ECCM = 202,
		GangLink = 316,
        ArmorHardener = 328,
        CloakingDevice = 330,
        WarpGate = 366,
        StripMiner = 464,
		AnchorableCargoContainerNEEDSUPDATE = 471,
        MiningCrystal = 482,
        FrequencyMiningLaser = 483,
        MissileLauncher_Cruise = 506,
        MissileLauncher_Rocket = 507,
        MissileLauncher_Siege = 508,
        MissileLauncher_Standard = 509,
        MissileLauncher_Heavy = 510,
        MissileLauncher_Assault = 511,
        MissileLauncher_Defender = 512,
        IndustrialCore = 515,
        DataMiner = 538,
        TractorBeam = 650,
        MercoxitMiningCrystal = 663,
        MissileLauncher_HeavyAssault = 771,
        MissileLauncher_Bomb = 862
    }

    public enum Modes
    {
        Stopped = 2,
        Warp = 3,
		Invulnerable = 11
    }

    public enum Capacities
    {
        JettisonContainer = 27500,
        OrcaCorpHangarArray = 40000,
		CorporateHangarArray = 1400000
    }

    public enum DroneStates
    {
        Idle,
        Fighting,
        Unknown_State_2,
        Unknown_State_3,
        Returning
    }

    public enum AmmoTypes
    {
        Projectile,
        Frequency,
        Hybrid
    }

    public enum TargetTypes
    {
        Mining,         // \
        Salvaging,      // /|- these two will be handled by Non-Combat. Salvaging optionally includes looting.
        Looting,        // Loot-only wrecks (faction/officer wrecks while ratting/mining?)
        Killing         // Handled by Combat
    }

    public enum TargetPriorities    //Lower is higher priority
    {
        Kill_WarpScrambler = 0,
        Kill_ElectronicWarfare,
        Kill_BattleShip,
        Kill_BattleCruiser,
        Kill_Cruiser,
        Kill_Destroyer,
        Kill_Frigate,
        Kill_LargeCollidableObject,
        Kill_Other,
        Wreck_Salvage,
        Wreck_Loot,
        Mining
    }

    public enum DropoffTypes
    {
        Station,            //Unload to station
        CorpHangarArray,    //Unload to a pos CHA  
		StationCorpHangar,	//Unload to a station CHA
        AnchoredContainer,  //Unload to an anchored can
        Jetcan,             //Jettison
        ShipBay             //Rorqual/Orca CHA
    }

	public enum BounceWarpStates
	{
		Idle,
		RemoveTemporaryBookmarks,
		CreateTemporaryBookmark,
		SetTemporaryBookmark,
		QueueDestinations
	}

	public enum StationDropoffStates
	{
		Idle,
		UnloadCargo,
		StackHangar,
		RearmCrystals
	}

	public enum StationCorpHangarDropoffStates
	{
		Idle,
		OpenCorpHangar,
		UnloadCargo,
		StackHangar,
		RearmCrystals
	}

	public enum JetcanDropoffStates
	{
		Idle,
		CreateCan,
		OpenCan,
		MarkCanFull,
		TransferCargo,
		StackCargo,
		WaitForPickup
	}

	public enum CorpHangarArrayDropoffStates
	{
		Idle,
		OpenCan,
		TransferCargo,
		StackCargo,
		RearmCrystals
	}

	public enum GoToDropoffStates
	{
		Idle,
		RemoveTemporaryMiningBookmarks,
		CreateTemporaryMiningBookmark,
		SetTemporaryMiningBookmark,
		GoToDropoff
	}

	public enum PulseSpeeds
	{
		Average,
		Fast,
		Slow,
		VerySlow,
        VeryFast,
        Hyper
	}

    public enum BotModes
    {
        Idle,
        Mining,
        Ratting,
        Hauling,
        Scavenging,
        Missioning,
        BoostCanOrca,
        BoostOrca,
        Freighting
    }

    public enum MissionStates
    {
        Offered = 1,
        Accepted = 2
    }

    public enum HaulerModes
    {
        /// <summary>
        /// Only go to pickup a can when we receive request events.
        /// </summary>
        [ProtoEnum(Name = "WaitForRequestEvent", Value = 0)]
        WaitForRequestEvent = 0,
        /// <summary>
        /// Cycle all members in the fleet without waiting for request events.
        /// </summary>
        [ProtoEnum(Name = "CycleFleetMembers", Value = 1)]
        CycleFleetMembers = 1
    }

    public enum FreighterModes
    {
        PointToPoint,
        AssetConsolidation
    }
}
