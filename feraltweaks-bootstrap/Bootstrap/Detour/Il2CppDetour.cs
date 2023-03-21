using FeralTweaksBootstrap.Detour;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FeralTweaksBootstrap
{
    internal class Il2CppDetour<TDelegate> : Il2CppInterop.Runtime.Injection.IDetour where TDelegate : Delegate
    {
        private static List<object> detourLock = new List<object>();
        private TDelegate target;

        private bool trampolineCreated;
        private bool applied;

        private IntPtr origPtr;
        private IntPtr detourPtr;
        private IntPtr trampolinePtr;
        private IntPtr funchook;

        public Il2CppDetour(nint original, TDelegate target)
        {
            this.origPtr = original;
            this.target = target;
        }

        public nint Target => origPtr;

        public nint Detour => detourPtr;

        public nint OriginalTrampoline => trampolinePtr;

        private void CreateTrampoline()
        {
            if (trampolineCreated)
                return; // Already created
            trampolineCreated = true;
            lock (detourLock)
                detourLock.Add(this);

            // Create the detour pointer
            IntPtr func = Marshal.GetFunctionPointerForDelegate(target);
            detourPtr = func;

            // Create funchook
            funchook = Funchook.FunchookCreate();

            // Create trampoline
            trampolinePtr = NativeDetours.CreateTrampoline(origPtr, detourPtr, funchook);
        }

        public void Apply()
        {
            // Create trampoline if needed
            CreateTrampoline();

            // Apply
            Funchook.FunchookInstall(funchook, 0);
            applied = true;
        }

        public void Dispose()
        {
            // Unhook
            if (applied)
                Funchook.FunchookUninstall(funchook, 0);
            if (trampolineCreated)
            {
                Funchook.FunchookDestroy(funchook);

                // Release lock
                lock (detourLock)
                    detourLock.Remove(this);
            }

            funchook = IntPtr.Zero;
            detourPtr = IntPtr.Zero;
            applied = false;
            trampolineCreated = false;
        }

        public T GenerateTrampoline<T>() where T : Delegate
        {
            // Create trampoline if needed
            CreateTrampoline();

            // Return
            return Marshal.GetDelegateForFunctionPointer<T>(trampolinePtr);
        }
    }
}