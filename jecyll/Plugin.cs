using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.IL2CPP.Logging;
using BepInEx.Logging;
using FeralTweaks.Mods;
using FeralTweaksBootstrap;
using HarmonyLib;

namespace jecyll
{
    public class Plugin : FeralTweaksMod
    {
        public override string ID => "jecyll";

        public override string Version => "1.0.0.A1";

        public override void Init()
        {
            // Load BepInEx DLL
            Assembly bepInDll = Assembly.LoadFile(Path.GetFullPath("BepInEx/core/BepInEx.IL2CPP.dll"));

            // Find preloader class
            Type entry = bepInDll.GetType("BepInEx.IL2CPP.UnityPreloaderRunner");
            MethodInfo main = entry.GetMethod("PreloaderMain", new Type[] { typeof(string[]) });

            // Prepare cli args
            string[] cmdline = Environment.GetCommandLineArgs();
            string[] args = new string[cmdline.Length - 1];
            for (int i = 1; i < cmdline.Length; i++)
                args[i - 1] = cmdline[i];

            // Add bepinex to assembly resolution
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                AssemblyName nm = new AssemblyName(args.Name);
                if (File.Exists("BepInEx/core/" + nm.Name + ".dll"))
                    return Assembly.LoadFile(Path.GetFullPath("BepInEx/core/" + nm.Name + ".dll"));
                return null;
            };

            // Simulate a BepInEx doorstop environment
            Environment.SetEnvironmentVariable("DOORSTOP_INVOKE_DLL_PATH", Path.GetFullPath("BepInEx/core/BepInEx.IL2CPP.dll"));

            // Load envvars
            Assembly bepInPreloaderDll = Assembly.LoadFile(Path.GetFullPath("BepInEx/core/BepInEx.Preloader.Core.dll"));
            bepInPreloaderDll.GetType("BepInEx.Preloader.Core.EnvVars").GetMethod("LoadVars", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[0]);

            // Load
            BepInLoader.LoadBepInEx(main, args);
        }

        public override void PostInit()
        {
            BepInLoader.PostInit();
        }

        protected override void Define()
        {
        }
    }
}
