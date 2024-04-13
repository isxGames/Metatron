using System;

namespace Metatron.Core.Interfaces
{
    public interface ILogging
    {
        void LogMessage(object sender, string sendingMethod, LogSeverityTypes logSeverity, string messageFormat, params object[] messageParameters);
        void LogTrace(object sender, string sendingMethod, string messageFormat = null);
        void LogTrace(object sender, string sendingMethod, string messageFormat, params object[] messageParameters);
        void LogMission(string missionHtml, string expiration, string missionName);
        event EventHandler<LogEventArgs> MessageLogged;
        void LogException(string moduleName, Exception e, string methodName, string message, params object[] args);
    }
}
