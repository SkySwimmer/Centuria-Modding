using System.Collections.Generic;
using FeralTweaks.BundleInjection;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public static class DragonSparkSkeletonPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActorBase), "MUpdateOffsetTransform")]
        public static void BuildMUpdateOffsetTransformMover(ActorBase __instance)
        {
            // Check object type
            if (__instance != null)
            {
                // Check if its a dragon
                if (__instance.gameObject != null && __instance.Info.actorClassDefID == "5035")
                {
                    // Find skeleton attachment for the spark
                    GameObject sparkAttachment = GetChild(__instance.BodySkeletonGameNodeJoint.gameObject, "Root/Spine01/Spine02/Spine03/Up_Back/Neck01/Neck02/Neck03/Head/Head_Spark/Head_Spark (BodyPart Avatar/Dragon/Spark/Spark000)");
                    if (sparkAttachment != null)
                    {
                        // Move it to the center of the head
                        sparkAttachment.transform.localPosition = new Vector3(0.0078f, 0, 0);
                    }
                }
            }
        } 

        private static GameObject GetChild(GameObject parent, string name)
        {
            if (name.Contains("/"))
            {
                string pth = name.Remove(name.IndexOf("/"));
                string ch = name.Substring(name.IndexOf("/") + 1);
                foreach (GameObject obj in GetChildren(parent))
                {
                    if (obj.name == pth || obj.name == name)
                    {
                        if (obj.name == name)
                            return obj; 
                        GameObject t = GetChild(obj, ch);
                        if (t != null)
                            return t;
                    }
                }
                return null;
            }
            Transform tr = parent.transform;
            Transform[] trs = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in trs)
            {
                if (t.name == name && t.parent == tr.gameObject.transform)
                {
                    return t.gameObject;
                }
            }
            return null;
        }

        private static GameObject[] GetChildren(this GameObject parent)
        {
            Transform tr = parent.transform;
            List<GameObject> children = new List<GameObject>();
            Transform[] trs = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform trCh in trs)
            {
                if (trCh.parent == tr.gameObject.transform)
                    children.Add(trCh.gameObject);
            }
            return children.ToArray();
        }    
    }
}