using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace FeralTweaksBootstrap
{
    internal class InteropLogger : ILogger
    {
        private static StreamWriter LogWriter;

        public static void LogInfo(string message)
        {
            LogWriter.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [INF] " + message);
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [INF] [Interop] " + message);
        }

        public static void LogWarn(string message)
        {
            LogWriter.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [WRN] " + message);
            Console.Error.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [WRN] [Interop] " + message);
        }

        public static void LogError(string message)
        {
            LogWriter.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [ERR] " + message);
            Console.Error.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [ERR] [Interop] " + message);
        }

        static InteropLogger()
        {
            // Set up log
            LogWriter = new StreamWriter("FeralTweaks/logs/interop.log");
            LogWriter.AutoFlush = true;
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