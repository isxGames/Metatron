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
    internal sealed class CorporationCache : ModuleBase, ICorporationCache
    {
        //SQL connection for access to the DB file
        private readonly SQLiteConnection _sqLiteConnection;

        private static readonly string FileName = "CorporationCache.bin";
        //SqlFileName = "Corporations.db";

        //File paths and connection strings
        private string _sqlDbFilePath = string.Empty;
        //_connectionString = string.Empty;

        //Callbacks
        private FileReadCallback<CachedCorporation> _loadCallback;
        //Temporary list<CachedCorpration> for creating the database
        List<CachedCorporation> _oldFileDbContents = new List<CachedCorporation>();

        private volatile List<CachedCorporation> _cachedCorporations = new List<CachedCorporation>();
        public ReadOnlyCollection<CachedCorporation> CachedCorporations
        {
            get { return _cachedCorporations.AsReadOnly(); }
        }

        private volatile Dictionary<Int64, CachedCorporation> _cachedCorporationsById = new Dictionary<long, CachedCorporation>();
        public Dictionary<Int64, CachedCorporation> CachedCorporationsById
        {
            get { return _cachedCorporationsById; }
        }

        private volatile List<Int64> _corporationsDoingGetInfo = new List<Int64>();
        private volatile List<Int64> _corporationsQueued = new List<Int64>();

        private readonly string _corporationDbFilePath = string.Empty;

        public CorporationCache()
        {
            //_sqLiteConnection = sqLiteConnection;
            IsEnabled = false;
            ModuleName = "CorporationCache";

            _corporationDbFilePath = Path.Combine(Metatron.DataDirectory, FileName);

            //_sqlDbFilePath = string.Format("{0}\\{1}", Metatron.DataDirectory, SqlFileName);
            //_connectionString = string.Format("Data Source={0};Version=3", _sqlDbFilePath);

            _loadCallback = LoadComplete;
        }

        private void LoadComplete(List<CachedCorporation> results)
        {
            var methodName = "LoadComplete";
            LogTrace(methodName);

            lock (CachedCorporations)
            {
                _cachedCorporations = results;
                foreach (var cachedCorporation in
                    _cachedCorporations.Where(cachedCorporation => !CachedCorporationsById.ContainsKey(cachedCorporation.CorporationId)))
                {
                    _cachedCorporationsById.Add(cachedCorporation.CorporationId, cachedCorporation);
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
                    OldLoadCorporationDatabase();
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
                    SaveCorporationDatabase();
                    _isCleaningUp = true;
                }
            }

            return IsCleanedUpOutOfFrame;
        }

        private void SaveCorporationDatabase()
        {
            var methodName = "_saveCorporationDB";
            LogTrace(methodName);

            _corporationsQueued.Clear();
            _corporationsDoingGetInfo.Clear();

            //Write our database
            var succeeded = false;
            var timeout = 5;

            while (!succeeded && timeout-- > 0)
            {
                try
                {
                    using (var fileStream = File.Open(_corporationDbFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        //Get a list from disk to update
                        var diskCorporations = Serializer.Deserialize<List<CachedCorporation>>(fileStream) ??
                                               new List<CachedCorporation>();

                        //Handle null list from deserialization

                        lock (CachedCorporations)
                        {
                            foreach (var localCorporation in CachedCorporations)
                            {
                                var matchFound = false;
                                //Find a match in disk corporations
                                for (var index = 0; index < diskCorporations.Count; index++)
                                {
                                    var diskCorporation = diskCorporations[index];

                                    if (localCorporation.CorporationId != diskCorporation.CorporationId)
                                        continue;

                                    matchFound = true;
                                    //Update if necessary
                                    if (localCorporation.LastUpdated.CompareTo(diskCorporation.LastUpdated) >= 0)
                                    {
                                        diskCorporations[index] = localCorporation;
                                    }
                                    break;
                                }
                                if (!matchFound)
                                {
                                    diskCorporations.Add(localCorporation);
                                }
                            }
                        }

                        //Clear the file
                        fileStream.Seek(0, SeekOrigin.Begin);
                        fileStream.SetLength(0);

                        //Save the updated database back to disk
                        //fileStream.Seek(0, SeekOrigin.Begin);
                        Serializer.Serialize(fileStream, diskCorporations);
                    }
                }
                catch (IOException e)
                {
                    LogException(e, methodName, "Caught exception while cleaning up CorporationCache:");
                    Thread.Sleep(50);
                }
                succeeded = true;
            }
            IsCleanedUpOutOfFrame = true;
        }

        private void LoadCorporationDatabase()
        {
            var methodName = "LoadCorporationDatabase";
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
                sqLiteCommand.CommandText = "SELECT name FROM sqlite_master WHERE name = 'corporations';";
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
                    "CREATE TABLE corporations (",
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
            if (!File.Exists(_corporationDbFilePath))
                return;

            //Deserialize a list of stuff
            _loadCallback = new FileReadCallback<CachedCorporation>(NewLoadFinished);
            Metatron.FileManager.QueueDeserialize(_corporationDbFilePath, _loadCallback);

            //Use a command
            using (var sqLiteCommand = _sqLiteConnection.CreateCommand())
            {
                //Loop all cached corporations
                foreach (var cachedCorporation in CachedCorporations)
                {
                    sqLiteCommand.CommandText = String.Concat(
                        "INSERT INTO corporations ('corpID', 'name', 'ticker', 'memberOfAlliance') VALUES (",
                        String.Format("{0}, '{2}', '{3}', {4});", cachedCorporation.CorporationId, cachedCorporation.Name,
                                      cachedCorporation.Ticker, cachedCorporation.MemberOfAlliance));
                    sqLiteCommand.ExecuteNonQuery();
                }
            }
        }

        private void NewLoadFinished(List<CachedCorporation> results)
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

        private void OldLoadCorporationDatabase()
        {
            var methodName = "OldLoadCorporationDatabase";
            LogTrace(methodName);

            if (!Directory.Exists(Metatron.DataDirectory))
            {
                Directory.CreateDirectory(Metatron.DataDirectory);
            }

            if (File.Exists(_corporationDbFilePath))
            {
                Metatron.FileManager.QueueDeserialize(_corporationDbFilePath, _loadCallback);
            }
            else
            {
                _loadCallback(new List<CachedCorporation>());
            }
        }

        public void GetCorporationInfo(Int64 corpId)
        {
            var methodName = "GetCorporationInfo";
            LogTrace(methodName, "CorporationID: {0}", corpId);

            if (_corporationsDoingGetInfo.Contains(corpId) || CachedCorporationsById.ContainsKey(corpId))
                return;

            //Only have 3 concurrent threads.
            lock (this)
            {
                if (_corporationsDoingGetInfo.Count < 3)
                {
                    if (!_corporationsQueued.Contains(corpId))
                    {
                        _corporationsDoingGetInfo.Add(corpId);
                        ThreadPool.QueueUserWorkItem(TryGetCorporationInfo, new CorporationStateInfo(corpId));
                        //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Trace,
                        //"GetCorpInfo", String.Format("Getting corp info for {0}. Running: {1}, Queued: {2}",
                        //corpID, _corporationsDoingGetInfo.Count, _corporationsQueued.Count)));
                    }
                }
                else
                {
                    _corporationsQueued.Add(corpId);
                    //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Trace,
                    //"GetCorpInfo", String.Format("Queueing corp info for {0}. Running: {1}, Queued: {2}",
                    //corpID, _corporationsDoingGetInfo.Count, _corporationsQueued.Count)));
                }
            }
        }

        private void TryGetCorporationInfo(object stateInfo)
        {
            var methodName = "TryGetCorporationInfo";

            try
            {
                //If stateinfo is for some fucked up reason null, just return. Dont' fuck with anything, just return.
                //Same for Logging
                if (stateInfo == null || Metatron.Logging == null)
                {
                    return;
                }

                var stateObject = stateInfo as CorporationStateInfo;
                if (stateObject == null)
                {
                    return;
                }

                var corpId = stateObject.CorporationId;
                LogTrace(methodName, "CorporationID: {0}", corpId);

                if (Metatron.CorporationCache.CachedCorporationsById.ContainsKey(corpId))
                {
                    //If we have queued requests, move one over because this one is done
                    lock (this)
                    {
                        if (_corporationsDoingGetInfo.Contains(corpId))
                        {
                            _corporationsDoingGetInfo.Remove(corpId);
                        }
                        if (_corporationsQueued.Count > 0)
                        {
                            _corporationsDoingGetInfo.Add(_corporationsQueued[0]);
                            ThreadPool.QueueUserWorkItem(TryGetCorporationInfo,
                                new CorporationStateInfo(_corporationsQueued[0]));
                            _corporationsQueued.RemoveAt(0);
                        }
                    }
                    return;
                }

                //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                //    "TryGetCorporationInfo", String.Format("Downloading information for corporation {0}...",
                //    corpID)));

                var cDbWebRequest = (HttpWebRequest)WebRequest.Create(
                    String.Format("https://esi.evetech.net/latest/corporations/{0}/?datasource=tranquility", corpId));
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
                            if (_corporationsDoingGetInfo.Contains(corpId))
                            {
                                _corporationsDoingGetInfo.Remove(corpId);
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

                    CachedCorporation tempCachedCorporation = new CachedCorporation();
                    ESICorporation eSICorporation = null;
                    try
                    {
                        eSICorporation = JsonConvert.DeserializeObject<ESICorporation>(json);
                    }
                    catch (Exception e)
                    {
                        LogMessage(methodName, LogSeverityTypes.Critical, $"Caught exception while parsing a Corporation API response: {e}");
                        return;
                    }
                    if (eSICorporation != null)
                    {
                        tempCachedCorporation.CorporationId = corpId;
                        tempCachedCorporation.Ticker = eSICorporation.Ticker;
                        tempCachedCorporation.MemberOfAlliance = eSICorporation.AllianceId;
                        tempCachedCorporation.Name = eSICorporation.Name;
                        tempCachedCorporation.LastUpdated = DateTime.Now;
                    }

                    // Perform any additional checks you need on the object here
                    
                    //Core.Metatron.Logging.LogMessage(Core.Metatron.CorporationDB, new LogEventArgs(LogSeverityTypes.Debug,
                    //    "TryGetCorporationInfo", String.Format("Got info: Name - {0}, Ticker - {1}, ID - {2}, AllianceID - {3}",
                    //    tempCachedCorporation.Name, tempCachedCorporation.Ticker, tempCachedCorporation.CorporationID,
                    //    tempCachedCorporation.MemberOfAlliance)));
                    lock (this)
                    {
                        if (!_cachedCorporations.Contains(tempCachedCorporation))
                        {
                            _cachedCorporations.Add(tempCachedCorporation);
                        }
                        if (!_cachedCorporationsById.ContainsKey(tempCachedCorporation.CorporationId))
                        {
                            _cachedCorporationsById.Add(tempCachedCorporation.CorporationId, tempCachedCorporation);
                        }

                        if (_corporationsDoingGetInfo.Contains(corpId))
                        {
                            _corporationsDoingGetInfo.Remove(corpId);
                        }
                        if (_corporationsQueued.Count > 0)
                        {
                            _corporationsDoingGetInfo.Add(_corporationsQueued[0]);
                            ThreadPool.QueueUserWorkItem(TryGetCorporationInfo,
                                new CorporationStateInfo(_corporationsQueued[0]));
                            _corporationsQueued.RemoveAt(0);
                        }
                        //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Trace,
                        //methodName, String.Format("Finishing for corp {0}. Running: {1}, Queued: {2}",
                        //corpID, _corporationsDoingGetInfo.Count, _corporationsQueued.Count)));
                    }
                }
            }
            catch (Exception e)
            {
                LogException(e, methodName, "Caght exception while updating CorporationCache:");
            }
        }

        public void RemoveCorporation(Int64 corpId)
        {
            var methodName = "RemoveCorporation";
            LogTrace(methodName, "corpId: {0}", corpId);

            var cachedCorporation = _cachedCorporations.FirstOrDefault(c => c.CorporationId == corpId);

            if (cachedCorporation == null) return;

            _cachedCorporationsById.Remove(corpId);
            _cachedCorporations.Remove(cachedCorporation);
        }

        private class CorporationStateInfo
        {
            public readonly Int64 CorporationId;

            public CorporationStateInfo(Int64 corporationId)
            {
                CorporationId = corporationId;
            }
        }
    }
    // ReSharper restore ConvertToConstant.Local
}
