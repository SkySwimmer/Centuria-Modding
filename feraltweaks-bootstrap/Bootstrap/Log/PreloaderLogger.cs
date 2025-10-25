using System;
using Microsoft.Extensions.Logging;

namespace FeralTweaksBootstrap
{
    internal class PreloaderLogger : ILogger
    {
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
                    Bootstrap.LogInfo(msg);
                    break;
                case LogLevel.Warning:
                    Bootstrap.LogWarn(msg);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Bootstrap.LogError(msg);
                    break;
            }
        }
    }
}
