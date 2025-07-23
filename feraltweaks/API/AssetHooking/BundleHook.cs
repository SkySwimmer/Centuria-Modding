using UnityEngine;
using Il2CppSystem;
using System.Collections.Generic;
using Il2CppInterop.Runtime;

namespace FeralTweaks.BundleInjection
{
    /// <summary>
    /// Asset Bundle Hook - Hooks into asset bundles loaded by Fer.al
    /// </summary>
    public abstract class BundleHook
    {
        private static List<BundleHook> bundleHooks = new List<BundleHook>();
        private List<InjectedAsset> assets = new List<InjectedAsset>();
        private List<AssetHook> hooks = new List<AssetHook>();
        private bool setup;

        /// <summary>
        /// Registers asset bundle hooks
        /// </summary>
        /// <param name="hook">Asset bundle hook to register</param>
        [System.Obsolete("Use ... instead")] // FIXME: figure out replacement
        public static void RegisterBundleHook(BundleHook hook)
        {
            if (!hook.setup)
            {
                hook.Setup();
                hook.setup = true;
            }
            bundleHooks.Add(hook);
        }

        /// <summary>
        /// Retrieves all registered asset bundle hooks
        /// </summary>
        /// <returns>Array of BundleHook instances</returns>
        public static BundleHook[] GetRegisteredBundleHooks()
        {
            return bundleHooks.ToArray();
        }

        /// <summary>
        /// Defines the bundle path to inject into
        /// </summary>
        public abstract string BundlePath { get; }

        /// <summary>
        /// Called to set up the bundle hook
        /// </summary>
        public abstract void Setup();

        /// <summary>
        /// Hooks into loading of assets
        /// </summary>
        /// <param name="assetName">Name of the asset to hook into</param>
        /// <param name="type">Type of the asset to hook into</param>
        /// <param name="hook">Asset load hook callback</param>
        protected void HookAsset(string assetName, Type type, AssetHook.AssetHookCallback hook)
        {
            hooks.Add(new AssetHook(assetName, type, hook));
        }

        /// <summary>
        /// Hooks into loading of assets
        /// </summary>
        /// <param name="assetName">Name of the asset to hook into</param>
        /// <param name="hook">Asset load hook callback</param>
        protected void HookAsset<T>(string assetName, AssetHook.AssetHookCallback<T> hook) where T : UnityEngine.Object
        {
            hooks.Add(new AssetHook(assetName, Il2CppType.From(typeof(T)), (def, bundle, assetName, type, asset) =>
            {
                // Cast
                T tRes = asset.TryCast<T>();
                if (tRes != null)
                    hook(def, bundle, assetName, type, tRes);
            }));
        }

        /// <summary>
        /// Injects assets
        /// </summary>
        /// <param name="assetName">Name of the asset to inject</param>
        /// <param name="type">Type of the asset to inject</param>
        /// <param name="hook">Asset load hook callback</param>
        protected void InjectAsset(string assetName, Type type, InjectedAsset.AssetLoader hook)
        {
            assets.Add(new InjectedAsset(assetName, type, hook));
        }

        /// <summary>
        /// Injects assets
        /// </summary>
        /// <param name="assetName">Name of the asset to inject</param>
        /// <param name="type">Type of the asset to inject</param>
        /// <param name="hook">Asset load hook callback</param>
        protected void InjectAsset<T>(string assetName, Type type, InjectedAsset.AssetLoader<T> hook) where T : UnityEngine.Object
        {
            assets.Add(new InjectedAsset(assetName, type, (def, bundle, assetName, type) =>
            {
                return hook(def, bundle, assetName, type);
            }));
        }

        /// <summary>
        /// Called when the asset bundle is loaded
        /// </summary>
        /// <param name="def">Manifest def object of the bundle</param>
        /// <param name="bundle">Asset bundle instance</param>
        public virtual void OnBundleLoaded(ManifestDef def, AssetBundle bundle) {}

        /// <summary>
        /// Retrieves all asset hooks
        /// </summary>
        /// <returns>Array of AssetHook instances</returns>
        public AssetHook[] GetAssetHooks()
        {
            return hooks.ToArray();
        }

        /// <summary>
        /// Retrieves all injected assets
        /// </summary>
        /// <returns>Array of InjectedAsset instances</returns>
        public InjectedAsset[] GetInjectedAssets()
        {
            return assets.ToArray();
        }
        
        /// <summary>
        /// Retrieves child game objects by name/path
        /// </summary>
        /// <param name="parent">Parent object</param>
        /// <param name="name">Name or path of child object to retrieve</param>
        /// <returns>GameObject instance or null</returns>
        public static GameObject GetChild(GameObject parent, string name)
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

        /// <summary>
        /// Retrieves all child objects of a game object
        /// </summary>
        /// <param name="parent">Object to retrieve the children of</param>
        /// <returns>Array of GameObject instances</returns>
        public static GameObject[] GetChildren(GameObject parent)
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