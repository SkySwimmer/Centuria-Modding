using System;
using System.Globalization;
using System.Reflection;
using CodeStage.AntiCheat.ObscuredTypes;
using FeralTweaks.Mods;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace EarlyAccessPorts.BetterGliding.Patches.AssemblyCSharp
{
    public class GlidingManagerPatch
    {

        public static float GlidingTurnSpeed = 0.1f;
        public static float GlidingGravity = 3f;
        public static bool AllowFlap = true;
        public static float FlapForce = 10f;
        public static long FlapCooldown = 200;
        public static float GlidingMaxRollAngle = 70f;
        public static float GlidingRollAmount = 12f;
        public static float GlidingSpeedMultiplier = 2f;

        public static bool WasGliding;
        public static long LastFlapTime;

        public static Vector3 lastPos;
        public static Vector3 lastAngles;
        public static long lastMove;

        private static bool patched;
        private static bool movedBackToCenter;
        private static long timeSinceGlide;
        private static Vector3 anglesAtGlide;

        private static bool inited = false;
        private static void Init()
        {
            if (inited)
                return;
            inited = true;
            
            // Load config
            if (BetterGlidingMod.PatchConfig.ContainsKey("GlidingTurnSpeed"))
                GlidingTurnSpeed = float.Parse(BetterGlidingMod.PatchConfig["GlidingTurnSpeed"], CultureInfo.InvariantCulture);
            if (BetterGlidingMod.PatchConfig.ContainsKey("GlidingGravity"))
                GlidingGravity = float.Parse(BetterGlidingMod.PatchConfig["GlidingGravity"], CultureInfo.InvariantCulture);
            if (BetterGlidingMod.PatchConfig.ContainsKey("GlidingMaxRollAngle"))
                GlidingMaxRollAngle = float.Parse(BetterGlidingMod.PatchConfig["GlidingMaxRollAngle"], CultureInfo.InvariantCulture);
            if (BetterGlidingMod.PatchConfig.ContainsKey("GlidingRollAmount"))
                GlidingRollAmount = float.Parse(BetterGlidingMod.PatchConfig["GlidingRollAmount"], CultureInfo.InvariantCulture);
            if (BetterGlidingMod.PatchConfig.ContainsKey("GlidingSpeedMultiplier"))
                GlidingSpeedMultiplier = float.Parse(BetterGlidingMod.PatchConfig["GlidingSpeedMultiplier"], CultureInfo.InvariantCulture);
            if (BetterGlidingMod.PatchConfig.ContainsKey("GlidingAllowFlap"))
                AllowFlap = BetterGlidingMod.PatchConfig["GlidingAllowFlap"].ToLower() == "true";
            if (BetterGlidingMod.PatchConfig.ContainsKey("GlidingFlapForce"))
                FlapForce = float.Parse(BetterGlidingMod.PatchConfig["GlidingFlapForce"], CultureInfo.InvariantCulture);
            if (BetterGlidingMod.PatchConfig.ContainsKey("GlidingFlapCooldown"))
                FlapCooldown = int.Parse(BetterGlidingMod.PatchConfig["GlidingFlapCooldown"]);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldXtHandler), "RequestUpdate", new Type[] { typeof(WorldObjectMoverNodeType), typeof(Vector3), typeof(Vector3), typeof(Quaternion), typeof(float), typeof(ActorActionType) })]
        public static void Sync(ref Quaternion rotation)
        {
            // Init
            Init();

            // Make it use body rotation instead so its not too janky
            Avatar_Local avatar = Avatar_Local.instance;
            if (avatar != null)
            {
                if (rotation == avatar.gameObject.transform.rotation)
                    rotation = avatar.BodyTransform.rotation;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GlidingManager), "MUpdate")]
        public static void Update(ref GlidingManager __instance)
        {
            // Init
            Init();
            
            // Apply
            __instance._glideTurnSpeed = GlidingTurnSpeed;
            __instance._glidingMaxRollAngle = GlidingMaxRollAngle;
            __instance._glidingRollAmount = GlidingRollAmount;
            __instance._maxSpeedMultiplier = GlidingSpeedMultiplier;

            // Get avatar
            Avatar_Local avatar = Avatar_Local.instance;
            if (avatar != null && avatar.Info != null)
            {
                // Flap logic
                long timeSinceLastFlap = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - LastFlapTime;
                if (timeSinceLastFlap > 500)
                {
                    // Use gravity
                    __instance._glidingGravityAmount = GlidingGravity;
                }
                else
                {
                    // Use force
                    __instance._glidingGravityAmount = -(FlapForce * 2);
                }

                // Check flap
                if (timeSinceLastFlap - 550 > FlapCooldown && (__instance.GlidingButtonDown || __instance.GlidingButton) && avatar.GlideState == ActorBase.EGlideState.Gliding && AllowFlap)
                {
                    // Flap
                    // TODO: animation
                    LastFlapTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }

                // Check movement
                if (avatar.transform.position.x <= lastPos.x + 0.001f && avatar.transform.position.x >= lastPos.x - 0.001f && avatar.transform.position.z <= lastPos.z + 0.001f && avatar.transform.position.z >= lastPos.z - 0.001f && avatar.GlideState != ActorBase.EGlideState.None)
                {
                    // Hasnt moved, check how long ago it was
                    long timeSinceMove = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastMove;
                    if (timeSinceMove > 500 && lastAngles.z != 0)
                    {
                        // Smooth roll back to 0
                        long timeRollSmoothStart = timeSinceMove - 500;
                        float rollLast = lastAngles.z;
                        if (rollLast > 180)
                            rollLast = rollLast - 360;
                        float rollStep = (rollLast > 0 ? rollLast : -rollLast) / 700f;
                        float rollResult = rollStep * timeRollSmoothStart;
                        if (rollResult > (rollLast > 0 ? rollLast : -rollLast))
                            rollResult = (rollLast > 0 ? rollLast : -rollLast);
                        if (rollLast > 0)
                            avatar.BodyTransform.localEulerAngles = new Vector3(avatar.BodyTransform.localEulerAngles.x, avatar.BodyTransform.localEulerAngles.y, rollLast - rollResult);
                        else
                            avatar.BodyTransform.localEulerAngles = new Vector3(avatar.BodyTransform.localEulerAngles.x, avatar.BodyTransform.localEulerAngles.y, rollLast + rollResult);
                    }
                    else
                        lastAngles = avatar.BodyTransform.localEulerAngles;
                }
                else
                {
                    lastMove = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    lastAngles = avatar.transform.localEulerAngles;
                }

                // Check if not gliding
                if (avatar.GlideState == ActorBase.EGlideState.None)
                    lastMove = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Check if no longer gliding
                if (avatar.GlideState == ActorBase.EGlideState.None && WasGliding)
                {
                    // Reset roll
                    movedBackToCenter = false;
                    timeSinceGlide = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    anglesAtGlide = lastAngles;
                }
                if (avatar.GlideState == ActorBase.EGlideState.None && !movedBackToCenter)
                {
                    // Smooth roll back to 0
                    long timeSinceMove = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timeSinceGlide;
                    long timeRollSmoothStart = timeSinceMove;
                    float rollLast = anglesAtGlide.z;
                    if (rollLast > 180)
                        rollLast = rollLast - 360;
                    float rollStep = (rollLast > 0 ? rollLast : -rollLast) / 700f;
                    float rollResult = rollStep * timeRollSmoothStart;
                    if (rollResult > (rollLast > 0 ? rollLast : -rollLast))
                        rollResult = (rollLast > 0 ? rollLast : -rollLast);
                    if (rollLast > 0)
                        avatar.BodyTransform.localEulerAngles = new Vector3(avatar.BodyTransform.localEulerAngles.x, avatar.BodyTransform.localEulerAngles.y, rollLast - rollResult);
                    else
                        avatar.BodyTransform.localEulerAngles = new Vector3(avatar.BodyTransform.localEulerAngles.x, avatar.BodyTransform.localEulerAngles.y, rollLast + rollResult);
                    if (lastAngles.z == 0)
                        movedBackToCenter = true;
                }

                // Save state
                WasGliding = avatar.GlideState != ActorBase.EGlideState.None;
                lastPos = avatar.OffsetTransform.position;
            }
        }
    }
}