using FeralTweaks;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class WindUpdraftPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(WindUpdraft), "MStart")]
        public static void MStart(ref WindUpdraft __instance)
        {
            if (!FeralTweaks.PatchConfig.ContainsKey("DisableUpdraftAudioSuppressor") || FeralTweaks.PatchConfig["DisableUpdraftAudioSuppressor"].ToLower() == "false")
                return;
            FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogInfo("Patching wind updrafts...");
            Transform ch = __instance._updraftEnterExitAudioTrigger.gameObject.transform.parent.Find("updraft_rune_emitter");
            if (ch != null)
                ch.gameObject.SetActive(false);
        }
    }
}
