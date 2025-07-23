using HarmonyLib;
using UnityEngine;
using Il2CppSystem;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Injection;
using Random = System.Random;
using FeralTweaks.Actions;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class EyeBlinkingPatch
    {
        public class FT_EyeDecalBlinkApplier : MonoBehaviour
        {
            private static List<string> loadingBundles = new List<string>();
            private static Random blinkRandom = new Random();
            private int activelyRenderingEyeDecals;
            private bool destroyed;
            public bool eyeBlinkDirty;
            public ActorBase actor;

            public FT_EyeDecalBlinkApplier() : base()
            { }

            public FT_EyeDecalBlinkApplier(System.IntPtr pointer) : base(pointer)
            { }

            public void OnMUpdateBodyParts()
            {
                // Check dirty
                if (actor.IsAnyDirty)
                    return;

                // Check dirty
                bool wasDirty = eyeBlinkDirty;

                // Update
                foreach (ActorBase.ActorBodyPart part in actor._attachedBodyParts)
                    OnMUpdate(part);

                // Mark no longer dirty
                if (wasDirty)
                    eyeBlinkDirty = false;
            }

            public void OnMUpdate(ActorBase.ActorBodyPart part)
            {
                if (eyeBlinkDirty)
                {
                    // Destroy renderer
                    if (part._eyeDecalBlinkRenderTexture != null)
                    {
                        Destroy(part._eyeDecalBlinkRenderTexture);
                        part._eyeDecalBlinkRenderTexture = null;
                    }
                }

                // Load eye decal blinking
                if (part._eyeDecalsInstantiated != null && activelyRenderingEyeDecals <= 0)
                {
                    foreach (ActorBase.ActorBodyPartInstantiatedEyeDecal decal in part._eyeDecalsInstantiated)
                    {
                        // Mark dirty if the renderer is missing
                        if (part._eyeDecalBlinkRenderTexture == null)
                            eyeBlinkDirty = true;
                            
                        // Check dirty
                        if (eyeBlinkDirty)
                        {
                            // Destroy generator objects
                            if (decal.shapeBlinkSprite != null)
                            {
                                Destroy(decal.shapeBlinkSprite);
                                decal.shapeBlinkSprite = null;
                            }
                            if (decal.mirrorShapeBlinkSprite != null)
                            {
                                Destroy(decal.mirrorShapeBlinkSprite);
                                decal.mirrorShapeBlinkSprite = null;
                            }
                        }

                        // Check sprite
                        if (decal.shapeBlinkSprite == null && decal.shapeSprite != null && decal.decalEntry != null)
                        {
                            // Increase
                            activelyRenderingEyeDecals++;

                            // Load decal
                            LoadEyeDecalBlink(decal, part);
                        }
                    }
                }

                // Handle blink
                HandleEyeBlink(part);
            }

            private void LoadEyeDecalBlink(ActorBase.ActorBodyPartInstantiatedEyeDecal decal, ActorBase.ActorBodyPart part)
            {
                // Load blink decal sprite
                // Find bundle def for decal
                BaseDef eyeDecalDef = CraftableItemChartData.GetDefWithDefID(actor.Info.eyeShapeDefID);
                BundleIDDefComponent bundle = eyeDecalDef.GetComponent<BundleIDDefComponent>();
                if (bundle != null && bundle.bundle.bundlePath != "")
                {
                    // Get blink bundle
                    string decalBlinkAsset = bundle.bundle.bundlePath + "_Blink";
                    ManifestDef def = ManifestChartData.GetDefWithPathAndQualityLevel(decalBlinkAsset, AssetQualityLevel.None, false);
                    if (def != null)
                    {
                        lock (loadingBundles)
                        {
                            // Add to loading
                            if (!loadingBundles.Contains(decalBlinkAsset))
                            {
                                // Load blink bundle
                                loadingBundles.Add(decalBlinkAsset);
                                GCR.instance.StartCoroutine(part.parentActorBase.gameObject.LoadBundledAssetAndWait(decalBlinkAsset, (Action<Sprite>)new System.Action<Sprite>(sprite =>
                                {
                                    // Check destroyed
                                    if (destroyed)
                                    {
                                        // Remove from loading
                                        lock (loadingBundles)
                                        {
                                            loadingBundles.Remove(decalBlinkAsset);
                                        }
                                        
                                        // Decrease loading counter
                                        activelyRenderingEyeDecals--;
                                        return;
                                    }

                                    // Sprite loaded, create main sprite object
                                    GameObject renderObjMain = new GameObject(eyeDecalDef.defName + "_Blink");
                                    SpriteRenderer decalRendererMain = renderObjMain.AddComponent<SpriteRenderer>();
                                    decalRendererMain.material = ActorBase._eyeShapeSharedMaterial;
                                    decalRendererMain.sprite = sprite;
                                    decal.shapeBlinkSprite = renderObjMain;
                                    renderObjMain.layer = GetLayer.Decal(false);
                                    renderObjMain.transform.parent = part._eyeDecalSpriteGroup;

                                    // Set color
                                    decalRendererMain.material.SetVector("_MainTexHSV", actor.Info.EyeShapeColorHSV.ToVector4());

                                    // Move
                                    renderObjMain.transform.localPosition = decal.decalEntry.position;
                                    renderObjMain.transform.localPosition = decal.decalEntry.position;
                                    renderObjMain.transform.localScale = Vector3.one * CoreSharedUtils.Map(actor.Info.eyeShapeScale, 0f, 1f, 0.8f, 1.2f) * decal.decalEntry.Scale;
                                    renderObjMain.transform.localEulerAngles = new Vector3(0f, 0f, decal.decalEntry.Rotation + decal.decalEntry.RotationCompensation);
                                    decalRendererMain.flipX = decal.decalEntry.flipX;
                                    decalRendererMain.flipY = decal.decalEntry.flipY;

                                    // Create mirror sprite object
                                    GameObject rendererObjMirror = null;
                                    SpriteRenderer decalRendererMirror = null;
                                    if (decal.decalEntry.mirror)
                                    {
                                        // Create sprite object
                                        rendererObjMirror = new GameObject(eyeDecalDef.defName + "_Blink");
                                        decalRendererMirror = rendererObjMirror.AddComponent<SpriteRenderer>();
                                        decalRendererMirror.material = ActorBase._eyeShapeSharedMaterial;
                                        decalRendererMirror.sprite = sprite;
                                        decal.mirrorShapeBlinkSprite = rendererObjMirror;
                                        rendererObjMirror.layer = GetLayer.Decal(false);
                                        rendererObjMirror.transform.parent = part._eyeDecalSpriteGroup;

                                        // Set color
                                        decalRendererMirror.material.SetVector("_MainTexHSV", actor.Info.EyeShapeColorHSV.ToVector4());

                                        // Move
                                        rendererObjMirror.transform.localPosition = decal.decalEntry.mirroredPosition;
                                        rendererObjMirror.transform.localScale = Vector3.one * CoreSharedUtils.Map(actor.Info.eyeShapeScale, 0f, 1f, 0.8f, 1.2f) * decal.decalEntry.Scale;
                                        rendererObjMirror.transform.localEulerAngles = new Vector3(0f, 0f, -decal.decalEntry.Rotation + decal.decalEntry.MirrorRotationCompensation);
                                        decalRendererMirror.flipX = !decal.decalEntry.flipX;
                                        decalRendererMirror.flipY = decal.decalEntry.flipY;
                                    }

                                    // Remove from loading
                                    lock (loadingBundles)
                                    {
                                        loadingBundles.Remove(decalBlinkAsset);
                                    }

                                    // Check if we need to render the eye decals
                                    if (activelyRenderingEyeDecals - 1 <= 0)
                                    {
                                        // Render decals
                                        if (part._eyeDecalBlinkRenderTexture == null)
                                        {
                                            // Create renderer
                                            RenderTexture rendererBlink = new RenderTexture(actor.DecalResolution, actor.DecalResolution, 0);
                                            rendererBlink.wrapMode = TextureWrapMode.Repeat;
                                            rendererBlink.name = actor.ClassDef.defName + "_" + part.bodyPartNode.defName + "_ActorEyeBlinkDecal";

                                            // Assign renderer
                                            part._eyeDecalBlinkRenderTexture = rendererBlink;


                                            // Open generator
                                            part._eyeDecalParentGroup.SetActive(true);
                                            part._eyeDecalSpriteGroup.gameObject.SetActive(true);
                                            part._eyeDecalCamera.gameObject.SetActive(true);

                                            // Switch off the eye decal game objects of the decal generator
                                            foreach (ActorBase.ActorBodyPartInstantiatedEyeDecal decal in part._eyeDecalsInstantiated)
                                            {
                                                if (decal.mirrorPupilSprite != null)
                                                    decal.mirrorPupilSprite.gameObject.SetActive(false);
                                                if (decal.mirrorShapeBlinkSprite != null)
                                                    decal.mirrorShapeBlinkSprite.gameObject.SetActive(false);
                                                if (decal.mirrorShapeClipSprite != null)
                                                    decal.mirrorShapeClipSprite.gameObject.SetActive(false);
                                                if (decal.mirrorShapeSprite != null)
                                                    decal.mirrorShapeSprite.gameObject.SetActive(false);
                                                if (decal.pupilSprite != null)
                                                    decal.pupilSprite.gameObject.SetActive(false);
                                                if (decal.shapeBlinkSprite != null)
                                                    decal.shapeBlinkSprite.gameObject.SetActive(false);
                                                if (decal.shapeClipSprite != null)
                                                    decal.shapeClipSprite.gameObject.SetActive(false);
                                                if (decal.shapeSprite != null)
                                                    decal.shapeSprite.gameObject.SetActive(false);
                                            }

                                            // Configure camera
                                            part._eyeDecalCamera.enabled = true;
                                            part._eyeDecalCamera.targetTexture = rendererBlink;
                                            part._eyeDecalCamera.clearFlags = CameraClearFlags.SolidColor;
                                            part._eyeDecalCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);

                                            // Render empty texture
                                            part._eyeDecalCamera.Render();

                                            // Enable blink sprites
                                            foreach (ActorBase.ActorBodyPartInstantiatedEyeDecal decal in part._eyeDecalsInstantiated)
                                            {
                                                if (decal.mirrorShapeBlinkSprite != null)
                                                    decal.mirrorShapeBlinkSprite.gameObject.SetActive(true);
                                                if (decal.shapeBlinkSprite != null)
                                                    decal.shapeBlinkSprite.gameObject.SetActive(true);
                                            }

                                            // Make camera save to texture renderer
                                            part._eyeDecalCamera.targetTexture = rendererBlink;

                                            // Render decal texture
                                            part._eyeDecalCamera.Render();

                                            // Hide generator
                                            part._eyeDecalCamera.enabled = false;
                                            part._eyeDecalParentGroup.SetActive(false);

                                            // Disable blink sprites
                                            foreach (ActorBase.ActorBodyPartInstantiatedEyeDecal decal in part._eyeDecalsInstantiated)
                                            {
                                                if (decal.mirrorShapeBlinkSprite != null)
                                                    decal.mirrorShapeBlinkSprite.gameObject.SetActive(false);
                                                if (decal.shapeBlinkSprite != null)
                                                    decal.shapeBlinkSprite.gameObject.SetActive(false);
                                            }
                                        }
                                    }

                                    // Decrease loading counter
                                    activelyRenderingEyeDecals--;
                                }), AssetQualityLevel.None, BundlePriority.Normal));
                            }
                            else
                            {
                                // Schedule load for when the other eye decal thats currently loading is done
                                FeralTweaksActions.Unity.Oneshot(() =>
                                {
                                    // Check destroyed
                                    if (destroyed)
                                    {
                                        // Decrease loading counter
                                        activelyRenderingEyeDecals--;

                                        // Stop loop
                                        return true;
                                    }

                                    // Check done
                                    lock (loadingBundles)
                                    {
                                        if (loadingBundles.Contains(decalBlinkAsset))
                                        {
                                            // Still loading
                                            return false;
                                        }
                                    }

                                    // Done, re-run LoadEyeDecalBlink
                                    LoadEyeDecalBlink(decal, part);

                                    // Return
                                    return true;
                                });
                            }
                        }
                    }
                    else
                    {
                        // Decrease loading counter
                        activelyRenderingEyeDecals--;
                    }
                }
                else
                {
                    // Decrease loading counter
                    activelyRenderingEyeDecals--;
                }
            }

            private void HandleEyeBlink(ActorBase.ActorBodyPart part)
            {
                // Check part
                if (part._eyeDecalBlinkRenderTexture != null && part._eyeDecalRenderTexture != null)
                {
                    // Blinking logic

                    // Check sleep
                    if (actor.BodyAnimator != null && actor.BodyAnimator.GetCurrentAnimatorStateInfo(0).IsName("Action_Sleep") && actor.BodyAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
                    {
                        // Sleep logic
                        if (!part._eyeBlinkState)
                        {
                            // Close eyes
                            foreach (Material mat in part.instanceMaterials)
                                mat.SetTexture(ActorBase.Decal2Tex, part._eyeDecalBlinkRenderTexture);
                        }

                        // Make sure the eyes open again after sleeping ends
                        part._eyeBlinkTimer = 0;
                        part._eyeBlinkState = true;
                    }
                    else
                    {
                        // Count timer down
                        part._eyeBlinkTimer -= Time.deltaTime;

                        // Check timer
                        if (part._eyeBlinkTimer <= 0)
                        {
                            // Switch blink state
                            if (!part._eyeBlinkState)
                            {
                                // Blink
                                foreach (Material mat in part.instanceMaterials)
                                    mat.SetTexture(ActorBase.Decal2Tex, part._eyeDecalBlinkRenderTexture);

                                // Open after 0.25 seconds
                                part._eyeBlinkTimer = 0.25f;
                            }
                            else
                            {
                                // Switch back to open eyes
                                foreach (Material mat in part.instanceMaterials)
                                    mat.SetTexture(ActorBase.Decal2Tex, part._eyeDecalRenderTexture);

                                // Blink again after 2-7 seconds
                                part._eyeBlinkTimer = 2f + blinkRandom.Next(0, 5);
                            }
                            part._eyeBlinkState = !part._eyeBlinkState;
                        }
                    }
                }
            }

            public void OnDestroy()
            {
                destroyed = true;
            }
        }

        private static bool inited;
        private static void Init()
        {
            if (inited)
                return;
            inited = true;

            // Inject classes
            ClassInjector.RegisterTypeInIl2Cpp<FT_EyeDecalBlinkApplier>();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActorBase), "MOnEnable")]
        public static void MOnEnable(ActorBase __instance)
        {
            // Init if needed
            Init();

            // Add blink
            if (__instance.gameObject.GetComponent<FT_EyeDecalBlinkApplier>() == null)
            {
                FT_EyeDecalBlinkApplier blinkApplier = __instance.gameObject.AddComponent<FT_EyeDecalBlinkApplier>();
                blinkApplier.actor = __instance;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActorBase), "MUpdateBodyParts")]
        public static void MUpdateBodyParts(ActorBase __instance)
        {
            // Init if needed
            Init();
            
            // Call blink update
            FT_EyeDecalBlinkApplier applier = __instance.gameObject.GetComponent<FT_EyeDecalBlinkApplier>();
            if (applier != null)
                applier.OnMUpdateBodyParts();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActorBase), "SetDirty")]
        public static void SetDirty(ActorBase __instance, ActorInfoDirtyType inType, bool inValue)
        {
            // Init if needed
            Init();
            
            // Mark blink dirty if needed
            FT_EyeDecalBlinkApplier applier = __instance.gameObject.GetComponent<FT_EyeDecalBlinkApplier>();
            if (applier != null)
            {
                switch (inType)
                {

                    case ActorInfoDirtyType.EyeAddRemove:
                    case ActorInfoDirtyType.EyeShapeSprite:
                    case ActorInfoDirtyType.EyePupilSprite:
                    case ActorInfoDirtyType.EyeShapePosition:
                    case ActorInfoDirtyType.EyePupilPosition:
                    case ActorInfoDirtyType.EyeShapeColor:
                    case ActorInfoDirtyType.EyePupilColor:
                        applier.eyeBlinkDirty = true;
                        break;

                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActorBase), "SetAllDirty")]
        public static void SetAllDirty(ActorBase __instance, bool inValue)
        {
            // Init if needed
            Init();
            
            // Mark blink dirty if needed
            FT_EyeDecalBlinkApplier applier = __instance.gameObject.GetComponent<FT_EyeDecalBlinkApplier>();
            if (applier != null)
                applier.eyeBlinkDirty = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActorBase), "ApplyMaterialKeywords")]
        public static void ApplyMaterialKeywords(ActorBase __instance)
        {
            // Init if needed
            Init();
            
            // Realtime decals
            foreach (Material material in __instance.AllBodyPartMaterials)
                material.EnableKeyword("REALTIMEDECALS");
        }

    }
}