using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Xml;
using System.Collections;
using System.Windows.Forms;
using LavishVMAPI;
using InnerSpaceAPI;
using LavishScriptAPI;
using Microsoft.Win32;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;   //GuidAttribute
using System.Reflection;                //Assembly
using System.Security.AccessControl;    //MutexAccessRule
using System.Security.Principal;        //SecuirtyIdentifier

namespace isxGamesPatcher
{
	public class Downloader
	{
		public bool Downloading;
        public string ErrorStr;

		public Downloader()
		{
			Downloading = false;
		}

		public bool DownloadFile(FileInfoClass FileInfo)
		{
			int bytesdone = 0;
			Stream RStream = null;
			Stream LStream = null;
			WebResponse response = null;
			try
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(FileInfo.Url);
				request.Headers.Set("Pragma", "no-cache");
				request.Headers.Set("cache-control", "no-cache");
				request.UserAgent = "isxGamesPatcher/" + isxGamesPatcher.Properties.Resources.VERSION;
				request.IfModifiedSince = DateTime.Now.AddYears(-1);
				request.KeepAlive = false;

				HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
				request.CachePolicy = noCachePolicy;
				
				if (request != null)
				{
					response = request.GetResponse();
					if (response != null)
					{
						if (FileInfo.PerformBackup != 0)
						{
							// We're getting a response, so move the current file out of the way.
							if (File.Exists(FileInfo.absFilePath + ".backup1"))
							{
								File.Delete(FileInfo.absFilePath + ".backup2");
								File.Move(FileInfo.absFilePath + ".backup1", FileInfo.absFilePath + ".backup2");
							}
							if (File.Exists(FileInfo.absFilePath))
								File.Move(FileInfo.absFilePath, FileInfo.absFilePath + ".backup1");

						}
						else
						{
                            if (File.Exists(FileInfo.absFilePath + ".isxGamesPatchertmp"))
                            {
                                File.SetAttributes(FileInfo.absFilePath + ".isxGamesPatchertmp", FileAttributes.Normal);
                                File.Delete(FileInfo.absFilePath + ".isxGamesPatchertmp");
                            }

                            if (File.Exists(FileInfo.absFilePath))
                            {
                                File.Move(FileInfo.absFilePath, FileInfo.absFilePath + ".isxGamesPatchertmp");
                                File.SetAttributes(FileInfo.absFilePath + ".isxGamesPatchertmp",FileAttributes.Normal);
                            }
						}
						string DestDir = Path.GetDirectoryName(FileInfo.absFilePath);
                        if (!Directory.Exists(DestDir))
                        {
                            // Create the directory it does not exist.
                            InnerSpace.Echo("isxGamesPatcher: " + "     Creating Directory " + DestDir);
                            try
                            {
                                Directory.CreateDirectory(DestDir);
                            }
                            catch (Exception e)
                            {
                                ErrorStr = e.ToString();
                                InnerSpace.Echo("isxGamesPatcher: " + "     ERROR: " + ErrorStr);
                                return false;
                            }
                        }

						RStream = response.GetResponseStream();
						LStream = File.Create(FileInfo.absFilePath);
						byte[] buffer = new byte[32678];
						int bytesRead;
						do
						{
							bytesRead = RStream.Read(buffer, 0, buffer.Length);
							LStream.Write(buffer, 0, bytesRead);
							bytesdone += bytesRead;
						} while (bytesRead > 0);
					}
				}
				Downloading = false;
			}
			catch (WebException e)
			{
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)e.Response;
                    switch (httpResponse.StatusCode)
                    {
                        case HttpStatusCode.NotFound:
                            ErrorStr = "HTTP Error: File Not Found: " + FileInfo.Url;
                            InnerSpace.Echo("isxGamesPatcher: " + "   " + ErrorStr);
                            break;
                        case HttpStatusCode.ServiceUnavailable:
                            ErrorStr = "HTTP Error: Server under high load, try later";
                            InnerSpace.Echo("isxGamesPatcher: " + "   " + ErrorStr);
                            break;
                        case HttpStatusCode.Unauthorized:
                            ErrorStr = "HTTP Error: Authorization Required";
                            InnerSpace.Echo("isxGamesPatcher: " + "   " + ErrorStr);
                            break;
						case HttpStatusCode.NotModified:
							ErrorStr = "HTTP Warning: Patcher Manifest is over a year old (" + FileInfo.Url + ")";
							InnerSpace.Echo("isxGamesPatcher: " + "   " + ErrorStr);
							break;
                        default:
                            ErrorStr = "HTTP Error: " + httpResponse.StatusDescription;
                            InnerSpace.Echo("isxGamesPatcher: " + "   " + ErrorStr);
                            break;
                    }
                }
                else
                {
                    ErrorStr = "HTTP Error: " + e.Status.ToString() + ": " + e.Message.ToString();
                    InnerSpace.Echo("isxGamesPatcher: " + "   " + ErrorStr);
                }
                return false;
            }
			catch (Exception e)
			{
                ErrorStr = "HTTP Error: Uknown Error: " + e.Message.ToString();
                InnerSpace.Echo("isxGamesPatcher: " + "   " + ErrorStr);
                return false;
			}
			finally
			{
				if (response != null) response.Close();
				if (RStream != null) RStream.Close();
				if (LStream != null) LStream.Close();
			}
			return bytesdone != 0;
		}
	}

	public class ProjectClass
	{
		public string ProjectName;
		public string ManifestURL;
		public double LocalVersion;
		public string Event_FileUpdatedStr;
		public string Event_UpdateErrorStr;
		public string Event_UpdateCompleteStr;
		public string Event_ParseManifestFailedStr;
		public uint Event_FileUpdated;
		public uint Event_UpdateError;
		public uint Event_UpdateComplete;
		public uint Event_ParseManifestFailed;
		public string InnerSpacePath;
		public ArrayList UpdatedFiles = new ArrayList();

		private void GetInnerSpacePath()
		{
			RegistryKey baseRegistryKey = Registry.LocalMachine;

			StringBuilder Path = new StringBuilder(2048);
			try
			{
				InnerSpacePath = InnerSpace.Path;
                if (InnerSpacePath == null)
                    throw new ArgumentNullException();
			}
			catch
			{
				RegistryKey sk = baseRegistryKey.OpenSubKey(@"SOFTWARE\Sony Online Entertainment\Station LaunchPad\BkMrks");
				try
				{
					LavishScript.DataParse<string>("${LavishScript.HomeDirectory}", ref InnerSpacePath);
				}
				catch
				{
					sk = baseRegistryKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\InnerSpace.exe");
					InnerSpacePath = (string)sk.GetValue("Path");
				}
			}
		}
		
		/* Prune files leftover from the update which were locked
		 * before we reloaded dll's and such
		 */
		public void PruneLockedFiles()
		{
			if (UpdatedFiles.Count == 0) return;
			string target;

			for (int i = 0; i < UpdatedFiles.Count; i++)
			{
				target = InnerSpacePath + "/" + UpdatedFiles[i].ToString() + ".isxGamesPatchertmp";
				if ( File.Exists(target))
				{
                    File.SetAttributes(target, FileAttributes.Normal);
					File.Delete(target);
				}
			}
		}
	
		// Returns whether or not a file was updated
		public bool FileWasUpdated(string Filename)
		{
			if (UpdatedFiles.Count == 0) return false;

			for (int i = 0; i < UpdatedFiles.Count; i++)
			{
				if (Filename.ToUpper() == UpdatedFiles[i].ToString()) return true;
			}
			return false;
		}
		
		public void SendEvent_FileUpdated(string filename)
		{
			if (Event_FileUpdatedStr.Length > 0)
			{
				string[] Args = {
					filename
				};
				LavishScript.Events.ExecuteEvent(Event_FileUpdated, Args);
			}
		}

		public void SendEvent_ParseManifestFailed(string errmsg)
		{
			if (Event_ParseManifestFailedStr.Length > 0)
			{
				string[] Args = {
					errmsg
				};
				LavishScript.Events.ExecuteEvent(Event_ParseManifestFailed, Args);
			}
		}

		public void SendEvent_UpdateError(string errmsg)
		{
			if (Event_UpdateErrorStr.Length > 0)
			{
				string[] Args = {
					errmsg
				};
				LavishScript.Events.ExecuteEvent(Event_UpdateError, Args);
			}
		}

		public void SendEvent_UpdateComplete()
		{
            //InnerSpace.Echo("isxGamesPatcher: " + "   " + "Sending Event: '" + Event_UpdateCompleteStr + "'");
			if (Event_UpdateCompleteStr.Length > 0)
			{
				LavishScript.Events.ExecuteEvent(Event_UpdateComplete);
			}
		}
		
		public void Event_Initialize()
		{
			if (Event_FileUpdatedStr.Length > 0)
				Event_FileUpdated = LavishScript.Events.RegisterEvent(this.Event_FileUpdatedStr);
			if (Event_UpdateErrorStr.Length > 0)
				Event_UpdateError = LavishScript.Events.RegisterEvent(this.Event_UpdateErrorStr);
			if (Event_UpdateCompleteStr.Length > 0)
				Event_UpdateComplete = LavishScript.Events.RegisterEvent(this.Event_UpdateCompleteStr);
			if (Event_ParseManifestFailedStr.Length > 0)
				Event_ParseManifestFailed = LavishScript.Events.RegisterEvent(this.Event_ParseManifestFailedStr);
		}

		public ProjectClass(string thisProjectName, string thisVersion,string thisURL)
		{
			ProjectName = thisProjectName;
			ManifestURL = thisURL;
			LocalVersion = Convert.ToDouble(thisVersion, new CultureInfo("en-US"));
			Event_FileUpdatedStr = "isxGamesPatcher_onFileUpdated";
			Event_UpdateErrorStr = "isxGamesPatcher_onUpdateError";
			Event_UpdateCompleteStr = "isxGamesPatcher_onUpdateComplete";
			Event_ParseManifestFailedStr = "isxGamesPatcher_onParseManifestFailed";

			this.GetInnerSpacePath();
			this.Event_Initialize();
		}
	}

	public class FileInfoClass
	{
		public string Filename;
		public string absFilePath;
		public string Url;
		public double ServerVersion;
		public string Hash;
		public uint PerformBackup;
        public string Event_FileUpdatedStr;
        public string Event_UpdateErrorStr;

		public FileInfoClass()
		{
			Filename = "";
			Url = "";
			ServerVersion = 0;
			Hash = "";
			PerformBackup = 1;
		}
	}

	public class Updater
	{
		ProjectClass Project;

		public Updater(ProjectClass ThisProject)
		{
			Project = ThisProject;
		}

		public bool CheckUpdates()
		{
            return (this.ParseManifest());	
		}

		public bool ParseManifest()
		{
			try
			{
                //InnerSpace.Echo("Starting ParseManifest()");
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Project.ManifestURL);
				request.Headers.Set("Pragma", "no-cache");
				request.Headers.Set("cache-control", "no-cache");
				request.UserAgent = "isxGamesPatcher/" + isxGamesPatcher.Properties.Resources.VERSION;
				request.IfModifiedSince = DateTime.Now.AddYears(-1);
				request.KeepAlive = false;

				HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
				request.CachePolicy = noCachePolicy;

                //InnerSpace.Echo(Project.ManifestURL);
				WebResponse response = request.GetResponse();
				Stream responseStream = response.GetResponseStream();
				XmlTextReader reader = new XmlTextReader(responseStream);
				XmlDocument doc = new XmlDocument();
				doc.Load(reader);

				response.Close();
				responseStream.Close();

				// PArse <Project>...</Project> set
				if (doc.GetElementsByTagName("Project").Count > 0)
				{
					XmlNodeList projectlist = doc.GetElementsByTagName("Project");
					XmlElement projectnode = (XmlElement)projectlist[0];
					if (projectnode.GetElementsByTagName("Name").Count > 0)
					{
						Project.ProjectName = ((XmlElement)projectnode.GetElementsByTagName("Name")[0]).FirstChild.Value;
					}
					if (projectnode.GetElementsByTagName("Event_FileUpdated").Count > 0)
					{
						Project.Event_FileUpdatedStr = ((XmlElement)projectnode.GetElementsByTagName("Event_FileUpdated")[0]).FirstChild.Value;
					}

					if (projectnode.GetElementsByTagName("Event_UpdateError").Count > 0)
					{
						Project.Event_UpdateErrorStr = ((XmlElement)projectnode.GetElementsByTagName("Event_UpdateError")[0]).FirstChild.Value;
					}
					if (projectnode.GetElementsByTagName("Event_UpdateComplete").Count > 0)
					{
						Project.Event_UpdateCompleteStr = ((XmlElement)projectnode.GetElementsByTagName("Event_UpdateComplete")[0]).FirstChild.Value;
					}

					Project.Event_Initialize(); // Re-Initialize Events now that the custom names are in place.
				}

				// Parse all <FileInfo>...</FileInfo> sets
				XmlNodeList filelist = doc.GetElementsByTagName("FileInfo");
				string info = "isxGamesPatcher: " + " Checking " + Project.ProjectName + ": " + filelist.Count + " file";
				info += (filelist.Count > 1) ? "s " : " ";
				info += "in manifest...";
				InnerSpace.Echo(info);
				for (int i = 0; i < filelist.Count; i++)
				{
					FileInfoClass FileInfo = new FileInfoClass();
					XmlElement currentFile = (XmlElement)filelist[i];
					try
					{
						FileInfo.Filename = ((XmlElement)currentFile.GetElementsByTagName("filename")[0]).FirstChild.Value;
						FileInfo.Url = ((XmlElement)currentFile.GetElementsByTagName("url")[0]).FirstChild.Value;
                        FileInfo.ServerVersion = Convert.ToDouble(((XmlElement)currentFile.GetElementsByTagName("version")[0]).FirstChild.Value, new CultureInfo("en-US"));
					}
					catch
					{
                        string errmsg = "Malformed Manifest File - Missing filename, Url, or version";
						InnerSpace.Echo("isxGamesPatcher:   Error - " + errmsg);
						Project.SendEvent_ParseManifestFailed(errmsg);
						Application.Exit();
                        return true;
					}

					// Parse optional nodes
					if (currentFile.GetElementsByTagName("hash").Count > 0)
					{
						FileInfo.Hash = ((XmlElement)currentFile.GetElementsByTagName("hash")[0]).FirstChild.Value;
					}
					if (currentFile.GetElementsByTagName("PerformBackup").Count > 0)
					{
						FileInfo.PerformBackup = Convert.ToByte(((XmlElement)currentFile.GetElementsByTagName("PerformBackup")[0]).FirstChild.Value);
					}

					FileInfo.absFilePath = Project.InnerSpacePath + "/" + FileInfo.Filename;
                    if (!this.CheckFile(FileInfo))
                        return false;
				}
			}
			catch (Exception e)
			{
				string errmsg = "isxGamesPatcher: " + "Exception: " + e.ToString();
				InnerSpace.Echo(errmsg);
				Project.SendEvent_ParseManifestFailed(errmsg);
				Application.Exit();
			}
            return true;
		}

		public bool CheckFile(FileInfoClass FileInfo)
		{
			try
			{
				// Check to see if the file needs updating.
				//InnerSpace.Echo("Checking if " + Project.LocalVersion.ToString() + " < " + FileInfo.ServerVersion.ToString());
				if ((Project.LocalVersion < FileInfo.ServerVersion) && (FileInfo.ServerVersion != 0))
				{
					if (File.Exists(FileInfo.absFilePath + ".nopatch"))
					{
						InnerSpace.Echo("isxGamesPatcher: " + "  Not Updating " + FileInfo.Filename + " (.nopatch)");
						return true;
					}

                    //////////////////
                    // If the patcher is already running, then let's end this application and give it time to finish
                    if (Program.NotExclusive)
                    {
                        InnerSpace.Echo("isxGamesPatcher:  The patcher is already working in another window.  Let's give it time to finish.");
                        return false;
                    }
                    //
                    /////////////////

					this.UpdateFile(FileInfo);
                    return true;
				}
			}
			catch (Exception e)
			{
				string errmsg = "isxGamesPatcher: " + "  " + e.ToString();
				InnerSpace.Echo(errmsg);
			}
            return true;
		}
		public void UpdateFile(FileInfoClass FileInfo)
		{
			try
			{
				Downloader Download = new Downloader();
				InnerSpace.Echo("isxGamesPatcher: " + "  Updating \"" + FileInfo.Filename + "\" from version " + Project.LocalVersion.ToString() + " to version " + FileInfo.ServerVersion.ToString());

				Download.Downloading = true;
                if (Download.DownloadFile(FileInfo))
                {
                    Project.UpdatedFiles.Add(FileInfo.Filename.ToUpper());
                    Project.SendEvent_FileUpdated(FileInfo.Filename);
                }
                else
                {
                    Project.SendEvent_UpdateError(Download.ErrorStr);
                }
			}
			catch (Exception e)
			{
				string errmsg = "  Error Updating: " + e.ToString();
                Project.SendEvent_UpdateError(errmsg);
				InnerSpace.Echo("isxGamesPatcher: " + errmsg);
				return;
			}
		}
	}

	static class Program
	{
        public static bool NotExclusive = false;

		static void SelfUpdate(string[] args)
		{
			/* First of all ...see if Patcher needs to be updated.
			 * We only call this function when a major system, like a DLL, is being updated,
			 * to avoid the overhead of checking dll versions and self versions for every script which gets
			 * executed that happens to be patcher-aware. 
			 * -- CyberTech 
			 */

			ProjectClass Project = new ProjectClass("isxGamesPatcher", isxGamesPatcher.Properties.Resources.VERSION, isxGamesPatcher.Properties.Resources.isxGamesPatcher_Manifest_File);

			Updater isxGamesPatcher_exe__Updater = new Updater(Project);
			isxGamesPatcher_exe__Updater.CheckUpdates();

			/*
			 * SECURITY NOTE - Do not allow updates to proceed without an update of the patcher, if the patcher has
			 * a pending update, due to the risk of maliciously redirected hardcoded hosts.  
			 * -- CyberTech
			 */
			if (Project.FileWasUpdated("EXTENSIONS/ISXGAMESPATCHER.EXE"))
			{
				InnerSpace.Echo("isxGamesPatcher: " + "    Your isxGamesPatcher is out-of-date and was updated!");
				InnerSpace.Echo("isxGamesPatcher: " + "    Restarting patch process in 2 seconds...\n");
				Thread.Sleep(2000);

                string cmd = "execute timedcommand 1 \"dotnet " + System.AppDomain.CurrentDomain.FriendlyName.ToString() + " -recurse isxGamesPatcher" + "\"";
				//string cmd = "dotnet " + System.AppDomain.CurrentDomain.FriendlyName.ToString() + "-recurse isxGamesPatcher";
				for (int i=0; i<args.Length; i++) 
					cmd = cmd + " " + args[i];

				LavishScript.ExecuteCommand(cmd);
				Application.Exit();
			}
            //InnerSpace.Echo("SelfUpdate() - Finished.");
		}

		[STAThread]
		static void Main(string[] args)
		{
            bool ReloadExtension = false;
            ProjectClass Project = null;
            
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            // get application GUID as defined in AssemblyInfo.cs
            string appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), false).GetValue(0)).Value.ToString();

            // unique id for global mutex - Global prefix means it is global to the machine
            string mutexId = string.Format("Global\\{{{0}}}", appGuid);
            //InnerSpace.Echo("DEBUG:  mutexId: " + mutexId.ToString());


            using (var mutex = new Mutex(false, mutexId))
            {
                var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
                var securitySettings = new MutexSecurity();
                securitySettings.AddAccessRule(allowEveryoneRule);
                mutex.SetAccessControl(securitySettings);

                var hasHandle = false;
                try
                {
                    try
                    {
                        hasHandle = mutex.WaitOne(50);
                        if (hasHandle == false)
                        {
                            //InnerSpace.Echo("DEBUG:  Another instance of isxGamesPatcher is running...");
                            NotExclusive = true;
                        }
                    }
                    catch (AbandonedMutexException)
                    {
                    }

                    if (args.Length > 2)
                    {
                        Project = new ProjectClass(args[0].ToString(), args[1].ToString(), args[2].ToString());
                        //InnerSpace.Echo("args[0]: \"" + args[0].ToString() + "\"");
                        //InnerSpace.Echo("args[1]: \"" + args[1].ToString() + "\"");
                        //InnerSpace.Echo("args[2]: \"" + args[2].ToString() + "\"");
                    }
                    else
                    {
                        InnerSpace.Echo("isxGamesPatcher: " + "Incorrect Command Line Parammeters");
                        InnerSpace.Echo("isxGamesPatcher: " + "Usage:");
                        InnerSpace.Echo("isxGamesPatcher: " + "\t dotnet isxGamesPatcher isxGamesPatcher appname currentversion http://example.com/appname_manifest.xml");
                    }

                    ////////////////////////// AMADEUS ////////////////////////////
                    // ACTUAL Main() routine starts here!
                    //InnerSpace.Echo("DEBUG:  Stopwatch: " + sw.ElapsedTicks.ToString() + " (" + sw.ElapsedMilliseconds.ToString() + ")");
                    try
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);

                        //InnerSpace.Echo("isxGamesPatcher: Starting (Ver. " + isxGamesPatcher.Properties.Resources.VERSION + " by CyberTech/Amadeus)");
                        InnerSpace.Echo("isxGamesPatcher: Starting");

                        Updater ThisUpdater = new Updater(Project);
                        switch (args[0].ToString().ToUpper())
                        {
                            case "ISXVG":
                                SelfUpdate(args);
                                if (ThisUpdater.CheckUpdates())
                                {
                                    if (Project.FileWasUpdated("EXTENSIONS/ISXDK34/ISXVG.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXVG Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxvg\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                    else if (Project.FileWasUpdated("EXTENSIONS/ISXDK35/ISXVG.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXVG Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxvg\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                    else if (Project.FileWasUpdated("EXTENSIONS/ISXDK36/ISXVG.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXVG Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxvg\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                    else if (Project.FileWasUpdated("EXTENSIONS/ISXDK37/ISXVG.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXVG Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxvg\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                }
                                else
                                {
                                    while (!mutex.WaitOne(5))
                                    {
                                        Thread.Sleep(50);
                                    }
                                    LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxvg\"");
                                    Thread.Sleep(500);
                                    ReloadExtension = true;
                                }
                                break;

                            case "ISXEQ2":
                                SelfUpdate(args);
                                if (ThisUpdater.CheckUpdates())
                                {
                                    if (Project.FileWasUpdated("EXTENSIONS/ISXDK34/ISXEQ2.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXEQ2 Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxeq2\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                    else if (Project.FileWasUpdated("EXTENSIONS/ISXDK35/ISXEQ2.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXEQ2 Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxeq2\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                    else if (Project.FileWasUpdated("EXTENSIONS/ISXDK36/ISXEQ2.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXEQ2 Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxeq2\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                    else if (Project.FileWasUpdated("EXTENSIONS/ISXDK37/ISXEQ2.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXEQ2 Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxeq2\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                }
                                else
                                {
                                    while (!mutex.WaitOne(5))
                                    {
                                        Thread.Sleep(50);
                                    }
                                    LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxeq2\"");
                                    Thread.Sleep(500);  
                                    ReloadExtension = true;
                                }
                                break;

                            case "ISXEVE":
                                SelfUpdate(args);
                                if (ThisUpdater.CheckUpdates())
                                {
                                    if (Project.FileWasUpdated("X64/EXTENSIONS/ISXDK35/ISXEVE.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXEVE Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxeve\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                    else if (Project.FileWasUpdated("X64/EXTENSIONS/ISXDK36/ISXEVE.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXEVE Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxeve\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                    else if (Project.FileWasUpdated("X64/EXTENSIONS/ISXDK37/ISXEVE.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXEVE Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxeve\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                }
                                else
                                {
                                    while (!mutex.WaitOne(5))
                                    {
                                        Thread.Sleep(50);
                                    }
                                    LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxeve\"");
                                    Thread.Sleep(500);
                                    ReloadExtension = true;
                                }
                                break;


                            case "ISXAION":
                                SelfUpdate(args);
                                if (ThisUpdater.CheckUpdates())
                                {
                                    if (Project.FileWasUpdated("EXTENSIONS/ISXDK34/ISXAION.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXAION Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxaion\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                    else if (Project.FileWasUpdated("EXTENSIONS/ISXDK35/ISXAION.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXAION Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxaion\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                    else if (Project.FileWasUpdated("EXTENSIONS/ISXDK36/ISXAION.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXAION Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxaion\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                    else if (Project.FileWasUpdated("EXTENSIONS/ISXDK37/ISXAION.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXAION Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxaion\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                }
                                else
                                {
                                    while (!mutex.WaitOne(5))
                                    {
                                        Thread.Sleep(50);
                                    }
                                    LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxaion\"");
                                    Thread.Sleep(500);
                                    ReloadExtension = true;
                                }
                                break;

                            case "ISXIM":
                                SelfUpdate(args);
                                if (ThisUpdater.CheckUpdates())
                                {
                                    if (Project.FileWasUpdated("EXTENSIONS/ISXDK34/ISXIM.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXIM Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxim\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                    else if (Project.FileWasUpdated("EXTENSIONS/ISXDK35/ISXIM.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXIM Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxim\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                    else if (Project.FileWasUpdated("EXTENSIONS/ISXDK36/ISXIM.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXIM Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxim\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                    else if (Project.FileWasUpdated("EXTENSIONS/ISXDK37/ISXIM.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXIM Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxim\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                }
                                else
                                {
                                    while (!mutex.WaitOne(5))
                                    {
                                        Thread.Sleep(50);
                                    }
                                    LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxim\"");
                                    Thread.Sleep(500);
                                    ReloadExtension = true;
                                }
                                break;


                            case "ISXSQLITE":
                                SelfUpdate(args);
                                if (ThisUpdater.CheckUpdates())
                                {
                                    if (Project.FileWasUpdated("EXTENSIONS/ISXDK34/ISXSQLITE.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXSQLITE Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxSQLite\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                    else if (Project.FileWasUpdated("EXTENSIONS/ISXDK35/ISXSQLITE.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXSQLITE Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxSQLite\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                    else if (Project.FileWasUpdated("EXTENSIONS/ISXDK36/ISXSQLITE.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXSQLITE Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxSQLite\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                    else if (Project.FileWasUpdated("EXTENSIONS/ISXDK37/ISXSQLITE.DLL"))
                                    {
                                        InnerSpace.Echo("isxGamesPatcher: " + " ISXSQLITE Patched - Reloading Extension...");
                                        LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxSQLite\"");
                                        Thread.Sleep(1000);
                                        ReloadExtension = true;
                                    }
                                }
                                else
                                {
                                    while (!mutex.WaitOne(5))
                                    {
                                        Thread.Sleep(50);
                                    }
                                    LavishScript.ExecuteCommand("execute timedcommand 2 \"ext -unload isxSQLite\"");
                                    Thread.Sleep(500);
                                    ReloadExtension = true;
                                }
                                break;

                            default:
                                ThisUpdater.CheckUpdates();
                                Project.PruneLockedFiles();
                                break;
                        }
                        InnerSpace.Echo("isxGamesPatcher: " + "Finished");
                        Project.SendEvent_UpdateComplete();

                    }
                    catch (Exception e)
                    {
                        string ErrorStr = e.ToString();
                        InnerSpace.Echo("isxGamesPatcher Main: " + "     ERROR: " + ErrorStr);
                    }
                    //
                    /////////////////////////// AMADEUS ////////////////////////////
                }
                finally
                {
                    if (ReloadExtension)
                    {
                        Thread.Sleep(500);
                        switch (args[0].ToString().ToUpper())
                        {
                            case "ISXEVE":          LavishScript.ExecuteCommand("execute timedcommand 2 \"ext isxeve\"");           break;
                            case "ISXVG":           LavishScript.ExecuteCommand("execute timedcommand 2 \"ext isxvg\"");            break;
                            case "ISXEQ2":          LavishScript.ExecuteCommand("execute timedcommand 2 \"ext isxeq2\"");           break;
                            case "ISXAION":         LavishScript.ExecuteCommand("execute timedcommand 2 \"ext isxaion\"");          break;
                            case "ISXIM":           LavishScript.ExecuteCommand("execute timedcommand 2 \"ext isxim\"");            break;
                            case "ISXSQLITE":       LavishScript.ExecuteCommand("execute timedcommand 2 \"ext isxSQLite\"");        break;
                            default:                                                                                                break;
                        }
                        Thread.Sleep(1000);
                        if (hasHandle)
                            mutex.ReleaseMutex();
                        Thread.Sleep(6000);
                        Project.PruneLockedFiles();
                        //InnerSpace.Echo("\ayEND!\ax");
                    }
                    else if (hasHandle)
                        mutex.ReleaseMutex();
                }
                //InnerSpace.Echo("END!");
            }
		}
	}
}