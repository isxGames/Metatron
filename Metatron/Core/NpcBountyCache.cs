using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using ProtoBuf;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    internal class NpcBountyCache : ModuleBase, INpcBountyCache
    {
	    private readonly Dictionary<string, int> _bountiesByName = new Dictionary<string, int>();

		private readonly FileReadCallback<NpcNameBountyPair> _fileReadCallback;
		private readonly FileWriteCallback _fileWriteCallback;

		private string _dataDirectory = String.Format("{0}\\Metatron\\data", Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath)),
			_filePath = string.Empty,
			_oldFilePath = string.Empty;

		public NpcBountyCache()
		{
			ModuleName = "NpcBountyCache";
			IsEnabled = false;

			_filePath = string.Format("{0}\\{1}", _dataDirectory, "NpcBounties.bin");
			_oldFilePath = string.Format("{0}\\{1}", _dataDirectory, "EVEDB_Spawns.xml");

			_fileReadCallback = FileReadCompleted;
			_fileWriteCallback = FileWriteCompleted;

			//Convert the old db if necessary
			try
			{
				ConvertEvebotBountyDatabase();
			}
// ReSharper disable EmptyGeneralCatchClause
			catch
// ReSharper restore EmptyGeneralCatchClause
			{ }
		}

		private void ConvertEvebotBountyDatabase()
		{
			var methodName = "ConvertEvebotBountyDatabase";
			LogTrace(methodName);

			//If the bounty xml doesn't exist...
			if (File.Exists(_filePath)) 
				return;

			//If the old file exists...
			if (File.Exists(_oldFilePath))
			{
				//Use a textReader
				using (TextReader textReader = new StreamReader(_oldFilePath))
				{
					//Get an xml document and load it from the textreader
					var document = new XmlDocument();

				    try
				    {
                        document.Load(textReader);
				    }
				    catch (XmlException xe)
				    {
				        LogException(xe, methodName, "Caught exception while loading the EVEBot bounty database:");
				        IsInitialized = true;
				        return;
				    }

					//Get a node list of all nodes
					var nodes = document.SelectNodes("/InnerSpaceSettings/Set/Set");

                    if (nodes == null)
                    {
                        LogMessage(methodName, LogSeverityTypes.Standard, "Error: Bounty file is malformed. Set \"/InnerSpaceSettings/Set/Set\" is missing.");
                        return;
                    }

					//Iterate all nodes
					foreach (XmlNode node in nodes)
					{
						//The attribute containing the NPC name is "Name"
					    if (node.Attributes == null) continue;

					    var name = node.Attributes["Name"].Value;

					    //The NPC bounty is the innertext of a child node
					    var childNode = node.SelectSingleNode("Setting");
					    
                        if (childNode == null)
                        {
                            LogMessage(methodName, LogSeverityTypes.Standard, "Error: Bounty file is malformed. Node {0} is missing element \"Setting\".", node);
                            continue;
                        }
					    
                        var bountyString = childNode.InnerText;
					    int bounty;
					    //Parse the bounty
					    int.TryParse(bountyString, out bounty);

					    //Add the pair to the dictionary
					    _bountiesByName.Add(name, bounty);
					}
				}

				//Commit the dictionary to disk
				var list = FlattenBountiesByNameIntoPairList();
				Metatron.FileManager.QueueOverwriteSerialize(_filePath, list, null);

				//If we had to commit it means we're already initialized.
				IsInitialized = true;
			}
			else
			{
				LogMessage(methodName, LogSeverityTypes.Standard, "Error; database not found. Please place \"EVEDB_Spawns.xml\" from EVEBot into the Metatron config directory.");
			}
		}

		public override bool Initialize()
		{
			IsCleanedUpOutOfFrame = false;
			if (!IsInitialized)
			{
				if (!_isInitializing)
				{
					_isInitializing = true;

					if (File.Exists(_filePath))
					{
						//Queue the read of the database
						Metatron.FileManager.QueueDeserialize(_filePath, _fileReadCallback);
					}
					else
					{
						//Nothing to read, no database. Bummer.
						IsInitialized = true;
						_isInitializing = false;
					}
				}
			}
			return IsInitialized;
		}

		private void FileReadCompleted(List<NpcNameBountyPair> result)
		{
			lock (_bountiesByName)
			{
				foreach (var pair in result)
				{
					_bountiesByName.Add(pair.Name, pair.Bounty);
				}

				IsInitialized = true;
				_isInitializing = false;
			}
		}

		public override bool OutOfFrameCleanup()
		{
			if (!IsCleanedUpOutOfFrame)
			{
				if (!_isCleaningUp)
				{
					_isCleaningUp = true;

					List<NpcNameBountyPair> list = FlattenBountiesByNameIntoPairList();
					Metatron.FileManager.QueueOverwriteSerialize(_filePath, list, _fileWriteCallback);
				}
			}
			return IsCleanedUpOutOfFrame;
		}

		private List<NpcNameBountyPair> FlattenBountiesByNameIntoPairList()
		{
			return _bountiesByName.Keys.ToList().Select(key => new NpcNameBountyPair(key, _bountiesByName[key])).ToList();
		}

		private void FileWriteCompleted()
		{
			IsCleanedUpOutOfFrame = true;
			_isCleaningUp = false;
		}

		public int GetBountyForNpc(string npcName)
		{
			var methodName = "GetBountyForNpc";
			LogTrace(methodName, "NPC Name: {0}", npcName);

			if (_bountiesByName.ContainsKey(npcName))
			{
				return _bountiesByName[npcName];
			}

			var nameCopy = npcName.Trim().Trim('\t');
			if (_bountiesByName.ContainsKey(npcName))
			{
				return _bountiesByName[nameCopy];
			}

			var bytes = Encoding.ASCII.GetBytes(npcName);
			var byteString = new StringBuilder();
			foreach (var charByte in bytes)
			{
				byteString.Append(String.Concat(charByte.ToString("x"), " "));
			}

			LogMessage(methodName, LogSeverityTypes.Standard, "Error; no bounty entry found for NPC named \'{0}\' ({1}). Let stealthy know the bounty for this NPC.",
				npcName, byteString);
			return 0;
		}
	}

	[ProtoContract]
	public class NpcNameBountyPair
	{
		[ProtoMember(1)]
		public string Name;
		[ProtoMember(2)]
		public int Bounty;

		public NpcNameBountyPair(string name, int bounty)
		{
			Name = name;
			Bounty = bounty;
		}

		public NpcNameBountyPair() { }
	}
}
