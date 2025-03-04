using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FeralTweaks;
using FeralTweaks.Mods;
using Il2CppInterop.Runtime.Injection;
using Newtonsoft.Json;
using UnityEngine;
using HarmonyLib;
using EarlyAccessPorts.MoreWinglessFliers.Patches.AssemblyCSharp;

namespace EarlyAccessPorts.MoreWinglessFliers
{
    public class MoreWinglessFliersMod : FeralTweaksMod
    {
        public static Dictionary<string, string> PatchConfig = new Dictionary<string, string>();

        public override void Init()
        {
            // Check if FT 1.8 is present
            if (typeof(feraltweaks.Patches.AssemblyCSharp.ChatPatches).Assembly.GetType("feraltweaks.Patches.AssemblyCSharp.NotificationPatches") != null)
            {
                // Error
                LogError("Running on FT 1.8+, disabling " + ID + "!");
                return;
            }

            // Load config
            LoadConfig();

            // Patch with harmony
            LogInfo("Applying patches...");
            ApplyPatches();
        }

        private void ApplyPatches()
        {
            // Patches
            ApplyPatch(typeof(GlidingManagerPatch));
        }

        public static void ApplyPatch(Type type)
        {
            FeralTweaksLoader.GetLoadedMod<MoreWinglessFliersMod>().LogInfo("Applying patch: " + type.FullName);
            Harmony.CreateAndPatchAll(type);
        }

        // Configuration parsing
        private void LoadConfig()
        {
            // Load config
            LogInfo("Loading configuration...");
            Directory.CreateDirectory(ConfigDir);
            if (!File.Exists(ConfigDir + "/settings.props"))
            {
                LogInfo("Writing defaults...");
                WriteDefaultConfig();
            }
            else
            {
                LogInfo("Processing data...");
                foreach (string line in File.ReadAllLines(ConfigDir + "/settings.props"))
                {
                    if (line == "" || line.StartsWith("#") || !line.Contains("="))
                        continue;
                    string key = line.Remove(line.IndexOf("="));
                    string value = line.Substring(line.IndexOf("=") + 1);
                    PatchConfig[key] = value;
                }
            }
            LogInfo("Configuration loaded.");
        }

        /// <summary>
        /// Writes the default configuration
        /// </summary>
        public static void WriteDefaultConfig()
        {
            File.WriteAllText(FeralTweaksLoader.GetLoadedMod<MoreWinglessFliersMod>().ConfigDir + "/settings.props",
                  "AllowDragonGlidingWithNoWings=true\n"
                + "AllowShinigamiGlidingWithNoWings=true\n");
        }
    }
}
