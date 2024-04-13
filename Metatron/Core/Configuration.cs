using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ProtoBuf;

namespace Metatron.Core
{
    //Notes on ProtoBuf-Net and deserailization
    //So far, it appears that with bools, a bool member initialized to 'true' will not be overwritten by a 'false' saved bool.
    //For instance, if UseRandomWaits is false when saved, and is default initialized to true, protobuf-net does not 'false' it.
    //To get around this, since C# defaults uninitialized bools to false, just don't initialize it 'true' unless making a new config.

    //[XmlRoot("Configuration")]
    [ProtoContract]
    public sealed class Configuration
    {
        //Mark these for serialization
        [ProtoMember(1, IsRequired = true)]
        public BotModes LastBotMode = BotModes.Idle;
        [ProtoMember(2)]
        public string HomeStation = string.Empty;

        [ProtoMember(3)]
        public SocialConfiguration SocialConfig = new SocialConfiguration();
        [ProtoMember(4)]
        public DefensiveConfiguration DefenseConfig = new DefensiveConfiguration();
        [ProtoMember(5)]
        public MissionConfiguration MissionConfig = new MissionConfiguration();
        [ProtoMember(6)]
        public MiningConfiguration MiningConfig = new MiningConfiguration();
        [ProtoMember(7)]
        public MovementConfiguration MovementConfig = new MovementConfiguration();
        [ProtoMember(8)]
        public MainConfiguration MainConfig = new MainConfiguration();
        [ProtoMember(9)]
        public FleetConfiguration FleetConfig = new FleetConfiguration();
        [ProtoMember(10)]
        public HaulingConfiguration HaulingConfig = new HaulingConfiguration();
        [ProtoMember(11)]
        public FreightingConfiguration FreightConfig = new FreightingConfiguration();
        [ProtoMember(14)]
        public AlertConfiguration AlertConfig = new AlertConfiguration();

        //Private constructor, no one else needs to instantiate this
        public Configuration()
        {
            LastBotMode = BotModes.Idle;
            HomeStation = string.Empty;
        }

        //Copy constructor
        /// <summary>
        /// Todo - finish this
        /// </summary>
        /// <param name="copy"></param>
        public Configuration(Configuration copy)
        {
            LastBotMode = copy.LastBotMode;
            HomeStation = copy.HomeStation;

            DefenseConfig = new DefensiveConfiguration(copy.DefenseConfig);
            FleetConfig = new FleetConfiguration(copy.FleetConfig);
            HaulingConfig = new HaulingConfiguration(copy.HaulingConfig);
            MainConfig = new MainConfiguration(copy.MainConfig);
            MiningConfig = new MiningConfiguration(copy.MiningConfig);
            MissionConfig = new MissionConfiguration(copy.MissionConfig);
            MovementConfig = new MovementConfiguration(copy.MovementConfig);
            SocialConfig = new SocialConfiguration(copy.SocialConfig);
            FreightConfig = new FreightingConfiguration(copy.FreightConfig);
            AlertConfig = new AlertConfiguration(copy.AlertConfig);
        }
    }

    [ProtoContract()]
    public sealed class SocialConfiguration
    {
        //Minimum standings
        [ProtoMember(1)]
        public float MinimumPilotStanding = 0;
        [ProtoMember(2)]
        public float MinimumCorpStanding = 0;
        [ProtoMember(3)]
        public float MinimumAllianceStanding = 0;

        //Whitelists and blacklists
        [ProtoMember(4)]
        public List<string> PilotWhitelist = new List<string>();
        [ProtoMember(5)]
        public List<string> CorpWhitelist = new List<string>();
        [ProtoMember(6)]
        public List<string> AllianceWhitelist = new List<string>();
        [ProtoMember(7)]
        public List<string> PilotBlacklist = new List<string>();
        [ProtoMember(8)]
        public List<string> CorpBlacklist = new List<string>();
        [ProtoMember(9)]
        public List<string> AllianceBlacklist = new List<string>();

        public SocialConfiguration()
        {

        }

