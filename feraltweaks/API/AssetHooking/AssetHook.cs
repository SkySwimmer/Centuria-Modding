using Il2CppSystem;
using UnityEngine;

namespace FeralTweaks.BundleInjection
{
    /// <summary>
    /// Asset hook information container
    /// </summary>
    public class AssetHook
    {
        internal AssetHook(string name, Type type, AssetHookCallback hook)
        {
            AssetName = name;
            AssetType = type;
            Hook = hook;
        }

        /// <summary>
        /// Asset hook delegate
        /// </summary>
        /// <param name="bundleDef">Asset bundle definition</param>
        /// <param name="assetBundle">Asset bundle instance</param>
        /// <param name="assetName">Asset name thats being requested</param>
        /// <param name="objectType">Type thats being requested</param>
        /// <param name="asset">Asset object that was loaded</param>
        public delegate void AssetHookCallback(ManifestDef bundleDef, AssetBundle assetBundle, string assetName, Type objectType, UnityEngine.Object asset);

        /// <summary>
        /// Asset hook delegate
        /// </summary>
        /// <param name="bundleDef">Asset bundle definition</param>
        /// <param name="assetBundle">Asset bundle instance</param>
        /// <param name="assetName">Asset name thats being requested</param>
        /// <param name="objectType">Type thats being requested</param>
        /// <param name="asset">Asset object that was loaded</param>
        public delegate void AssetHookCallback<T>(ManifestDef bundleDef, AssetBundle assetBundle, string assetName, Type objectType, T asset) where T : UnityEngine.Object;

        /// <summary>
        /// Name of the asset to hook into
        /// </summary>
        public string AssetName { get; private set; }

        /// <summary>
        /// Type of the asset to hook into
        /// </summary>
        public Type AssetType { get; private set; }

        /// <summary>
        /// Asset hook call
        /// </summary>
        public AssetHookCallback Hook { get; private set; }


    }
}