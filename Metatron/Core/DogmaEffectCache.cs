using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;

//for reading API results
using System.Net;
using System.Xml;

//for thread-related stuff in parsing
using System.Threading;

//For (de)serialization
using ProtoBuf;
using Metatron.Core.Interfaces;
using System.Data.SQLite;
using System.Text.Json.Serialization;
using System.Text.Json;
using Newtonsoft.Json;

//For DB

namespace Metatron.Core
{
    // ReSharper disable ConvertToConstant.Local
    internal sealed class DogmaEffectCache : ModuleBase, IDogmaEffectCache
    {
        //SQL connection for access to the DB file
        private readonly SQLiteConnection _sqLiteConnection;

        private static readonly string FileName = "DogmaEffectCache.bin";
        //SqlFileName = "DogmaEffects.db";

        //File paths and connection strings
        private string _sqlDbFilePath = string.Empty;
        //_connectionString = string.Empty;

        //Callbacks
        private FileReadCallback<CachedDogma.Effect> _loadCallback;
        //Temporary list<CachedCorpration> for creating the database
        List<CachedDogma.Effect> _oldFileDbContents = new List<CachedDogma.Effect>();

        private volatile List<CachedDogma.Effect> _cachedDogmaEffects = new List<CachedDogma.Effect>();
        public ReadOnlyCollection<CachedDogma.Effect> CachedDogmaEffects
        {
            get { return _cachedDogmaEffects.AsReadOnly(); }
        }

        private volatile Dictionary<int, CachedDogma.Effect> _cachedDogmaEffectsById = new Dictionary<int, CachedDogma.Effect>();
        public Dictionary<int, CachedDogma.Effect> CachedDogmaEffectsById
        {
            get { return _cachedDogmaEffectsById; }
        }

        private volatile List<int> _dogmaEffectsDoingGetInfo = new List<int>();
        private volatile List<int> _dogmaEffectsQueued = new List<int>();

        private readonly string _dogmaEffectDbFilePath = string.Empty;

        public DogmaEffectCache()
        {
            //_sqLiteConnection = sqLiteConnection;
            IsEnabled = false;
            ModuleName = "DogmaEffectCache";

            _dogmaEffectDbFilePath = Path.Combine(Metatron.DataDirectory, FileName);

            //_sqlDbFilePath = string.Format("{0}\\{1}", Metatron.DataDirectory, SqlFileName);
            //_connectionString = string.Format("Data Source={0};Version=3", _sqlDbFilePath);

            _loadCallback = LoadComplete;
        }

        private void LoadComplete(List<CachedDogma.Effect> results)
        {
            var methodName = "LoadComplete";
            LogTrace(methodName);

            lock (CachedDogmaEffects)
            {
                _cachedDogmaEffects = results;
                foreach (var cachedDogmaEffect in
                    _cachedDogmaEffects.Where(cachedDogmaEffect => !CachedDogmaEffectsById.ContainsKey(cachedDogmaEffect.EffectId)))
                {
                    _cachedDogmaEffectsById.Add(cachedDogmaEffect.EffectId, cachedDogmaEffect);
                }
            }
            IsInitialized = true;
        }

        public override bool Initialize()
        {
            var methodName = "Initialize";
            LogTrace(methodName);

            IsCleanedUpOutOfFrame = false;
            if (!IsInitialized)
            {
                if (!_isInitializing)
                {
                    _isInitializing = true;

                    if (!Directory.Exists(Metatron.DataDirectory))
                    {
                        Directory.CreateDirectory(Metatron.DataDirectory);
                    }
                    OldLoadDogmaEffectDatabase();
                }
            }

            return IsInitialized;
        }

        public override bool OutOfFrameCleanup()
        {
            var methodName = "OutOfFrameCleanup";
            LogTrace(methodName);

            if (!IsCleanedUpOutOfFrame)
            {
                if (!_isCleaningUp)
                {
                    SaveDogmaEffectDatabase();
                    _isCleaningUp = true;
                }
            }

            return IsCleanedUpOutOfFrame;
        }