        /// <summary>
        /// Copy the values of a SocialConfiguration to another SocialConfiguration
        /// </summary>
        /// <param name="copy"></param>
        public SocialConfiguration(SocialConfiguration copy)
        {
            AllianceBlacklist = new List<string>(copy.AllianceBlacklist);
            AllianceWhitelist = new List<string>(copy.AllianceWhitelist);
            CorpBlacklist = new List<string>(copy.CorpBlacklist);
            CorpWhitelist = new List<string>(copy.CorpWhitelist);
            MinimumAllianceStanding = copy.MinimumAllianceStanding;
            MinimumCorpStanding = copy.MinimumCorpStanding;
            MinimumPilotStanding = copy.MinimumPilotStanding;
            PilotBlacklist = new List<string>(copy.PilotBlacklist);
            PilotWhitelist = new List<string>(copy.PilotWhitelist);
        }
    }

    [ProtoContract()]
    public sealed class DefensiveConfiguration
    {
        //Minimum tank pcts
        [ProtoMember(1)]
        public int MinimumShieldPct;
        [ProtoMember(2)]
        public int MinimumArmorPct;
        [ProtoMember(3)]
        public int MinimumCapPct;
        [ProtoMember(4)]
        public int ResumeShieldPct;
        [ProtoMember(5)]
        public int ResumeCapPct;

        //Flee options
        [ProtoMember(6)]
        public bool RunOnNonWhitelistedPilot;
        [ProtoMember(7)]
        public bool RunOnBlacklistedPilot;
        [ProtoMember(8)]
        public bool RunOnLowTank;
        [ProtoMember(9)]
        public bool RunOnLowCap;
        [ProtoMember(10)]
        public bool RunIfTargetJammed;
        [ProtoMember(11)]
        public bool RunOnLowAmmo;
        //Misc options
        [ProtoMember(12)]
        public bool PreferStationsOverSafespots;
        [ProtoMember(13)]
        public bool AlwaysShieldBoost;
        [ProtoMember(14)]
        public bool QuitIfScrammedAndLocalUnsafe;
        [ProtoMember(15)]
        public bool RearmAtBookmarks;
        //Flee onstandings
        [ProtoMember(16)]
        public bool RunOnMeToPilot;
        [ProtoMember(17)]
        public bool RunOnCorpToPilot;
        [ProtoMember(18)]
        public bool RunOnMeToCorp;
        [ProtoMember(19)]
        public bool RunOnCorpToCorp;
        [ProtoMember(20)]
        public bool RunOnCorpToAlliance;
        [ProtoMember(21)]
        public bool RunOnAllianceToAlliance;

        //Min Drones
        [ProtoMember(22)]
        public bool RunOnLowDrones;
        [ProtoMember(23)]
        public int MinimumNumDrones;

        //Bugfix
        [ProtoMember(24)]
        public bool DisableStandingsChecks;
        [ProtoMember(25)]
        public bool WaitAfterFleeing;
        [ProtoMember(26)]
        public int MinutesToWait;

        public DefensiveConfiguration()
        {

        }

        public DefensiveConfiguration(DefensiveConfiguration copy)
        {
            MinimumArmorPct = copy.MinimumArmorPct;
            MinimumCapPct = copy.MinimumCapPct;
            MinimumNumDrones = copy.MinimumNumDrones;
            MinimumShieldPct = copy.MinimumShieldPct;
            PreferStationsOverSafespots = copy.PreferStationsOverSafespots;
            QuitIfScrammedAndLocalUnsafe = copy.QuitIfScrammedAndLocalUnsafe;
            RearmAtBookmarks = copy.RearmAtBookmarks;
            ResumeCapPct = copy.ResumeCapPct;
            ResumeShieldPct = copy.ResumeShieldPct;
            RunIfTargetJammed = copy.RunIfTargetJammed;
            RunOnAllianceToAlliance = copy.RunOnAllianceToAlliance;
            RunOnBlacklistedPilot = copy.RunOnBlacklistedPilot;
            RunOnCorpToAlliance = copy.RunOnCorpToAlliance;
            RunOnCorpToCorp = copy.RunOnCorpToCorp;
            RunOnCorpToPilot = copy.RunOnCorpToPilot;
            RunOnLowAmmo = copy.RunOnLowAmmo;
            RunOnLowCap = copy.RunOnLowCap;
            RunOnLowDrones = copy.RunOnLowDrones;
            RunOnLowTank = copy.RunOnLowTank;
            RunOnMeToCorp = copy.RunOnMeToCorp;
            RunOnMeToPilot = copy.RunOnMeToPilot;
            RunOnNonWhitelistedPilot = copy.RunOnNonWhitelistedPilot;
            DisableStandingsChecks = copy.DisableStandingsChecks;
            WaitAfterFleeing = copy.WaitAfterFleeing;
            MinutesToWait = copy.MinutesToWait;
        }
    }

