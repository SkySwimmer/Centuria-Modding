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
        private Dictionary<string, string> unityProfilerCallMap = new Dictionary<string, string>
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

        public override RuntimeInvokeDetour run()
        {
            bool done = false;
            bool doneI = false;

            // Inner method
            RuntimeInvokeDetour detourExec = (method, obj, parameters, except) =>
            { 
                IntPtr clsP = IL2CPP.il2cpp_method_get_class(method);
                string cls = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_type_get_name(IL2CPP.il2cpp_class_get_type(clsP)));
                string methodName = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_method_get_name(method));
                if (!doneI || !done)
                {
                    if (!done && methodName == "Internal_ActiveSceneChanged" && cls == "UnityEngine.SceneManagement.SceneManager")
                    {
                        // Wrap up and unhook
                        IntPtr res = Original(method, obj, parameters, except);
                        done = true;

                        // Call loader
                        FeralTweaksLoader.SetupUnity();

                        // Return result
                        return res;
                    }
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
                        {
                            chain = (method, obj, parameters, except) =>
                            {
                                return d(method, obj, parameters, except);
                            };
                        }
                    }
                }

                // Return
                return chain(method, obj, parameters, except);
            };

            // Actual detour
            RuntimeInvokeDetour detourMain = (method, obj, parameters, except) =>
            {
                // Check if profiler is active
                RuntimeInvokeDetour detourToRun = detourExec;
                if (FeralTweaksProfiler.IsEnabled)
                {
                    // Box
                    detourToRun = (method, obj, parameters, except) =>
                    {
                        // Profiler hooks
                        IntPtr clsP = IL2CPP.il2cpp_method_get_class(method);
                        string cls = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_type_get_name(IL2CPP.il2cpp_class_get_type(clsP)));
                        string methodName = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_method_get_name(method));

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
                        IntPtr res = detourExec(method, obj, parameters, except);

                        // Post-execute
                        
                        // Close frame if needed
                        if (frame != null)
                            frame.CloseFrame();

                        // Return
                        return res;
                    };
                }

                // Run
                return detourToRun(method, obj, parameters, except);
            };
            return detourMain;
        }
    }
}