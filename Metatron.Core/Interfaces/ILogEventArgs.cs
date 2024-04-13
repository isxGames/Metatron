namespace Metatron.Core.Interfaces
{
    public interface ILogEventArgs
    {
        LogSeverityTypes Severity { get; }
        string Message { get; }
        string Sender { get; }
        string FormattedMessage { get; }
    }

    public enum LogSeverityTypes
    {
        /// <summary>
        /// Log and print to screen only if the DEBUG symbol is built
        /// </summary>
        Debug,
        /// <summary>
        /// Log and print to screen
        /// </summary>
        Standard,
        /// <summary>
        /// Log to critical log and print to screen
        /// </summary>
        Critical,
        /// <summary>
        /// Used for tracing calls in ISXEVE and Metatron for debugging purposes
        /// </summary>
        Trace,
        /// <summary>
        /// Used for profiling performance.
        /// </summary>
        Profiling
    }
}
