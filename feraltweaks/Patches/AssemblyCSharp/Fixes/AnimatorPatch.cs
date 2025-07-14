using HarmonyLib;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public static class AvatarSoundAndAnimPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Animator), "Update")]
        public static void Update(Animator __instance)
        {
            if (__instance.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                // Make it so animations dont go out of sync or refuse to play
                __instance.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                __instance.OnCullingModeChanged();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActorBase), "MUpdate")]
        public static void MUpdate(ActorBase __instance)
        {
            if (__instance._cachedChildRenderers != null)
            {
                foreach (Renderer r in __instance._cachedChildRenderers)
                {
                    // Make it so avatar parts dont just VANISH while at certain angles
                    SkinnedMeshRenderer rend = r.TryCast<SkinnedMeshRenderer>();
                    if (rend != null)
                        rend.updateWhenOffscreen = true;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Behaviour), "set_enabled")]
        public static bool set_enabled(Behaviour __instance)
        {
            Animator ani = __instance.TryCast<Animator>();
            if (ani != null)
            {
                // Prevent disable if an animator for the avi
                // As ww has some botched off-screen render nonsense
                if (__instance.transform.parent != null && __instance.transform.parent.parent != null && __instance.transform.parent.parent.parent != null && __instance.transform.parent.parent.parent.gameObject != null&& __instance.transform.parent.parent.parent.gameObject.GetComponent<ActorBase>() != null)
                {
                    // Prevent
                    return false;
                }
            }
            return true;
        }
    }
}