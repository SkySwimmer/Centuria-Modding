using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class WorldObjectManagerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldObjectManager), "OnWorldObjectInfoMessage")]
        public static bool OnWorldObjectInfoMessage(WorldObjectInfoMessage message, ref WorldObjectManager __instance)
        {
            // Fix the bug with networked objects
            WorldObject obj = null;
            if (__instance._objects._objectsById.ContainsKey(message.Id))
                obj = __instance._objects._objectsById[message.Id];
            if (obj == null && message.DefId != "852" && message.DefId != "1751")
            {
                // Find in world
                obj = QuestManager.instance.GetWorldObject(message.Id);
            }
            if (obj == null)
            {
                // Find in scene
                foreach (WorldObject wO in GameObject.FindObjectsOfType<WorldObject>())
                {
                    if (wO.Id == message.Id)
                    {
                        obj = wO;
                        break;
                    }
                }
            }
            if (obj == null)
            {
                // Create
                obj = __instance.CreateObject(message);
            }

            // Add if needed
            if (!__instance._objects._objectsById.ContainsKey(message.Id))
                __instance._objects.Add(obj);

            // Load object
            obj.OnObjectInfo(message);

            // Patch
            if (FeralTweaks.PatchConfig.GetValueOrDefault("JiggleResourceInteractions", "false").ToLower() == "true")
            {
                NetworkedObjectInfo info = obj.gameObject.GetComponent<NetworkedObjectInfo>();
                if (info != null && info.actorType == NetworkedObjectInfo.EActorType.harvestItem)
                {
                    Interactable inter = obj.gameObject.gameObject.GetComponent<Interactable>();
                    if (inter != null)
                    {
                        // Set jiggle if not a skyjelly
                        if (inter.interactableDefId != "28486" && inter.interactableDefId != "28512" && inter.interactableDefId != "5615")
                            inter._jiggleWhileInteracting = true;
                    }
                }
            }

            // Override position and rotation
            if ((FeralTweaks.PatchConfig.ContainsKey("OverrideReplicate-" + message.Id) && FeralTweaks.PatchConfig["OverrideReplicate-" + message.Id].ToLower() == "true") || (FeralTweaks.PatchConfig.ContainsKey("EnableReplication") && FeralTweaks.PatchConfig["EnableReplication"].ToLower() == "true" && (!FeralTweaks.PatchConfig.ContainsKey("OverrideReplicate-" + message.Id) || FeralTweaks.PatchConfig["OverrideReplicate-" + message.Id].ToLower() != "false")))
            {
                // Only do this if its enabled, otherwise it can be buggy
                obj.transform.position = new Vector3(message.LastMove.position.x, message.LastMove.position.y, message.LastMove.position.z);
                obj.transform.rotation = new Quaternion(message.LastMove.rotation.x, message.LastMove.rotation.y, message.LastMove.rotation.z, message.LastMove.rotation.w);

                // Move the npc if its a npc
                try
                {
                    ActorNPCSpawner spawner = FeralTweaksNetworkHandler.GetNpcSpawnerFrom(obj);
                    if (spawner != null)
                    {
                        // Move
                        spawner.ActorBase.gameObject.transform.position = new Vector3(message.LastMove.position.x, message.LastMove.position.y, message.LastMove.position.z);
                        spawner.ActorBase.gameObject.transform.rotation = new Quaternion(message.LastMove.rotation.x, message.LastMove.rotation.y, message.LastMove.rotation.z, message.LastMove.rotation.w);
                    }
                }
                catch
                {
                    // Il2cpp or unity goof
                }
            }
            return false;
        }
    }
}