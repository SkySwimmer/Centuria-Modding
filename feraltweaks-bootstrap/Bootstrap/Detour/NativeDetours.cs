using Iced.Intel;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FeralTweaksBootstrap.Detour
{
    /// <summary>
    /// Tool to create detours in native code
    /// </summary>
    public static class NativeDetours
    {
        /// <summary>
        /// Creates a detour
        /// </summary>
        /// <param name="location">Target pointer</param>
        /// <param name="detourMethod">Detour method container</param>
        public static unsafe void CreateDetour<T>(IntPtr location, DetourContainer<T> detourMethod) where T : Delegate
        {
            // Create the detour pointer
            IntPtr func = Marshal.GetFunctionPointerForDelegate(detourMethod.Detour);
            detourMethod.DetourPtr = func;

            // Create the funchook instance
            IntPtr funchook = Funchook.FunchookCreate();

            // Create trampoline
            IntPtr trampolinePtr = CreateTrampoline(location, detourMethod.DetourPtr, funchook);

            // Set info
            detourMethod.Setup(funchook, location, trampolinePtr);
        }

        /// <summary>
        /// Creates a trampoline pointer
        /// </summary>
        /// <param name="function">Target function</param>
        /// <param name="hook">Hook pointer</param>
        /// <param name="funchook">Funchook instance</param>
        /// <returns>Trampoline pointer</returns>
        public static unsafe IntPtr CreateTrampoline(IntPtr function, IntPtr hook, IntPtr funchook)
        {
            IntPtr trampoline = function;
            Funchook.FunchookPrepare(funchook, (void**)&trampoline, hook);
            return trampoline;
        }
    }
}