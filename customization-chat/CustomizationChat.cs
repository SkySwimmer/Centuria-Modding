using System;
using FeralTweaks;
using FeralTweaks.Mods;
using CustomizationChat.Actions;
using CustomizationChat.Patches;
using HarmonyLib;

namespace CustomizationChat
{
    public class CustomizationChat : FeralTweaksMod
    {
        public override void Init()
        {
            // Start action thread
            FeralTweaksActionManager.StartActionThread();

            // Patch with harmony
            LogInfo("Applying patches...");
            ApplyPatches();
        }

        private void ApplyPatches()
        {
            // Patches
            ApplyPatch(typeof(UpdateHook));
            ApplyPatch(typeof(OpenCreatureMenuHook));
        }

        public static void ApplyPatch(Type type)
        {
            FeralTweaksLoader.GetLoadedMod<CustomizationChat>().LogInfo("Applying patch: " + type.FullName);
            Harmony.CreateAndPatchAll(type);
        }
    }
}
