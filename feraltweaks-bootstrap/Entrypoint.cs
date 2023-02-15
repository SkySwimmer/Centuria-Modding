using System;
using System.IO;

namespace Doorstop
{
    public static class Entrypoint
    {
        public static void Start()
        {
            try
            {
                FeralTweaksBootstrap.Bootstrap.Start();
            }
            catch (Exception e)
            {
                File.WriteAllText("exceptionlog.log", "Uncaught exception: " + e);
                Environment.Exit(1);
            }
        }
    }
}
