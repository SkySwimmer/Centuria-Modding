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
            private WorldObjectInfoMessage worldObjectInfo;
            private bool doCallOriginal;
            private bool busy;
            private bool expectBuildComplete;
            public Vector3 posOriginal;
            public Quaternion rotOriginal;

            public FT_AvatarTeleportAnimator(IntPtr pointer) : base(pointer)
            {
            }

            public void OnFinishedBuilding()
            {
                // Check
                if (expectBuildComplete)
                {
                    // Disable build handler
                    expectBuildComplete = false;

                    // Clear action
                    avatar._nextActionType = ActorActionType.None;
                    avatar._nextActionBreakLoop = true;

                    // Play sound
                    FeralAudioInfo audioInfo = new FeralAudioInfo();
                    audioInfo.eventRef = "event:/cutscenes/boundary_camera_fade_in";
                    FeralAudioBehaviour behaviour = avatar.gameObject.GetComponent<FeralAudioBehaviour>();
                    if (behaviour != null)
                        behaviour.Play(audioInfo, null, Il2CppType.Of<Il2CppSystem.Nullable<float>>().GetConstructor(new Il2CppSystem.Type[] { Il2CppType.Of<float>() }).Invoke(new Il2CppSystem.Object[] { Il2CppSystem.Single.Parse("0") }).Cast<Il2CppSystem.Nullable<float>>());

                    // Hide avatar
                    bool readyToShow = false;
                    avatar.transform.position = new Vector3(0, -1000, 0);

                    // Hide from minimap
                    MinimapBlip blip = avatar.gameObject.GetComponent<MinimapBlip>();
                    bool blipWasEnabled = false;
                    if (blip != null)
                    {
                        blipWasEnabled = blip.enabled;
                        blip.enabled = false;
                    }

                    // Teleport in
                    GCR.instance.StartCoroutine(avatar.TransitionArrival(false, false, "teleport"));
                    FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                    {
                        avatar.transform.rotation = rotOriginal;
                        if (!readyToShow && (avatar.transform.position.x != 0 || avatar.transform.position.y != -1000 || avatar.transform.position.z != 0))
                        {
                            // Update original position and move out of view
                            avatar.transform.position = new Vector3(0, -1000, 0);
                        }
                        
                        // Wait for transition
                        if (!avatar.IsTransitionArriving)
                            return false;

                        // Run
                        long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                        {
                            if (!readyToShow && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start >= 500)
                            {
                                readyToShow = true;
                                avatar.transform.rotation = rotOriginal;
                                avatar.transform.position = posOriginal;
                            }
                            else
                            {
                                avatar.transform.rotation = rotOriginal;
                                if (!readyToShow && (avatar.transform.position.x != 0 || avatar.transform.position.y != -1000 || avatar.transform.position.z != 0))
                                {
                                    // Update original position and move out of view
                                    avatar.transform.position = new Vector3(0, -1000, 0);
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
                            worldObjectInfo = null;
                            busy = false;

                            // Show on minimap
                            if (blip != null)
                            {
                                blip.enabled = blipWasEnabled;
                            }

                            // Check despawn
                            if (wantsToDespawn)
                            {
                                // Despawn
                                DespawnAvatar();
                            }

                            // Return
                            return true;
                        });

                        // Return
                        return true;
                    });
                }
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
                    this.worldObjectInfo = worldObjectInfo;
                    return true;
                }

                // Check type
                if (worldObjectInfo.LastMove.nodeType != WorldObjectMoverNodeType.InitPosition || IsJoiningWorld)
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
                    this.worldObjectInfo = worldObjectInfo;
                    DespawnAvatar(false);
                    return true;
                }
                busy = true;
                spawned = true;

                // Teleport in
                avatar.transform.position = new Vector3(0, -1000, 0);
                expectBuildComplete = true;
                doCallOriginal = true;
                avatar.OnObjectInfo(worldObjectInfo);
                doCallOriginal = false;

                // Return
                return true;
            }

            public bool DespawnAvatar(bool doDelete = true)
            {
                // Call original if needed
                if (doCallOriginal)
                    return false;

                // Check if busy
                if (busy)
                {
                    // Queue
                    wantsToDespawn = true;
                    return true;
                }

                // Check if despawned
                if (despawned)
                    return false;
                if (doDelete)
                    despawned = true;
                spawned = false;
                busy = true;

                // Clear action
                avatar._nextActionType = ActorActionType.None;
                avatar._nextActionBreakLoop = true;

                // Play sound
                FeralAudioInfo audioInfo = new FeralAudioInfo();
                audioInfo.eventRef = "event:/cutscenes/boundary_camera_fade_out";
                FeralAudioBehaviour behaviour = avatar.gameObject.GetComponent<FeralAudioBehaviour>();
                if (behaviour != null)
                    behaviour.Play(audioInfo, null, Il2CppType.Of<Il2CppSystem.Nullable<float>>().GetConstructor(new Il2CppSystem.Type[] { Il2CppType.Of<float>() }).Invoke(new Il2CppSystem.Object[] { Il2CppSystem.Single.Parse("0") }).Cast<Il2CppSystem.Nullable<float>>());

                // Teleport away
                GCR.instance.StartCoroutine(avatar.TransitionDeparture(false, false, "teleport"));
                FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                {
                    // Wait for transition
                    if (!avatar.IsTransitionDeparting)
                        return false;

                    // Run
                    FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                    {
                        // Wait for transition
                        if (avatar.IsTransitionDeparting)
                            return false;

                        // Wait an extra second
                        bool readyToShow = false;
                        long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        avatar.transform.position = new Vector3(0, -1000, 0);
                        FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
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
                                if (!readyToShow && (avatar.transform.position.x != 0 || avatar.transform.position.y != -1000 || avatar.transform.position.z != 0))
                                {
                                    // Update original position and move out of view
                                    avatar.transform.position = new Vector3(0, -1000, 0);
                                    return false;
                                }
                                else if (readyToShow)
                                    avatar.transform.position = posOriginal;
                            }

                            // Reset
                            avatar.transform.rotation = rotOriginal;
                            avatar.transform.position = posOriginal;

                            // Delete object
                            if (doDelete && !wantsToSpawn)
                            {
                                doCallOriginal = true;
                                avatar.Delete();
                                doCallOriginal = false;
                            }
                            else if (wantsToSpawn)
                                despawned = false;
                            busy = false;

                            // Check if wanting to spawn
                            if (wantsToSpawn)
                            {
                                // Spawn
                                SpawnAvatar(worldObjectInfo);
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
        }

        public static class PlayerObjectPatches
        {
            private static bool patched;
            public static class PatchesLate
            {
                [HarmonyPostfix]
                [HarmonyPatch(typeof(ActorBase), "MUpdateOffsetTransform")]
                public static void BuildMUpdateOffsetTransformMover(ActorBase __instance)
                {
                    // Check object type
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
                            teleporter.posOriginal = message.Node.position;
                            teleporter.rotOriginal = message.Node.rotation;
                            if (teleporter != null && !teleporter.spawned)
                            {
                                // Prevent interfering with teleportation process
                                return false;
                            }
                        }
                    }
                    return true;
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Avatar_Network), "OnObjectInfo")]
            public static bool OnObjectInfo(ref Avatar_Network __instance, WorldObjectInfoMessage inInfoMessage)
            {
                if (!patched)
                    Harmony.CreateAndPatchAll(typeof(PatchesLate));
                patched = true;

                // Find avatar teleporter
                FT_AvatarTeleportAnimator teleporter = __instance.gameObject.GetComponent<FT_AvatarTeleportAnimator>();
                if (teleporter == null)
                {
                    // Create
                    teleporter = __instance.gameObject.AddComponent<FT_AvatarTeleportAnimator>();
                    teleporter.avatar = __instance;
                    teleporter.posOriginal = inInfoMessage.LastMove.position;
                    teleporter.rotOriginal = new Quaternion(inInfoMessage.LastMove.rotation.x, inInfoMessage.LastMove.rotation.y, inInfoMessage.LastMove.rotation.z, inInfoMessage.LastMove.rotation.w + 180);
                }

                // Spawn
                return !teleporter.SpawnAvatar(inInfoMessage);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(WorldObject), "Delete")]
            public static bool OnDelete(ref WorldObject __instance)
            {
                // Check object type
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
                IsJoiningWorld = true;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(WorldObjectManager), "OnWorldObjectInfoAvatarLocalMessage")]
            public static void OnWorldObjectInfoAvatarLocalMessage()
            {
                IsJoiningWorld = false;
            }
        }

        private static bool patched;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CoreChartDataManager), "SetChartObjectInstances")]
        public static void SetChartObjectInstances()
        {
            if (patched)
                return;
            patched = true;

            // Add classes
            ClassInjector.RegisterTypeInIl2Cpp<FT_AvatarTeleportAnimator>();
            Harmony.CreateAndPatchAll(typeof(PlayerObjectPatches));
        }
        
    }
}