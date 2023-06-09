using System;

namespace FeralTweaks.Logging.Impl
{
    public class SplitLoggerImpl : Logger, ILoggerImplementationProvider
    {
        protected static ILoggerImplementationProvider ImplProvider1 = new ConsoleLoggerImpl();
        protected static ILoggerImplementationProvider ImplProvider2 = new FileLoggerImpl(null);

        private Logger Logger1;
        private Logger Logger2;

        public SplitLoggerImpl() { }
        private SplitLoggerImpl(Logger logger1, Logger logger2) {
            Logger1 = logger1;
            Logger2 = logger2;
        }

        public Logger CreateInstance(string name)
        {
            return new SplitLoggerImpl(ImplProvider1.CreateInstance(name), ImplProvider2.CreateInstance(name));
        }

        private LogLevel level = LogLevel.GLOBAL;
        public override LogLevel Level { get => (level == LogLevel.GLOBAL ? Logger.GlobalLogLevel : level); set { level = value; Logger1.Level = value; Logger2.Level = value; } }

        public override void Log(LogLevel level, string message)
        {
            Logger1.Log(level, message);
            Logger2.Log(level, message);
        }

        public override void Log(LogLevel level, string message, Exception exception)
        {
            Logger1.Log(level, message, exception);
            Logger2.Log(level, message, exception);
        }
    }
}
