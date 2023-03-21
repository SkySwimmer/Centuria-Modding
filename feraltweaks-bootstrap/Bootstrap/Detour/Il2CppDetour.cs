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

        private IntPtr origPtr;
        private bool trampolineCreated;
        private IntPtr detourPtr;
        private MethodInfo trampolineMethod;
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

            // Create funchook
            funchook = Funchook.FunchookCreate();

            // Create the detour pointer
            IntPtr func = Marshal.GetFunctionPointerForDelegate(target);
            detourPtr = func;

            // Create trampoline
            trampolinePtr = NativeDetours.CreateTrampoline(origPtr, detourPtr, funchook);
        }

        public void Apply()
        {
            // Create trampoline if needed
            CreateTrampoline();

            // Apply
            Funchook.FunchookInstall(funchook, 0);
        }

        public void Dispose()
        {
            // Unhook
            Funchook.FunchookUninstall(funchook, 0);
            Funchook.FunchookDestroy(funchook);

            // Release lock
            lock (detourLock)
                detourLock.Remove(this);
        }

        public T GenerateTrampoline<T>() where T : Delegate
        {
            // Create trampoline if needed
            CreateTrampoline();

            // Create proxy
            if (trampolineMethod == null)
                trampolineMethod = DetourHelper.GenerateNativeProxy(trampolinePtr, typeof(T).GetMethod("Invoke"));

            // Return
            return Marshal.GetDelegateForFunctionPointer<T>(trampolinePtr);
        }
    }
}