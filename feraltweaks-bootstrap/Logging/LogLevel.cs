namespace FeralTweaks.Logging {
    public enum LogLevel
    {

        /// <summary>
        /// No messages
        /// </summary>
        QUIET,

        /// <summary>
        /// Fatal error log messages
        /// </summary>
        FATAL,

        /// <summary>
        /// Error log messages
        /// </summary>
        ERROR,

        /// <summary>
        /// Warning log messages
        /// </summary>
        WARN,

        /// <summary>
        /// Info log messages
        /// </summary>
        INFO,

        /// <summary>
        /// Trace log messages
        /// </summary>
        TRACE,

        /// <summary>
        /// Debug log messages
        /// </summary>
        DEBUG,

        /// <summary>
        /// Use global logging
        /// </summary>
        GLOBAL,

        /// <summary>
        /// Use global console logging
        /// </summary>
        GLOBAL_CONSOLE
    }

}