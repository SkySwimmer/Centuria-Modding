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

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class PlayerJumpIncreasePatch
    {

        public static float JumpForceFactor = 1.0f;
        private static bool patched;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CoreChartDataManager), "SetChartObjectInstances")]
        public static void SetChartObjectInstances()
        {
            if (patched)
                return;
            patched = true;

            // Load factor
            if (FeralTweaks.PatchConfig.ContainsKey("JumpIncreaseFactor"))
                JumpForceFactor = float.Parse(FeralTweaks.PatchConfig["JumpIncreaseFactor"], NumberFormatInfo.InvariantInfo);

            // Add patches
            Harmony.CreateAndPatchAll(typeof(PlayerJumpIncreasePatchesLate));
        }

        public static class PlayerJumpIncreasePatchesLate
        {
            private static float lastJumpForce;

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Avatar_Local), "MUpdate")]
            public static void MUpdate(ref Avatar_Local __instance)
            {
                if (lastJumpForce != __instance.MoverJumpForce)
                {
                    // Update force
                    lastJumpForce = __instance.MoverJumpForce * JumpForceFactor;
                    __instance._moverJumpForce = lastJumpForce;
                }
            }
        }

    }
}