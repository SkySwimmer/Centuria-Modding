using FeralTweaks;
using FeralTweaksBootstrap.Detour;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using FeralTweaks.Logging.Impl;
using Logger = FeralTweaks.Logging.Logger;
using System.IO;
using System.Reflection;
using static FeralTweaks.Mods.FeralTweaksMod;
using FeralTweaks.Actions;
using FeralTweaks.Mods;
using FeralTweaks.Profiler.Profiling;
using FeralTweaks.Profiler.API;
using System.Collections.Generic;

namespace FeralTweaksBootstrap
{
    /// <summary>
    /// Runtime invoke delegate
    /// </summary>
    /// <param name="method">Method pointer</param>
    /// <param name="obj">Object pointer</param>
    /// <param name="parameters">Parameter pointer</param>
    /// <param name="exc">Exception pointer</param>
    /// <returns>Result pointer</returns>
    public delegate IntPtr RuntimeInvokeDetour(IntPtr method, IntPtr obj, IntPtr parameters, IntPtr exc);

    internal class RuntimeInvokeDetourContainer : DetourContainer<RuntimeInvokeDetour>
    {
        // FIXME: automate below with RuntimeInvokeUnityProfilingHookAttribute
        private static Dictionary<string, string> unityProfilerCallMap = new Dictionary<string, string>
        {
            ["Awake"] = BaseProfilerLayers.UNITYENGINE_LIFECYCLE_AWAKE,
            ["OnEnable"] = BaseProfilerLayers.UNITYENGINE_LIFECYCLE_ONENABLE,
            ["OnDisable"] = BaseProfilerLayers.UNITYENGINE_LIFECYCLE_ONDISABLE,
            ["Reset"] = BaseProfilerLayers.UNITYENGINE_LIFECYCLE_RESET,
            ["Start"] = BaseProfilerLayers.UNITYENGINE_LIFECYCLE_START,
            ["FixedUpdate"] = BaseProfilerLayers.UNITYENGINE_LIFECYCLE_FIXEDUPDATE,
            ["Update"] = BaseProfilerLayers.UNITYENGINE_LIFECYCLE_UPDATE,
            ["LateUpdate"] = BaseProfilerLayers.UNITYENGINE_LIFECYCLE_LATEUPDATE,
            ["OnDestroy"] = BaseProfilerLayers.UNITYENGINE_LIFECYCLE_ONDESTROY
        };

        private bool executedFtSetup = false;

        private IntPtr ExecRun(string cls, string methodName, IntPtr clsP, IntPtr typeP, IntPtr method, IntPtr obj, IntPtr parameters, IntPtr except)
        {
            if (!executedFtSetup && methodName == "Internal_ActiveSceneChanged" && cls == "UnityEngine.SceneManagement.SceneManager")
            {
                // Wrap up and unhook
                IntPtr res = Original(method, obj, parameters, except);
                executedFtSetup = true;

                // Call loader
                FeralTweaksLoader.SetupUnity();

                // Return result
                return res;
            }

            // Handle update
            if (methodName == "Update" && cls == "FeralTweaks.FrameUpdateHandler")
            {
                // Call update
                FeralTweaksActions.CallUpdate();
                return Original(method, obj, parameters, except);
            }

            // Handle differently
            // Check any mods intercepting the raw method
            // And build the execution chain
            RuntimeInvokeDetour chain = Original;
            foreach (FeralTweaksMod mod in FeralTweaksLoader.modsOrdered)
            {
                // Check if mod registered any handlers
                foreach (RawInjectionHandler detour in mod._rawDetours)
                {
                    RuntimeInvokeDetour d = detour(methodName, cls, clsP, obj, method, parameters, chain);
                    if (d != null)
                        chain = d;
                }
            }

            // Return
            return chain(method, obj, parameters, except);
        }

        private IntPtr ExecProfiledRun(string cls, string methodName, IntPtr clsP, IntPtr typeP, IntPtr method, IntPtr obj, IntPtr parameters, IntPtr except)
        {
            // Lifecycle
            // FIXME: add support for methods annotated with RuntimeInitializeOnLoadMethodAttribute (static)

            // Pre-execute

            // Frame object
            ProfilerFrame frame = null;

            // Check type of call
            if (unityProfilerCallMap.ContainsKey(methodName))
            {
                // Check object
                if (Il2CppType.Of<Component>().IsAssignableFrom(Il2CppType.TypeFromPointer(clsP)))
                    frame = ProfilerFrames.OfCurrentThread.OpenFrame(unityProfilerCallMap[methodName], unityProfilerCallMap[methodName], cls + " " + methodName);
            }

            // Attach details if needed
            if (frame != null)
            {
                // Attach details
                // FIXME: attach details such as class, namespace, object, whatever
            }

            // Execute
            IntPtr res = ExecRun(cls, methodName, clsP, typeP, method, obj, parameters, except);

            // Post-execute

            // Close frame if needed
            if (frame != null)
                frame.CloseFrame();

            // Return
            return res;
        }

        public override RuntimeInvokeDetour run()
        {
            // Return detour
            return (method, obj, parameters, except) =>
            {
                IntPtr clsP = IL2CPP.il2cpp_method_get_class(method);
                IntPtr typeP = IL2CPP.il2cpp_class_get_type(clsP);
                
                IntPtr tNPtr = IL2CPP.il2cpp_type_get_name(typeP);
                string cls = Marshal.PtrToStringAnsi(tNPtr);
                IL2CPP.il2cpp_free(tNPtr);

                IntPtr mNPtr = IL2CPP.il2cpp_method_get_name(method);
                string methodName = Marshal.PtrToStringAnsi(mNPtr);
                IL2CPP.il2cpp_free(mNPtr);
                
                if (FeralTweaksProfiler.IsEnabled)
                    return ExecProfiledRun(cls, methodName, clsP, typeP, method, obj, parameters, except);
                return ExecRun(cls, methodName, clsP, typeP, method, obj, parameters, except);
            };
        }
    }
}