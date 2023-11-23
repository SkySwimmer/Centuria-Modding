using HarmonyLib;
using Newtonsoft.Json;
using StrayTech;
using System.Collections.Generic;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class ActionWheelPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_Window_AvatarActionWheel), "OnOpen")]
        public static void OnOpen(UI_Window_AvatarActionWheel __instance)
        {
            // Populate item dictionary
            Dictionary<string, Item> items = new Dictionary<string, Item>();
            Il2CppSystem.Collections.Generic.List<Item> col = new Il2CppSystem.Collections.Generic.List<Item>(UserManager.Me.Inventory.GetAllOfType(ItemType.AvatarAction).Cast<Il2CppSystem.Collections.Generic.IEnumerable<Item>>());
            foreach (Item itm in col)
            {
                items[itm.defID] = itm;
            }

            // Get order
            string[] order = JsonConvert.DeserializeObject<string[]>(FeralTweaks.PatchConfig.GetValueOrDefault("DefaultAvatarActionOrder", "[8930, 9108, 9116, 9121, 9122, 9143, 9151, 9190]"));

            // Apply
            Il2CppSystem.Collections.Generic.List<UI_AvatarActionWheelItem> itms = __instance._avatarActionWheelItems;
            for (int i = 0; i < itms.Count; i++)
            {
                UI_AvatarActionWheelItem itm = itms[i];
                if (i < order.Length)
                {
                    // Check current
                    if (items.ContainsKey(order[i]))
                    {
                        // Find
                        Item action = items[order[i]];

                        // Setup
                        itm.Setup(action.GetDefComponent<AvatarActionDefComponent>());

                        // Remove
                        col.Remove(action);

                        // Continue
                        continue;
                    }
                }

                // Get and remove first
                if (col.Count > 0)
                {
                        // Find
                    Item action = col[0];

                    // Setup
                    itm.Setup(action.GetDefComponent<AvatarActionDefComponent>());

                    // Remove
                    col.Remove(action);
                }
            }
        }
    }
}