    [ProtoContract()]
    public sealed class MissionConfiguration
    {
        [ProtoMember(1)]
        public bool RunCourierMissions;
        [ProtoMember(2)]
        public bool RunTradeMissions;
        [ProtoMember(3)]
        public bool RunMiningMissions;
        [ProtoMember(4)]
        public bool RunEncounterMissions;

        [ProtoMember(5)]
        public bool AvoidLowSec;

        [ProtoMember(6)]
        public List<string> Agents = new List<string>();
        [ProtoMember(7)]
        public List<string> ResearchAgents = new List<string>();

        public MissionConfiguration()
        {

        }

        public MissionConfiguration(MissionConfiguration copy)
        {
            RunCourierMissions = copy.RunCourierMissions;
            RunTradeMissions = copy.RunTradeMissions;
            RunMiningMissions = copy.RunMiningMissions;
            RunEncounterMissions = copy.RunEncounterMissions;

            AvoidLowSec = copy.AvoidLowSec;

            Agents = copy.Agents;
            ResearchAgents = copy.ResearchAgents;
        }
    }

    [ProtoContract()]
    public sealed class MiningConfiguration
    {
        [ProtoMember(1)]
        public bool DistributeLasers;
        [ProtoMember(2)]
        public bool IceMining;
        [ProtoMember(3)]
        public string JetcanNameFormat = "CORP HH:MM FULL";
        [ProtoMember(4)]
        public bool StripMine;
        [ProtoMember(5)]
        public double StripRangeMultiplier;

        [ProtoMember(6)]
        public bool BookmarkLastPosition;

        //Long fucking list of types of ore and ice to mine
        [ProtoMember(7)]
        public Dictionary<string, bool> Ore_DoMine = new Dictionary<string, bool>();
        [ProtoMember(8)]
        public List<string> Ore_Priority = new List<string>();

        //Now for ice
        [ProtoMember(9)]
        public Dictionary<string, bool> Ice_DoMine = new Dictionary<string, bool>();
        [ProtoMember(10)]
        public List<string> Ice_Priority = new List<string>();

        [ProtoMember(11)]
        public bool ShortCycle;
        [ProtoMember(12)]
        public bool DelayActivation;
        [ProtoMember(13)]
        public int NumCrystalsToCarry;
        [ProtoMember(14)]
        public double MinDistanceToPlayers;
        [ProtoMember(15)]
        public bool UseMiningDrones;

        public MiningConfiguration()
        {

        }

        public MiningConfiguration(MiningConfiguration copy)
        {
            BookmarkLastPosition = copy.BookmarkLastPosition;
            DelayActivation = copy.DelayActivation;
            DistributeLasers = copy.DistributeLasers;
            Ice_DoMine = new Dictionary<string, bool>(copy.Ice_DoMine);
            Ice_Priority = new List<string>(copy.Ice_Priority);
            IceMining = copy.IceMining;
            JetcanNameFormat = copy.JetcanNameFormat;
            MinDistanceToPlayers = copy.MinDistanceToPlayers;
            NumCrystalsToCarry = copy.NumCrystalsToCarry;
            Ore_DoMine = new Dictionary<string, bool>(copy.Ore_DoMine);
            Ore_Priority = new List<string>(copy.Ore_Priority);
            ShortCycle = copy.ShortCycle;
            StripMine = copy.StripMine;
            StripRangeMultiplier = copy.StripRangeMultiplier;
            UseMiningDrones = copy.UseMiningDrones;
        }
    }

