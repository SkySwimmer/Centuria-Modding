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
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActorBase), "DecalResolution", MethodType.Getter)]
        public static bool DecalResolution(ActorBase __instance, ref int __result)
        {
            // Check quality
            switch (GlobalSettingsManager.instance.quality)
            {
                case DeviceQualityLevel.Default:
                    // Check config
                    if (FeralTweaks.PatchConfig.ContainsKey("DecalResolutionMid"))
                        __result = int.Parse(FeralTweaks.PatchConfig["DecalResolutionMid"]);
                    else
                        __result = 1024;
                    break;
                case DeviceQualityLevel.Unsupported:
                    // Check config
                    if (FeralTweaks.PatchConfig.ContainsKey("DecalResolutionLow"))
                        __result = int.Parse(FeralTweaks.PatchConfig["DecalResolutionLow"]);
                    else
                        __result = 512;
                    break;
                case DeviceQualityLevel.Lowest:
                    // Check config
                    if (FeralTweaks.PatchConfig.ContainsKey("DecalResolutionLow"))
                        __result = int.Parse(FeralTweaks.PatchConfig["DecalResolutionLow"]);
                    else
                        __result = 512;
                    break;
                case DeviceQualityLevel.Low:
                    // Check config
                    if (FeralTweaks.PatchConfig.ContainsKey("DecalResolutionLow"))
                        __result = int.Parse(FeralTweaks.PatchConfig["DecalResolutionLow"]);
                    else
                        __result = 512;
                    break;
                case DeviceQualityLevel.Medium:
                    // Check config
                    if (FeralTweaks.PatchConfig.ContainsKey("DecalResolutionMid"))
                        __result = int.Parse(FeralTweaks.PatchConfig["DecalResolutionMid"]);
                    else
                        __result = 1024;
                    break;
                case DeviceQualityLevel.High:
                    // Check config
                    if (FeralTweaks.PatchConfig.ContainsKey("DecalResolutionHigh"))
                        __result = int.Parse(FeralTweaks.PatchConfig["DecalResolutionHigh"]);
                    else
                        __result = 2048;
                    break;
                case DeviceQualityLevel.Highest:
                    // Check config
                    if (FeralTweaks.PatchConfig.ContainsKey("DecalResolutionHigh"))
                        __result = int.Parse(FeralTweaks.PatchConfig["DecalResolutionHigh"]);
                    else
                        __result = 2048;
                    break;
            }
            return false;
        }
    }
}