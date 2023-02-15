using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class UI_Window_ResetPasswordPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_Window_ResetPassword), "Setup")]
        public static void Setup(string inEmail, ref UI_Window_ResetPassword __instance)
        {
            if (__instance._emailInput == null)
            {
                return;
            }

            // Log
            FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogInfo("Patching password reset window...");

            // Check AllowNonEmailUsernames
            if (Plugin.PatchConfig.GetValueOrDefault("AllowNonEmailUsernames", "false").ToLower() == "true")
            {
                __instance._emailInput.contentType = TMPro.TMP_InputField.ContentType.Standard;
                __instance._emailInput.characterValidation = TMPro.TMP_InputField.CharacterValidation.None;
                if (Plugin.PatchConfig.ContainsKey("UserNameMaxLength"))
                    __instance._emailInput.characterLimit = int.Parse(Plugin.PatchConfig["UserNameMaxLength"]);
            }
            __instance._emailInput.text = inEmail;
            __instance._resetBtn.interactable = __instance.IsValidEmail();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_ResetPassword), "IsValidEmail")]
        public static bool IsValidEmail(ref UI_Window_ResetPassword __instance, ref bool __result)
        {
            if (!Plugin.PatchConfig.ContainsKey("UserNameRegex"))
                return true;

            __result = Regex.Match(__instance.Email, Plugin.PatchConfig["UserNameRegex"]).Success;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_ResetPassword), "OnEmailChanged")]
        public static bool OnEmailChanged(ref UI_Window_ResetPassword __instance)
        {
            if (!Plugin.PatchConfig.ContainsKey("UserNameRegex"))
                return true;

            __instance._resetBtn.interactable = Regex.Match(__instance.Email, Plugin.PatchConfig["UserNameRegex"]).Success;
            return false;
        }
    }
}
