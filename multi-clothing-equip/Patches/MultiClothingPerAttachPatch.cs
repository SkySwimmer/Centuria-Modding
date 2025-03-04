using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace EarlyAccessPorts.MultiClothingEquip.Patches
{
    public class MultiClothingPerAttachPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActorInfo), "RemoveClothingItemsOnGroup")]
        public static bool RemoveClothingItemsOnGroup(ref List<ActorInfoClothingItem> __result)
        {
            // Check if enabled
            if (!MultiClothingEquipMod.PatchConfig.ContainsKey("AllowMultipleClothingItemsOfSameType") || MultiClothingEquipMod.PatchConfig["AllowMultipleClothingItemsOfSameType"].ToLower() == "false")
                return true;

            // Ignore
            __result = new List<ActorInfoClothingItem>();
            return false;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActorBase), "RemoveAttachedClothingItem")]
        public static bool RemoveAttachedClothingItem(ActorBase __instance, ActorBase.AttachedClothingItem inAttachedItem)
        {
            // Check if enabled
            if (!MultiClothingEquipMod.PatchConfig.ContainsKey("AllowMultipleClothingItemsOfSameType") || MultiClothingEquipMod.PatchConfig["AllowMultipleClothingItemsOfSameType"].ToLower() == "false")
                return true;

            // Destroy
            inAttachedItem.Destroy();

            // Remove
            __instance._attachedClothingItems.Remove(inAttachedItem);

            // Prevent original from being called
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActorBase), "AddAttachedClothingItemWithDefComponent")]
        public static bool AddAttachedClothingItemWithDefComponent(ActorBase __instance, ActorClothingDefComponent inDefComponent, ref ActorBase.AttachedClothingItem __result)
        {
            // Check if enabled
            if (!MultiClothingEquipMod.PatchConfig.ContainsKey("AllowMultipleClothingItemsOfSameType") || MultiClothingEquipMod.PatchConfig["AllowMultipleClothingItemsOfSameType"].ToLower() == "false")
                return true;

            // Check def
            if (inDefComponent == null)
            {
                // Error
                Debug.LogError("Cannot attach null clothing item");
                __result = null;
                return false;
            }
            if (inDefComponent.AttachNodeDefComponent == null)
            {
                // Error
                Debug.LogError("Cannot attach null clothing item");
                __result = null;
                return false;
            }

            // Locate attach def
            BaseDef def = inDefComponent.AttachNodeDefComponent.def;

            // If null, use first
            if (def == null)
                def = inDefComponent.AttachNodeGroupDef.AttachNodes[0];

            // Create clothing item instance
            ActorBase.AttachedClothingItem itm = new ActorBase.AttachedClothingItem();
            itm.parentActorBase = __instance;
            itm.clothingDefComponent = inDefComponent;
            itm.attachNode = inDefComponent.AttachNodeDefComponent;

            // Add
            __instance._attachedClothingItems.Add(itm);

            // Create instance
            itm.Instantiate();

            // Return
            __result = itm;

            // Prevent original from being called
            return false;
        }
    }
}