using FeralTweaks;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public static class BundlePatches
    {
        public static Dictionary<string, string> AssetBundlePaths = new Dictionary<string, string>();
        private static bool patched = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ManifestDef), "BundleCacheFilePath", MethodType.Getter)]
        public static bool PatchBundleFilePath(ManifestDef __instance, ref string __result)
        {
            // Find bundle
            if (AssetBundlePaths.ContainsKey(__instance.defID))
            {
                // Found it
                __result = AssetBundlePaths[__instance.defID];
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CoreChartDataManager), "SetChartObjectInstances")]
        public static void SetChartObjectInstances()
        {
            if (patched)
                return;
            patched = true;

            // Get chart
            FeralTweaksLoader.GetLoadedMod<Plugin>().LogInfo("Patching bundle manifest chart...");
            ManifestChartData chart = CoreChartDataManager.coreInstance.manifestChartData;

            // Go through all defs
            foreach (string asset in AssetBundlePaths.Keys)
            {
                // Find existing def, if none create one
                ManifestDef def = null;
                foreach (ManifestDef defI in chart.defList)
                {
                    if (defI.defID == asset)
                    {
                        def = defI;
                        break;
                    }
                }

                // Create def if needed
                if (def == null)
                {
                    FeralTweaksLoader.GetLoadedMod<Plugin>().LogInfo("Creating bundle def: " + asset + "...");
                    def = new ManifestDef();
                    chart.defList.Add(def);
                }
                else
                    FeralTweaksLoader.GetLoadedMod<Plugin>().LogInfo("Patching bundle def: " + asset + "...");

                // Update def
                def.fileName = asset;
                def.defID = asset;
                def.hash = new DateTimeOffset(File.GetLastWriteTimeUtc(AssetBundlePaths[asset])).ToUnixTimeMilliseconds().ToString();
                def.defName = CoreBundleManager.GetBundleIDFromFileName(asset);
                def.lowerDefName = CoreBundleManager.GetBundleIDFromFileName(asset);
                FeralTweaksLoader.GetLoadedMod<Plugin>().LogDebug("Def ID: " + def.defID);
                FeralTweaksLoader.GetLoadedMod<Plugin>().LogDebug("Def file name: " + def.fileName);
                FeralTweaksLoader.GetLoadedMod<Plugin>().LogDebug("Def timestamp: " + def.hash);
                FeralTweaksLoader.GetLoadedMod<Plugin>().LogDebug("Def name: " + def.defName);
                FeralTweaksLoader.GetLoadedMod<Plugin>().LogDebug("Lower def name: " + def.lowerDefName);
            }
        }
    }

}
