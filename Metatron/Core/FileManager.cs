using System.Collections.Generic;
using System.IO;
using System.Threading;
using ProtoBuf;
using Metatron.Core.Interfaces;
using System;

namespace Metatron.Core
{
    public sealed class FileManager : ModuleBase, IFileManager
    {
        private volatile Dictionary<string, Thread> _workerThreadsByRequestedFile = new Dictionary<string, Thread>();

        private int _cleanupAttempts = 5;

        public FileManager()
        {
            IsEnabled = false;
            ModuleName = "FileManager";
        }

    	public override bool OutOfFrameCleanup()
        {
			var methodName = "OutOfFrameCleanup";

            if (!IsCleanedUpOutOfFrame)
            {
                if (_workerThreadsByRequestedFile.Count == 0)
                {
                    IsCleanedUpOutOfFrame = true;
                }
                else if (_cleanupAttempts-- <= 0)
                {
                    lock (_workerThreadsByRequestedFile)
                    {
                        foreach (var path in _workerThreadsByRequestedFile.Keys)
                        {
							LogMessage(methodName, LogSeverityTypes.Standard, "Aborting serialization of file \"{0}\"...",
								path);
                            _workerThreadsByRequestedFile[path].Abort();
                        }
                        _workerThreadsByRequestedFile.Clear();
                    }
                    IsCleanedUpOutOfFrame = true;
                }
            }

            return IsCleanedUpOutOfFrame;
        }

        #region Deserialization / Loading
        public void QueueDeserialize<T>(string filePath, FileReadCallback<T> callback) where T : new()
        {
            var methodName = "QueueDeserialze";
            LogTrace(methodName, "File Path: {0}", filePath);

            //Build a new thread for our operation
            var thread = new Thread(DeserializeFile<T>) {Name = "FileManager Deserializer", IsBackground = true};

        	//Add the thread to the dict then start it
            var stateInfo = new FileReadStateObject<T>(filePath, callback);
            lock (_workerThreadsByRequestedFile)
            {
                _workerThreadsByRequestedFile.Add(filePath, thread);

                //foreach (var key in _workerThreadsByRequestedFile.Keys)
                //{
                //    LogTrace(methodName, "Dictionary has file path: {0}", key);
                //}

                //LogTrace(methodName, "Started thread for file path: {0}", filePath);
                _workerThreadsByRequestedFile[filePath].Start(stateInfo);
            }
        }

        private void DeserializeFile<T>(object stateObject) where T : new()
        {
			var methodName = "DeserializeFile";
            //Get the stateobject
            var fileStateObject = (FileReadStateObject<T>)stateObject;
			LogTrace(methodName, "State Object: {0}", fileStateObject.FilePath);

            var succeeded = false;
            var timeout = 10;
            var retval = new List<T>();

            while (!succeeded)
            {
                try
                {
                    if (File.Exists(fileStateObject.FilePath))
                    {
                        using (var fileStream = File.Open(fileStateObject.FilePath, FileMode.Open, FileAccess.Read))
                        {
                            retval = Serializer.Deserialize<List<T>>(fileStream);
                        }
                    }
					succeeded = true;
                }
                catch (FileNotFoundException e)
                {
					LogException(e, methodName, "Caught FileNotFoundException deserializing object.");
                    break;
                }
                catch (EndOfStreamException e)
                {
					LogException(e, methodName, "Caught EndOfStreamException deserializing state object.");
                    File.Delete(fileStateObject.FilePath);
                    break;
                }
                //Can occur when file is busy
                catch (IOException e)
                {
                    if (timeout-- <= 0)
                    {
                        LogException(e, methodName, "Caught IOException deserializing state object, giving up and returning an empty list.");
                        break;
                    }

                    LogException(e, methodName, "Caught IOException deserializing state object, will retry in 25ms.");
                	Thread.Sleep(25);
                }
                catch (Exception e)
                {
                    LogException(e, methodName, $"Caught uncaught exception deserializing state object.");
                    File.Delete(fileStateObject.FilePath);
                    break;
                }
            }

            if (retval == null)
                retval = new List<T>();

            if (fileStateObject.Callback != null)
            {
                fileStateObject.Callback(retval);
            }

            //RemoveBookmarkAndCacheEntry the thread
            lock (_workerThreadsByRequestedFile)
            {
                if (_workerThreadsByRequestedFile.ContainsKey(fileStateObject.FilePath))
                {
                    //LogTrace(methodName, "Removing thread for file path: {0}", fileStateObject.FilePath);
                    _workerThreadsByRequestedFile.Remove(fileStateObject.FilePath);
                }
            }
        }
        #endregion

        #region Serialization / Saving
        public void QueueOverwriteSerialize<T>(string filePath, T objectToWrite, FileWriteCallback callback) where T : new()
        {
            var methodName = "QueueOverwriteSerialize";
            LogTrace(methodName, "FilePath: {0}", filePath);

            var stateInfo = new FileWriteStateObject<T>(filePath, callback, objectToWrite);

            //Build a new thread...
            var thread = new Thread(OverwriteSerializeFile<T>)
                         	{
                         		Name = "FileManager Serializer",
                         		IsBackground = true
                         	};

        	//Add the thread to the dictionary and start it

            lock (_workerThreadsByRequestedFile)
            {
                _workerThreadsByRequestedFile.Add(filePath, thread);

                //foreach (var key in _workerThreadsByRequestedFile.Keys)
                //{
                //    LogTrace(methodName, "Dictionary has file path: {0}", key);
                //}

                //LogTrace(methodName, "Started thread for file path: {0}", filePath);
                _workerThreadsByRequestedFile[filePath].Start(stateInfo);
            }
        }

        private void OverwriteSerializeFile<T>(object stateObject) where T : new()
        {
			var methodName = "OverwriteSerializeFile";

            //Get the stateinfo
            var fileStateObject = (FileWriteStateObject<T>)stateObject;

			LogTrace(methodName, "StateObject: {0}", fileStateObject.FilePath);

            //Loop a few times if necessary
            var succeeded = false;
            var timeout = 10;

            while (!succeeded)
            {
                try
                {
                    using (var fileStream = File.Open(fileStateObject.FilePath, FileMode.Create))
                    {
                        Serializer.Serialize(fileStream, fileStateObject.ObjectToWrite);
                        succeeded = true;
                    }
                }
                //File was busy.
                catch (IOException e)
                {
                	LogException(e, methodName, "Caught IOException serializing state object.");
					Echo(string.Format("Caught IOException serializing state object. Message:\n{0}\nStack Trace:\n{1}",
						e.Message, e.StackTrace));
                    if (timeout-- <= 0)
                    {
                        break;
                    }

                	Thread.Sleep(25);
                }
            }

            if (fileStateObject.Callback != null)
            {
                fileStateObject.Callback();
            }

            lock (_workerThreadsByRequestedFile)
            {
                if (_workerThreadsByRequestedFile.ContainsKey(fileStateObject.FilePath))
                {
                    //LogTrace(methodName, "Removing thread for file path: {0}", fileStateObject.FilePath);
                    _workerThreadsByRequestedFile.Remove(fileStateObject.FilePath);
                }
            }
        }
        #endregion
    }
}
