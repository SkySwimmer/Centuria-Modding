using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public static class GlobalSettingsManagerPatch 
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GlobalSettingsManager), "ProdBaseURL", MethodType.Getter)]
        public static void GetProdBaseURL(ref string __result)
        {
            __result = Plugin.PatchConfig.GetValueOrDefault("GameAssetsProd", "https://emuferal.ddns.net/feralassets/");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GlobalSettingsManager), "StageBaseURL", MethodType.Getter)]
        public static void GetStageBaseURL(ref string __result)
        {
            __result = Plugin.PatchConfig.GetValueOrDefault("GameAssetsStage", "https://emuferal.ddns.net/feralassetsstage/");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GlobalSettingsManager), "DevBaseURL", MethodType.Getter)]
        public static void GetDevBaseURL(ref string __result)
        {
            __result = Plugin.PatchConfig.GetValueOrDefault("GameAssetsDev", "https://emuferal.ddns.net/feralassetsdev/");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GlobalSettingsManager), "SharedBaseURL", MethodType.Getter)]
        public static void GetSharedBaseURL(ref string __result)
        {
            __result = Plugin.PatchConfig.GetValueOrDefault("GameAssetsShared", "https://emuferal.ddns.net/feralassets-s2/");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GlobalSettingsManager), "StageSharedBaseURL", MethodType.Getter)]
        public static void GetStageSharedBaseURL(ref string __result)
        {
            __result = Plugin.PatchConfig.GetValueOrDefault("GameAssetsStageShared", "https://emuferal.ddns.net/feralassetsstage-s2/");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GlobalSettingsManager), "DevSharedBaseURL", MethodType.Getter)]
        public static void GetDevSharedBaseURL(ref string __result)
        {
            __result = Plugin.PatchConfig.GetValueOrDefault("GameAssetsDevShared", "https://emuferal.ddns.net/feralassetsdev-s2/");
        }
    }
}