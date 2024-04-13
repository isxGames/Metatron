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
    internal sealed class DogmaAttributeCache : ModuleBase, IDogmaAttributeCache
    {
        //SQL connection for access to the DB file
        private readonly SQLiteConnection _sqLiteConnection;

        private static readonly string FileName = "DogmaAttributeCache.bin";
        //SqlFileName = "DogmaAttributes.db";

        //File paths and connection strings
        private string _sqlDbFilePath = string.Empty;
        //_connectionString = string.Empty;

        //Callbacks
        private FileReadCallback<CachedDogma.Attribute> _loadCallback;
        //Temporary list<CachedCorpration> for creating the database
        List<CachedDogma.Attribute> _oldFileDbContents = new List<CachedDogma.Attribute>();

        private volatile List<CachedDogma.Attribute> _cachedDogmaAttributes = new List<CachedDogma.Attribute>();
        public ReadOnlyCollection<CachedDogma.Attribute> CachedDogmaAttributes
        {
            get { return _cachedDogmaAttributes.AsReadOnly(); }
        }

        private volatile Dictionary<int, CachedDogma.Attribute> _cachedDogmaAttributesById = new Dictionary<int, CachedDogma.Attribute>();
        public Dictionary<int, CachedDogma.Attribute> CachedDogmaAttributesById
        {
            get { return _cachedDogmaAttributesById; }
        }

        private volatile List<int> _dogmaAttributesDoingGetInfo = new List<int>();
        private volatile List<int> _dogmaAttributesQueued = new List<int>();

        private readonly string _dogmaAttributeDbFilePath = string.Empty;

        public DogmaAttributeCache()
        {
            //_sqLiteConnection = sqLiteConnection;
            IsEnabled = false;
            ModuleName = "DogmaAttributeCache";

            _dogmaAttributeDbFilePath = Path.Combine(Metatron.DataDirectory, FileName);

            //_sqlDbFilePath = string.Format("{0}\\{1}", Metatron.DataDirectory, SqlFileName);
            //_connectionString = string.Format("Data Source={0};Version=3", _sqlDbFilePath);

            _loadCallback = LoadComplete;
        }

        private void LoadComplete(List<CachedDogma.Attribute> results)
        {
            var methodName = "LoadComplete";
            LogTrace(methodName);

            lock (CachedDogmaAttributes)
            {
                _cachedDogmaAttributes = results;
                foreach (var cachedDogmaAttribute in
                    _cachedDogmaAttributes.Where(cachedDogmaAttribute => !CachedDogmaAttributesById.ContainsKey(cachedDogmaAttribute.AttributeId)))
                {
                    _cachedDogmaAttributesById.Add(cachedDogmaAttribute.AttributeId, cachedDogmaAttribute);
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
                    OldLoadDogmaAttributeDatabase();
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
                    SaveDogmaAttributeDatabase();
                    _isCleaningUp = true;
                }
            }

            return IsCleanedUpOutOfFrame;
        }

        private void SaveDogmaAttributeDatabase()
        {
            var methodName = "_saveDogmaAttributeDB";
            LogTrace(methodName);

            _dogmaAttributesQueued.Clear();
            _dogmaAttributesDoingGetInfo.Clear();

            //Write our database
            var succeeded = false;
            var timeout = 5;

            while (!succeeded && timeout-- > 0)
            {
                try
                {
                    using (var fileStream = File.Open(_dogmaAttributeDbFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        //Get a list from disk to update
                        var diskDogmaAttributes = Serializer.Deserialize<List<CachedDogma.Attribute>>(fileStream) ??
                                               new List<CachedDogma.Attribute>();

                        //Handle null list from deserialization

                        lock (CachedDogmaAttributes)
                        {
                            foreach (var localDogmaAttribute in CachedDogmaAttributes)
                            {
                                var matchFound = false;
                                //Find a match in disk dogmaAttributes
                                for (var index = 0; index < diskDogmaAttributes.Count; index++)
                                {
                                    var diskDogmaAttribute = diskDogmaAttributes[index];

                                    if (localDogmaAttribute.AttributeId != diskDogmaAttribute.AttributeId)
                                        continue;

                                    matchFound = true;
                                    //Update if necessary
                                    if (localDogmaAttribute.LastUpdated.CompareTo(diskDogmaAttribute.LastUpdated) >= 0)
                                    {
                                        diskDogmaAttributes[index] = localDogmaAttribute;
                                    }
                                    break;
                                }
                                if (!matchFound)
                                {
                                    diskDogmaAttributes.Add(localDogmaAttribute);
                                }
                            }
                        }

                        //Clear the file
                        fileStream.Seek(0, SeekOrigin.Begin);
                        fileStream.SetLength(0);

                        //Save the updated database back to disk
                        //fileStream.Seek(0, SeekOrigin.Begin);
                        Serializer.Serialize(fileStream, diskDogmaAttributes);
                    }
                }
                catch (IOException e)
                {
                    LogException(e, methodName, "Caught exception while cleaning up DogmaAttributeCache:");
                    Thread.Sleep(50);
                }
                succeeded = true;
            }
            IsCleanedUpOutOfFrame = true;
        }

        private void LoadDogmaAttributeDatabase()
        {
            var methodName = "LoadDogmaAttributeDatabase";
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
                sqLiteCommand.CommandText = "SELECT name FROM sqlite_master WHERE name = 'dogmaAttributes';";
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
                    "CREATE TABLE dogmaAttributes (",
                    "id integer primary key autoincrement, ",
                    "attributeID integer, ",
                    "name varchar(40), ",
                    "description text, ",
                    "iconID integer, ",
                    "displayName varchar(40), ",
                    "stackable integer, ",
                    "unitID integer, ",
                    "high_is_good integer",
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
            if (!File.Exists(_dogmaAttributeDbFilePath))
                return;

            //Deserialize a list of stuff
            _loadCallback = new FileReadCallback<CachedDogma.Attribute>(NewLoadFinished);
            Metatron.FileManager.QueueDeserialize(_dogmaAttributeDbFilePath, _loadCallback);

            //Use a command
            using (var sqLiteCommand = _sqLiteConnection.CreateCommand())
            {
                //Loop all cached dogmaAttributes
                foreach (var cachedDogmaAttribute in CachedDogmaAttributes)
                {
                    sqLiteCommand.CommandText = String.Concat(
                        "INSERT INTO dogmaAttributes ('attributeID', 'name', 'description', 'iconID', 'displayName', 'stackable', 'unitID', 'high_is_good') VALUES (",
                        String.Format("{0}, '{1}', '{2}', {3}, '{4}', {5}, {6}, {7});", cachedDogmaAttribute.AttributeId, cachedDogmaAttribute.Name,
                                      cachedDogmaAttribute.Description, cachedDogmaAttribute.IconId, cachedDogmaAttribute.DisplayName, cachedDogmaAttribute.Stackable ? 1 : 0, cachedDogmaAttribute.UnitId, cachedDogmaAttribute.HighIsGood ? 1 : 0));
                    sqLiteCommand.ExecuteNonQuery();
                }
            }
        }

        private void NewLoadFinished(List<CachedDogma.Attribute> results)
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

        private void OldLoadDogmaAttributeDatabase()
        {
            var methodName = "OldLoadDogmaAttributeDatabase";
            LogTrace(methodName);

            if (!Directory.Exists(Metatron.DataDirectory))
            {
                Directory.CreateDirectory(Metatron.DataDirectory);
            }

            if (File.Exists(_dogmaAttributeDbFilePath))
            {
                Metatron.FileManager.QueueDeserialize(_dogmaAttributeDbFilePath, _loadCallback);
            }
            else
            {
                _loadCallback(new List<CachedDogma.Attribute>());
            }
        }

        public void GetDogmaAttributeInfo(int attributeId)
        {
            var methodName = "GetDogmaAttributeInfo";
            LogTrace(methodName, "DogmaAttributeID: {0}", attributeId);

            if (_dogmaAttributesDoingGetInfo.Contains(attributeId) || CachedDogmaAttributesById.ContainsKey(attributeId))
                return;

            //Only have 3 concurrent threads.
            lock (this)
            {
                if (_dogmaAttributesDoingGetInfo.Count < 3)
                {
                    if (!_dogmaAttributesQueued.Contains(attributeId))
                    {
                        _dogmaAttributesDoingGetInfo.Add(attributeId);
                        ThreadPool.QueueUserWorkItem(TryGetDogmaAttributeInfo, new DogmaAttributeStateInfo(attributeId));
                        //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Trace,
                        //"GetCorpInfo", String.Format("Getting corp info for {0}. Running: {1}, Queued: {2}",
                        //corpID, _dogmaAttributesDoingGetInfo.Count, _dogmaAttributesQueued.Count)));
                    }
                }
                else
                {
                    _dogmaAttributesQueued.Add(attributeId);
                    //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Trace,
                    //"GetCorpInfo", String.Format("Queueing corp info for {0}. Running: {1}, Queued: {2}",
                    //corpID, _dogmaAttributesDoingGetInfo.Count, _dogmaAttributesQueued.Count)));
                }
            }
        }

        private void TryGetDogmaAttributeInfo(object stateInfo)
        {
            var methodName = "TryGetDogmaAttributeInfo";

            try
            {
                //If stateinfo is for some fucked up reason null, just return. Dont' fuck with anything, just return.
                //Same for Logging
                if (stateInfo == null || Metatron.Logging == null)
                {
                    return;
                }

                var stateObject = stateInfo as DogmaAttributeStateInfo;
                if (stateObject == null)
                {
                    return;
                }

                var attributeId = stateObject.DogmaAttributeId;
                LogTrace(methodName, "DogmaAttributeID: {0}", attributeId);

                if (Metatron.DogmaAttributeCache.CachedDogmaAttributesById.ContainsKey(attributeId))
                {
                    //If we have queued requests, move one over because this one is done
                    lock (this)
                    {
                        if (_dogmaAttributesDoingGetInfo.Contains(attributeId))
                        {
                            _dogmaAttributesDoingGetInfo.Remove(attributeId);
                        }
                        if (_dogmaAttributesQueued.Count > 0)
                        {
                            _dogmaAttributesDoingGetInfo.Add(_dogmaAttributesQueued[0]);
                            ThreadPool.QueueUserWorkItem(TryGetDogmaAttributeInfo,
                                new DogmaAttributeStateInfo(_dogmaAttributesQueued[0]));
                            _dogmaAttributesQueued.RemoveAt(0);
                        }
                    }
                    return;
                }

                //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                //    "TryGetDogmaAttributeInfo", String.Format("Downloading information for dogmaAttribute {0}...",
                //    corpID)));

                var cDbWebRequest = (HttpWebRequest)WebRequest.Create(
                    String.Format("https://esi.evetech.net/latest/dogma/attributes/{0}/?datasource=tranquility", attributeId));
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
                            if (_dogmaAttributesDoingGetInfo.Contains(attributeId))
                            {
                                _dogmaAttributesDoingGetInfo.Remove(attributeId);
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

                    CachedDogma.Attribute tempCachedDogmaAttribute = new CachedDogma.Attribute();
                    ESIDogmaAttribute eSIDogmaAttribute = null;
                    try
                    {
                        eSIDogmaAttribute = JsonConvert.DeserializeObject<ESIDogmaAttribute>(json);
                    }
                    catch (Exception e)
                    {
                        LogMessage(methodName, LogSeverityTypes.Critical, $"Caught exception while parsing a DogmaAttribute API response: {e}");
                        return;
                    }
                    if (eSIDogmaAttribute != null)
                    {
                        tempCachedDogmaAttribute.AttributeId = attributeId;
                        tempCachedDogmaAttribute.UnitId = eSIDogmaAttribute.UnitId.HasValue ? eSIDogmaAttribute.UnitId.Value : 0;
                        tempCachedDogmaAttribute.IconId = eSIDogmaAttribute.IconId.HasValue ? eSIDogmaAttribute.IconId.Value : 0;
                        tempCachedDogmaAttribute.Name = eSIDogmaAttribute.Name;
                        tempCachedDogmaAttribute.LastUpdated = DateTime.Now;
                        tempCachedDogmaAttribute.DefaultValue = eSIDogmaAttribute.DefaultValue.HasValue ? eSIDogmaAttribute.DefaultValue.Value : 0;
                        tempCachedDogmaAttribute.Stackable = eSIDogmaAttribute.Stackable;
                        tempCachedDogmaAttribute.Description = eSIDogmaAttribute.Description;
                        tempCachedDogmaAttribute.DisplayName = eSIDogmaAttribute.DisplayName;
                        tempCachedDogmaAttribute.HighIsGood = eSIDogmaAttribute.HighIsGood;
                    }

                    // Perform any additional checks you need on the object here
                    
                    //Core.Metatron.Logging.LogMessage(Core.Metatron.DogmaAttributeDB, new LogEventArgs(LogSeverityTypes.Debug,
                    //    "TryGetDogmaAttributeInfo", String.Format("Got info: Name - {0}, Ticker - {1}, ID - {2}, AllianceID - {3}",
                    //    tempCachedDogma.Attribute.Name, tempCachedDogma.Attribute.Ticker, tempCachedDogma.Attribute.DogmaAttributeID,
                    //    tempCachedDogma.Attribute.MemberOfAlliance)));
                    lock (this)
                    {
                        if (!_cachedDogmaAttributes.Contains(tempCachedDogmaAttribute))
                        {
                            _cachedDogmaAttributes.Add(tempCachedDogmaAttribute);
                        }
                        if (!_cachedDogmaAttributesById.ContainsKey(tempCachedDogmaAttribute.AttributeId))
                        {
                            _cachedDogmaAttributesById.Add(tempCachedDogmaAttribute.AttributeId, tempCachedDogmaAttribute);
                        }

                        if (_dogmaAttributesDoingGetInfo.Contains(attributeId))
                        {
                            _dogmaAttributesDoingGetInfo.Remove(attributeId);
                        }
                        if (_dogmaAttributesQueued.Count > 0)
                        {
                            _dogmaAttributesDoingGetInfo.Add(_dogmaAttributesQueued[0]);
                            ThreadPool.QueueUserWorkItem(TryGetDogmaAttributeInfo,
                                new DogmaAttributeStateInfo(_dogmaAttributesQueued[0]));
                            _dogmaAttributesQueued.RemoveAt(0);
                        }
                        //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Trace,
                        //methodName, String.Format("Finishing for corp {0}. Running: {1}, Queued: {2}",
                        //corpID, _dogmaAttributesDoingGetInfo.Count, _dogmaAttributesQueued.Count)));
                    }
                }
            }
            catch (Exception e)
            {
                LogException(e, methodName, "Caght exception while updating DogmaAttributeCache:");
            }
        }

        public void RemoveDogmaAttribute(int attributeId)
        {
            var methodName = "RemoveDogmaAttribute";
            LogTrace(methodName, "attributeId: {0}", attributeId);

            var cachedDogmaAttribute = _cachedDogmaAttributes.FirstOrDefault(c => c.AttributeId == attributeId);

            if (cachedDogmaAttribute == null) return;

            _cachedDogmaAttributesById.Remove(attributeId);
            _cachedDogmaAttributes.Remove(cachedDogmaAttribute);
        }

        private class DogmaAttributeStateInfo
        {
            public readonly int DogmaAttributeId;

            public DogmaAttributeStateInfo(int dogmaAttributeId)
            {
                DogmaAttributeId = dogmaAttributeId;
            }
        }
    }
    // ReSharper restore ConvertToConstant.Local
}
