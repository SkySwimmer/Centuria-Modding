using System;
using System.Globalization;
using System.Reflection;
using FeralTweaks.Mods;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using Newtonsoft.Json;
using StrayTech;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class ActorScalingPatch
    {
        public static float ActorScaleMultiplier = 1.0f;
        public static float ActorScaleMultiplierLowerMost = 0.5f;
        private static bool inited;
        public static void Init()
        {
            if (inited)
                return;
            inited = true;

            // Load factor
            if (FeralTweaks.PatchConfig.ContainsKey("ActorScaleMultiplier"))
                ActorScaleMultiplier = float.Parse(FeralTweaks.PatchConfig["ActorScaleMultiplier"], NumberFormatInfo.InvariantInfo);
            if (FeralTweaks.PatchConfig.ContainsKey("ActorScaleMultiplierLowerMost"))
                ActorScaleMultiplierLowerMost = float.Parse(FeralTweaks.PatchConfig["ActorScaleMultiplierLowerMost"], NumberFormatInfo.InvariantInfo);
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActorBase), "BodyTransform", MethodType.Getter)]
        public static void BodyTransformGetter(ref ActorBase __instance, ref Transform __result)
        {
            __result.localScale = new Vector3(__instance.BodyScale, __instance.BodyScale, __instance.BodyScale);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActorBase), "BodyScale", MethodType.Getter)]
        public static void BodyScaleGetter(ref ActorBase __instance, ref float __result)
        {
            Init();
            if (__instance.Info == null)
                return;

            // Update if needed
            // Find 'scale' scale group
            ActorInfoScaleGroup scale = __instance.Info.GetScaleGroupWithDefID("709");
            if (scale != null)
            {
                // Get scale
                float scaleF = scale.scale;
                if (scaleF < 0)
                {
                    // Calculate using lowermost
                    float range = 1f - ActorScaleMultiplierLowerMost;
                    if (range < 0f)
                        range = 0f;
                    if (range > 1f)
                        range = 1f;
                    float step = range / ActorScaleMultiplier;
                    float value = -scaleF * step;
                    __result = 1f - value;
                }
                else  if (scaleF > 1)
                {
                    // Add
                    __result = scaleF;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FeralCameraStateSettings), "MouseZoomingDistance", MethodType.Getter)]
        public static void MouseZoomingDistanceGetter(ref Vector2 __result)
        {
            Avatar_Local avatar = Avatar_Local.instance;
            if (avatar != null)
            {
                // Get scale
                float scaleF = avatar.BodyScale;
                if (scaleF > 1)
                    __result = new Vector2(__result.x * scaleF, __result.y * scaleF);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FeralCameraStateSettings), "MouseZoomingOffsetMax", MethodType.Getter)]
        public static void MouseZoomingOffsetMaxGetter(ref Vector2 __result)
        {
            Avatar_Local avatar = Avatar_Local.instance;
            if (avatar != null)
            {
                // Get scale
                float scaleF = avatar.BodyScale;
                if (scaleF > 1)
                    __result = new Vector2(__result.x * scaleF, __result.y * scaleF);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActorBase), "MoverMaxSpeed", MethodType.Getter)]
        public static void MoverMaxSpeedGetter(ref float __result, ActorBase __instance)
        {
            // Get scale
            float scaleF = __instance.BodyScale;
            if (scaleF > 2f)
                __result = __result * (scaleF / 2);
        }
    }
}