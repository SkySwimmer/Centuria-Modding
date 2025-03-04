using System;
using System.Globalization;
using System.Reflection;
using CodeStage.AntiCheat.ObscuredTypes;
using FeralTweaks.Mods;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using Newtonsoft.Json;
using StrayTech;
using UnityEngine;

namespace EarlyAccessPorts.AvatarScaleUnlocker.Patches.AssemblyCSharp
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

        private static bool inited;
        public static void Init()
        {
            if (inited)
                return;
            inited = true;

            // Inject
            ClassInjector.RegisterTypeInIl2Cpp<FT_ScalingVarsContainer>();
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
                __instance._slider.Slider.value = 1 / (feraltweaks.Patches.AssemblyCSharp.ActorScalingPatch.ActorScaleMultiplier * 2) * (__instance.ActorInfo.GetScaleGroupWithDefID("709").scale + feraltweaks.Patches.AssemblyCSharp.ActorScalingPatch.ActorScaleMultiplier);
                
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
                    inScale = (inScale * (feraltweaks.Patches.AssemblyCSharp.ActorScalingPatch.ActorScaleMultiplier * 2)) - feraltweaks.Patches.AssemblyCSharp.ActorScalingPatch.ActorScaleMultiplier;
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
    }
}