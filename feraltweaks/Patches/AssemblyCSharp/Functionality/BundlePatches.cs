using FeralTweaks;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using WW.Waiters;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public static class BundlePatches
    {
        public static Dictionary<string, string> AssetBundlePaths = new Dictionary<string, string>();
        public static Dictionary<string, ManifestDef> AddedManifestDefs = new Dictionary<string, ManifestDef>();
        private static bool patched = false;
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(WaitController), "Update")]
        public static void Update()
        {
            // Prevent cached charts from removing
            ManifestChartData chart = CoreChartDataManager.coreInstance.manifestChartData;
            foreach (ManifestDef def in AddedManifestDefs.Values)
            {
                if (!chart._parsedDefsByID.ContainsKey(def.defID))
                    chart._parsedDefsByID.Add(def.defID, def);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CoreChartDataManager), "SetChartObjectInstances")]
        public static void SetChartObjectInstances()
        {
            if (patched)
                return;
            patched = true;

            // Get chart
            FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogInfo("Patching bundle manifest chart...");
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
                    FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogInfo("Creating bundle def: " + asset + "...");
                    def = new ManifestDef();
                    chart.defList.Add(def);
                    chart._parsedDefsByID.Add(asset, def);
                    AddedManifestDefs[asset]=def;
                }
                else
                    FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogInfo("Patching bundle def: " + asset + "...");

                // Update def
                def.fileName = asset;
                def.defID = asset;
                def._downloadURL = new Uri(AssetBundlePaths[asset]).AbsoluteUri;
                def.hash = new DateTimeOffset(File.GetLastWriteTimeUtc(AssetBundlePaths[asset])).ToUnixTimeMilliseconds().ToString();
                def.defName = CoreBundleManager.GetBundleIDFromFileName(asset);
                def.lowerDefName = CoreBundleManager.GetBundleIDFromFileName(asset);
                FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogDebug("Def ID: " + def.defID);
                FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogDebug("Def file name: " + def.fileName);
                FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogDebug("Def timestamp: " + def.hash);
                FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogDebug("Def name: " + def.defName);
                FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogDebug("Lower def name: " + def.lowerDefName);
            }
        }
    }

}
