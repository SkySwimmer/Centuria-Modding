using CustomizationChat.Actions;
using HarmonyLib;
using WW.Waiters;

namespace CustomizationChat.Patches
{
    public class UpdateHook
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(WaitController), "Update")]
        private static void Update(ref WaitController __instance)
        {
            // Call update
            FeralTweaksActionManager.CallUpdate();
        }
    }
}