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
using StrayTech;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class ActorScalingPatch
    {
        public class FT_ScalingVarsContainer : MonoBehaviour
        {
            public FT_ScalingVarsContainer() : base()
            { }

            public FT_ScalingVarsContainer(System.IntPtr pointer) : base(pointer)
            { }

            public bool scalingReady = false;
        }

        public static float ActorScaleMultiplier = 1.0f;
        public static float ActorScaleMultiplierLowerMost = 0.5f;
        private static bool inited;
        public static void Init()
        {
            if (inited)
                return;
            inited = true;

            // Inject
            ClassInjector.RegisterTypeInIl2Cpp<FT_ScalingVarsContainer>();

            // Load factor
            if (FeralTweaks.PatchConfig.ContainsKey("ActorScaleMultiplier"))
                ActorScaleMultiplier = float.Parse(FeralTweaks.PatchConfig["ActorScaleMultiplier"], NumberFormatInfo.InvariantInfo);
            if (FeralTweaks.PatchConfig.ContainsKey("ActorScaleMultiplierLowerMost"))
                ActorScaleMultiplierLowerMost = float.Parse(FeralTweaks.PatchConfig["ActorScaleMultiplierLowerMost"], NumberFormatInfo.InvariantInfo);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Control_ScaleGroupSlider), "Setup")]
        public static void SetupPre(ref UI_Control_ScaleGroupSlider __instance)
        {
            Init();

            // Check group
            if (__instance.ScaleGroup != null && __instance.ScaleGroup.scaleGroupDefID == "709")
            {
                // Create vars if needed
                FT_ScalingVarsContainer vars = __instance.gameObject.GetComponent<FT_ScalingVarsContainer>();
                if (vars == null)
                    vars = __instance.gameObject.AddComponent<FT_ScalingVarsContainer>();
                vars.scalingReady = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_Control_ScaleGroupSlider), "Setup")]
        public static void Setup(ref UI_Control_ScaleGroupSlider __instance)
        {
            Init();

            // Check group
            if (__instance.ScaleGroup != null && __instance.ScaleGroup.scaleGroupDefID == "709")
            {
                // Update
                __instance._slider.Slider.value = 1 / (ActorScaleMultiplier * 2) * (__instance.ActorInfo.GetScaleGroupWithDefID("709").scale + ActorScaleMultiplier);
                
                // Update vars
                FT_ScalingVarsContainer vars = __instance.gameObject.GetComponent<FT_ScalingVarsContainer>();
                if (vars != null)
                    vars.scalingReady = true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Control_ScaleGroupSlider), "SliderValueChanged_Scale")]
        public static bool SliderValueChanged_Scale(ref UI_Control_ScaleGroupSlider __instance, float inScale)
        {
            Init();

            // Check group
            if (__instance.ScaleGroup != null && __instance.ScaleGroup.scaleGroupDefID == "709")
            {
                // Check ready
                FT_ScalingVarsContainer vars = __instance.gameObject.GetComponent<FT_ScalingVarsContainer>();
                if (vars != null && vars.scalingReady)
                {
                    // Update
                    inScale = (inScale * (ActorScaleMultiplier * 2)) - ActorScaleMultiplier;
                    __instance.ScaleGroup.scale = inScale;
                    __instance.ActorInfo.GetScaleGroupWithDefID("709").scale = inScale;

                    // Update scale of avi
                    Avatar_Local avatar = Avatar_Local.instance;
                    if (avatar != null)
                        avatar.BodyTransform.localScale = new Vector3(avatar.BodyScale, avatar.BodyScale, avatar.BodyScale);

                    // Upscale editor avi
                    foreach (WorldObject obj in __instance.ActorInfo.RegisteredWorldObjects)
                    {
                        ActorBase actor = obj.TryCast<ActorBase>();
                        if (actor != null)
                            actor.BodyTransform.localScale = new Vector3(actor.BodyScale, actor.BodyScale, actor.BodyScale);
                    }
                }

                // Deny default
                return false;
            }

            // Allow default
            return true;
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