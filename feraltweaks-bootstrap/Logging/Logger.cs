using System;
using FeralTweaks.Logging.Impl;

namespace FeralTweaks.Logging {

    public abstract class Logger
    {
        public static string GlobalMessagePrefix;
        protected static ILoggerImplementationProvider provider = new SplitLoggerImpl();

        /// <summary>
        /// Creates a new logger instance
        /// </summary>
        /// <param name="name">Logger name</param>
        /// <returns>New Logger instance</returns>
        public static Logger GetLogger(string name)
        {
            return provider.CreateInstance(name);
        }

        /// <summary>
        /// Defines the global log level
        /// </summary>
        public static LogLevel GlobalLogLevel = LogLevel.INFO;

        /// <summary>
        /// Defines the global console log level
        /// </summary>
        public static LogLevel GlobalConsoleLogLevel = LogLevel.GLOBAL;


        /// <summary>
        /// Defines the log level
        /// </summary>
        public abstract LogLevel Level { get; set; }

        /// <summary>
        /// Prints a message to the log
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Log message</param>
        public abstract void Log(LogLevel level, string message);

        /// <summary>
        /// Prints a message to the log
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Log message</param>
        /// <param name="exception">Exception to log</param>
        public abstract void Log(LogLevel level, string message, Exception exception);

        /// <summary>
        /// Logs a new debug message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void Debug(string message) {
            Log(LogLevel.DEBUG, message);
        }

        /// <summary>
        /// Logs a new debug message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="exception">Exception to log</param>
        public void Debug(string message, Exception exception) {
            Log(LogLevel.DEBUG, message, exception);
        }

        /// <summary>
        /// Logs a new trace message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void Trace(string message) {
            Log(LogLevel.TRACE, message);
        }

        /// <summary>
        /// Logs a new trace message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="exception">Exception to log</param>
        public void Trace(string message, Exception exception) {
            Log(LogLevel.TRACE, message, exception);
        }

        /// <summary>
        /// Logs a new info message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void Info(string message) {
            Log(LogLevel.INFO, message);
        }

        /// <summary>
        /// Logs a new info message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="exception">Exception to log</param>
        public void Info(string message, Exception exception) {
            Log(LogLevel.INFO, message, exception);
        }

        /// <summary>
        /// Logs a new warning message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void Warn(string message) {
            Log(LogLevel.WARN, message);
        }

        /// <summary>
        /// Logs a new warning message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="exception">Exception to log</param>
        public void Warn(string message, Exception exception) {
            Log(LogLevel.WARN, message, exception);
        }

        /// <summary>
        /// Logs a new error message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void Error(string message) {
            Log(LogLevel.ERROR, message);
        }

        /// <summary>
        /// Logs a new error message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="exception">Exception to log</param>
        public void Error(string message, Exception exception) {
            Log(LogLevel.ERROR, message, exception);
        }

        /// <summary>
        /// Logs a new atal error message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void Fatal(string message) {
            Log(LogLevel.FATAL, message);
        }

        /// <summary>
        /// Logs a new fatal error message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="exception">Exception to log</param>
        public void Fatal(string message, Exception exception) {
            Log(LogLevel.FATAL, message, exception);
        }
    }

}