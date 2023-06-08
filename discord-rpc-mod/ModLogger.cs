using System;
using DiscordRPC.Logging;
using FeralTweaksBootstrap;

namespace FeralDiscordRpcMod
{
    internal class ModLogger : ILogger
    {
        public RpcMod mod;
        public LogLevel Level { get => Bootstrap.DebugLogging ? LogLevel.Trace : LogLevel.Info ; set  { } }

        public void Error(string message, params object[] args)
        {
            message = appendTo(message, args);
            mod.LogError(message);
        }

        private string appendTo(string message, object[] args)
        {
            for (int i = 0; i < args.Length; i++)
                message = message.Replace("{" + i + "}", args[i] == null ? "null" : args[i].ToString());
            return message;
        }

        public void Info(string message, params object[] args)
        {
            message = appendTo(message, args);
            mod.LogInfo(message);
        }

        public void Trace(string message, params object[] args)
        {
            message = appendTo(message, args);
            mod.LogDebug(message);
        }

        public void Warning(string message, params object[] args)
        {
            message = appendTo(message, args);
            mod.LogWarn(message);
        }
    }
}