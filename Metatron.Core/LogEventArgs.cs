using System;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    public sealed class LogEventArgs : EventArgs, ILogEventArgs
    {
        public LogSeverityTypes Severity { get; private set; }
        public string Message { get; private set; }
        public string Sender { get; private set; }

        public string FormattedMessage { get; private set; }

        public bool IsDebug { get; private set; }

        public LogEventArgs(bool isDebug, LogSeverityTypes severity, string sender, string message)
        {
            Severity = severity;
            Sender = sender;
            Message = message;

            IsDebug = isDebug;

            FormatMessage();
        }

        public LogEventArgs(bool isDebug, string sender, LogSeverityTypes logSeverity, string messageFormat, params object[] messageParameters)
        {
            Sender = sender;
            Message = messageParameters != null ? string.Format(messageFormat, messageParameters) : messageFormat;
            Severity = logSeverity;

            IsDebug = isDebug;

            FormatMessage();
        }

        private void FormatMessage()
        {
            if (Severity != LogSeverityTypes.Trace)
            {
                FormattedMessage = String.Format("{0} | {1} | {2} | {3}",
                    DateTime.Now.ToString("hh':'mm':'ss tt"),
                    Severity.ToString().PadRight(8),
                    Sender.PadRight(40),
                    Message);
            }
            else if (IsDebug) //No point doing trace formatting if we're not a debug build; minor optimization
            {
                FormattedMessage = String.Format("{0} | {1} | {2}",
                    DateTime.Now.ToString("hh':'mm':'ss tt"),
                    Sender.PadRight(40),
                    String.Format("Params: {0}", Message));
            }
        }
    }
}
