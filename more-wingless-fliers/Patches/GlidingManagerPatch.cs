using System;
using System.Globalization;
using System.Reflection;
using CodeStage.AntiCheat.ObscuredTypes;
using FeralTweaks.Mods;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace EarlyAccessPorts.MoreWinglessFliers.Patches.AssemblyCSharp
{
    public class GlidingManagerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GlidingManager), "MUpdate")]
        public static void Update(ref GlidingManager __instance)
        {
            // Get avatar
            Avatar_Local avatar = Avatar_Local.instance;
            if (avatar != null && avatar.Info != null)
            {
                // Check if a dragon or shinigami, if so, override wings to allow without
                if ((avatar.Info.actorClassDefID == "5035" && MoreWinglessFliersMod.PatchConfig.ContainsKey("AllowDragonGlidingWithNoWings") && MoreWinglessFliersMod.PatchConfig["AllowDragonGlidingWithNoWings"].ToLower() == "true") || (avatar.Info.actorClassDefID == "23970" && MoreWinglessFliersMod.PatchConfig.ContainsKey("AllowShinigamiGlidingWithNoWings") && MoreWinglessFliersMod.PatchConfig["AllowShinigamiGlidingWithNoWings"].ToLower() == "true"))
                {
                    // Override
                    if (!__instance._hasWingsEquipped.GetDecrypted())
                        __instance._hasWingsEquipped = new ObscuredBool(true);
                }
            }
        }
    }
}