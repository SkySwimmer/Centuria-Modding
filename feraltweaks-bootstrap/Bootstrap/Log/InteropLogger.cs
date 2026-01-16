using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using FeralTweaks.Logging;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace FeralTweaksBootstrap
{
    internal class InteropLogger : ILogger
    {
        private static Logger logger;

        public static Logger Logger
        {
            get
            {
                return logger;
            }
        }

        public static void LogInfo(string message)
        {
            logger.Info(message);
        }

        public static void LogWarn(string message)
        {
            logger.Warn(message);
        }

        public static void LogError(string message)
        {
            logger.Error(message);
        }

        static InteropLogger()
        {
            // Set up log
            logger = Logger.GetLogger("Interop");
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new DummyDisposable();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel <= LogLevel.Information;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string msg = state.ToString();
            if (msg == null)
                return;
            if (exception != null)
            {
                // Add to message
                msg += "\nException: " + exception + ":\n" + exception.StackTrace;
            }
            switch (logLevel)
            {
                case LogLevel.Information:
                    LogInfo(msg);
                    break;
                case LogLevel.Warning:
                    LogWarn(msg);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    LogError(msg);
                    break;
            }
        }
    }

}