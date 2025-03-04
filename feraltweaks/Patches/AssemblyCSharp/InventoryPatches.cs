using HarmonyLib;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using Il2CppInterop.Runtime.Injection;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Threading;
using FeralTweaks.Mods;
using FeralTweaks;
using Newtonsoft.Json;
using WW.Waiters;
using StrayTech;
using Il2CppInterop.Runtime;
using FeralTweaks.Mods.Charts;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class InventoryPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MyUserInfo), "ParseLoginData")]
        public static void ParseLoginData(MyUserInfo __instance)
        {
            // Add default client-side-controlled items to inventory
            // Find all item defs with the component AlwaysInClientInventoryDefComponent
            foreach (BaseDef def in CoreChartDataManagerPatch.DefCache.Values)
            {
                // Get component
                AlwaysInClientInventoryDefComponent autoAddittonRules = def.GetComponent<AlwaysInClientInventoryDefComponent>();
                if (autoAddittonRules != null)
                {
                    // Check if it requires items to be present
                    if (!autoAddittonRules.requireOwnedItems)
                    {
                        // Add
                        autoAddittonRules.AddToInventory(__instance.Inventory);
                    }
                }
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Inventory), "AddFromServer")]
        public static void AddFromServer(Inventory __instance, Item item)
        {
            // Handle added item
            
            // Find all item defs with the component AlwaysInClientInventoryDefComponent
            // This time, with requireOwnedItems enabled, and check if all needed items are present
            // If so, add the item to the inventory
            foreach (BaseDef def in CoreChartDataManagerPatch.DefCache.Values)
            {
                // Get component
                AlwaysInClientInventoryDefComponent autoAddittonRules = def.GetComponent<AlwaysInClientInventoryDefComponent>();
                if (autoAddittonRules != null)
                {
                    // Check if it requires items to be present
                    if (autoAddittonRules.requireOwnedItems)
                    {
                        // Verify items
                        bool valid = true;
                        foreach (string id in autoAddittonRules.requiredOwnedItemDefIDs)
                        {
                            if (id != item.defID)
                            {
                                // Check if in inventory
                                if (__instance.GetItemByDefId(id) == null)
                                {
                                    // Not present
                                    valid = false;
                                    break;
                                }
                            }
                        }

                        // Add to inventory if valid
                        if (valid)
                            autoAddittonRules.AddToInventory(__instance);
                    }
                }
            }
        }
    }
}