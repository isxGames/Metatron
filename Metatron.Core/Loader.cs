using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using InnerSpaceAPI;
using LavishScriptAPI;
using LavishVMAPI;

[assembly: System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)]

namespace Metatron.Core
{
    public class Loader
    {
        private readonly EventHandler<LSEventArgs> _MetatronUpdateCompleted, _MetatronUpdated, _missionDatabaseUpdateCompleted, _npcBountiesUpdateCompleted, _possibleEwarNpcNamesUpdateCompleted;
        private volatile bool _isMetatronUpdateComplete, _wasMetatronUpdated, _isMissionDatabaseUpdateComplete, _isNpcBountiesUpdateCompleted, _isPossibleEwarNpcNamesUpdateComplete;

        private readonly string _productVersion;
        private readonly string[] _args;

        public bool LoadedSuccessfully;
        public string LoadErrorMessage;

        private static readonly uint _minimumInnerSpaceBuild = 5866;
        private static readonly DateTime _minimumIsxeveVersionDate = new DateTime(2013, 09, 11);
        private static readonly int _minimumIsxeveVersionBuild = 0002;

        public Loader(string productVersion, string[] args)
        {
            _MetatronUpdateCompleted = MetatronUpdateCompleted;
            _MetatronUpdated = MetatronUpdated;
            _missionDatabaseUpdateCompleted = MissionDatabaseUpdateCompleted;
            _npcBountiesUpdateCompleted = NpcBountiesUpdateCompleted;
            _possibleEwarNpcNamesUpdateCompleted = PossibleEwarNpcNamesUpdateCompleted;

            _productVersion = productVersion;
            for (var indexOf = _productVersion.IndexOf('.'); indexOf >= 0; indexOf = _productVersion.IndexOf('.'))
                _productVersion = _productVersion.Remove(indexOf, 1);

            _args = args;
        }

        public void Load()
        {
            if (!IsRunningInInnerSpace())
            {
                LoadErrorMessage = "Error: Metatron must be started through InnerSpace, not by directly running the program.";
                return;
            }

            CheckForUpdates();

            if (_wasMetatronUpdated)
                return;

            LoadErrorMessage = PerformSafetyChecks();

            if (LoadErrorMessage != null)
                return;

            LoadedSuccessfully = true;
        }

        private static string PerformSafetyChecks()
        {
            if (!IsIsxeveLoaded())
                return "Error: ISXEVE not detected. Metatron requires ISXEVE to run.";

            if (!IsMinimumIsxeveVersionLoaded())
            {
                var stringBuilder = new StringBuilder();

                var minimumIsxeveVersion = string.Format("{0}.{1}", _minimumIsxeveVersionDate.ToString("yyyyMMdd"), _minimumIsxeveVersionBuild);
                var errorVersionLine = "Error: The loaded ISXEVE is out of date. ISXEVE version {0} or later is required for Metatron to run well.";
                var formattedVersionLine = string.Format(errorVersionLine, minimumIsxeveVersion);

                stringBuilder.AppendLine(formattedVersionLine);
                stringBuilder.AppendLine(@"Official test builds can be found here: http://www.isxgames.com/isxeve/test/");
                stringBuilder.AppendLine("Other test builds can frequently be found in the #isxeve topic or by asking in #isxeve.");
                stringBuilder.AppendLine("If you have any questions, contact NostraThomas in #isxeve");

                return stringBuilder.ToString();
            }

            if (IsNewVirtualInputPresent())
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.AppendLine($"InnerSpace's New Virtual Input has been detected as enabled. This prevents Metatron from working correctly.")
                    .AppendLine($"Please disable this by right clicking on Innerspace in the system tray and clicking 'Configuration' and checking 'Disable New Virtual Input'. You will need to restart both EVE and Metatron.");

                return stringBuilder.ToString();
            }

            if (!CheckInnerSpaceVersion())
                return "Error: You are running an outdated build of InnerSpace. Please ensure you're running development builds and patch InnerSpace.";

            return null;
        }

        private static bool IsNewVirtualInputPresent()
        {
            string ldioOutput = null;

            try
            {
                LavishScript.DataParse("${ldiopatch.Get[route-input-devices](exists)}", ref ldioOutput);
            }
            catch (Exception ex)
            {
                ldioOutput = null;
            }
            
            if (ldioOutput == "TRUE")
            {
                return true;
            }
            return false;
        }

