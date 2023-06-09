using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace FeralTweaks.Logging.Impl
{
    public class FileLoggerImpl : Logger, ILoggerImplementationProvider
    {
        private string Source;
        private StreamWriter FileWriter;

        public FileLoggerImpl(string source) {
            Source = source;

            if (source != null)
            {
                // Create log folder
                Directory.CreateDirectory("FeralTweaks/logs");

                // Create log file
                try
                {
                    FileWriter = new StreamWriter("FeralTweaks/logs/" + source.ToLower() + ".log");
                }
                catch
                {
                    if (FeralTweaksBootstrap.Bootstrap.loaderReady && FeralTweaks.FeralTweaksLoader.Logger != null)
                        FeralTweaks.FeralTweaksLoader.LogWarn("Warning! Unable to open log file FeralTweaks/logs/" + source + ".log! Not writing to a log file!");
                    else
                        FeralTweaksBootstrap.Bootstrap.LogWarn("Warning! Unable to open log file FeralTweaks/logs/" + source + ".log! Not writing to a log file!");
                }
            }
        }

        public Logger CreateInstance(string name)
        {
            return new FileLoggerImpl(name);
        }

        private LogLevel level = LogLevel.GLOBAL;
        public override LogLevel Level { get => (level == LogLevel.GLOBAL_CONSOLE ? (Logger.GlobalConsoleLogLevel == LogLevel.GLOBAL ? Logger.GlobalLogLevel : Logger.GlobalConsoleLogLevel) : (level == LogLevel.GLOBAL ? Logger.GlobalLogLevel : level)); set => level = value; }

        public override void Log(LogLevel level, string message)
        {
            if (FileWriter == null)
                return;
            if (Level != LogLevel.QUIET && Level >= level) {
                lock(FileWriter)
                {
                    string msg = "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToString("HH:mm:ss") + "] [" + level.ToString() + "] " + GlobalMessagePrefix + message;
                    FileWriter.WriteLine(msg);
                    FileWriter.Flush();
                }
            }
        }

        public override void Log(LogLevel level, string message, Exception exception)
        {
            if (FileWriter == null)
                return;
            if (Level != LogLevel.QUIET && Level >= level) {
                string msg = "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToString("HH:mm:ss") + "] [" + level.ToString() + "] " + GlobalMessagePrefix + message;
                FileWriter.WriteLine(msg);
                FileWriter.WriteLine("Exception: " + exception.GetType().FullName + (exception.Message != null ? ": " + exception.Message : ""));
                Exception e = exception.InnerException;
                while (e != null)
                {
                    FileWriter.WriteLine("Caused by: " + exception.GetType().FullName + (e.Message != null ? ": " + e.Message : ""));
                    e = e.InnerException;
                }
                FileWriter.WriteLine(exception.StackTrace);
                FileWriter.Flush();
            }
        }
    }
}
