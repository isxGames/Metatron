using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.IO;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    internal sealed class MissionDatabase : ModuleBase, IMissionDatabase
    {
		private readonly List<Mission> _missions = new List<Mission>();

	    private readonly string _missionDbFileName = "MissionDatabase.xml";
        private readonly string _missionDbFilePath = string.Empty;

		DateTime _lastModified = DateTime.MinValue;

		public MissionDatabase()
		{
			ModuleName = "MissionDatabase";
			IsEnabled = false;
			_missionDbFilePath = Path.Combine(Metatron.DataDirectory, _missionDbFileName);
		}

		public override bool Initialize()
		{
			if (!IsInitialized)
			{
				LoadMissionDatabase();
				IsInitialized = true;
			}
			return IsInitialized;
		}

		public void ReloadMissionDatabase()
		{
			var methodName = "ReloadMissionDatabase";
			LogTrace(methodName);

			LoadMissionDatabase();
		}

		private void LoadMissionDatabase()
		{
			var methodName = "LoadMissionDatabase";
			LogTrace(methodName);

			//Clear any existing entries
			_missions.Clear();

			//If the file doesn't exist, return
			if (!File.Exists(_missionDbFilePath))
				return;

			//If we haven't set lastModified or File lastModified is past stored lastModified
			if (_lastModified != DateTime.MinValue && File.GetLastWriteTime(_missionDbFilePath).CompareTo(_lastModified) < 0)
				return;

			_lastModified = File.GetLastWriteTime(_missionDbFilePath);

			//Get a textreader for the file
			using (TextReader textReader = new StreamReader(_missionDbFilePath))
			{
				//XmlDocument for working with the XML
				var xmlDocument = new XmlDocument();
				//Load it from the textreader
			    try
			    {
                    xmlDocument.Load(textReader);
			    }
			    catch (XmlException e)
			    {
			        LogException(e, methodName, "Caught exception while loading the MissionDatabase:");

			        IsInitialized = true;
			        return;
			    }
				

				//Get a node list for the document. It should -never- be an empty set.
				var xmlNodes = xmlDocument.SelectNodes("/Missions/Mission");

                if (xmlNodes == null)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Error: {0} is malformed. Nodes \"/Missions/Mission\" are missing.");
                    return;
                }

				//Iterate them. This level would be each Mission element.
				foreach (XmlNode missionNode in xmlNodes)
				{
					//Grab the mission name...
					var missionName = missionNode.Attributes["Name"].Value;

					//Create a mission for this name
					var mission = new Mission(missionName);

					//Try to parse the IsEmpireKill flag
					var isEmpireKillAttribute = missionNode.Attributes["IsEmpireKill"];
					if (isEmpireKillAttribute != null)
					{
						bool isEmpireKill;
						bool.TryParse(isEmpireKillAttribute.Value, out isEmpireKill);
						mission.IsEmpireKill = isEmpireKill;
					}

					//Grab the match type...
					var nameMatchTypeAttribute = missionNode.Attributes["NameMatchType"];
					//Set it if not null
					if (nameMatchTypeAttribute != null)
					{
						mission.NameMatchType = nameMatchTypeAttribute.Value;
					}

				    var ratTypeAttribute = missionNode.Attributes["RatTypes"];
                    if (ratTypeAttribute != null)
                    {
                        var ratTypes = ratTypeAttribute.Value.Split(',');
                        mission.RatTypes.AddRange(ratTypes);
                    }

					//Get the ActionSets node...
					var actionSets = missionNode.SelectNodes("ActionSets/ActionSet");

					//Iterate each actionset node.
					foreach (XmlNode actionSetNode in actionSets)
					{
						//Get the level, convert it to int
						var missionLevelString = actionSetNode.Attributes["Level"].Value;
						int missionLevel;
						int.TryParse(missionLevelString, out missionLevel);

						//Create an ActionSet
						var actionSet = new ActionSet(missionLevel);

						//Get the Actions node...
						var actions = actionSetNode.SelectNodes("Actions/Action");

						//Iterate each action node
						foreach (XmlNode actionNode in actions)
						{
							//Get the name of the action
							var actionName = actionNode.Attributes["Name"].Value;

							//Create an action
							var action = new Action(actionName);

							//get the child nodes
							foreach (XmlNode actionParameterNode in actionNode.ChildNodes)
							{
								//Three possibilities here - GroupID, TargetName, and NearWreck
								switch (actionParameterNode.Name)
								{
									case "GateName":
										action.GateName = actionParameterNode.InnerText;
										break;
									case "TimeoutSeconds":
										int timeoutSeconds;
										int.TryParse(actionParameterNode.InnerText, out timeoutSeconds);
										action.TimeoutSeconds = timeoutSeconds;
										break;
									case "GroupID":
										int groupId;
										int.TryParse(actionParameterNode.InnerText, out groupId);
										action.GroupId = groupId;
										break;
									case "NearWreck":
										action.NearWreck = actionParameterNode.InnerText;
										break;
									case "ItemName":
										action.ItemName = actionParameterNode.InnerText;
										break;
									case "Distance":
										int distance;
										int.TryParse(actionParameterNode.InnerText, out distance);
										action.Distance = distance;
										break;
									case "TypeID":
										int typeId;
										int.TryParse(actionParameterNode.InnerText, out typeId);
										action.TypeId = typeId;
										break;
									case "TargetName":
										action.TargetName = actionParameterNode.InnerText;
										break;
									case "BreakOnTargeted":
										bool breakOnTargeted;
										bool.TryParse(actionParameterNode.InnerText, out breakOnTargeted);
										action.BreakOnTargeted = breakOnTargeted;
										break;
									case "BreakOnSpawn":
										bool breakOnSpawn;
										bool.TryParse(actionParameterNode.InnerText, out breakOnSpawn);
										action.BreakOnSpawn = breakOnSpawn;
										break;
									case "PossibleTriggerNames":
										foreach (XmlNode possibleTriggerNameNode in actionParameterNode.ChildNodes)
										{
											action.PossibleTriggerNames.Add(possibleTriggerNameNode.InnerText);
										}
										break;
									case "DoNotKillNames":
										foreach (XmlNode doNotkillNameNode in actionParameterNode.ChildNodes)
										{
											action.DoNotKillNames.Add(doNotkillNameNode.InnerText);
										}
										break;
                                    case "PreventSalvageBookmark":
								        bool preventSalvageBookmark;
								        bool.TryParse(actionParameterNode.InnerText, out preventSalvageBookmark);
								        action.PreventSalvageBookmark = preventSalvageBookmark;
								        break;
								}
							}

							//Ok, now the action's built. Add it to the action set.
							actionSet.Actions.Add(action);
						}

						//Ok, now the action set is built. Add it to the mission.
						mission.ActionSets.Add(actionSet);
					}

					//Aaand now the mission is parsed. Add it to the list.
					_missions.Add(mission);
				}
			}
		}

		/// <summary>
		/// Get a mission from the database matching the given name, if it exists.
		/// Otherwise return null.
		/// </summary>
		/// <param name="missionName"></param>
		/// <returns></returns>
		public Mission GetMissionByName(string missionName)
		{
			return _missions.FirstOrDefault(mission => mission.Name.Equals(missionName, StringComparison.InvariantCultureIgnoreCase));
		}

        public List<DamageProfile> GetNpcResistanceProfiles(CachedMission cachedMission)
        {
            var damageProfiles = new List<DamageProfile>();

            var mission = Metatron.MissionDatabase.GetMissionByName(cachedMission.Name);

            if (mission == null || (mission.RatTypes.Count == 0 && cachedMission.ParsedRatTypes.Count == 0))
            {
                damageProfiles.Add(DamageProfile.Default);
                return damageProfiles;
            }

            if (mission.RatTypes.Count == 0)
                mission.RatTypes = cachedMission.ParsedRatTypes;

            damageProfiles.AddRange(mission.RatTypes.Select(DamageProfile.GetNpcDamageProfile));

            return damageProfiles;
        }
	}
}
