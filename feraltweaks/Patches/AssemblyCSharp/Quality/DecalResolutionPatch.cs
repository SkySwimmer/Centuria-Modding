using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class DecalResolutionPatch
    {
        public static class PlayerObjectPatches
        {
            public static class PatchesLate
            {
                [HarmonyPrefix]
                [HarmonyPatch(typeof(ActorBase), "DecalResolution", MethodType.Getter)]
                public static bool DecalResolution(ActorBase __instance, ref int __result)
                {
                    // Check config
                    if (FeralTweaks.PatchConfig.ContainsKey("DecalResolution"))
                        __result = int.Parse(FeralTweaks.PatchConfig["DecalResolution"]);
                    else
                        __result = 2048;
                    return false;
                }
            }
        }

        private static bool patched;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CoreChartDataManager), "SetChartObjectInstances")]
        public static void SetChartObjectInstances()
        {
            if (patched)
                return;
            patched = true;

            // PAtch
            Harmony.CreateAndPatchAll(typeof(PlayerObjectPatches));
        }
        
    }
}