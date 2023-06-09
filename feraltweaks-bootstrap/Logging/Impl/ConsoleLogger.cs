using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace FeralTweaks.Logging.Impl
{
    public class ConsoleLoggerImpl : Logger, ILoggerImplementationProvider
    {
        private string Source;
        public ConsoleLoggerImpl() { }
        public ConsoleLoggerImpl(string source)
        {
            Source = source;
        }

        public Logger CreateInstance(string name)
        {
            return new ConsoleLoggerImpl(name);
        }

        private static bool writing = false;
        private LogLevel level = LogLevel.GLOBAL;
        public override LogLevel Level { get => (level == LogLevel.GLOBAL_CONSOLE ? (Logger.GlobalConsoleLogLevel == LogLevel.GLOBAL ? Logger.GlobalLogLevel : Logger.GlobalConsoleLogLevel) : level); set => level = value; }

        public override void Log(LogLevel level, string message)
        {
            if (Level != LogLevel.QUIET && Level >= level)
            {
                while (writing)
                    Thread.Sleep(1);
                writing = true;
                string pref = "[" + DateTime.Now.ToString("HH:mm:ss") + " " + (level.ToString().Length < 5 ? " " + level.ToString() : level.ToString()) + "] [" + Source + "]: ";
                if (level > LogLevel.WARN)
                    Console.Write(pref);
                else
                    Console.Error.Write(pref);
                if (level == LogLevel.INFO)
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                else if (level >= LogLevel.TRACE)
                    Console.ForegroundColor = ConsoleColor.Blue;
                else if (level <= LogLevel.ERROR)
                    Console.ForegroundColor = ConsoleColor.Red;
                else if (level == LogLevel.WARN)
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                if (level <= LogLevel.WARN)
                {
                    // Error log messages
                    Console.Error.WriteLine(GlobalMessagePrefix + message);
                }
                else
                {
                    // Normal log messages
                    Console.WriteLine(GlobalMessagePrefix + message);
                }
                Console.ResetColor();
                writing = false;
            }
        }

        public override void Log(LogLevel level, string message, Exception exception)
        {
            if (Level != LogLevel.QUIET && Level >= level)
            {
                while (writing)
                    Thread.Sleep(1);
                writing = true;
                string pref = "[" + DateTime.Now.ToString("HH:mm:ss") + " " + (level.ToString().Length < 5 ? " " + level.ToString() : level.ToString()) + "] [" + Source + "]: ";
                if (level > LogLevel.WARN)
                    Console.Write(pref);
                else
                    Console.Error.Write(pref);
                if (level == LogLevel.INFO)
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                else if (level >= LogLevel.TRACE)
                    Console.ForegroundColor = ConsoleColor.Blue;
                else if (level <= LogLevel.ERROR)
                    Console.ForegroundColor = ConsoleColor.Red;
                else if (level == LogLevel.WARN)
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                if (level <= LogLevel.WARN)
                {
                    // Error log messages
                    Console.Error.WriteLine(GlobalMessagePrefix + message);
                    Console.Error.WriteLine("Exception: " + exception.GetType().FullName + (exception.Message != null ? ": " + exception.Message : ""));
                    Console.Error.WriteLine(exception.StackTrace);
                }
                else
                {
                    // Normal log messages
                    Console.WriteLine(GlobalMessagePrefix + message);
                    Console.WriteLine("Exception: " + exception.GetType().FullName + (exception.Message != null ? ": " + exception.Message : ""));
                    Exception e = exception.InnerException;
                    while (e != null)
                    {
                        Console.WriteLine("Caused by: " + exception.GetType().FullName + (e.Message != null ? ": " + e.Message : ""));
                        e = e.InnerException;
                    }
                    Console.WriteLine(exception.StackTrace);
                }
                Console.ResetColor();
                writing = false;
            }
        }
    }
}