        private static bool IsMinimumIsxeveVersionLoaded()
        {
            string isxeveVersion = null;

            try
            {
                LavishScript.DataParse("${ISXEVE.Version}", ref isxeveVersion);
            }
            catch (Exception)
            {
                isxeveVersion = null;
            }

            if (string.IsNullOrEmpty(isxeveVersion))
                return false;

            var fragments = isxeveVersion.Split('.');

            if (fragments.Length != 2)
                return false;

            var dateString = fragments[0];
            var versionString = fragments[1];

            DateTime? date;
            try
            {
                date = DateTime.ParseExact(dateString, "yyyyMMdd", null);
            }
            catch (FormatException)
            {
                return false;
            }

            int version;
            if (!int.TryParse(versionString, out version))
                return false;

            if (date > _minimumIsxeveVersionDate)
                return true;

            if (date == _minimumIsxeveVersionDate && version >= _minimumIsxeveVersionBuild)
                return true;

            return false;
        }

        private static bool IsRunningInInnerSpace()
        {
            return InnerSpaceAPI.InnerSpace.BuildNumber > 0;
        }

        private static bool IsIsxeveLoaded()
        {
            var isxEveLoaded = false;

            try
            {
                LavishScript.DataParse("${ISXEVE(exists)}", ref isxEveLoaded);
            }
            catch { }

            return isxEveLoaded;
        }

        private static bool CheckInnerSpaceVersion()
        {
            return InnerSpaceAPI.InnerSpace.BuildNumber >= _minimumInnerSpaceBuild;
        }

        private void CheckForUpdates()
        {
            UpdateMetatron();
            UpdateMissionDatabase();
            //UpdateNpcBounties();
            UpdatePossibleEwarNpcNames();

            //If we updated file, relaunch Metatron.
            if (!_wasMetatronUpdated)
                return;

            var command = new StringBuilder(String.Format("TimedCommand 10 \"dotnet Metatron\""));

            if (_args.Length > 0)
                command.Append(" true");

            LavishScript.ExecuteCommand(command.ToString());
        }

        private void UpdatePossibleEwarNpcNames()
        {
            LavishScript.Events.AttachEventTarget("PossibleEwarNpcNames_OnUpdateComplete", _possibleEwarNpcNamesUpdateCompleted);

            var possibleEwarNpcNamesVersion = ReadPossibleEwarNpcNamesVersion();
            LavishScript.ExecuteCommand(
                String.Format("dotnet {0} isxGamesPatcher {0} {1} https://github.com/isxGames/Metatron/releases/download/latest/isxGamesPatcher_PossibleEwarNpcNames.xml",
                "PossibleEwarNpcNames", possibleEwarNpcNamesVersion));

            var sanityCounter = 300;
            while (!_isPossibleEwarNpcNamesUpdateComplete && sanityCounter > 0)
            {
                Frame.Wait(false);
                sanityCounter--;
            }

            LavishScript.Events.DetachEventTarget("PossibleEwarNpcNames_OnUpdateComplete", _possibleEwarNpcNamesUpdateCompleted);
        }

        private void UpdateNpcBounties()
        {
            var npcBountiesPath = string.Format("{0}\\{1}", Path.Combine(Path.Combine(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "Metatron"), "Data"), "NpcBounties.bin");

            if (File.Exists(npcBountiesPath))
            {
                var fileInfo = new FileInfo(npcBountiesPath);
                if (fileInfo.Length > 0) return;
            }

            LavishScript.Events.AttachEventTarget("npcBounties_OnUpdateComplete", _npcBountiesUpdateCompleted);

            LavishScript.ExecuteCommand(
                String.Format("dotnet {0} isxGamesPatcher {0} {1} https://github.com/isxGames/Metatron/releases/download/latest/isxGamesPatcher_NpcBounties.xml",
                "NpcBounties", 0));

            var sanityCounter = 300;
            while (!_isNpcBountiesUpdateCompleted && sanityCounter > 0)
            {
                Frame.Wait(false);
                sanityCounter--;
            }

            LavishScript.Events.DetachEventTarget("npcBounties_OnUpdateComplete", _npcBountiesUpdateCompleted);
        }

