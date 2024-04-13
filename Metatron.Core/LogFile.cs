using System;
using System.IO;

namespace Metatron.Core
{
    public class LogFile : IDisposable
    {
        public LogFileTypes LogFileType;
        public long Timestamp;
        public string Directory;

        private string _namePrefix;
        public string NamePrefix
        {
            get { return _namePrefix; }
            set
            {
                var fileMode = FileMode.Create;

                if (_namePrefix != null)
                {
                    var sourceFileName = GetFormattedFileName(_namePrefix);
                    var destinationFileName = GetFormattedFileName(value);
                    _fileStream.Close();
                    File.Move(sourceFileName, destinationFileName);
                    fileMode = FileMode.Append;
                }

                _namePrefix = value;

                var filePath = GetFormattedFileName(_namePrefix);
                var stream = File.Open(filePath, fileMode, FileAccess.Write, FileShare.ReadWrite);
                _fileStream = new StreamWriter(stream) { AutoFlush = true };
            }
        }

        private StreamWriter _fileStream;

        private bool _isDisposed;

        public LogFile(LogFileTypes logFileType, long timestamp, string directory, string namePrefix)
        {
            LogFileType = logFileType;
            Timestamp = timestamp;
            Directory = directory;
            NamePrefix = namePrefix;
        }

        ~LogFile()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;

            _isDisposed = true;

            if (isDisposing)
            {
                GC.SuppressFinalize(this);
            }

            _fileStream.Flush();
            _fileStream.Close();
            _fileStream.Dispose();
        }

        public void WriteMessage(string formattedMessage)
        {
            _fileStream.WriteLine(formattedMessage);
        }

        private string GetFormattedFileName(string namePrefix)
        {
            var format = Path.Combine(Directory, String.Format("{0} {1} {2}.txt", namePrefix, Timestamp, LogFileType));
            return format;
        }
    }

    public enum LogFileTypes
    {
        Standard,
        Critical,
        Profiling,
        Trace
    }
}
