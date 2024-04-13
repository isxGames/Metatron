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
    internal sealed class AllianceCache : ModuleBase, IAllianceCache
    {
        //SQL connection for access to the DB file
        private readonly SQLiteConnection _sqLiteConnection;

        private static readonly string FileName = "AllianceCache.bin";
        //SqlFileName = "Alliances.db";

        //File paths and connection strings
        private string _sqlDbFilePath = string.Empty;
        //_connectionString = string.Empty;

        //Callbacks
        private FileReadCallback<CachedAlliance> _loadCallback;
        //Temporary list<CachedCorpration> for creating the database
        List<CachedAlliance> _oldFileDbContents = new List<CachedAlliance>();

        private volatile List<CachedAlliance> _cachedAlliances = new List<CachedAlliance>();
        public ReadOnlyCollection<CachedAlliance> CachedAlliances
        {
            get { return _cachedAlliances.AsReadOnly(); }
        }

        private volatile Dictionary<Int64, CachedAlliance> _cachedAlliancesById = new Dictionary<long, CachedAlliance>();
        public Dictionary<Int64, CachedAlliance> CachedAlliancesById
        {
            get { return _cachedAlliancesById; }
        }

        private volatile List<Int64> _alliancesDoingGetInfo = new List<Int64>();
        private volatile List<Int64> _alliancesQueued = new List<Int64>();

        private readonly string _allianceDbFilePath = string.Empty;

        public AllianceCache()
        {
            //_sqLiteConnection = sqLiteConnection;
            IsEnabled = false;
            ModuleName = "AllianceCache";

            _allianceDbFilePath = Path.Combine(Metatron.DataDirectory, FileName);

            //_sqlDbFilePath = string.Format("{0}\\{1}", Metatron.DataDirectory, SqlFileName);
            //_connectionString = string.Format("Data Source={0};Version=3", _sqlDbFilePath);

            _loadCallback = LoadComplete;
        }

        private void LoadComplete(List<CachedAlliance> results)
        {
            var methodName = "LoadComplete";
            LogTrace(methodName);

            lock (CachedAlliances)
            {
                _cachedAlliances = results;
                foreach (var cachedAlliance in
                    _cachedAlliances.Where(cachedAlliance => !CachedAlliancesById.ContainsKey(cachedAlliance.AllianceId)))
                {
                    _cachedAlliancesById.Add(cachedAlliance.AllianceId, cachedAlliance);
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
                    OldLoadAllianceDatabase();
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
                    SaveAllianceDatabase();
                    _isCleaningUp = true;
                }
            }

            return IsCleanedUpOutOfFrame;
        }

        private void SaveAllianceDatabase()
        {
            var methodName = "_saveAllianceDB";
            LogTrace(methodName);

            _alliancesQueued.Clear();
            _alliancesDoingGetInfo.Clear();

            //Write our database
            var succeeded = false;
            var timeout = 5;

            while (!succeeded && timeout-- > 0)
            {
                try
                {
                    using (var fileStream = File.Open(_allianceDbFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        //Get a list from disk to update
                        var diskAlliances = Serializer.Deserialize<List<CachedAlliance>>(fileStream) ??
                                               new List<CachedAlliance>();

                        //Handle null list from deserialization

                        lock (CachedAlliances)
                        {
                            foreach (var localAlliance in CachedAlliances)
                            {
                                var matchFound = false;
                                //Find a match in disk alliances
                                for (var index = 0; index < diskAlliances.Count; index++)
                                {
                                    var diskAlliance = diskAlliances[index];

                                    if (localAlliance.AllianceId != diskAlliance.AllianceId)
                                        continue;

                                    matchFound = true;
                                    //Update if necessary
                                    if (localAlliance.LastUpdated.CompareTo(diskAlliance.LastUpdated) >= 0)
                                    {
                                        diskAlliances[index] = localAlliance;
                                    }
                                    break;
                                }
                                if (!matchFound)
                                {
                                    diskAlliances.Add(localAlliance);
                                }
                            }
                        }

                        //Clear the file
                        fileStream.Seek(0, SeekOrigin.Begin);
                        fileStream.SetLength(0);

                        //Save the updated database back to disk
                        //fileStream.Seek(0, SeekOrigin.Begin);
                        Serializer.Serialize(fileStream, diskAlliances);
                    }
                }
                catch (IOException e)
                {
                    LogException(e, methodName, "Caught exception while cleaning up AllianceCache:");
                    Thread.Sleep(50);
                }
                succeeded = true;
            }
            IsCleanedUpOutOfFrame = true;
        }

        private void LoadAllianceDatabase()
        {
            var methodName = "LoadAllianceDatabase";
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
                sqLiteCommand.CommandText = "SELECT name FROM sqlite_master WHERE name = 'alliances';";
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
                    "CREATE TABLE alliances (",
                    "id integer primary key autoincrement, ",
                    "corpID integer, ",
                    "name varchar(40), ",
                    "ticker varchar(6), ",
                    "allianceID integer",
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
            if (!File.Exists(_allianceDbFilePath))
                return;

            //Deserialize a list of stuff
            _loadCallback = new FileReadCallback<CachedAlliance>(NewLoadFinished);
            Metatron.FileManager.QueueDeserialize(_allianceDbFilePath, _loadCallback);

            //Use a command
            using (var sqLiteCommand = _sqLiteConnection.CreateCommand())
            {
                //Loop all cached alliances
                foreach (var cachedAlliance in CachedAlliances)
                {
                    sqLiteCommand.CommandText = String.Concat(
                        "INSERT INTO alliances ('corpID', 'name', 'ticker', 'memberOfAlliance') VALUES (",
                        String.Format("{0}, '{2}', '{3}');", cachedAlliance.AllianceId, cachedAlliance.Name,
                                      cachedAlliance.Ticker));
                    sqLiteCommand.ExecuteNonQuery();
                }
            }
        }

        private void NewLoadFinished(List<CachedAlliance> results)
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

        private void OldLoadAllianceDatabase()
        {
            var methodName = "OldLoadAllianceDatabase";
            LogTrace(methodName);

            if (!Directory.Exists(Metatron.DataDirectory))
            {
                Directory.CreateDirectory(Metatron.DataDirectory);
            }

            if (File.Exists(_allianceDbFilePath))
            {
                Metatron.FileManager.QueueDeserialize(_allianceDbFilePath, _loadCallback);
            }
            else
            {
                _loadCallback(new List<CachedAlliance>());
            }
        }

        public void GetAllianceInfo(Int64 corpId)
        {
            var methodName = "GetAllianceInfo";
            LogTrace(methodName, "AllianceID: {0}", corpId);

            if (_alliancesDoingGetInfo.Contains(corpId) || CachedAlliancesById.ContainsKey(corpId))
                return;

            //Only have 3 concurrent threads.
            lock (this)
            {
                if (_alliancesDoingGetInfo.Count < 3)
                {
                    if (!_alliancesQueued.Contains(corpId))
                    {
                        _alliancesDoingGetInfo.Add(corpId);
                        ThreadPool.QueueUserWorkItem(TryGetAllianceInfo, new AllianceStateInfo(corpId));
                        //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Trace,
                        //"GetCorpInfo", String.Format("Getting corp info for {0}. Running: {1}, Queued: {2}",
                        //corpID, _alliancesDoingGetInfo.Count, _alliancesQueued.Count)));
                    }
                }
                else
                {
                    _alliancesQueued.Add(corpId);
                    //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Trace,
                    //"GetCorpInfo", String.Format("Queueing corp info for {0}. Running: {1}, Queued: {2}",
                    //corpID, _alliancesDoingGetInfo.Count, _alliancesQueued.Count)));
                }
            }
        }

        private void TryGetAllianceInfo(object stateInfo)
        {
            var methodName = "TryGetAllianceInfo";

            try
            {
                //If stateinfo is for some fucked up reason null, just return. Dont' fuck with anything, just return.
                //Same for Logging
                if (stateInfo == null || Metatron.Logging == null)
                {
                    return;
                }

                var stateObject = stateInfo as AllianceStateInfo;
                if (stateObject == null)
                {
                    return;
                }

                var corpId = stateObject.AllianceId;
                LogTrace(methodName, "AllianceID: {0}", corpId);

                if (Metatron.AllianceCache.CachedAlliancesById.ContainsKey(corpId))
                {
                    //If we have queued requests, move one over because this one is done
                    lock (this)
                    {
                        if (_alliancesDoingGetInfo.Contains(corpId))
                        {
                            _alliancesDoingGetInfo.Remove(corpId);
                        }
                        if (_alliancesQueued.Count > 0)
                        {
                            _alliancesDoingGetInfo.Add(_alliancesQueued[0]);
                            ThreadPool.QueueUserWorkItem(TryGetAllianceInfo,
                                new AllianceStateInfo(_alliancesQueued[0]));
                            _alliancesQueued.RemoveAt(0);
                        }
                    }
                    return;
                }

                //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                //    "TryGetAllianceInfo", String.Format("Downloading information for alliance {0}...",
                //    corpID)));

                var cDbWebRequest = (HttpWebRequest)WebRequest.Create(
                    String.Format("https://esi.evetech.net/latest/alliances/{0}/?datasource=tranquility", corpId));
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
                            if (_alliancesDoingGetInfo.Contains(corpId))
                            {
                                _alliancesDoingGetInfo.Remove(corpId);
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

                    CachedAlliance tempCachedAlliance = new CachedAlliance();
                    ESIAlliance eSIAlliance = null;
                    try
                    {
                        eSIAlliance = JsonConvert.DeserializeObject<ESIAlliance>(json);
                    }
                    catch (Exception e)
                    {
                        LogMessage(methodName, LogSeverityTypes.Critical, $"Caught exception while parsing a Alliance API response: {e}");
                        return;
                    }
                    if (eSIAlliance != null)
                    {
                        tempCachedAlliance.AllianceId = corpId;
                        tempCachedAlliance.Ticker = eSIAlliance.Ticker;
                        tempCachedAlliance.Name = eSIAlliance.Name;
                        tempCachedAlliance.LastUpdated = DateTime.Now;
                    }

                    // Perform any additional checks you need on the object here

                    //Core.Metatron.Logging.LogMessage(Core.Metatron.AllianceDB, new LogEventArgs(LogSeverityTypes.Debug,
                    //    "TryGetAllianceInfo", String.Format("Got info: Name - {0}, Ticker - {1}, ID - {2}, AllianceID - {3}",
                    //    tempCachedAlliance.Name, tempCachedAlliance.Ticker, tempCachedAlliance.AllianceID,
                    //    tempCachedAlliance.MemberOfAlliance)));
                    lock (this)
                    {
                        if (!_cachedAlliances.Contains(tempCachedAlliance))
                        {
                            _cachedAlliances.Add(tempCachedAlliance);
                        }
                        if (!_cachedAlliancesById.ContainsKey(tempCachedAlliance.AllianceId))
                        {
                            _cachedAlliancesById.Add(tempCachedAlliance.AllianceId, tempCachedAlliance);
                        }

                        if (_alliancesDoingGetInfo.Contains(corpId))
                        {
                            _alliancesDoingGetInfo.Remove(corpId);
                        }
                        if (_alliancesQueued.Count > 0)
                        {
                            _alliancesDoingGetInfo.Add(_alliancesQueued[0]);
                            ThreadPool.QueueUserWorkItem(TryGetAllianceInfo,
                                new AllianceStateInfo(_alliancesQueued[0]));
                            _alliancesQueued.RemoveAt(0);
                        }
                        //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Trace,
                        //methodName, String.Format("Finishing for corp {0}. Running: {1}, Queued: {2}",
                        //corpID, _alliancesDoingGetInfo.Count, _alliancesQueued.Count)));
                    }
                }
            }
            catch (Exception e)
            {
                LogException(e, methodName, "Caght exception while updating AllianceCache:");
            }
        }

        public void RemoveAlliance(Int64 corpId)
        {
            var methodName = "RemoveAlliance";
            LogTrace(methodName, "corpId: {0}", corpId);

            var cachedAlliance = _cachedAlliances.FirstOrDefault(c => c.AllianceId == corpId);

            if (cachedAlliance == null) return;

            _cachedAlliancesById.Remove(corpId);
            _cachedAlliances.Remove(cachedAlliance);
        }

        private class AllianceStateInfo
        {
            public readonly Int64 AllianceId;

            public AllianceStateInfo(Int64 allianceId)
            {
                AllianceId = allianceId;
            }
        }
    }
    // ReSharper restore ConvertToConstant.Local
}
