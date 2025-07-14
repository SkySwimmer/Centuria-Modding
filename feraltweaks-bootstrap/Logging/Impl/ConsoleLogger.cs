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
        public override LogLevel Level { get => (level == LogLevel.GLOBAL_CONSOLE ? (Logger.GlobalConsoleLogLevel == LogLevel.GLOBAL ? Logger.GlobalLogLevel : Logger.GlobalConsoleLogLevel) : (level == LogLevel.GLOBAL ? Logger.GlobalLogLevel : level)); set => level = value; }

        public override void Log(LogLevel level, string message)
        {
            if (Level != LogLevel.QUIET && Level >= level)
            {
                while (writing)
                    Thread.Sleep(1);
                writing = true;
                string pref = "[" + DateTime.Now.ToString("HH:mm:ss") + " " + (level.ToString().Length < 5 ? " " + level.ToString() : level.ToString()) + "] [" + Source + "]: ";
                if (!FeralTweaksBootstrap.Bootstrap.showConsole)
                {
                    if (level <= LogLevel.WARN)
                    {
                        Console.Error.WriteLine(pref + GlobalMessagePrefix + message);
                        if (Debugger.IsAttached)
                            System.Diagnostics.Debug.WriteLine(pref + GlobalMessagePrefix + message);
                    }
                    else
                    {
                        Console.WriteLine(pref + GlobalMessagePrefix + message);
                        if (Debugger.IsAttached)
                            System.Diagnostics.Debug.WriteLine(pref + GlobalMessagePrefix + message);
                    }
                    writing = false;
                    return;
                }
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
                if (Debugger.IsAttached)
                    System.Diagnostics.Debug.WriteLine(pref + GlobalMessagePrefix + message);
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
                if (!FeralTweaksBootstrap.Bootstrap.showConsole)
                {
                    if (level <= LogLevel.WARN)
                    {
                        Console.Error.WriteLine(pref + GlobalMessagePrefix + message);
                        Console.Error.WriteLine("Exception: " + exception.GetType().FullName + (exception.Message != null ? ": " + exception.Message : ""));
                        Console.Error.WriteLine(exception.StackTrace);
                        Exception e = exception.InnerException;
                        while (e != null)
                        {
                            Console.Error.WriteLine("Caused by: " + e.GetType().FullName + (e.Message != null ? ": " + e.Message : ""));
                            Console.Error.WriteLine(e.StackTrace);
                            e = e.InnerException;
                        }
                        if (Debugger.IsAttached)
                        {
                            System.Diagnostics.Debug.WriteLine(pref + GlobalMessagePrefix + message);
                            System.Diagnostics.Debug.WriteLine("Exception: " + exception.GetType().FullName + (exception.Message != null ? ": " + exception.Message : ""));
                            System.Diagnostics.Debug.WriteLine(exception.StackTrace);
                            Exception e2 = exception.InnerException;
                            while (e2 != null)
                            {
                                System.Diagnostics.Debug.WriteLine("Caused by: " + e2.GetType().FullName + (e2.Message != null ? ": " + e2.Message : ""));
                                System.Diagnostics.Debug.WriteLine(e2.StackTrace);
                                e2 = e2.InnerException;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine(pref + GlobalMessagePrefix + message);
                        Console.WriteLine("Exception: " + exception.GetType().FullName + (exception.Message != null ? ": " + exception.Message : ""));
                        Console.WriteLine(exception.StackTrace);
                        Exception e = exception.InnerException;
                        while (e != null)
                        {
                            Console.WriteLine("Caused by: " + e.GetType().FullName + (e.Message != null ? ": " + e.Message : ""));
                            Console.WriteLine(e.StackTrace);
                            e = e.InnerException;
                        }
                        if (Debugger.IsAttached)
                        {
                            System.Diagnostics.Debug.WriteLine(pref + GlobalMessagePrefix + message);
                            System.Diagnostics.Debug.WriteLine("Exception: " + exception.GetType().FullName + (exception.Message != null ? ": " + exception.Message : ""));
                            System.Diagnostics.Debug.WriteLine(exception.StackTrace);
                            Exception e2 = exception.InnerException;
                            while (e2 != null)
                            {
                                System.Diagnostics.Debug.WriteLine("Caused by: " + e2.GetType().FullName + (e2.Message != null ? ": " + e2.Message : ""));
                                System.Diagnostics.Debug.WriteLine(e2.StackTrace);
                                e2 = e2.InnerException;
                            }
                        }
                    }
                    writing = false;
                    return;
                }
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
                    Exception e = exception.InnerException;
                    while (e != null)
                    {
                        Console.Error.WriteLine("Caused by: " + e.GetType().FullName + (e.Message != null ? ": " + e.Message : ""));
                        Console.Error.WriteLine(e.StackTrace);
                        e = e.InnerException;
                    }
                }
                else
                {
                    // Normal log messages
                    Console.WriteLine(GlobalMessagePrefix + message);
                    Console.WriteLine("Exception: " + exception.GetType().FullName + (exception.Message != null ? ": " + exception.Message : ""));
                    Console.WriteLine(exception.StackTrace);
                    Exception e = exception.InnerException;
                    while (e != null)
                    {
                        Console.WriteLine("Caused by: " + e.GetType().FullName + (e.Message != null ? ": " + e.Message : ""));
                        Console.WriteLine(e.StackTrace);
                        e = e.InnerException;
                    }
                }
                Console.ResetColor();
                if (Debugger.IsAttached)
                {
                    System.Diagnostics.Debug.WriteLine(pref + GlobalMessagePrefix + message);
                    System.Diagnostics.Debug.WriteLine("Exception: " + exception.GetType().FullName + (exception.Message != null ? ": " + exception.Message : ""));
                    System.Diagnostics.Debug.WriteLine(exception.StackTrace);
                    Exception e2 = exception.InnerException;
                    while (e2 != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Caused by: " + e2.GetType().FullName + (e2.Message != null ? ": " + e2.Message : ""));
                        System.Diagnostics.Debug.WriteLine(e2.StackTrace);
                        e2 = e2.InnerException;
                    }
                }
                writing = false;
            }
        }
    }
}