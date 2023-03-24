using System;
using System.IO;
using System.Reflection;

namespace ftl_cli
{
    class Program
    {
        static void Main(string[] args)
        {
            // Add assemblies to assembly resolution
            AppDomain.CurrentDomain.AssemblyResolve += (s, args) =>
            {
                // Attempt to resolve
                AssemblyName nm = new AssemblyName(args.Name);

                // Find file
                if (File.Exists("FeralTweaks/" + nm.Name + ".dll"))
                    return Assembly.LoadFile(Path.GetFullPath("FeralTweaks/" + nm.Name + ".dll"));

                // Not found
                return null;
            };

            // Run FTL
            Run();
        }

        private static void Run()
        {
            Console.WriteLine("FeralTweaksLoader Command Line Wrapper");
            Console.WriteLine("Copyright(c) AerialWorks Technologies, licensed GPL-2.0");
            Console.WriteLine("Use `ftl --help` for a list of arguments.");
            Console.WriteLine();
            FeralTweaksBootstrap.Bootstrap.Start();
            FeralTweaksBootstrap.Bootstrap.LogInfo("FTL exited, running through the CLI wrapper, not starting the game!");
        }
    }
}
