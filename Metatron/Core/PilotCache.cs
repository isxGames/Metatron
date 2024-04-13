using System;
using System.Collections.Generic;
using EVE.ISXEVE;
using ProtoBuf;
using System.IO;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    public sealed class PilotCache : ModuleBase, IPilotCache
    {
        private Dictionary<Int64, CachedPilot> _cachedPilotsById = new Dictionary<long, CachedPilot>();

	    public Dictionary<Int64, CachedPilot> CachedPilotsById
	    {
	        get { return _cachedPilotsById; }
	    }

		private string _configDirectory = string.Empty;
		private string _configFile = string.Empty;

		public PilotCache()
		{
            ModuleManager.ModulesToPulse.Add(this);
		    ModuleName = "PilotCache";
		    PulseFrequency = 1;
		    IsEnabled = true;
		}

        public override void Pulse()
        {
            if (!ShouldPulse()) return;

            if (string.IsNullOrEmpty(Metatron.MeCache.Name)) return;

            LoadPilotCache(Metatron.MeCache.Name);

            IsEnabled = false;
        }

        public override bool OutOfFrameCleanup()
        {
            if (IsCleanedUpOutOfFrame) return IsCleanedUpOutOfFrame;

            using (Stream tw = File.Create(_configFile))
            {
                Serializer.Serialize(tw, _cachedPilotsById);
            }

            IsCleanedUpOutOfFrame = true;
            return IsCleanedUpOutOfFrame;
        }

        public void AddPilot(Pilot pilot, string corporationName, string allianceName)
		{
            var cachedPilot = new CachedPilot(pilot, corporationName, allianceName);
            AddPilot(cachedPilot);
		}

		public void AddPilot(CachedPilot cp)
		{
            lock (_cachedPilotsById)
			{
				_cachedPilotsById.Add(cp.CharID, cp);
			}
		}

		public void LoadPilotCache(string charName)
		{
            var methodName = "LoadPilotCache";
			LogTrace(methodName, "Character: {0}", charName);

			_configDirectory = String.Format("{0}\\Metatron\\config", Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath));

			if (!Directory.Exists(_configDirectory))
			{
				Directory.CreateDirectory(_configDirectory);
			}

            _configFile = Path.Combine(_configDirectory, String.Format("{0} Pilot Cache.bin", charName));

		    if (!File.Exists(_configFile)) return;

		    try
		    {
		        using (Stream fs = File.Open(_configFile, FileMode.Open))
		        {
		            var cachedPilotsById = Serializer.Deserialize<Dictionary<Int64, CachedPilot>>(fs);

		            if (cachedPilotsById == null)
		                LogMessage(methodName, LogSeverityTypes.Debug, "Deserialized a null object for the Pilot Cache.");

		            _cachedPilotsById = cachedPilotsById;
		        }
		    }
		    catch (Exception e)
		    {
		        LogMessage(methodName, LogSeverityTypes.Debug, "Caught exception: {0}, stack trace: {1}",
		                   e.Message, e.StackTrace);

		        LogMessage(methodName, LogSeverityTypes.Standard, "Unable to load pilot cache; creating a new one.");

		        _cachedPilotsById = new Dictionary<long, CachedPilot>();
		    }
		}
	}
}
