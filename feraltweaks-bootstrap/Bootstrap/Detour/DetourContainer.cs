using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FeralTweaksBootstrap.Detour
{
    /// <summary>
    /// Detour container
    /// </summary>
    /// <typeparam name="T">Delegate type</typeparam>
    public abstract class DetourContainer<T> where T : Delegate
    {
        private T orig;
        private T detour;
        internal IntPtr location;
        internal IntPtr detourPtr;
        internal IntPtr trampoline;
        internal IntPtr funchook;
        internal static List<object> detourLock = new List<object>();

        internal void Setup(IntPtr funchook, IntPtr loc, IntPtr trampoline)
        {
            this.funchook = funchook;
            this.location = loc;
            this.trampoline = trampoline;

            // Apply
            Funchook.FunchookInstall(funchook, 0);

            // Set original
            T dele = Marshal.GetDelegateForFunctionPointer<T>(trampoline);
            lock (detourLock)
                detourLock.Add(this);
            orig = dele;
        }
        public abstract T run();

        /// <summary>
        /// Unhooks the detour
        /// </summary>
        public void Unhook()
        {
            Funchook.FunchookUninstall(funchook, 0);
            Funchook.FunchookDestroy(funchook);
            lock (detourLock)
                detourLock.Remove(this);
        }

        public IntPtr OriginalPtr
        {
            get
            {
                return location;
            }
        }


        public IntPtr DetourPtr
        {
            get
            {
                return detourPtr;
            }
            internal set
            {
                detourPtr = value;
            }
        }

        public IntPtr TrampolinePtr
        {
            get
            {
                return trampoline;
            }
        }

        public T Original
        {
            get
            {
                return orig;
            }
        }

        public T Detour
        {
            get
            {
                if (detour == null)
                    detour = run();
                return detour;
            }
        }
    }

}
