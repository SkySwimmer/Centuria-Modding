using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class NpcHeadRotationPatch
    {
        private static bool injected;

        public class FT_HeadRotationFixer : MonoBehaviour
        {
            public FT_HeadRotationFixer() : base() { }
            public FT_HeadRotationFixer(IntPtr ptr) : base(ptr) { }
            
            public FaceLocalPlayer rotController;

            public void FixedUpdate()
            {
                // Call update
                if (rotController != null)
                    rotController.MUpdate();
            }
        }

        public class FT_AnimationOverrideUpdater : MonoBehaviour
        {
            public FT_AnimationOverrideUpdater() : base() { }
            public FT_AnimationOverrideUpdater(IntPtr ptr) : base(ptr) { }

            public Animator animator;
            public ActorBase actor;
            public FaceLocalPlayer actorRotator;
            public bool triedRetrievingRotator = false;

            public long lastUpdate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            public float rotationAnimLast;

            public void Update()
            {
                if (animator != null)
                {
                    // Update animator
                    if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastUpdate >= 16)
                    {
                        // Store head rotation if possible
                        Vector3 rot = new Vector3(0, 0, 0);
                        if (actorRotator != null)
                            rot = actorRotator._headNode.localEulerAngles - new Vector3(0, rotationAnimLast, 0); // Get current and subtract last animation vector to get look vector offset

                        // Update animator
                        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                        animator.Update((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastUpdate) / 1000);
                        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                        lastUpdate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                        // Restore head rotation if present
                        if (actorRotator != null)
                        {
                            // Add to rotation
                            rotationAnimLast = actorRotator._headNode.localEulerAngles.y;
                            actorRotator._headNode.localEulerAngles += new Vector3(0, rot.y, 0);
                        }
                    }
                    
                    // Check null
                    if (actorRotator == null && actor != null && !triedRetrievingRotator)
                    {
                        actorRotator = actor.GetComponent<FaceLocalPlayer>();
                        triedRetrievingRotator = true;
                    }
                    
                    // Call update
                    if (actorRotator != null)
                        actorRotator.MUpdate();
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FaceLocalPlayer), "MStart")]
        public static void MStart(FaceLocalPlayer __instance)
        {
            if (!injected)
            {
                ClassInjector.RegisterTypeInIl2Cpp<FT_HeadRotationFixer>();
                ClassInjector.RegisterTypeInIl2Cpp<FT_AnimationOverrideUpdater>();
            }
            injected = true;

            // Add if missing
            __instance.gameObject.AddComponent<FT_HeadRotationFixer>().rotController = __instance;

            // Find animation controller
            ActorBase actor = __instance.gameObject.GetComponent<ActorBase>();
            if (actor != null && actor.BodyAnimator != null && actor.BodyAnimator.gameObject != null)
            {
                // Get updater
                FT_AnimationOverrideUpdater updater = actor.BodyAnimator.gameObject.GetComponent<FT_AnimationOverrideUpdater>();
                if (updater == null)
                    updater = actor.BodyAnimator.gameObject.AddComponent<FT_AnimationOverrideUpdater>();
                updater.actorRotator = __instance;
                updater.triedRetrievingRotator = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActorBase), "InitAnimation")]
        public static void InitAnimation(ref ActorBase __instance)
        {
            if (__instance.BodyAnimator != null && __instance.TryCast<NPCBase>() != null && __instance.BodyAnimator.gameObject != null)
            {
                // Configure animators
                __instance.BodyAnimator.updateMode = AnimatorUpdateMode.Normal;
                __instance.BodyAnimator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                // Add animator controller or update if needed
                FT_AnimationOverrideUpdater updater = __instance.BodyAnimator.gameObject.GetComponent<FT_AnimationOverrideUpdater>();
                if (updater == null)
                    updater = __instance.BodyAnimator.gameObject.AddComponent<FT_AnimationOverrideUpdater>();
                updater.actor = __instance;
                updater.animator = __instance.BodyAnimator;
                if (updater.actorRotator == null)
                    updater.actorRotator = __instance.gameObject.GetComponent<FaceLocalPlayer>();
                updater.triedRetrievingRotator = false;
            }
        }
    }
}