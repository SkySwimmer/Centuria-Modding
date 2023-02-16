using HarmonyLib;
using System.Collections.Generic;
using System.IO;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class UI_Window_ChangeDisplayNamePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_ChangeDisplayName), "Setup")]
        public static void Setup(ref UI_Window_ChangeDisplayName __instance)
        {
            if (__instance._usernameInput == null)
            {
                return;
            }

            // Log
            FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogInfo("Patching display name change window...");

            // Check FlexibleDisplayNames
            if (Plugin.PatchConfig.GetValueOrDefault("FlexibleDisplayNames", "false").ToLower() == "true")
            {
                __instance._usernameInput.contentType = TMPro.TMP_InputField.ContentType.Standard;
                __instance._usernameInput.characterValidation = TMPro.TMP_InputField.CharacterValidation.None;
                if (Plugin.PatchConfig.ContainsKey("DisplayNameMaxLength"))
                    __instance._usernameInput.characterLimit = int.Parse(Plugin.PatchConfig["DisplayNameMaxLength"]);
            }
        }
    }
}
