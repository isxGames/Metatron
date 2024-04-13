using Metatron.Core.Interfaces;
using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Metatron.Core
{
    internal sealed class EntityCache : ModuleBase, IEntityCache
    {
        //SQL connection for access to the DB file
        private readonly SQLiteConnection _sqLiteConnection;

        private static readonly string FileName = "EntityCache.bin";
        //SqlFileName = "Entities.db";

        //File paths and connection strings
        private string _sqlDbFilePath = string.Empty;
        //_connectionString = string.Empty;

        //Callbacks
        private FileReadCallback<CachedEntity> _loadCallback;
        //Temporary list<CachedCorpration> for creating the database
        List<CachedEntity> _oldFileDbContents = new List<CachedEntity>();

        private volatile List<CachedEntity> _cachedEntities = new List<CachedEntity>();
        public ReadOnlyCollection<CachedEntity> CachedEntities
        {
            get { return _cachedEntities.AsReadOnly(); }
        }

        private volatile Dictionary<Int64, CachedEntity> _cachedEntitiesById = new Dictionary<long, CachedEntity>();
        public Dictionary<Int64, CachedEntity> CachedEntitiesById
        {
            get { return _cachedEntitiesById; }
        }

        private volatile List<Int64> _entitiesDoingGetInfo = new List<Int64>();
        private volatile List<Int64> _entitiesQueued = new List<Int64>();

        private readonly string _entityDbFilePath = string.Empty;

        public EntityCache()
        {
            //_sqLiteConnection = sqLiteConnection;
            IsEnabled = false;
            ModuleName = "EntityCache";

            _entityDbFilePath = Path.Combine(Metatron.DataDirectory, FileName);

            //_sqlDbFilePath = string.Format("{0}\\{1}", Metatron.DataDirectory, SqlFileName);
            //_connectionString = string.Format("Data Source={0};Version=3", _sqlDbFilePath);

            _loadCallback = LoadComplete;
        }

        private void LoadComplete(List<CachedEntity> results)
        {
            var methodName = "LoadComplete";
            LogTrace(methodName);

            lock (CachedEntities)
            {
                _cachedEntities = results;
                foreach (var cachedEntity in
                    _cachedEntities.Where(cachedEntity => !CachedEntitiesById.ContainsKey(cachedEntity.TypeID)))
                {
                    _cachedEntitiesById.Add(cachedEntity.TypeID, cachedEntity);
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
                    OldLoadEntityDatabase();
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
                    SaveEntityDatabase();
                    _isCleaningUp = true;
                }
            }

            return IsCleanedUpOutOfFrame;
        }

        private void SaveEntityDatabase()
        {
            var methodName = "_saveEntityDB";
            LogTrace(methodName);

            _entitiesQueued.Clear();
            _entitiesDoingGetInfo.Clear();

            //Write our database
            var succeeded = false;
            var timeout = 5;

            while (!succeeded && timeout-- > 0)
            {
                try
                {
                    using (var fileStream = File.Open(_entityDbFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        //Get a list from disk to update
                        var diskEntities = Serializer.Deserialize<List<CachedEntity>>(fileStream) ??
                                               new List<CachedEntity>();

                        //Handle null list from deserialization

                        lock (CachedEntities)
                        {
                            foreach (var localEntity in CachedEntities)
                            {
                                var matchFound = false;
                                //Find a match in disk entities
                                for (var index = 0; index < diskEntities.Count; index++)
                                {
                                    var diskEntity = diskEntities[index];

                                    if (localEntity.TypeID != diskEntity.TypeID)
                                        continue;

                                    matchFound = true;
                                    //Update if necessary
                                    if (localEntity.LastUpdated.CompareTo(diskEntity.LastUpdated) >= 0)
                                    {
                                        diskEntities[index] = localEntity;
                                    }
                                    break;
                                }
                                if (!matchFound)
                                {
                                    diskEntities.Add(localEntity);
                                }
                            }
                        }

                        //Clear the file
                        fileStream.Seek(0, SeekOrigin.Begin);
                        fileStream.SetLength(0);

                        //Save the updated database back to disk
                        //fileStream.Seek(0, SeekOrigin.Begin);
                        Serializer.Serialize(fileStream, diskEntities);
                    }
                }
                catch (IOException e)
                {
                    LogException(e, methodName, "Caught exception while cleaning up EntityCache:");
                    Thread.Sleep(50);
                }
                succeeded = true;
            }
            IsCleanedUpOutOfFrame = true;
        }

        private void LoadEntityDatabase()
        {
            var methodName = "LoadEntityDatabase";
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

            // Get an SQL command
            using (var sqLiteCommand = _sqLiteConnection.CreateCommand())
            {
                // Check if the 'entities' table exists
                sqLiteCommand.CommandText = "SELECT name FROM sqlite_master WHERE name = 'entities';";
                var entitiesTableExists = sqLiteCommand.ExecuteReader().Read();

                // If the 'entities' table doesn't exist, create it
                if (!entitiesTableExists)
                {
                    sqLiteCommand.CommandText = String.Concat(
                        "CREATE TABLE entities (",
                        "id INTEGER PRIMARY KEY AUTOINCREMENT, ",
                        "typeID INTEGER, ",
                        "name VARCHAR(40), ",
                        "description TEXT, ",
                        "mass REAL, ",
                        "radius REAL",
                        ");"
                    );
                    sqLiteCommand.ExecuteNonQuery();
                }

                // Check if the 'dogma_attributes' table exists
                sqLiteCommand.CommandText = "SELECT name FROM sqlite_master WHERE name = 'dogma_attributes';";
                var dogmaAttributesTableExists = sqLiteCommand.ExecuteReader().Read();

                // If the 'dogma_attributes' table doesn't exist, create it
                if (!dogmaAttributesTableExists)
                {
                    sqLiteCommand.CommandText = String.Concat(
                        "CREATE TABLE dogma_attributes (",
                        "id INTEGER PRIMARY KEY AUTOINCREMENT, ",
                        "entity_id INTEGER, ",
                        "attribute_id INTEGER, ",
                        "value REAL, ",
                        "FOREIGN KEY (entity_id) REFERENCES entities (id)",
                        ");"
                    );
                    sqLiteCommand.ExecuteNonQuery();
                }


                PopulateDatabaseFromFile();

            }
        }

        private void PopulateDatabaseFromFile()
        {
            // If the old corp DB file exists...
            if (!File.Exists(_entityDbFilePath))
                return;

            // Deserialize a list of stuff
            _loadCallback = new FileReadCallback<CachedEntity>(NewLoadFinished);
            Metatron.FileManager.QueueDeserialize(_entityDbFilePath, _loadCallback);

            // Use a command
            using (var sqLiteCommand = _sqLiteConnection.CreateCommand())
            {
                // Loop all cached entities
                foreach (var cachedEntity in CachedEntities)
                {
                    // Insert into the 'entities' table
                    sqLiteCommand.CommandText = String.Format(
                        "INSERT INTO entities ('typeID', 'name', 'description', 'mass', 'radius') VALUES ({0}, '{1}', '{2}', {3}, {4});",
                        cachedEntity.TypeID, cachedEntity.Name, cachedEntity.Description, cachedEntity.Mass, cachedEntity.Radius
                    );
                    sqLiteCommand.ExecuteNonQuery();

                    // Get the last inserted id
                    sqLiteCommand.CommandText = "SELECT last_insert_rowid();";
                    long lastEntityId = (long)sqLiteCommand.ExecuteScalar();

                    // Insert the Dogma attributes for this entity into the 'dogma_attributes' table
                    foreach (var attribute in cachedEntity.DogmaAttributes)
                    {
                        sqLiteCommand.CommandText = String.Format(
                            "INSERT INTO dogma_attributes ('entity_id', 'attribute_id', 'value') VALUES ({0}, {1}, {2});",
                            lastEntityId, attribute.Id, attribute.Value
                        );
                        sqLiteCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private void NewLoadFinished(List<CachedEntity> results)
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

        private void OldLoadEntityDatabase()
        {
            var methodName = "OldLoadEntityDatabase";
            LogTrace(methodName);

            if (!Directory.Exists(Metatron.DataDirectory))
            {
                Directory.CreateDirectory(Metatron.DataDirectory);
            }

            if (File.Exists(_entityDbFilePath))
            {
                Metatron.FileManager.QueueDeserialize(_entityDbFilePath, _loadCallback);
            }
            else
            {
                _loadCallback(new List<CachedEntity>());
            }
        }

        public void GetEntityInfo(Int64 typeId)
        {
            var methodName = "GetEntityInfo";
            LogTrace(methodName, "EntityID: {0}", typeId);

            if (_entitiesDoingGetInfo.Contains(typeId) || CachedEntitiesById.ContainsKey(typeId))
                return;

            //Only have 3 concurrent threads.
            lock (this)
            {
                if (_entitiesDoingGetInfo.Count < 3)
                {
                    if (!_entitiesQueued.Contains(typeId))
                    {
                        _entitiesDoingGetInfo.Add(typeId);
                        ThreadPool.QueueUserWorkItem(TryGetEntityInfo, new EntityStateInfo(typeId));
                        //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Trace,
                        //"GetCorpInfo", String.Format("Getting corp info for {0}. Running: {1}, Queued: {2}",
                        //corpID, _entitiesDoingGetInfo.Count, _entitiesQueued.Count)));
                    }
                }
                else
                {
                    _entitiesQueued.Add(typeId);
                    //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Trace,
                    //"GetCorpInfo", String.Format("Queueing corp info for {0}. Running: {1}, Queued: {2}",
                    //corpID, _entitiesDoingGetInfo.Count, _entitiesQueued.Count)));
                }
            }
        }

        private void TryGetEntityInfo(object stateInfo)
        {
            var methodName = "TryGetEntityInfo";

            try
            {
                //If stateinfo is for some fucked up reason null, just return. Dont' fuck with anything, just return.
                //Same for Logging
                if (stateInfo == null || Metatron.Logging == null)
                {
                    return;
                }

                var stateObject = stateInfo as EntityStateInfo;
                if (stateObject == null)
                {
                    return;
                }

                var typeId = stateObject.EntityId;
                LogTrace(methodName, "EntityID: {0}", typeId);

                if (Metatron.EntityCache.CachedEntitiesById.ContainsKey(typeId))
                {
                    //If we have queued requests, move one over because this one is done
                    lock (this)
                    {
                        if (_entitiesDoingGetInfo.Contains(typeId))
                        {
                            _entitiesDoingGetInfo.Remove(typeId);
                        }
                        if (_entitiesQueued.Count > 0)
                        {
                            _entitiesDoingGetInfo.Add(_entitiesQueued[0]);
                            ThreadPool.QueueUserWorkItem(TryGetEntityInfo,
                                new EntityStateInfo(_entitiesQueued[0]));
                            _entitiesQueued.RemoveAt(0);
                        }
                    }
                    return;
                }

                //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                //    "TryGetEntityInfo", String.Format("Downloading information for entity {0}...",
                //    corpID)));

                var cDbWebRequest = (HttpWebRequest)WebRequest.Create(
                    String.Format("https://esi.evetech.net/latest/universe/types/{0}/?datasource=tranquility", typeId));
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
                            if (_entitiesDoingGetInfo.Contains(typeId))
                            {
                                _entitiesDoingGetInfo.Remove(typeId);
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

                    CachedEntity tempCachedEntity = new CachedEntity();
                    ESIEntity eSIEntity = null;
                    try
                    {
                        eSIEntity = JsonConvert.DeserializeObject<ESIEntity>(json);
                    }
                    catch (Exception e)
                    {
                        LogMessage(methodName, LogSeverityTypes.Critical, $"Caught exception while parsing a Entity API response: {e}");
                        return;
                    }
                    if (eSIEntity != null)
                    {
                        tempCachedEntity.TypeID = typeId;
                        tempCachedEntity.Radius = eSIEntity.Radius;
                        tempCachedEntity.Mass = eSIEntity.Mass;
                        tempCachedEntity.Name = eSIEntity.Name;
                        tempCachedEntity.Description = eSIEntity.Description;
                        tempCachedEntity.GroupID = eSIEntity.GroupId;

                        // Populate DogmaAttributes with both AttributeId and Value
                        tempCachedEntity.DogmaAttributes = eSIEntity.DogmaAttributes.Select(a => new CachedEntity.DogmaAttribute
                        {
                            Id = a.AttributeId,
                            Value = a.Value
                        }).ToList();

                        tempCachedEntity.LastUpdated = DateTime.Now;
                    }
                    // Perform any additional checks you need on the object here

                    //Core.Metatron.Logging.LogMessage(Core.Metatron.EntityDB, new LogEventArgs(LogSeverityTypes.Debug,
                    //    "TryGetEntityInfo", String.Format("Got info: Name - {0}, Ticker - {1}, ID - {2}, AllianceID - {3}",
                    //    tempCachedEntity.Name, tempCachedEntity.Ticker, tempCachedEntity.EntityID,
                    //    tempCachedEntity.MemberOfAlliance)));
                    lock (this)
                    {
                        if (!_cachedEntities.Contains(tempCachedEntity))
                        {
                            _cachedEntities.Add(tempCachedEntity);
                        }
                        if (!_cachedEntitiesById.ContainsKey(tempCachedEntity.TypeID))
                        {
                            _cachedEntitiesById.Add(tempCachedEntity.TypeID, tempCachedEntity);
                        }

                        if (_entitiesDoingGetInfo.Contains(typeId))
                        {
                            _entitiesDoingGetInfo.Remove(typeId);
                        }
                        if (_entitiesQueued.Count > 0)
                        {
                            _entitiesDoingGetInfo.Add(_entitiesQueued[0]);
                            ThreadPool.QueueUserWorkItem(TryGetEntityInfo,
                                new EntityStateInfo(_entitiesQueued[0]));
                            _entitiesQueued.RemoveAt(0);
                        }
                        //Core.Metatron.Logging.LogMessage(Instance.ObjectName, new LogEventArgs(LogSeverityTypes.Trace,
                        //methodName, String.Format("Finishing for corp {0}. Running: {1}, Queued: {2}",
                        //corpID, _entitiesDoingGetInfo.Count, _entitiesQueued.Count)));
                    }
                }
            }
            catch (Exception e)
            {
                LogException(e, methodName, "Caght exception while updating EntityCache:");
            }
        }

        public void RemoveEntity(Int64 typeId)
        {
            var methodName = "RemoveEntity";
            LogTrace(methodName, "typeId: {0}", typeId);

            var cachedEntity = _cachedEntities.FirstOrDefault(c => c.TypeID == typeId);

            if (cachedEntity == null) return;

            _cachedEntitiesById.Remove(typeId);
            _cachedEntities.Remove(cachedEntity);
        }

        private class EntityStateInfo
        {
            public readonly Int64 EntityId;

            public EntityStateInfo(Int64 entityId)
            {
                EntityId = entityId;
            }
        }

    }
}
