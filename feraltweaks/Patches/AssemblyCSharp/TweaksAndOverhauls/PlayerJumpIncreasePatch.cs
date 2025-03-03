﻿using System;
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
        private static bool inited;
        public static void Init()
        {
            if (inited)
                return;
            inited = true;

            // Load factor
            if (FeralTweaks.PatchConfig.ContainsKey("JumpIncreaseFactor"))
                JumpForceFactor = float.Parse(FeralTweaks.PatchConfig["JumpIncreaseFactor"], NumberFormatInfo.InvariantInfo);
        }

        private static float lastJumpForce;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Avatar_Local), "MUpdate")]
        public static void MUpdate(ref Avatar_Local __instance)
        {
            Init();
            if (lastJumpForce != __instance.MoverJumpForce)
            {
                // Update force
                lastJumpForce = __instance.MoverJumpForce * JumpForceFactor;
                __instance._moverJumpForce = lastJumpForce;
            }
        }

    }
}