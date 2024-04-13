using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    internal class PossibleEwarNpcs : ModuleBase, IPossibleEwarNpcs
    {
		private readonly List<string> _possibleEwarNpcNames = new List<string>();

		private readonly string _databaseFileName = "PossibleEwarNpcNames.xml";
		private readonly string _databaseFilePath;

		public PossibleEwarNpcs()
		{
			ModuleName = "PossibleEwarNpcDatabase";

			IsEnabled = false;

			_databaseFilePath = Path.Combine(Metatron.DataDirectory, _databaseFileName);
		}

		public override bool Initialize()
		{
			if (!IsInitialized)
			{
				LoadDatabase();
				IsInitialized = true;
			}

			return IsInitialized;
		}

		private void LoadDatabase()
		{
			var methodName = "LoadDatabase";
			LogTrace(methodName);

			if (!File.Exists(_databaseFilePath)) return;

			using (var streamReader = new StreamReader(_databaseFilePath))
			{
				var xmlDocument = new XmlDocument();

			    try
			    {
                    xmlDocument.Load(streamReader);
			    }
			    catch (XmlException xe)
			    {
			        LogException(xe, methodName, "Caught exception while loading PossibleEwarNpcs database:");
			    }

				var xmlNodes = xmlDocument.SelectNodes("/PossibleEwarNpcNames/PossibleEwarNpcName");

				if (xmlNodes == null)
				{
					LogMessage(methodName, LogSeverityTypes.Debug, "Error: Got a null result for /PossibleEwarNpcNames/PossibleEwarNpcName");
					return;
				}

				foreach (XmlNode xmlNode in xmlNodes)
				{
					var possibleEwarNpcName = xmlNode.InnerXml;
					_possibleEwarNpcNames.Add(possibleEwarNpcName);
				}
			}
		}

		public bool IsInDatabase(string name)
		{
			return _possibleEwarNpcNames.Any(possibleEwarNpcName => possibleEwarNpcName.Equals(name, StringComparison.InvariantCultureIgnoreCase));
		}
	}
}
