using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class TradeLimitPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_TradeItemQuantity), "ChosenQuantity")]
        [HarmonyPatch(MethodType.Setter)]
        public static bool ChosenQuantity_SET(ref UI_Window_TradeItemQuantity __instance, ref int value)
        {
            if (!FeralTweaks.PatchConfig.ContainsKey("TradeItemLimit"))
                return true;

            int limit = int.Parse(FeralTweaks.PatchConfig["TradeItemLimit"]);
            int quantityAvailable = __instance._itemQuantity;
            int newQuantity = value;
            if (newQuantity > quantityAvailable)
                newQuantity = quantityAvailable;
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
        [HarmonyPatch(typeof(UI_Window_TradeItemQuantity), "BtnClicked_Increase")]
        public static bool BtnClicked_Increase(ref UI_Window_TradeItemQuantity __instance)
        {
            if (!FeralTweaks.PatchConfig.ContainsKey("TradeItemLimit"))
                return true;

            int limit = int.Parse(FeralTweaks.PatchConfig["TradeItemLimit"]);
            int quantityAvailable = __instance._itemQuantity;
            int newQuantity = __instance._chosenQuantity + 1;
            if (newQuantity > quantityAvailable)
                newQuantity = quantityAvailable;
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
            if (!FeralTweaks.PatchConfig.ContainsKey("TradeItemLimit"))
                return true;

            int limit = int.Parse(FeralTweaks.PatchConfig["TradeItemLimit"]);
            int newQuantity = __instance._chosenQuantity - 1;
            int quantityAvailable = __instance._itemQuantity;
            if (newQuantity > quantityAvailable)
                newQuantity = quantityAvailable;
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
