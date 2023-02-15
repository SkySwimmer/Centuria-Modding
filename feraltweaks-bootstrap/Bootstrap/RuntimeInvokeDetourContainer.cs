using FeralTweaks;
using FeralTweaksBootstrap.Detour;
using Il2CppInterop.Runtime;
using System;
using System.Runtime.InteropServices;

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
        public override RuntimeInvokeDetour run()
        {
            return (method, obj, parameters, except) =>
            {
                string cls = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(IL2CPP.il2cpp_method_get_class(method)));
                string methodName = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_method_get_name(method));
                if (methodName == "Internal_ActiveSceneChanged")
                {
                    // Wrap up and unhook
                    IntPtr res = Original(method, obj, parameters, except);
                    Unhook();

                    // Finish loading
                    FeralTweaksLoader.LoadFinish();

                    // Return result
                    return res;
                }
                return Original(method, obj, parameters, except);
            };
        }
    }
}
