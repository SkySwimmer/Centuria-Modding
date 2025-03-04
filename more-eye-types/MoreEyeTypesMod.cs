using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FeralTweaks;
using FeralTweaks.Mods;
using EarlyAccessPorts.MoreEyeTypes.Patches.AssemblyCSharp;
using Il2CppInterop.Runtime.Injection;
using Newtonsoft.Json;
using UnityEngine;
using HarmonyLib;

namespace EarlyAccessPorts.MoreEyeTypes
{
    public class MoreEyeTypesMod : FeralTweaksMod
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
            ApplyPatch(typeof(BundlePatches));

            // Add chart patches
            LogInfo("Adding chart patches...");
            foreach (FileInfo file in new DirectoryInfo(ModBaseDirectory + "/content/chartpatches").GetFiles("*.cdpf", SearchOption.AllDirectories))
            {
                string patch = File.ReadAllText(file.FullName).Replace("\t", "    ").Replace("\r", "");
                feraltweaks.FeralTweaks.Patches[patch] = file.Name;
            }

            // Add bundles
            LogInfo("Adding bundle patches...");

            // Find files
            DirectoryInfo dir = new DirectoryInfo(ModBaseDirectory + "/content/assetbundles");
            foreach (FileInfo file in dir.GetFiles("*.unity3d", SearchOption.AllDirectories))
            {
                // Get path
                string filePath = Path.GetRelativePath(dir.FullName, file.FullName).Replace(Path.DirectorySeparatorChar, '/');
                string bundleId = filePath.Replace("/", "_").Remove(filePath.LastIndexOf(".unity3d"));

                // Log
                LogInfo("Found asset for '" + bundleId + "', file path: " + file.FullName);
                feraltweaks.Patches.AssemblyCSharp.BundlePatches.AssetBundlePaths[bundleId] = file.FullName;
            }
        }
        
        public static void ApplyPatch(Type type)
        {
            FeralTweaksLoader.GetLoadedMod<MoreEyeTypesMod>().LogInfo("Applying patch: " + type.FullName);
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
            File.WriteAllText(FeralTweaksLoader.GetLoadedMod<MoreEyeTypesMod>().ConfigDir + "/settings.props",
                  "\n");
        }
    }
}