        private void SaveDogmaEffectDatabase()
        {
            var methodName = "_saveDogmaEffectDB";
            LogTrace(methodName);

            _dogmaEffectsQueued.Clear();
            _dogmaEffectsDoingGetInfo.Clear();

            //Write our database
            var succeeded = false;
            var timeout = 5;

            while (!succeeded && timeout-- > 0)
            {
                try
                {
                    using (var fileStream = File.Open(_dogmaEffectDbFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        //Get a list from disk to update
                        var diskDogmaEffects = Serializer.Deserialize<List<CachedDogma.Effect>>(fileStream) ??
                                               new List<CachedDogma.Effect>();

                        //Handle null list from deserialization

                        lock (CachedDogmaEffects)
                        {
                            foreach (var localDogmaEffect in CachedDogmaEffects)
                            {
                                var matchFound = false;
                                //Find a match in disk dogmaEffects
                                for (var index = 0; index < diskDogmaEffects.Count; index++)
                                {
                                    var diskDogmaEffect = diskDogmaEffects[index];

                                    if (localDogmaEffect.EffectId != diskDogmaEffect.EffectId)
                                        continue;

                                    matchFound = true;
                                    //Update if necessary
                                    if (localDogmaEffect.LastUpdated.CompareTo(diskDogmaEffect.LastUpdated) >= 0)
                                    {
                                        diskDogmaEffects[index] = localDogmaEffect;
                                    }
                                    break;
                                }
                                if (!matchFound)
                                {
                                    diskDogmaEffects.Add(localDogmaEffect);
                                }
                            }
                        }

                        //Clear the file
                        fileStream.Seek(0, SeekOrigin.Begin);
                        fileStream.SetLength(0);

                        //Save the updated database back to disk
                        //fileStream.Seek(0, SeekOrigin.Begin);
                        Serializer.Serialize(fileStream, diskDogmaEffects);
                    }
                }
                catch (IOException e)
                {
                    LogException(e, methodName, "Caught exception while cleaning up DogmaEffectCache:");
                    Thread.Sleep(50);
                }
                succeeded = true;
            }
            IsCleanedUpOutOfFrame = true;
        }

        private void LoadDogmaEffectDatabase()
        {
            var methodName = "LoadDogmaEffectDatabase";
            LogTrace(methodName);

            //Check and add the DB file if necessary
            EnsureDatabaseFileExists();
            //Check and add the corp DB table
            CheckAddCorpDatabaseTable();
        }

        private void CheckAddCorpDatabaseTable()
        {
            var methodName = "CheckAddCorpDatabaseTable";
            LogTrace(methodName);

            //Get an SQLcommand
            using (var sqLiteCommand = _sqLiteConnection.CreateCommand())
            {
                //query tables from the master table looking for our table
                sqLiteCommand.CommandText = "SELECT name FROM sqlite_master WHERE name = 'dogmaEffects';";
                //Get a reader with results
                var tableExists = false;
                using (var sqLiteDataReader = sqLiteCommand.ExecuteReader())
                {
                    //If there are any results the table exists
                    tableExists = sqLiteDataReader.Read();
                }

                //if the table doesn't exist...
                if (tableExists)
                    return;

                //build it!
                sqLiteCommand.CommandText = String.Concat(
                    "CREATE TABLE dogmaEffects (",
                    "id integer primary key autoincrement, ",
                    "effectID integer, ",
                    "name varchar(40), ",
                    "description text, ",
                    "dischargeAttributeID integer, ",
                    "displayName varchar(40), ",
                    "durationAttributeId integer, ",
                    "effectCategory integer, ",
                    "effectID integer, ",
                    "electronicChange integer, ",
                    "falloffAttributeID integer, ",
                    "iconID integer, ",
                    "isAssistance integer, ",
                    "isOffensive integer, ",
                    "isWarpSafe integer, ",
                    "postExpression integer, ",
                    "preExpression integer, ",
                    "rangeAttributeID integer, ",
                    "rangeChance integer, ",
                    "trackingSpeedAttributeID integer, ",
                    "disallow_auto_repeat integer",
                    ");"
                    );
                sqLiteCommand.ExecuteNonQuery();

                //Try to populate the DB from file
                PopulateDatabaseFromFile();
            }
        }

        private void PopulateDatabaseFromFile()
        {
            //If the old corp DB file exists...
            if (!File.Exists(_dogmaEffectDbFilePath))
                return;

            //Deserialize a list of stuff
            _loadCallback = new FileReadCallback<CachedDogma.Effect>(NewLoadFinished);
            Metatron.FileManager.QueueDeserialize(_dogmaEffectDbFilePath, _loadCallback);

            //Use a command
            using (var sqLiteCommand = _sqLiteConnection.CreateCommand())
            {
                //Loop all cached dogmaEffects
                foreach (var cachedDogmaEffect in CachedDogmaEffects)
                {
                    sqLiteCommand.CommandText = String.Concat(
                        "INSERT INTO dogmaEffects ('corpID', 'name', 'description', 'dischargeAttributeID', 'displayName', 'durationAttributeID', 'effectCategory', 'effectID', 'electronicChange', 'falloffAttributeID', 'iconID', 'isAssistance', 'isOffensive', 'isWarpSafe', 'postExpression', 'preExpression', 'rangeAttributeID', 'rangeChance', 'trackingSpeedAttributeID', 'disallow_auto_repeat') VALUES (",
                        String.Format("{0}, '{1}', '{2}', {3}, '{4}', {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20});", cachedDogmaEffect.EffectId, cachedDogmaEffect.Name,
                                      cachedDogmaEffect.Description, cachedDogmaEffect.DischargeAttributeId, cachedDogmaEffect.DisplayName, cachedDogmaEffect.DurationAttributeId, cachedDogmaEffect.EffectCategory, cachedDogmaEffect.EffectId, cachedDogmaEffect.ElectronicChange, cachedDogmaEffect.FalloffAttributeId, cachedDogmaEffect.IconId,
                                      cachedDogmaEffect.IsAssistance ? 1 : 0, cachedDogmaEffect.IsOffensive ? 1 : 0, cachedDogmaEffect.IsWarpSafe ? 1 : 0, cachedDogmaEffect.PostExpression, cachedDogmaEffect.PreExpression, cachedDogmaEffect.RangeAttributeId, cachedDogmaEffect.RangeChance, cachedDogmaEffect.TrackingSpeedAttributeId, cachedDogmaEffect.DisallowAutoRepeat ? 1 : 0));
                    sqLiteCommand.ExecuteNonQuery();
                }
            }
        }

        private void NewLoadFinished(List<CachedDogma.Effect> results)
        {
            lock (_oldFileDbContents)
            {
                _oldFileDbContents = results;
            }
        }

        private void EnsureDatabaseFileExists()
        {
            var methodName = "EnsureDatabaseFileExists";
            LogTrace(methodName);

            //If the DB file doesn't exists...
            if (!File.Exists(_sqlDbFilePath))
            {
                //Create it
                SQLiteConnection.CreateFile(_sqlDbFilePath);
            }
        }

        private void OldLoadDogmaEffectDatabase()
        {
            var methodName = "OldLoadDogmaEffectDatabase";
            LogTrace(methodName);

            if (!Directory.Exists(Metatron.DataDirectory))
            {
                Directory.CreateDirectory(Metatron.DataDirectory);
            }

            if (File.Exists(_dogmaEffectDbFilePath))
            {
                Metatron.FileManager.QueueDeserialize(_dogmaEffectDbFilePath, _loadCallback);
            }
            else
            {
                _loadCallback(new List<CachedDogma.Effect>());
            }
        }

        public void GetDogmaEffectInfo(int effectId)
        {
            var methodName = "GetDogmaEffectInfo";
            LogTrace(methodName, "DogmaEffectID: {0}", effectId);

            if (_dogmaEffectsDoingGetInfo.Contains(effectId) || CachedDogmaEffectsById.ContainsKey(effectId))
                return;

            //Only have 3 concurrent threads.
            lock (this)
            {
                if (_dogmaEffectsDoingGetInfo.Count < 3)
                {
                    if (!_dogmaEffectsQueued.Contains(effectId))
                    {
                        _dogmaEffectsDoingGetInfo.Add(effectId);
                        ThreadPool.QueueUserWorkItem(TryGetDogmaEffectInfo, new DogmaEffectStateInfo(effectId));
                        //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Trace,
                        //"GetCorpInfo", String.Format("Getting corp info for {0}. Running: {1}, Queued: {2}",
                        //corpID, _dogmaEffectsDoingGetInfo.Count, _dogmaEffectsQueued.Count)));
                    }
                }
                else
                {
                    _dogmaEffectsQueued.Add(effectId);
                    //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Trace,
                    //"GetCorpInfo", String.Format("Queueing corp info for {0}. Running: {1}, Queued: {2}",
                    //corpID, _dogmaEffectsDoingGetInfo.Count, _dogmaEffectsQueued.Count)));
                }
            }
        }

        private void TryGetDogmaEffectInfo(object stateInfo)
        {
            var methodName = "TryGetDogmaEffectInfo";

            try
            {
                //If stateinfo is for some fucked up reason null, just return. Dont' fuck with anything, just return.
                //Same for Logging
                if (stateInfo == null || Metatron.Logging == null)
                {
                    return;
                }

                var stateObject = stateInfo as DogmaEffectStateInfo;
                if (stateObject == null)
                {
                    return;
                }

                var effectId = stateObject.DogmaEffectId;
                LogTrace(methodName, "DogmaEffectID: {0}", effectId);

                if (Metatron.DogmaEffectCache.CachedDogmaEffectsById.ContainsKey(effectId))
                {
                    //If we have queued requests, move one over because this one is done
                    lock (this)
                    {
                        if (_dogmaEffectsDoingGetInfo.Contains(effectId))
                        {
                            _dogmaEffectsDoingGetInfo.Remove(effectId);
                        }
                        if (_dogmaEffectsQueued.Count > 0)
                        {
                            _dogmaEffectsDoingGetInfo.Add(_dogmaEffectsQueued[0]);
                            ThreadPool.QueueUserWorkItem(TryGetDogmaEffectInfo,
                                new DogmaEffectStateInfo(_dogmaEffectsQueued[0]));
                            _dogmaEffectsQueued.RemoveAt(0);
                        }
                    }
                    return;
                }

                //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                //    "TryGetDogmaEffectInfo", String.Format("Downloading information for dogmaEffect {0}...",
                //    corpID)));

                var cDbWebRequest = (HttpWebRequest)WebRequest.Create(
                    String.Format("https://esi.evetech.net/latest/dogma/effects/{0}/?datasource=tranquility", effectId));
                HttpWebResponse cDbWebResponse;
                try
                {
                    cDbWebResponse = (HttpWebResponse)cDbWebRequest.GetResponse();
                }
                catch (WebException ex)
                {
                    //Check for server unavailable -- eveonline.com is down
                    if (ex.Message == "The remote server returned an error: (503) Server Unavailable." ||
                        ex.Message == "Remote server returned: (503) The server is not available." ||
                        ex.Message.Contains("The server committed a protocol violation."))
                    {
                        lock (this)
                        {
                            if (_dogmaEffectsDoingGetInfo.Contains(effectId))
                            {
                                _dogmaEffectsDoingGetInfo.Remove(effectId);
                            }
                        }
                        return;
                    }
                    throw;
                }

                using (var cDbStream = cDbWebResponse.GetResponseStream())
                {
                    if (cDbStream == null) return;

                    StreamReader reader = new StreamReader(cDbStream);
                    string json = reader.ReadToEnd();

                    CachedDogma.Effect tempCachedDogmaEffect = new CachedDogma.Effect();
                    ESIDogmaEffect eSIDogmaEffect = null;
                    try
                    {
                        eSIDogmaEffect = JsonConvert.DeserializeObject<ESIDogmaEffect>(json);
                    }
                    catch (Exception e)
                    {
                        LogMessage(methodName, LogSeverityTypes.Critical, $"Caught exception while parsing a DogmaEffect API response: {e}");
                        return;
                    }
                    if (eSIDogmaEffect != null)
                    {
                        tempCachedDogmaEffect.EffectId = effectId;
                        tempCachedDogmaEffect.Name = eSIDogmaEffect.Name;
                        tempCachedDogmaEffect.Description = eSIDogmaEffect.Description;
                        tempCachedDogmaEffect.DisplayName = eSIDogmaEffect.DisplayName;
                        tempCachedDogmaEffect.IsAssistance = eSIDogmaEffect.IsAssistance;
                        tempCachedDogmaEffect.IsOffensive = eSIDogmaEffect.IsOffensive;
                        tempCachedDogmaEffect.RangeChance = eSIDogmaEffect.RangeChance;
                        tempCachedDogmaEffect.RangeAttributeId = eSIDogmaEffect.RangeAttributeId.HasValue ? eSIDogmaEffect.RangeAttributeId.Value : 0;
                        tempCachedDogmaEffect.EffectCategory = eSIDogmaEffect.EffectCategory.HasValue ? eSIDogmaEffect.EffectCategory.Value : 0;
                        tempCachedDogmaEffect.DisallowAutoRepeat = eSIDogmaEffect.DisallowAutoRepeat;
                        tempCachedDogmaEffect.DischargeAttributeId = eSIDogmaEffect.DischargeAttributeId.HasValue ? eSIDogmaEffect.DischargeAttributeId.Value : 0;
                        tempCachedDogmaEffect.DurationAttributeId = eSIDogmaEffect.DurationAttributeId.HasValue ? eSIDogmaEffect.DurationAttributeId.Value : 0;
                        tempCachedDogmaEffect.ElectronicChange = eSIDogmaEffect.ElectronicChance;
                        tempCachedDogmaEffect.FalloffAttributeId = eSIDogmaEffect.FalloffAttributeId.HasValue ? eSIDogmaEffect.FalloffAttributeId.Value : 0;
                        tempCachedDogmaEffect.IconId = eSIDogmaEffect.IconId.HasValue ? eSIDogmaEffect.IconId.Value : 0;
                        tempCachedDogmaEffect.IsWarpSafe = eSIDogmaEffect.IsWarpSafe;
                        tempCachedDogmaEffect.PostExpression = eSIDogmaEffect.PostExpression.HasValue ? eSIDogmaEffect.PostExpression.Value : 0;
                        tempCachedDogmaEffect.PreExpression = eSIDogmaEffect.PreExpression.HasValue ? eSIDogmaEffect.PreExpression.Value : 0;
                        tempCachedDogmaEffect.TrackingSpeedAttributeId = eSIDogmaEffect.TrackingSpeedAttributeId.HasValue ? eSIDogmaEffect.TrackingSpeedAttributeId.Value : 0;
                        tempCachedDogmaEffect.LastUpdated = DateTime.Now;
                    }

                    // Perform any additional checks you need on the object here
                    
                    //Core.Metatron.Logging.LogMessage(Core.Metatron.DogmaEffectDB, new LogEventArgs(LogSeverityTypes.Debug,
                    //    "TryGetDogmaEffectInfo", String.Format("Got info: Name - {0}, Ticker - {1}, ID - {2}, AllianceID - {3}",
                    //    tempCachedDogma.Effect.Name, tempCachedDogma.Effect.Ticker, tempCachedDogma.Effect.DogmaEffectID,
                    //    tempCachedDogma.Effect.MemberOfAlliance)));
                    lock (this)
                    {
                        if (!_cachedDogmaEffects.Contains(tempCachedDogmaEffect))
                        {
                            _cachedDogmaEffects.Add(tempCachedDogmaEffect);
                        }
                        if (!_cachedDogmaEffectsById.ContainsKey(tempCachedDogmaEffect.EffectId))
                        {
                            _cachedDogmaEffectsById.Add(tempCachedDogmaEffect.EffectId, tempCachedDogmaEffect);
                        }

                        if (_dogmaEffectsDoingGetInfo.Contains(effectId))
                        {
                            _dogmaEffectsDoingGetInfo.Remove(effectId);
                        }
                        if (_dogmaEffectsQueued.Count > 0)
                        {
                            _dogmaEffectsDoingGetInfo.Add(_dogmaEffectsQueued[0]);
                            ThreadPool.QueueUserWorkItem(TryGetDogmaEffectInfo,
                                new DogmaEffectStateInfo(_dogmaEffectsQueued[0]));
                            _dogmaEffectsQueued.RemoveAt(0);
                        }
                        //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Trace,
                        //methodName, String.Format("Finishing for corp {0}. Running: {1}, Queued: {2}",
                        //corpID, _dogmaEffectsDoingGetInfo.Count, _dogmaEffectsQueued.Count)));
                    }
                }
            }
            catch (Exception e)
            {
                LogException(e, methodName, "Caght exception while updating DogmaEffectCache:");
            }
        }

        public void RemoveDogmaEffect(int effectId)
        {
            var methodName = "RemoveDogmaEffect";
            LogTrace(methodName, "effectId: {0}", effectId);

            var cachedDogmaEffect = _cachedDogmaEffects.FirstOrDefault(c => c.EffectId == effectId);

            if (cachedDogmaEffect == null) return;

            _cachedDogmaEffectsById.Remove(effectId);
            _cachedDogmaEffects.Remove(cachedDogmaEffect);
        }

        private class DogmaEffectStateInfo
        {
            public readonly int DogmaEffectId;

            public DogmaEffectStateInfo(int dogmaEffectId)
            {
                DogmaEffectId = dogmaEffectId;
            }
        }
    }
    // ReSharper restore ConvertToConstant.Local
}