    [ProtoContract()]
    public sealed class HaulingConfiguration
    {
        [ProtoMember(1, IsRequired = true)]
        public DropoffTypes DropoffType;
        [ProtoMember(2)]
        public string DropoffName;
        [ProtoMember(3)]
        public int DropoffID;
        [ProtoMember(4)]
        public int HangarDivision;
        [ProtoMember(5, IsRequired = true)]
        public HaulerModes HaulerMode;
        [ProtoMember(6)]
        public string PickupSystemBookmark;
        [ProtoMember(7)]
        public double CargoFullThreshold = 0;
        [ProtoMember(8)]
        public bool AlwaysPopCans = false;


        public HaulingConfiguration()
        {

        }

        public HaulingConfiguration(HaulingConfiguration copy)
        {
            DropoffType = copy.DropoffType;
            DropoffName = copy.DropoffName;
            DropoffID = copy.DropoffID;
            HangarDivision = copy.HangarDivision;
            HaulerMode = copy.HaulerMode;
            PickupSystemBookmark = copy.PickupSystemBookmark;
            CargoFullThreshold = copy.CargoFullThreshold;
        }
    }

    [ProtoContract()]
    public sealed class MovementConfiguration
    {
        [ProtoMember(1)]
        public bool UseBeltBookmarks;
        [ProtoMember(2)]
        public bool UseRandomBeltOrder;
        [ProtoMember(3)]
        public bool BounceWarp;
        [ProtoMember(4)]
        public double MaxSlowboatTime;

        [ProtoMember(5)]
        public string SafeBookmarkPrefix;
        [ProtoMember(6)]
        public string AsteroidBeltBookmarkPrefix;
        [ProtoMember(7)]
        public string IceBeltBookmarkPrefix;
        [ProtoMember(8)]
        public string AmmoRearmBookmarkPrefix;
        [ProtoMember(9)]
        public string TemporaryBeltBookMarkPrefix;
        [ProtoMember(10)]
        public string TemporaryCanBookMarkPrefix;

        public MovementConfiguration()
        {

        }

        public MovementConfiguration(MovementConfiguration copy)
        {
            UseBeltBookmarks = copy.UseBeltBookmarks;
            UseRandomBeltOrder = copy.UseRandomBeltOrder;
            BounceWarp = copy.BounceWarp;
            MaxSlowboatTime = copy.MaxSlowboatTime;
            SafeBookmarkPrefix = copy.SafeBookmarkPrefix;
            AsteroidBeltBookmarkPrefix = copy.AsteroidBeltBookmarkPrefix;
            IceBeltBookmarkPrefix = copy.IceBeltBookmarkPrefix;
            AmmoRearmBookmarkPrefix = copy.AmmoRearmBookmarkPrefix;
            TemporaryBeltBookMarkPrefix = copy.TemporaryBeltBookMarkPrefix;
            TemporaryCanBookMarkPrefix = copy.TemporaryCanBookMarkPrefix;
        }
    }

    [ProtoContract()]
    public sealed class MainConfiguration
    {
        [ProtoMember(1)]
        public bool UseRandomWaits;
        [ProtoMember(2)]
        public bool UseRelaunching;
        [ProtoMember(3)]
        public string CharacterSetToRelaunch;
        [ProtoMember(4)]
        public bool RelaunchAfterDowntime;
        [ProtoMember(5)]
        public bool Disable3DRender;
        [ProtoMember(6)]
        public bool DisableUIRender;
        [ProtoMember(7, IsRequired = true)]
        public PulseSpeeds PulseSpeed;
        [ProtoMember(8)]
        public bool UseMaxRuntime;
        [ProtoMember(9)]
        public int MaxRuntime;
        [ProtoMember(10)]
        public bool UseResumeAfter;
        [ProtoMember(11)]
        public int ResumeAfter;
        [ProtoMember(12)]
        public bool EnableUiIpc = false;

        public MainConfiguration()
        {

        }

        public MainConfiguration(MainConfiguration copy)
        {
            UseRandomWaits = copy.UseRandomWaits;
            UseRelaunching = copy.UseRelaunching;
            CharacterSetToRelaunch = copy.CharacterSetToRelaunch;
            RelaunchAfterDowntime = copy.RelaunchAfterDowntime;
            Disable3DRender = copy.Disable3DRender;
            DisableUIRender = copy.DisableUIRender;
            PulseSpeed = copy.PulseSpeed;
            UseMaxRuntime = copy.UseMaxRuntime;
            MaxRuntime = copy.MaxRuntime;
            UseResumeAfter = copy.UseResumeAfter;
            ResumeAfter = copy.ResumeAfter;
            EnableUiIpc = copy.EnableUiIpc;
        }
    }

