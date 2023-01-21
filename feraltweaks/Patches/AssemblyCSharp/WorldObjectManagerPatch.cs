using BepInEx;
using BepInEx.Logging;
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

            // Override position and rotation
            if ((Plugin.PatchConfig.ContainsKey("OverrideReplicate-" + message.Id) && Plugin.PatchConfig["OverrideReplicate-" + message.Id] == "True") || (Plugin.PatchConfig.ContainsKey("EnableReplication") && Plugin.PatchConfig["EnableReplication"] == "True" && (!Plugin.PatchConfig.ContainsKey("OverrideReplicate-" + message.Id) || Plugin.PatchConfig["OverrideReplicate-" + message.Id] != "False")))
            {
                // Only do this if its enabled, otherwise it can be buggy
                obj.transform.position = new Vector3(message.LastMove.position.x, message.LastMove.position.y, message.LastMove.position.z);
                obj.transform.rotation = new Quaternion(message.LastMove.rotation.x, message.LastMove.rotation.y, message.LastMove.rotation.z, message.LastMove.rotation.w);

                // Move the npc if its a npc
                try
                {
                    ActorNPCSpawner spawner = Plugin.GetNpcSpawnerFrom(obj);
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