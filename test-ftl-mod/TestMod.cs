using System;
using FeralTweaks.Mods;
using FeralTweaks;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;

namespace TestFtlMod
{
    public class TestMod : FeralTweaksMod
    {
        public override void Init()
        {
            Harmony.CreateAndPatchAll(typeof(TestPatches));
        }
    }

    public static class TestPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DownloadingBundle), "StartDownload")]
        public static void StartDownloadPatch(DownloadingBundle __instance)
        {
            __instance = __instance;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ManifestDef), "LoadEntry")]
        public static void LoadEntryPatch(ManifestDef __instance)
        {
            if (__instance.defID == "win32_actors_avatars_fox_bodyparts_ears_ears000_default_texture")
                __instance = __instance;
        }
    }
}