        private void UpdateMetatron()
        {

            LavishScript.Events.AttachEventTarget("Metatron_OnFileUpdated", _MetatronUpdated);
            //LavishScript.Events.AttachEventTarget("Metatron_OnFileUpdated", _MetatronUpdated);

#if DEBUG
#else
			LavishScript.ExecuteCommand(
				String.Format("dotnet {0} isxGamesPatcher {0} {1} https://github.com/isxGames/Metatron/releases/download/latest/isxGamesPatcher_Metatron.xml",
				"Metatron", _productVersion));
#endif

            //wait for UpdateComplete
            var sanityCounter = 300;    //5 seconds @ 60fps, 10 seconds at 30fps
            while (!_isMetatronUpdateComplete && sanityCounter > 0)
            {
                Frame.Wait(false);
                sanityCounter--;
            }

            LavishScript.Events.DetachEventTarget("Metatron_OnFileUpdated", _MetatronUpdated);
            //LavishScript.Events.DetachEventTarget("Metatron_OnFileUpdated", _MetatronUpdated);

        }

        private void UpdateMissionDatabase()
        {
            LavishScript.Events.AttachEventTarget("missiondatabase_OnUpdateComplete", _missionDatabaseUpdateCompleted);

            var missionDatabaseVersion = ReadMissionDatabaseVersion();
            LavishScript.ExecuteCommand(
                String.Format("dotnet {0} isxGamesPatcher {0} {1} https://github.com/isxGames/Metatron/releases/download/latest/isxGamesPatcher_MissionDatabase.xml",
                "MissionDatabase", missionDatabaseVersion));

            var sanityCounter = 300;
            while (!_isMissionDatabaseUpdateComplete && sanityCounter > 0)
            {
                Frame.Wait(false);
                sanityCounter--;
            }

            LavishScript.Events.DetachEventTarget("missiondatabase_OnUpdateComplete", _missionDatabaseUpdateCompleted);
        }

        private void MetatronUpdateCompleted(object sender, LSEventArgs e)
        {
            _isMetatronUpdateComplete = true;
        }

        private void MetatronUpdated(object sender, LSEventArgs e)
        {
            _wasMetatronUpdated = true;
        }

        private void MissionDatabaseUpdateCompleted(object sender, LSEventArgs e)
        {
            _isMissionDatabaseUpdateComplete = true;
        }

        private int ReadMissionDatabaseVersion()
        {
            var xmlDocument = new XmlDocument();

            var path = string.Format("{0}\\{1}", Path.Combine(Path.Combine(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "Metatron"), "Data"), "MissionDatabase.xml");

            if (!File.Exists(path))
                return 0;

            xmlDocument.Load(path);

            var missionsNode = xmlDocument.SelectSingleNode("/Missions");

            if (missionsNode == null)
                return 0;

            var versionAttribute = missionsNode.Attributes["MissionDatabaseVersion"];

            if (versionAttribute == null)
                return 0;

            var versionString = versionAttribute.Value;

            var returnValue = 0;

            int.TryParse(versionString, out returnValue);

            return returnValue;
        }

        private int ReadPossibleEwarNpcNamesVersion()
        {
            var xmlDocument = new XmlDocument();

            var path = string.Format("{0}\\{1}", Path.Combine(Path.Combine(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "Metatron"), "Data"), "PossibleEwarNpcNames.xml");

            if (!File.Exists(path))
                return 0;

            xmlDocument.Load(path);

            var missionsNode = xmlDocument.SelectSingleNode("/PossibleEwarNpcNames");

            if (missionsNode == null)
                return 0;

            var versionAttribute = missionsNode.Attributes["Version"];

            if (versionAttribute == null)
                return 0;

            var versionString = versionAttribute.Value;

            var returnValue = 0;

            int.TryParse(versionString, out returnValue);

            return returnValue;
        }

        private void NpcBountiesUpdateCompleted(object sender, LSEventArgs e)
        {
            _isNpcBountiesUpdateCompleted = true;
        }

        private void PossibleEwarNpcNamesUpdateCompleted(object sender, LSEventArgs e)
        {
            _isPossibleEwarNpcNamesUpdateComplete = true;
        }
    }
}
