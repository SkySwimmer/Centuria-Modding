using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using BepInEx.IL2CPP;
using BepInEx.IL2CPP.Hook;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using MonoMod.Utils;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;

namespace jecyll
{
    [HarmonyPatch(typeof(ClassInjector))]
    public static class ClassInjectorPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("RegisterTypeInIl2Cpp", new Type[] { typeof(Type), typeof(RegisterTypeOptions) })]
        public static bool RegisterTypeInIl2Cpp(Type type, RegisterTypeOptions options)
        {
            // Delegate to Il2CppInterop
            Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp(type, new Il2CppInterop.Runtime.Injection.RegisterTypeOptions()
            {
                LogSuccess = options.LogSuccess,
                InterfacesResolver = options.InterfacesResolver
            });

            return false;
        }
    }

    [HarmonyPatch(typeof(IL2CPPChainloader))]
    public static class IL2CPPChainloaderPatch
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(BaseChainloader<BasePlugin>), nameof(BaseChainloader<BasePlugin>.Initialize))]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void InitializeDummy(IL2CPPChainloader instance, string gameExePath = null) { }

        [HarmonyPrefix]
        [HarmonyPatch("Initialize")]
        public static bool Initialize(string gameExePath, ref IL2CPPChainloader __instance)
        {
            // Assign stuff
            GeneratedDatabasesUtil.DatabasesLocationOverride = Preloader.IL2CPPUnhollowedPath;
            PatchManager.ResolvePatcher += IL2CPPDetourMethodPatcher.TryResolve;

            // Init base
            InitializeDummy(__instance, gameExePath);
            IL2CPPChainloader.Instance = __instance;

            // Set detour
            ClassInjector.Detour = new UnhollowerDetourHandler();

            return false;
        }
    }

    [HarmonyPatch(typeof(PlatformHelper))]
    public static class PlatformHelperPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Current", MethodType.Setter)]
        public static bool SetCurrentPlatform()
        {
            if ((bool)typeof(PlatformHelper).GetField("_currentLocked", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null))
                return false;
            return true;
        }
    }

}
