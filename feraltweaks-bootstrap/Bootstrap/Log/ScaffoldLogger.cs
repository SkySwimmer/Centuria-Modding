using System;
using FeralTweaks.Logging;

namespace FeralTweaksBootstrap
{
    internal class ScaffoldLoggerProvider : ScaffoldSharp.Logging.ILoggerImplementationProvider
    {
        public ScaffoldSharp.Logging.Logger CreateInstance(string name)
        {
            return new ScaffoldLogger(name);
        }
    }

    internal class ScaffoldLogger : ScaffoldSharp.Logging.Logger
    {
        private Logger delegateLogger;

        internal ScaffoldLogger(string source)
        {
            delegateLogger = Logger.GetLogger(source);
        }

        public static void Bind()
        {
            provider = new ScaffoldLoggerProvider();
        }

        private LogLevel MapLevel(ScaffoldSharp.Logging.LogLevel level)
        {
            switch(level)
            {
                case ScaffoldSharp.Logging.LogLevel.DEBUG:
                    return LogLevel.DEBUG;
                case ScaffoldSharp.Logging.LogLevel.TRACE:
                    return LogLevel.TRACE;
                case ScaffoldSharp.Logging.LogLevel.INFO:
                    return LogLevel.INFO;
                case ScaffoldSharp.Logging.LogLevel.WARN:
                    return LogLevel.WARN;
                case ScaffoldSharp.Logging.LogLevel.ERROR:
                    return LogLevel.ERROR;
                case ScaffoldSharp.Logging.LogLevel.FATAL:
                    return LogLevel.FATAL;
                case ScaffoldSharp.Logging.LogLevel.QUIET:
                    return LogLevel.QUIET;
                case ScaffoldSharp.Logging.LogLevel.GLOBAL:
                    return LogLevel.GLOBAL;
                case ScaffoldSharp.Logging.LogLevel.GLOBAL_CONSOLE:
                    return LogLevel.GLOBAL_CONSOLE;
                default:
                    return LogLevel.DEBUG;
            }
        }

        private ScaffoldSharp.Logging.LogLevel MapLevel(LogLevel level)
        {
            switch(level)
            {
                case LogLevel.DEBUG:
                    return ScaffoldSharp.Logging.LogLevel.DEBUG;
                case LogLevel.TRACE:
                    return ScaffoldSharp.Logging.LogLevel.TRACE;
                case LogLevel.INFO:
                    return ScaffoldSharp.Logging.LogLevel.INFO;
                case LogLevel.WARN:
                    return ScaffoldSharp.Logging.LogLevel.WARN;
                case LogLevel.ERROR:
                    return ScaffoldSharp.Logging.LogLevel.ERROR;
                case LogLevel.FATAL:
                    return ScaffoldSharp.Logging.LogLevel.FATAL;
                case LogLevel.QUIET:
                    return ScaffoldSharp.Logging.LogLevel.QUIET;
                case LogLevel.GLOBAL:
                    return ScaffoldSharp.Logging.LogLevel.GLOBAL;
                case LogLevel.GLOBAL_CONSOLE:
                    return ScaffoldSharp.Logging.LogLevel.GLOBAL_CONSOLE;
                default:
                    return ScaffoldSharp.Logging.LogLevel.DEBUG;
            }
        }

        public override ScaffoldSharp.Logging.LogLevel Level { get => MapLevel(delegateLogger.Level); set => delegateLogger.Level = MapLevel(value); }

        public override void Log(ScaffoldSharp.Logging.LogLevel level, string message)
        {
            delegateLogger.Log(MapLevel(level), message);
        }

        public override void Log(ScaffoldSharp.Logging.LogLevel level, string message, Exception exception)
        {
            delegateLogger.Log(MapLevel(level), message, exception);
        }
    }
}