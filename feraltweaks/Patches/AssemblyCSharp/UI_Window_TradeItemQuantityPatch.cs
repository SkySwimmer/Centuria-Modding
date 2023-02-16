using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class UI_Window_TradeItemQuantityPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_TradeItemQuantity), "ChosenQuantity")]
        [HarmonyPatch(MethodType.Setter)]
        public static bool ChosenQuantity_SET(ref UI_Window_TradeItemQuantity __instance, ref int value)
        {
            if (!Plugin.PatchConfig.ContainsKey("TradeItemLimit"))
                return true;

            int limit = int.Parse(Plugin.PatchConfig["TradeItemLimit"]);
            if (value < 0)
                value = 0;
            else if (value > limit)
                value = limit;
            __instance._chosenQuantity = value;
            __instance._inputField.SetTextWithoutNotify(value.ToString());
            __instance.RefreshQuantity();

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_TradeItemQuantity), "BtnClicked_Increase")]
        public static bool BtnClicked_Increase(ref UI_Window_TradeItemQuantity __instance)
        {
            if (!Plugin.PatchConfig.ContainsKey("TradeItemLimit"))
                return true;

            int limit = int.Parse(Plugin.PatchConfig["TradeItemLimit"]);
            int newQuantity = __instance._chosenQuantity + 1;
            if (newQuantity < 0)
                newQuantity = 0;
            else if (newQuantity > limit)
                newQuantity = limit;
            __instance._chosenQuantity = newQuantity;
            __instance._inputField.SetTextWithoutNotify(newQuantity.ToString());
            __instance.RefreshQuantity();

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_TradeItemQuantity), "BtnClicked_Decrease")]
        public static bool BtnClicked_Decrease(ref UI_Window_TradeItemQuantity __instance)
        {
            if (!Plugin.PatchConfig.ContainsKey("TradeItemLimit"))
                return true;

            int limit = int.Parse(Plugin.PatchConfig["TradeItemLimit"]);
            int newQuantity = __instance._chosenQuantity - 1;
            if (newQuantity < 0)
                newQuantity = 0;
            else if (newQuantity > limit)
                newQuantity = limit;
            __instance._chosenQuantity = newQuantity;
            __instance._inputField.SetTextWithoutNotify(newQuantity.ToString());
            __instance.RefreshQuantity();

            return false;
        }

    }
}