    /// <summary>
    /// Hold all fleet-related variables
    /// </summary>
    [ProtoContract()]
    public sealed class FleetConfiguration
    {
        [ProtoMember(1)]
        public List<int> BuddyCharIDsToInvite = new List<int>();
        [ProtoMember(2)]
        public bool DoFleetInvites = false;
        [ProtoMember(3)]
        public List<int> FleetCharIDsToSkip = new List<int>();
        [ProtoMember(4)]
        public bool OnlyHaulForSkipList = false;

        public FleetConfiguration()
        {

        }

        public FleetConfiguration(FleetConfiguration copy)
        {
            BuddyCharIDsToInvite = new List<int>(copy.BuddyCharIDsToInvite);
            DoFleetInvites = copy.DoFleetInvites;
            FleetCharIDsToSkip = new List<int>(copy.FleetCharIDsToSkip);
            OnlyHaulForSkipList = copy.OnlyHaulForSkipList;
        }
    }

    /// <summary>
    /// Hold any freighting-related variables
    /// </summary>
    [ProtoContract()]
    public sealed class FreightingConfiguration
    {
        [ProtoMember(1, IsRequired = true)]
        public FreighterModes FreighterMode;

        [ProtoMember(2, IsRequired = true)]
        public DropoffTypes PickupType;
        [ProtoMember(3)]
        public string PickupName;
        [ProtoMember(4)]
        public int PickupID;
        [ProtoMember(5, IsRequired = true)]
        public DropoffTypes DropoffType;
        [ProtoMember(6)]
        public string DropoffName;
        [ProtoMember(7)]
        public int DropoffID;
        [ProtoMember(8)]
        public int DropoffFolder;

        public FreightingConfiguration()
        {

        }

        public FreightingConfiguration(FreightingConfiguration copy)
        {
            FreighterMode = copy.FreighterMode;
            PickupType = copy.PickupType;
            PickupName = copy.PickupName;
            PickupID = copy.PickupID;
            DropoffType = copy.DropoffType;
            DropoffName = copy.DropoffName;
            DropoffID = copy.DropoffID;
            DropoffFolder = copy.DropoffFolder;
        }
    }

    /// <summary>
    /// Hold any alert-related configuration variables
    /// </summary>
    [ProtoContract()]
    public sealed class AlertConfiguration
    {
        [ProtoMember(1)]
        public bool UseAlerts;

        [ProtoMember(2)]
        public bool AlertOnLocalUnsafe;
        [ProtoMember(3)]
        public bool AlertOnLocalChat;
        [ProtoMember(4)]
        public bool AlertOnFactionSpawn;
        [ProtoMember(5)]
        public bool AlertOnLowAmmo;
        [ProtoMember(6)]
        public bool AlertOnFreighterNoPickup;
        [ProtoMember(7)]
        public bool AlertOnLongRandomWait;
        [ProtoMember(8)]
        public bool AlertOnPlayerNear;
        [ProtoMember(9)]
        public bool AlertOnFlee;
        [ProtoMember(10)]
        public bool AlertOnTargetJammed;
        [ProtoMember(11)]
        public bool AlertOnWarpJammed;

        public AlertConfiguration()
        {

        }

        public AlertConfiguration(AlertConfiguration copy)
        {
            UseAlerts = copy.UseAlerts;
            AlertOnLocalUnsafe = copy.AlertOnLocalUnsafe;
            AlertOnLocalChat = copy.AlertOnLocalChat;
            AlertOnFactionSpawn = copy.AlertOnFactionSpawn;
            AlertOnLowAmmo = copy.AlertOnLowAmmo;
            AlertOnFreighterNoPickup = copy.AlertOnFreighterNoPickup;
            AlertOnLongRandomWait = copy.AlertOnLongRandomWait;
            AlertOnPlayerNear = copy.AlertOnPlayerNear;
            AlertOnFlee = copy.AlertOnFlee;
        }
    }
}