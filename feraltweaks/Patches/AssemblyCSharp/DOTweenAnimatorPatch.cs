using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class DOTweenAnimatorPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DOTweenAnimator), "OnEnable")]
        public static void OnEnable(ref DOTweenAnimator __instance)
        {
            // Check scene
            GameObject obj = __instance.gameObject;
            if (obj.scene.name == "CityFera")
            {
                // Check object
                if (obj.transform.parent != null && obj.transform.parent.gameObject.name.ToLower().Contains("floatingrocks"))
                {
                    // Check patch state
                    if (FeralTweaks.PatchConfig.GetValueOrDefault("CityFeraMovingRocks", "false").ToLower() == "true")
                    {
                        // Patch
                        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                            // Set root
                            renderer.staticBatchRootTransform = renderer.transform;
                        }
                    }

                    // Check portal patch
                    if (FeralTweaks.PatchConfig.GetValueOrDefault("CityFeraTeleporterSFX", "false").ToLower() == "true")
                    {
                        // Patch portals
                        GameObject portals = GameObject.Find("Portals");
                        if (portals != null)
                        {
                            // Find first fx object
                            GameObject fx = null;
                            foreach (GameObject child in GetChildren(portals))
                            {
                                if (GetChild(child, "FX_CF_PortalGlow") != null)
                                {
                                    fx = GetChild(child, "FX_CF_PortalGlow");
                                    break;
                                }
                            }

                            // Add to missing
                            foreach (GameObject child in GetChildren(portals))
                            {
                                if (GetChild(child, "FX_CF_PortalGlow") == null && GetChild(child, "CF_PortalFrame") != null)
                                {
                                    GameObject c = GameObject.Instantiate(fx);
                                    c.name = fx.name;
                                    c.transform.parent = child.transform;
                                    c.transform.position = child.transform.position;
                                    c.SetActive(false);
                                    c.SetActive(true);
                                    GameObject.Destroy(GetChild(c, "UpwardGlow"));
                                    GameObject.Destroy(GetChild(c, "OutwardGlow"));
                                }
                            }
                        }
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
                    if (obj.name == pth)
                    {
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

        private static GameObject[] GetChildren(GameObject parent)
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
