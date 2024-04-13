using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using LavishVMAPI;
using ProtoBuf;
using Metatron.Core.Config;
using Metatron.Core.EventCommunication;
using Metatron.Core.Extensions;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    // ReSharper disable ConvertToConstant.Local
    // ReSharper disable StringIndexOfIsCultureSpecific.1

    public sealed class ConfigurationManager : ModuleBase, IConfigurationManager
    {
        //EventHandler for updating config profile references
        private readonly EventHandler<ConfigurationFilesUpdatedEventArgs> _configurationFilesUpdated;
		private readonly static string IoExceptionFileUsedByOtherProcess = "because it is being used by another process.";

        private readonly Dictionary<string, Dictionary<string, ConfigProperty>> _configProfilesByNameByName = new Dictionary<string, Dictionary<string, ConfigProperty>>();
        public Dictionary<string, Dictionary<string, ConfigProperty>> ConfigProfilesByName
        {
            get { return _configProfilesByNameByName; }
        }
 
		public Dictionary<string, ConfigProperty> ActiveConfigProfile
		{
            get { return _configuration.ConfigProperties; }
            set { _configuration.ConfigProperties = value; }
		}
		public string ActiveConfigName { get; private set; }

		//Event for notifying things config has loaded
		public event EventHandler ConfigLoaded;

        private readonly IConfiguration _configuration;
        private readonly IMeCache _meCache;


		public ConfigurationManager(IConfiguration configuration, IMeCache meCache)
		{
		    _configuration = configuration;
		    _meCache = meCache;

		    //This should default to "not cleaned up".
			IsCleanedUpOutOfFrame = false;

			IsEnabled = false;
			ModuleName = "ConfigurationManager";

            _configurationFilesUpdated = OnConfigurationFilesUpdated;
			Metatron.EventCommunications.ConfigurationFilesUpdatedEvent.EventRaised += _configurationFilesUpdated;
		}

		public override bool OutOfFrameCleanup()
		{
			if (!IsCleanedUpOutOfFrame)
			{
				if (!_isCleaningUp)
				{
					//Save any active config profile
					if (ActiveConfigName != String.Empty && ActiveConfigProfile != null)
					{
						SaveConfigProfile(ActiveConfigName);
					}
					_isCleaningUp = true;
				}
			}
			return IsCleanedUpOutOfFrame;
		}

		private void FileWriteComplete()
		{
			//Only set "IsCleanedUpOutOfFrame" if we're actually cleaning up or we'll end up
			//skipping cleanup
			if (_isCleaningUp)
			{
				lock (this)
				{
					IsCleanedUpOutOfFrame = true;
				}
			}
		}

        private void OnConfigurationFilesUpdated(object sender, ConfigurationFilesUpdatedEventArgs e)
		{
			var methodName = "EVENT_UpdateConfigurationFile";
			LogTrace(methodName, "OldFileName: {0}, NewFileName: {1}",
				e.OldFileName, e.NewFileName);

			//If I've got a new name...
			if (e.NewFileName != string.Empty)
			{
				//If I've also got an old name it means rename
				if (e.OldFileName != string.Empty)
				{
                    if (_configProfilesByNameByName.ContainsKey(e.OldFileName))
					{
						//Temp reference to avoid losing the ref
                        lock (_configProfilesByNameByName)
						{
                            var tempConfig = _configProfilesByNameByName[e.OldFileName];

							//RemoveBookmarkAndCacheEntry the old ref
                            _configProfilesByNameByName.Remove(e.OldFileName);

							//Add the new ref
                            _configProfilesByNameByName.Add(e.NewFileName, tempConfig);
						}
					}
				}
				else
				{
					//No old name means add
                    if (!_configProfilesByNameByName.ContainsKey(e.NewFileName))
					{
						//This means re-load files.
						LoadConfigurationFiles();
					}
				}
			}
			else
			{
				//No new name means erase.
                if (_configProfilesByNameByName.ContainsKey(e.OldFileName))
				{
                    _configProfilesByNameByName.Remove(e.OldFileName);
				}
			}

			//Fire the "config reloaded" event to reload the UI
			OnConfigLoaded();
		}

		#region Management methods
		public void LoadConfigurationFiles()
		{
			var methodName = "LoadConfigurationFiles";
			LogTrace(methodName);

			var configFiles = Directory.GetFiles(String.Format("{0}\\Metatron\\config", Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath))).ToList();

			//Loop all the results to load them
			foreach (var file in configFiles)
			{
				//validate files
				if (!file.Contains("Config-") || !file.EndsWith(".bin"))
				{
					continue;
				}

				//Split it on the \\ because it gives full path
				var parts = file.Split('\\');
				var last = parts.Last();
				//Make sure to strip the '.bin' and the Config-
				var profileName = last.Remove(last.IndexOf(".bin"), 4);
				profileName = profileName.Remove(profileName.IndexOf("Config-"), 7);

				//If we've already got the file loaded, continue
                if (_configProfilesByNameByName.ContainsKey(profileName))
				{
					continue;
				}

				Dictionary<string, ConfigProperty> tempConfig = null;
				if (File.Exists(file))
				{
					var succeeded = false;
					while (!succeeded)
					{
						try
						{
							//Open a stream for deserializing it
							using (Stream fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
							{
								//Actually deserialize it
								tempConfig = Serializer.Deserialize<Dictionary<string, ConfigProperty>>(fileStream);
								succeeded = true;
							}
						}
						catch (EndOfStreamException)
						{
							LogMessage(methodName, LogSeverityTypes.Standard, "Error; Config profile \"{0}\" is corrupted. Deleting it.",
								profileName);
							File.Delete(file);
						    break;
						}
						catch (IOException e)
						{
							if (e.Message.Contains(IoExceptionFileUsedByOtherProcess))
							{
								LogMessage(methodName, LogSeverityTypes.Standard, "Error; Config profile \"{0}\" is being read by another process. Waiting 50ms before trying again.",
									profileName);
								System.Threading.Thread.Sleep(50);
							}
							//this is the only one we don't break loop for
						}
						catch (ProtoException e)
						{
                            LogException(e, methodName, "Error; Config profile \"{0}\" is not deserializable. Deleting it.",
                                profileName);
							File.Delete(file);
						    break;
						}
						catch (ArgumentNullException)
						{
							LogMessage(methodName, LogSeverityTypes.Standard, "Error; Config profile \"{0}\" is of an unrecognized (likely pre-0.5.4.7) format and cannot be converted. Deleting it.",
								profileName);
							File.Delete(file);
						    break;
						}
						catch (Exception e)
						{
							LogException(e, methodName, "Caught exception.");
						    break;
						}
					}
				}

				if (tempConfig != null)
				{
                    _configProfilesByNameByName.Add(profileName, tempConfig);
				}
				else
				{
					LogMessage(methodName, LogSeverityTypes.Standard, "Error; could not deserialize config profile \"{0}\" for unknown reasons.",
						profileName);
					File.Delete(file);
				}
			}

			//Loada config profile if necessary.
			if (ActiveConfigProfile != null) 
				return;

			//Now that all the profiles are loaded, try to make the correct one active.
			//Load the Metatron\\config\\SBProfiles.txt file and try to figure out which profile this character is using
			var profilesFilePath = string.Format("{0}\\{1}", String.Format("{0}\\Metatron\\config", Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath)), "SBProfiles.txt");
			if (File.Exists(profilesFilePath))
			{
				string name = string.Empty, profile = string.Empty;

				using (var fileStream = new StreamReader(File.Open(profilesFilePath, FileMode.Open)))
				{
					//Loop until we run out of lines
					while (fileStream.Peek() != -1)
					{
						var line = fileStream.ReadLine();

					    if (line == null) continue;

						//Format: Some Character,Config-Some Config.bin\nAnotherCharacter,Config-Another Config.bin\netc...
						var parts = line.Split(',');

						//If this is our character's config
                        if (parts.Length != 2 || parts[0] != _meCache.Name || !_configProfilesByNameByName.ContainsKey(parts[1])) 
							continue;

						name = parts[0];
						profile = parts[1];
						break;
					}
				}

				if (name != string.Empty &&
                    _configProfilesByNameByName.ContainsKey(profile))
				{
					LoadConfigProfile(profile, false);
					//Make it active
					LogMessage(methodName, LogSeverityTypes.Standard, "Making this character's default profile '{0}' active.",
						ActiveConfigName);
				}
			}

			//If the file didn't exist or we couldn't find a matching default config, load the first config.
			if (ActiveConfigProfile == null)
			{
				//If we have a first profile, use it.
                if (_configProfilesByNameByName.Count > 0)
				{
					LogMessage(methodName, LogSeverityTypes.Standard, "Could not find a default profile for this character; making first profile active.");
                    LoadConfigProfile(_configProfilesByNameByName.Keys.First(), false);
				}
				else
				{
					//Otherwise make a new profile
                    var profileName = String.Format("{0} Default Profile", _meCache.Name);
					AddConfigProfile(profileName, false);
                    ActiveConfigProfile = _configProfilesByNameByName[profileName];
					ActiveConfigName = profileName;
					LogMessage(methodName, LogSeverityTypes.Standard, "Could not find a profile to use; creating a new profile.");
					SaveConfigProfile(ActiveConfigName);
				}
			}
			OnConfigLoaded();
		}

        public void RenameConfigProfile(string currentProfileName, string newProfileName)
		{
			var methodName = "RenameConfigProfile";
			LogTrace(methodName, "CurrentProfileName: {0}, NewProfileName: {1}", currentProfileName, newProfileName);

            if (!_configProfilesByNameByName.ContainsKey(currentProfileName)) 
				return;

			//Build the filepaths
			string currentFilePath = String.Format("{0}\\Config-{1}.bin", String.Format("{0}\\Metatron\\config", Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath)), currentProfileName),
				    newFilePath = String.Format("{0}\\Config-{1}.bin", String.Format("{0}\\Metatron\\config", Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath)), newProfileName);

			//Rename the actual file, if it exists
			if (!File.Exists(currentFilePath)) 
				return;

			var succeeded = false;
			var resetEvent = new System.Threading.AutoResetEvent(false);
			while (!succeeded)
			{
				try
				{
					File.Move(currentFilePath, newFilePath);
					succeeded = true;
				}
				catch (Exception e)
				{
					LogException(e, methodName, "Caught exception while trying to rename config profile \"{0}\":", currentProfileName);
					resetEvent.WaitOne(50);
				}
			}

            //Update the references if it succeeded
            //TODO: Fix when we have ExecuteTimedCommand
            //Metatron.EventCommunications.ConfigurationFilesUpdatedEvent.SendEventFromArgs(
            //    Metatron.ModuleManager.UplinkName, currentProfileName, newProfileName);

            //Rename the profile
            //Lock to avoid thread issues
            lock (_configProfilesByNameByName)
            {
                var tempConfig = _configProfilesByNameByName[currentProfileName];
                _configProfilesByNameByName.Remove(currentProfileName);
                _configProfilesByNameByName.Add(newProfileName, tempConfig);

                if (ActiveConfigName == currentProfileName)
                {
                    ActiveConfigName = newProfileName;
                }
                OnConfigLoaded();
			}
		}

        public void AddConfigProfile(string configProfileName, bool reloadConfig)
		{
			var methodName = "AddConfigProfile";
			LogTrace(methodName, "ConfigProfileName: {0}, ReloadConfig: {1}",
				configProfileName, reloadConfig);

			//Lock to avoid thread issues
            lock (_configProfilesByNameByName)
			{
                if (_configProfilesByNameByName.ContainsKey(configProfileName)) 
					return;

				var config = new Dictionary<string, ConfigProperty>(ConfigurationBase.DefaultConfigProperties);

                _configProfilesByNameByName.Add(configProfileName, config);

				//notify other instances of the new config profile
                //TODO: Fix when we have ExecuteTimedCommand
                //Metatron.EventCommunications.ConfigurationFilesUpdatedEvent.SendEventFromArgs(
                //    Metatron.ModuleManager.UplinkName, string.Empty, configProfileName);

				if (reloadConfig)
					OnConfigLoaded();
			}
		}

        public void CopyConfigProfile(string configProfileName)
		{
			var methodName = "CopyConfigProfile";
			LogTrace(methodName);

			var newProfileName = String.Format("Copy of {0}", configProfileName);
            if (!_configProfilesByNameByName.ContainsKey(configProfileName))
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Config profiles does not contain source profile \"{0}\".", configProfileName);
                return;
            }

            if (_configProfilesByNameByName.ContainsKey(newProfileName))
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Config profiles already contains new profile \"{0}\".", newProfileName);
                return;
            }

            lock (_configProfilesByNameByName)
			{
				//Get the current existing configuration
                var currentConfig = _configProfilesByNameByName[configProfileName];

				//Create a new copy
				//var newConfig = new Dictionary<string, ConfigProperty>(currentConfig);
                //var newConfig = new Dictionary<string, ConfigProperty>();
                //foreach (var pair in currentConfig)
                //{
                //    newConfig.Add(pair.Key, pair.Value.Copy());
                //}
			    Dictionary<string, ConfigProperty> newConfig;
                using (var memoryStream = new MemoryStream())
                {
                    Serializer.Serialize(memoryStream, currentConfig);
                    memoryStream.Position = 0;
                    newConfig = Serializer.Deserialize<Dictionary<string, ConfigProperty>>(memoryStream);
                }

				//Save the new config
                _configProfilesByNameByName.Add(newProfileName, newConfig);
				SaveConfigProfile(newProfileName);

				//Notify other instances of the new config
                //TODO: Fix when we have ExecuteTimedCommand
                //Metatron.EventCommunications.ConfigurationFilesUpdatedEvent.SendEventFromArgs(
                //    Metatron.ModuleManager.UplinkName, string.Empty, newProfileName);

				//Reload the UI
				OnConfigLoaded();
			}
		}

        public void RemoveConfigProfile(string configProfileName)
		{
			var methodName = "RemoveConfigProfile";
			LogTrace(methodName, "ConfigProfileName: {0}", configProfileName);

            lock (_configProfilesByNameByName)
			{
                if (_configProfilesByNameByName.ContainsKey(configProfileName))
				{
					//Now remove the reference
                    _configProfilesByNameByName.Remove(configProfileName);
				}
			}

			//Notify other sessions of the removal
            //TODO: Fix when we have ExecuteTimedCommand
            //Metatron.EventCommunications.ConfigurationFilesUpdatedEvent.SendEventFromArgs(
            //    Metatron.ModuleManager.UplinkName, configProfileName, string.Empty);

			var configFilePath = String.Format("{0}\\Config-{1}.bin", String.Format("{0}\\Metatron\\config", 
				Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath)), configProfileName);

		    if (!File.Exists(configFilePath)) return;

		    try
		    {
		        File.Delete(configFilePath);
		    }
		    catch (IOException e)
		    {
		        LogException(e, methodName, "Caught exception while trying to remove config profile \"{0}\": {1}", configProfileName, e.Message);
		    }
		}

		public void LoadConfigProfile(string configProfileName)
		{
			LoadConfigProfile(configProfileName, true);
		}

		private void LoadConfigProfile(string configProfileName, bool fireEvent)
		{
			var methodName = "LoadConfigProfile";
			LogTrace(methodName, "ConfigProfileName: {0}, FireEvent: {1}",
				configProfileName, fireEvent);

			//First, save the active config profile.
			if (ActiveConfigName != string.Empty && ActiveConfigProfile != null)
			{
				SaveConfigProfile(ActiveConfigName);
			}

			//Lock to prevent thread issues
            lock (_configProfilesByNameByName)
			{
                //LogMessage(methodName, LogSeverityTypes.Debug, "Attempting to load profile \"{0}\", isValid: {1}",
                //    configProfileName, _configProfilesByNameByName.ContainsKey(configProfileName));
				//Next, load the new profile
                if (_configProfilesByNameByName.ContainsKey(configProfileName))
				{
                    if (ActiveConfigProfile != null)
					{
                        lock (ActiveConfigProfile)
						{
							ActiveConfigName = configProfileName;
                            ActiveConfigProfile = _configProfilesByNameByName[configProfileName];
						}
					}
					else
					{
						ActiveConfigName = configProfileName;
                        ActiveConfigProfile = _configProfilesByNameByName[configProfileName];
					}

				    if (fireEvent)
				        OnConfigLoaded();
				}
			}

			//Finally, save the 'active' profile in SBProfiles.
			var profilesFilePath = string.Format("{0}\\{1}", String.Format("{0}\\Metatron\\config", Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath)), "SBProfiles.txt");

			//First read in the dictionary of shit already written
			var charProfiles = new Dictionary<string, string>();
			var succeeded = false;

			if (File.Exists(profilesFilePath))
			{
				//Loop to keep trying to read in case another process is
				while (!succeeded)
				{
					try
					{
						using (var stream = new StreamReader(File.Open(profilesFilePath, FileMode.Open)))
						{
							while (stream.Peek() != -1)
							{
								//Read the line and split it
								var line = stream.ReadLine();

							    if (line == null) continue;

								var parts = line.Split(',');

								//Make sure I have the right # of parts and haven't added this char's profile
								if (parts.Length == 2 && !charProfiles.ContainsKey(parts[0]))
								{
									charProfiles.Add(parts[0], parts[1]);
								}
							}
							//Stop looping
							succeeded = true;
						}
					}
					//Do a small wait before trying to read again
					catch (Exception e)
					{
						//Log the error for future use
						LogException(e, methodName, "Caught exception while trying to read default profiles:");
						//Sleep the thread before trying again since this should only happen if the
						//file is currently busy
						System.Threading.Thread.Sleep(50);
					}
				}
			}

			//make sure we have our profile in the char dictionary
			if (charProfiles.ContainsKey(Metatron.MeCache.Name))
			{
				charProfiles[Metatron.MeCache.Name] = ActiveConfigName;
			}
			else
			{
				charProfiles.Add(Metatron.MeCache.Name, ActiveConfigName);
			}

			//Write out the dictionary
			succeeded = false;
			var resetEvent = new System.Threading.AutoResetEvent(false);
			//Keep trying to write the file 'til we've succeeded
			while (!succeeded)
			{
				try
				{
					using (var stream = new StreamWriter(File.Create(profilesFilePath)))
					{
						foreach (var line in charProfiles.Keys.ToList().Select(key => String.Format("{0},{1}", key, charProfiles[key])))
						{
							stream.WriteLine(line);
						}
						//Exit the loop
						succeeded = true;
					}
				}
				//Do a small sleep to give any other processes time to finish writing
				catch (Exception e)
				{
					LogException(e, methodName, "Caught exception while trying to write default config profiles:");
					resetEvent.WaitOne(50);
				}
			}
		}

		public void SaveConfigProfile(string configProfileName)
		{
			var methodName = "SaveConfigProfile";
			LogTrace(methodName, "ConfigProfileName: {0}", configProfileName);

            if (!_configProfilesByNameByName.ContainsKey(configProfileName))
				return;

			//Get the path to save at
			var configProfilePath = String.Format("{0}\\Config-{1}.bin",
				String.Format("{0}\\Metatron\\config", Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath)), configProfileName);
			Metatron.FileManager.QueueOverwriteSerialize(configProfilePath,
                new Dictionary<string, ConfigProperty>(ActiveConfigProfile), FileWriteComplete);
		}

		public void OnConfigLoaded()
		{
			if (ConfigLoaded != null)
				ConfigLoaded(ActiveConfigProfile, new EventArgs());
		}
		#endregion
	}
    // ReSharper restore StringIndexOfIsCultureSpecific.1
    // ReSharper restore ConvertToConstant.Local
}
