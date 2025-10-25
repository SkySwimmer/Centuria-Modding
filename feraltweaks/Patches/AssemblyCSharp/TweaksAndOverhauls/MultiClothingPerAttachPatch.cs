using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class MultiClothingPerAttachPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActorInfo), "RemoveClothingItemsOnGroup")]
        public static bool RemoveClothingItemsOnGroup(ref List<ActorInfoClothingItem> __result)
        {
            // Check if enabled
            if (!FeralTweaks.PatchConfig.ContainsKey("AllowMultipleClothingItemsOfSameType") || FeralTweaks.PatchConfig["AllowMultipleClothingItemsOfSameType"].ToLower() == "false")
                return true;

            // Ignore
            __result = new List<ActorInfoClothingItem>();
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActorInfo), "AddClothingItem", new System.Type[] { typeof(Item) })]
        public static bool AddClothingItem(ref Item inItem, ref ActorInfo __instance, ref Il2CppSystem.ValueTuple<ActorInfoClothingItem, List<ActorInfoClothingItem>> __result)
        {
            // Check if enabled
            if (!FeralTweaks.PatchConfig.ContainsKey("AllowMultipleClothingItemsOfSameType") || FeralTweaks.PatchConfig["AllowMultipleClothingItemsOfSameType"].ToLower() == "false")
                return true;

            // Replace implementation

            // Get components
            ColorableItemComponent colorable = inItem.GetComponent<ColorableItemComponent>();
            ActorClothingDefComponent itemDef = inItem.GetDefComponent<ActorClothingDefComponent>();
            if (colorable == null || itemDef == null)
            {
                // Missing
                __result = new Il2CppSystem.ValueTuple<ActorInfoClothingItem, List<ActorInfoClothingItem>>(null, null);
                return false;
            }

            // Check if attach group is present
            if (itemDef.AttachNodeGroupDef == null)
            {
                // Missing
                __result = new Il2CppSystem.ValueTuple<ActorInfoClothingItem, List<ActorInfoClothingItem>>(null, null);
                return false;
            }
            if (itemDef.AttachNodeDefComponent == null || !itemDef.AttachNodeGroupDef.Contains(itemDef.AttachNodeDefComponent.def))
            {
                // Missing
                __result = new Il2CppSystem.ValueTuple<ActorInfoClothingItem, List<ActorInfoClothingItem>>(null, null);
                return false;
            }

            // Add item
            ActorInfoClothingItem attachedItem = new ActorInfoClothingItem(colorable);
            __instance.AddClothingItem(attachedItem);
            __result = new Il2CppSystem.ValueTuple<ActorInfoClothingItem, List<ActorInfoClothingItem>>(attachedItem, new List<ActorInfoClothingItem>());

            // Prevent original
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActorBase), "RemoveAttachedClothingItem")]
        public static bool RemoveAttachedClothingItem(ActorBase __instance, ActorBase.AttachedClothingItem inAttachedItem)
        {
            // Check if enabled
            if (!FeralTweaks.PatchConfig.ContainsKey("AllowMultipleClothingItemsOfSameType") || FeralTweaks.PatchConfig["AllowMultipleClothingItemsOfSameType"].ToLower() == "false")
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
            if (!FeralTweaks.PatchConfig.ContainsKey("AllowMultipleClothingItemsOfSameType") || FeralTweaks.PatchConfig["AllowMultipleClothingItemsOfSameType"].ToLower() == "false")
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