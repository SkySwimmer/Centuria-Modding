using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.IL2CPP.Logging;
using BepInEx.Logging;
using FeralTweaksBootstrap;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;

namespace jecyll
{
    internal class BepInLoader
    {
        internal static void LoadBepInEx(MethodInfo main, string[] args)
        {
            // Apply patches
            Harmony.CreateAndPatchAll(typeof(PlatformHelperPatch));
            Harmony.CreateAndPatchAll(typeof(IL2CPPChainloaderPatch));
            Harmony.CreateAndPatchAll(typeof(ClassInjectorPatch));

            // Run BepInEx!
            main.Invoke(null, new object[] { args });
        }

        internal static void PostInit()
        {
            // Post-init BepInEx
            try
            {
                IL2CPPChainloader.Instance.Execute();
            }
            catch (Exception ex)
            {
                var logger = Logger.CreateLogSource("Chainloader");
                logger.Log(LogLevel.Fatal, "Unable to execute IL2CPP chainloader");
                logger.Log(LogLevel.Error, ex);
            }
        }
    }
}