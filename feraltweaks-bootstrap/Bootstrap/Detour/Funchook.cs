using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FeralTweaksBootstrap.Detour
{
    public static unsafe class Funchook
    {
        public enum FunchookResult
        {
            InternalError = -1,
            Success = 0,
            OutOfMemory = 1,
            AlreadyInstalled = 2,
            Disassembly = 3,
            IPRelativeOffset = 4,
            CannotFixIPRelative = 5,
            FoundBackJump = 6,
            TooShortInstructions = 7,
            MemoryAllocation = 8,
            MemoryFunction = 9,
            NotInstalled = 10,
            NoAvailableRegisters = 11
        }

        [DllImport("funchook", EntryPoint = "funchook_create", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FunchookCreate();

        [DllImport("funchook", EntryPoint = "funchook_prepare", CallingConvention = CallingConvention.Cdecl)]
        public static extern FunchookResult FunchookPrepare(IntPtr funchook, void** tragetFunc, IntPtr hookFunc);

        [DllImport("funchook", EntryPoint = "funchook_install", CallingConvention = CallingConvention.Cdecl)]
        public static extern FunchookResult FunchookInstall(IntPtr funchook, int flags);

        [DllImport("funchook", EntryPoint = "funchook_uninstall", CallingConvention = CallingConvention.Cdecl)]
        public static extern FunchookResult FunchookUninstall(IntPtr funchook, int flags);

        [DllImport("funchook", EntryPoint = "funchook_destroy", CallingConvention = CallingConvention.Cdecl)]
        public static extern FunchookResult FunchookDestroy(IntPtr funchook);

        [DllImport("funchook", EntryPoint = "funchook_error_message", CallingConvention = CallingConvention.Cdecl)]
        public static extern string FunchookErrorMessage(IntPtr funchook);

    }
}
