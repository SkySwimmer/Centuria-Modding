using FeralTweaks.Actions;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class PlayerLoginLogoutAnimsPatch
    {
        public static bool IsJoiningWorld;
        public class FT_AvatarTeleportAnimator : MonoBehaviour
        {
            public Avatar_Network avatar;
            private bool despawned;
            public bool spawned;
            private bool wantsToSpawn;
            private bool wantsToDespawn;
            private bool wantsToDestroy;
            private bool blipWasEnabledGlobal;
            private List<WorldObjectInfoMessage> pendingWorldObjectInfos = new List<WorldObjectInfoMessage>();
            public bool expectFirstMove = false;
            private bool doCallOriginal;
            public bool busy;
            private bool expectBuildComplete;
            public Vector3 posOriginal;
            public Quaternion rotOriginal;

            public FT_AvatarTeleportAnimator(IntPtr pointer) : base(pointer)
            {
            }

            public void WantToSpawn()
            {
                wantsToSpawn = true;
            }

            public void OnFinishedBuilding()
            {
                // Check
                if (expectBuildComplete)
                {
                    // Disable build handler
                    expectBuildComplete = false;

                    // Teleport in
                    TeleportIn();
                }
            }

            private void TeleportIn()
            {
                // Clear action
                avatar._nextActionType = ActorActionType.None;
                avatar._nextActionBreakLoop = true;

                // Hide from minimap
                MinimapBlip blip = avatar.gameObject.GetComponent<MinimapBlip>();
                bool blipWasEnabled = false;
                if (blip != null)
                {
                    blipWasEnabled = blip.enabled;
                    blip.enabled = false;
                }

                // Hide avatar
                bool readyToShow = false;
                avatar.transform.position = new Vector3(0, -10000, 0);

                // Teleport in
                GCR.instance.StartCoroutine(avatar.TransitionArrival(false, false, "teleport"));
                FeralTweaksActions.Unity.Oneshot(() =>
                {
                    if (avatar == null || avatar.transform == null)
                        return true; // Crashed
                        
                    avatar.transform.rotation = rotOriginal;
                    if (!readyToShow && (avatar.transform.position.x != 0 || avatar.transform.position.y != -10000 || avatar.transform.position.z != 0))
                    {
                        // Update original position and move out of view
                        avatar.transform.position = new Vector3(0, -10000, 0);
                    }

                    // Wait for transition
                    if (!avatar.IsTransitionArriving)
                        return false;

                    // Run
                    long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    FeralTweaksActions.Unity.Oneshot(() =>
                    {
                        if (avatar == null || avatar.transform == null)
                            return true; // Crashed

                        if (!readyToShow && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start >= 500)
                        {
                            readyToShow = true;
                            avatar.transform.rotation = rotOriginal;
                            avatar.transform.position = posOriginal;
                        }
                        else
                        {
                            avatar.transform.rotation = rotOriginal;
                            if (!readyToShow && (avatar.transform.position.x != 0 || avatar.transform.position.y != -10000 || avatar.transform.position.z != 0))
                            {
                                // Update original position and move out of view
                                avatar.transform.position = new Vector3(0, -10000, 0);
                                return false;
                            }
                            else if (readyToShow)
                                avatar.transform.position = posOriginal;
                        }

                        // Wait for transition
                        if (avatar.IsTransitionArriving)
                            return false;

                        // Done spawning
                        avatar.transform.rotation = rotOriginal;
                        avatar.transform.position = posOriginal;
                        wantsToSpawn = false;
                        busy = false;

                        // Show on minimap
                        if (blip != null)
                        {
                            blip.enabled = blipWasEnabled;
                            if (blipWasEnabledGlobal)
                                blip.enabled = true;
                            blipWasEnabledGlobal = false;
                        }

                        // Check despawn
                        if (wantsToDespawn)
                        {
                            // Despawn
                            DespawnAvatar();
                        }
                        else 
                        {
                            WorldObjectInfoMessage msg = null;
                            lock (pendingWorldObjectInfos)
                            {
                                // Check if done
                                if (pendingWorldObjectInfos.Count != 0)
                                {
                                    // Continue spawning
                                    msg = pendingWorldObjectInfos[0];
                                    pendingWorldObjectInfos.RemoveAt(0);
                                }
                            }
                            if (msg != null)
                            {
                                spawned = true;
                                SpawnAvatar(msg);
                            }
                        }

                        // Return
                        return true;
                    });

                    // Return
                    return true;
                });
            }

            public bool SpawnAvatar(WorldObjectInfoMessage worldObjectInfo)
            {
                // Call original if needed
                if (doCallOriginal)
                    return false;

                // Check busy
                if (busy)
                {
                    // Queue
                    wantsToSpawn = true;
                    addInfo(worldObjectInfo);
                    return true;
                }

                // Check type
                if ((worldObjectInfo != null && worldObjectInfo.LastMove.nodeType != WorldObjectMoverNodeType.InitPosition) || IsJoiningWorld)
                {
                    // Mark spawned
                    spawned = true;
                    return false; // Dont handle non-default, they arent newly joined players
                }

                // Check if already spawned
                if (spawned)
                {
                    // Despawn first
                    wantsToSpawn = true;
                    DespawnAvatar(false, worldObjectInfo);
                    return true;
                }
                busy = true;
                spawned = true;

                // Hide from minimap
                MinimapBlip blip = avatar.gameObject.GetComponent<MinimapBlip>();
                if (blip != null && blip.enabled)
                {
                    blipWasEnabledGlobal = blip.enabled;
                    blip.enabled = false;
                }

                // Teleport in
                avatar.transform.position = new Vector3(0, -10000, 0);
                if (worldObjectInfo != null)
                {
                    // Rebuild avi
                    expectBuildComplete = true;
                    doCallOriginal = true;
                    expectFirstMove = true;
                    avatar.OnObjectInfo(worldObjectInfo);
                    doCallOriginal = false;
                }
                else
                {
                    // Run teleporter without rebuilding the avi
                    TeleportIn();
                }

                // Return
                return true;
            }

            public bool DespawnAvatar(bool deleteObject = true, WorldObjectInfoMessage returningMessage = null)
            {
                // Call original if needed
                if (doCallOriginal)
                    return false;

                // Check if busy
                if (busy)
                {
                    // Queue
                    if (deleteObject)
                        wantsToDestroy = true;
                    wantsToDespawn = true;
                    return true;
                }

                // Check if despawned
                if (despawned)
                    return false;
                if (deleteObject)
                    despawned = true;
                if (deleteObject)
                    wantsToDestroy = true;
                expectFirstMove = false;
                spawned = false;
                busy = true;

                // Clear action
                avatar._nextActionType = ActorActionType.None;
                avatar._nextActionBreakLoop = true;

                // Teleport away
                GCR.instance.StartCoroutine(avatar.TransitionDeparture(false, false, "teleport"));
                FeralTweaksActions.Unity.Oneshot(() =>
                {
                    // Wait for transition
                    if (!avatar.IsTransitionDeparting)
                        return false;

                    // Run
                    FeralTweaksActions.Unity.Oneshot(() =>
                    {
                        // Wait for transition
                        if (avatar.IsTransitionDeparting)
                            return false;
                        if (avatar.transform == null)
                            return true;

                        // Hide from minimap
                        MinimapBlip blip = avatar.gameObject.GetComponent<MinimapBlip>();
                        if (blip != null && blip.enabled)
                        {
                            blipWasEnabledGlobal = blip.enabled;
                            blip.enabled = false;
                        }

                        // Wait an extra second
                        bool readyToShow = false;
                        long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        avatar.transform.position = new Vector3(0, -10000, 0);
                        FeralTweaksActions.Unity.Oneshot(() =>
                        {
                            if (!readyToShow && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start >= 1000)
                            {
                                readyToShow = true;
                                avatar.transform.rotation = rotOriginal;
                                avatar.transform.position = posOriginal;
                                return false;
                            }
                            else
                            {
                                avatar.transform.rotation = rotOriginal;
                                if (!readyToShow && (avatar.transform.position.x != 0 || avatar.transform.position.y != -10000 || avatar.transform.position.z != 0))
                                {
                                    // Update original position and move out of view
                                    avatar.transform.position = new Vector3(0, -10000, 0);
                                    return false;
                                }
                                else if (readyToShow)
                                    avatar.transform.position = posOriginal;
                            }

                            // Reset
                            avatar.transform.rotation = rotOriginal;
                            avatar.transform.position = posOriginal;

                            // Delete object
                            if (wantsToDestroy)
                            {
                                doCallOriginal = true;
                                avatar.Delete();
                                doCallOriginal = false;
                            }
                            else if (wantsToSpawn)
                                despawned = false;
                            busy = false;

                            // Check if wanting to spawn
                            if (wantsToSpawn && returningMessage != null && !wantsToDestroy)
                            {
                                // Spawn
                                SpawnAvatar(returningMessage);
                            }

                            // Return
                            return true;
                        });

                        // Return
                        return true;
                    });

                    // Return
                    return true;
                });

                // Return
                return true;
            }

            private void addInfo(WorldObjectInfoMessage info)
            {
                if (info == null)
                    return;
                lock (pendingWorldObjectInfos)
                {
                    // Add
                    pendingWorldObjectInfos.Add(info);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActorBase), "MUpdateOffsetTransform")]
        public static void BuildMUpdateOffsetTransformMover(ActorBase __instance)
        {
            // Check object type
            Init();
            if (__instance != null)
            {
                Avatar_Network avatar = __instance.TryCast<Avatar_Network>();
                if (avatar != null && avatar.gameObject != null)
                {
                    // Find avatar teleporter
                    FT_AvatarTeleportAnimator teleporter = avatar.gameObject.GetComponent<FT_AvatarTeleportAnimator>();
                    if (teleporter != null)
                        teleporter.OnFinishedBuilding();
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldXtHandler), "HandleMove")]
        public static bool OnMoveMessage(WorldObjectMoveMessage message)
        {
            // Find object
            Init();
            WorldObjectManager manager = WorldObjectManager.instance;
            if (manager._objects._objectsById.ContainsKey(message.ObjectId))
            {
                // Check object type
                WorldObject obj = manager._objects._objectsById[message.ObjectId];
                Avatar_Network avatar = obj.TryCast<Avatar_Network>();
                if (avatar != null && avatar.gameObject != null)
                {
                    // Find avatar teleporter
                    FT_AvatarTeleportAnimator teleporter = avatar.gameObject.GetComponent<FT_AvatarTeleportAnimator>();
                    if (teleporter != null && (!teleporter.spawned || teleporter.busy))
                    {
                        // Prevent interfering with teleportation process
                        teleporter.posOriginal = message.Node.position;
                        teleporter.rotOriginal = message.Node.rotation;
                        return false;
                    }

                    // Check type
                    if (teleporter != null && message.Node.nodeType == WorldObjectMoverNodeType.InitPosition && !teleporter.expectFirstMove)
                    {
                        // Teleport to destination

                        // Schedule respawn
                        teleporter.WantToSpawn();

                        // Despawn first
                        teleporter.DespawnAvatar(false);

                        // Update coordinates
                        teleporter.posOriginal = message.Node.position;
                        teleporter.rotOriginal = message.Node.rotation;

                        // Prevent default move
                        return false;
                    }
                    else
                    {
                        // Update
                        teleporter.posOriginal = message.Node.position;
                        teleporter.rotOriginal = message.Node.rotation;
                        teleporter.expectFirstMove = false;
                    }
                }
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Avatar_Network), "OnObjectInfo")]
        public static bool OnObjectInfo(ref Avatar_Network __instance, WorldObjectInfoMessage inInfoMessage)
        {
            if (inInfoMessage.DefId != "852")
                return true;

            // Find avatar teleporter
            Init();
            FT_AvatarTeleportAnimator teleporter = __instance.gameObject.GetComponent<FT_AvatarTeleportAnimator>();
            if (teleporter == null)
            {
                // Create
                teleporter = __instance.gameObject.AddComponent<FT_AvatarTeleportAnimator>();
                teleporter.avatar = __instance;
                teleporter.posOriginal = inInfoMessage.LastMove.position;
                teleporter.rotOriginal = Quaternion.Euler(new Quaternion(inInfoMessage.LastMove.rotation.x, inInfoMessage.LastMove.rotation.y, inInfoMessage.LastMove.rotation.z, inInfoMessage.LastMove.rotation.w + 180).ToEulerAngles() + new Vector3(0, 180, 0));
            }

            // Spawn
            return !teleporter.SpawnAvatar(inInfoMessage);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldObject), "Delete")]
        public static bool OnDelete(ref WorldObject __instance)
        {
            // Check object type
            Init();
            Avatar_Network avatar = __instance.TryCast<Avatar_Network>();
            if (avatar != null)
            {
                // Find avatar teleporter
                FT_AvatarTeleportAnimator teleporter = avatar.gameObject.GetComponent<FT_AvatarTeleportAnimator>();
                if (teleporter != null)
                    return !teleporter.DespawnAvatar();
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RoomManager), "OnRoomJoinSuccessResponse")]
        public static void OnRoomJoinSuccessResponse()
        {
            Init();
            IsJoiningWorld = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CoreMessageManager), "SendMessageToRegisteredListeners")]
        public static void SendMessageToRegisteredListeners(CoreMessageManager __instance, string tag, IMessage inMessage)
        {
            Init();
            WorldObjectInfoAvatarLocalMessage oial = inMessage.TryCast<WorldObjectInfoAvatarLocalMessage>();
            if (oial != null)
                IsJoiningWorld = false;
        }

        private static bool inited;
        private static void Init()
        {
            if (inited)
                return;
            inited = true;

            // Add classes
            ClassInjector.RegisterTypeInIl2Cpp<FT_AvatarTeleportAnimator>();
        }

    }
}