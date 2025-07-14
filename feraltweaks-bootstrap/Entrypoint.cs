using System;
using System.Diagnostics;
using System.IO;

namespace Doorstop
{
    public static class Entrypoint
    {
        public static void Start()
        {
            try
            {
                FeralTweaksBootstrap.Bootstrap.EnableExceptionCatcher = true;
                FeralTweaksBootstrap.Bootstrap.Start();
            }
            catch (Exception e)
            {
                if (!FeralTweaksBootstrap.Bootstrap.FatalExceptionLogged)
                {
                    FeralTweaksBootstrap.Bootstrap.FatalExceptionLogged = true;
                    Directory.CreateDirectory("FeralTweaks");
                    File.WriteAllText("FeralTweaks/exceptionlog.log", "Uncaught exception: " + e);
                    FeralTweaks.Logging.Logger.GetLogger("Preloader").Fatal("Uncaught exception!", e);
                }
                if (Debugger.IsAttached)
                    throw;
                Environment.Exit(1);
            }
        }
    }
}
