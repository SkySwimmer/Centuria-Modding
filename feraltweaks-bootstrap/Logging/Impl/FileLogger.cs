using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace FeralTweaks.Logging.Impl
{
    /// <summary>
    /// File logger implementation
    /// </summary>
    public class FileLoggerImpl : Logger, ILoggerImplementationProvider
    {
        private StreamWriter FileWriter;

        private static Dictionary<string, StreamWriter> writerMemory = new Dictionary<string, StreamWriter>();

        internal FileLoggerImpl()
        {
        }

        /// <summary>
        /// Creates a new file logger
        /// </summary>
        /// <param name="source">Logger source name</param>
        public FileLoggerImpl(string source)
        {
            if (source != null)
            {
                // Create log folder
                Directory.CreateDirectory("FeralTweaks/logs");

                // Create log file
                try
                {
                    lock (writerMemory)
                    {
                        if (writerMemory.ContainsKey(source))
                            FileWriter = writerMemory[source];
                        else
                        {
                            FileWriter = new StreamWriter("FeralTweaks/logs/" + source.ToLower() + ".log");
                            writerMemory[source] = FileWriter;
                        }
                    }
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

        /// <inheritdoc/>
        public Logger CreateInstance(string name)
        {
            return new FileLoggerImpl(name);
        }

        private LogLevel level = LogLevel.GLOBAL;

        /// <inheritdoc/>
        public override LogLevel Level { get => (level == LogLevel.GLOBAL_CONSOLE ? (Logger.GlobalConsoleLogLevel == LogLevel.GLOBAL ? Logger.GlobalLogLevel : Logger.GlobalConsoleLogLevel) : (level == LogLevel.GLOBAL ? Logger.GlobalLogLevel : level)); set => level = value; }


        /// <inheritdoc/>
        public override void Log(LogLevel level, string message)
        {
            if (FileWriter == null)
                return;
            if (Level != LogLevel.QUIET && Level >= level)
            {
                lock (FileWriter)
                {
                    string msg = "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToString("HH:mm:ss") + "] [" + level.ToString() + "] " + GlobalMessagePrefix + message;
                    FileWriter.WriteLine(msg);
                    FileWriter.Flush();
                }
            }
        }

        /// <inheritdoc/>
        public override void Log(LogLevel level, string message, Exception exception)
        {
            if (FileWriter == null)
                return;
            if (Level != LogLevel.QUIET && Level >= level)
            {
                string msg = "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToString("HH:mm:ss") + "] [" + level.ToString() + "] " + GlobalMessagePrefix + message;
                lock (FileWriter)
                {
                    FileWriter.WriteLine(msg);
                    FileWriter.WriteLine("Exception: " + exception.GetType().FullName + (exception.Message != null ? ": " + exception.Message : ""));
                    FileWriter.WriteLine(exception.StackTrace);
                    Exception e = exception.InnerException;
                    while (e != null)
                    {
                        FileWriter.WriteLine("Caused by: " + e.GetType().FullName + (e.Message != null ? ": " + e.Message : ""));
                        FileWriter.WriteLine(exception.StackTrace);
                        e = e.InnerException;
                    }
                    FileWriter.Flush();
                }
            }
        }
    }
}