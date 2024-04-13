using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    internal sealed class Logging : ModuleBase, IDisposable, ILogging
    {
        public event EventHandler<LogEventArgs> MessageLogged;

        private readonly string _missionLogDirectory, _datedLogDirectory;

    	private readonly long _startFileTime = DateTime.Now.ToFileTime();

        private readonly AutoResetEvent _canDisposeResetEvent = new AutoResetEvent(true);

        private readonly Dictionary<LogFileTypes, LogFile> _logFilesByType;

        private bool _isDisposed;

        internal Logging()
        {
			IsEnabled = false;
			ModuleName = "Logging";

            ModuleManager.ModulesToDispose.Add(this);

            var logFileDirectory = Path.Combine(Metatron.Directory, "Logs");

            _datedLogDirectory = Path.Combine(logFileDirectory,
                String.Format("{0}-{1}-{2}", DateTime.Now.Year.ToString("0000"), DateTime.Now.Month.ToString("00"), DateTime.Now.Day.ToString("00")));
            TryCreateDirectory(_datedLogDirectory);

            _missionLogDirectory = Path.Combine(_datedLogDirectory, "Mission HTML");
            TryCreateDirectory(_missionLogDirectory);

            var sessionObj = LavishScriptAPI.LavishScript.Objects.GetObject("Session");
            var session = LavishScriptAPI.LavishScriptObject.IsNullOrInvalid(sessionObj) ? "Invalid Session" : sessionObj.GetValue<string>();

            _logFilesByType = new Dictionary<LogFileTypes, LogFile>
                                  {
                                      { LogFileTypes.Standard, new LogFile(LogFileTypes.Standard, _startFileTime, _datedLogDirectory, session)},
                                      { LogFileTypes.Critical, new LogFile(LogFileTypes.Critical, _startFileTime, _datedLogDirectory, session)},
                                      { LogFileTypes.Profiling, new LogFile(LogFileTypes.Profiling, _startFileTime, _datedLogDirectory, session)},
                                      { LogFileTypes.Trace, new LogFile(LogFileTypes.Trace, _startFileTime, _datedLogDirectory, session)}
                                  };

			MessageLogged += ProcessLogMessage;
        }

        #region IDisposable implementors
        private void Dispose(bool disposing)
        {
            if (_isDisposed) 
                return;

            _isDisposed = true;

            if (disposing)
            {
                GC.SuppressFinalize(this);

                MessageLogged -= ProcessLogMessage;
                _canDisposeResetEvent.WaitOne(5000);

                foreach (var logFile in _logFilesByType.Values)
                {
                    logFile.Dispose();
                }

                _logFilesByType.Clear();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        public override bool Initialize()
        {
            if (IsInitialized)
                return true;

            RenameLogFiles();

            IsInitialized = true;
            return true;
        }

        private void RenameLogFiles()
        {
            foreach (var logFile in _logFilesByType.Values)
            {
                logFile.NamePrefix = Metatron.MeCache.Name;
            }
        }

        private void TryCreateDirectory(string path)
        {
            if (Directory.Exists(path))
                return;

            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception e)
            {
                LogMessage("TryCreateDirectory", LogSeverityTypes.Critical, "Could not create directory \"{0}\": .", path, e.Message);
            }
        }

        public void LogException(string moduleName, Exception e, string methodName, string message, params object[] args)
        {
            LogMessage(moduleName, methodName, LogSeverityTypes.Standard, message, args);
            LogMessage(moduleName, methodName, LogSeverityTypes.Standard, "Message: {0}", e.Message);
            LogMessage(moduleName, methodName, LogSeverityTypes.Standard, "Stack Trace: {0}", e.StackTrace);
            LogMessage(moduleName, methodName, LogSeverityTypes.Standard, "Inner Exception: {0}", e.InnerException);
        }

    	public void LogMessage(object sender, string sendingMethod, LogSeverityTypes logSeverity, string messageFormat, params object[] messageParameters)
        {
            var senderText = sender is ModuleBase ? ((ModuleBase)sender).ModuleName : sender.ToString().Split('.').Last();
            var source = string.Format("{0}.{1}", senderText, sendingMethod);

            var eventArgs = new LogEventArgs(Metatron.IsDebug, source, logSeverity, messageFormat, messageParameters);

            if (MessageLogged != null)
                MessageLogged(senderText, eventArgs);
        }

		public void LogTrace(object sender, string sendingMethod, string messageFormat = null)
		{
			if (messageFormat == null)
				messageFormat = string.Empty;

			LogTrace(sender, sendingMethod, messageFormat, null);
		}

		public void LogTrace(object sender, string sendingMethod, string messageFormat, params object[] messageParameters)
		{
			var senderText = sender is ModuleBase ? ((ModuleBase)sender).ModuleName : sender.ToString().Split('.').Last();
			var source = string.Format("{0}.{1}", senderText, sendingMethod);

			var eventArgs = new LogEventArgs(Metatron.IsDebug, source, LogSeverityTypes.Trace, messageFormat, messageParameters);

			if (MessageLogged != null)
				MessageLogged(senderText, eventArgs);
		}

		private void ProcessLogMessage(object sender, LogEventArgs e)
		{
			if ((!Metatron.IsDebug) && e.Severity == LogSeverityTypes.Trace)
				return;

			_canDisposeResetEvent.Reset();

            LogFile logFile;
            switch (e.Severity)
            {
                case LogSeverityTypes.Critical:
                    logFile = _logFilesByType[LogFileTypes.Critical];
                    break;
                case LogSeverityTypes.Trace:
                    logFile = _logFilesByType[LogFileTypes.Trace];
                    break;
                case LogSeverityTypes.Profiling:
                    logFile = _logFilesByType[LogFileTypes.Profiling];
                    break;
                default:
                    logFile = _logFilesByType[LogFileTypes.Standard];
                    break;
            }

            logFile.WriteMessage(e.FormattedMessage);

		    _canDisposeResetEvent.Set();
		}

    	public void LogMission(string missionHtml, string expiration, string missionName)
        {
            var filePrefix = Convert.ToBase64String(Encoding.ASCII.GetBytes(expiration.ToCharArray()));
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                while (missionName.Contains(c))
                {
                    missionName = missionName.Remove(missionName.IndexOf(c), 1);
                }
            }
            while (missionName.Contains('.'))
            {
                missionName = missionName.Remove(missionName.IndexOf('.'), 1);
            }

            using (TextWriter textWriter = new StreamWriter(Path.Combine(_missionLogDirectory, string.Format("{0} {1}.html", filePrefix, missionName))))
            {
                textWriter.Write(missionHtml);
            }
        }
    }
}
