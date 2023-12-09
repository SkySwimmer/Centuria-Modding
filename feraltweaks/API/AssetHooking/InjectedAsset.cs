using Il2CppSystem;
using UnityEngine;

namespace FeralTweaks.BundleInjection
{
    /// <summary>
    /// Injected asset information container
    /// </summary>
    public class InjectedAsset
    {
        internal InjectedAsset(string name, Type type, AssetLoader loader)
        {
            AssetName = name;
            AssetType = type;
            Loader = loader;
        }

        /// <summary>
        /// Asset loader delegate
        /// </summary>
        /// <param name="bundleDef">Asset bundle definition</param>
        /// <param name="assetBundle">Asset bundle instance</param>
        /// <param name="assetName">Asset name thats being requested</param>
        /// <param name="requestedType">Type thats being requested</param>
        /// <returns>UnityEngine.Object instance or null</returns>
        public delegate UnityEngine.Object AssetLoader(ManifestDef bundleDef, AssetBundle assetBundle, string assetName, Type requestedType);

        /// <summary>
        /// Asset loader delegate
        /// </summary>
        /// <param name="bundleDef">Asset bundle definition</param>
        /// <param name="assetBundle">Asset bundle instance</param>
        /// <param name="assetName">Asset name thats being requested</param>
        /// <param name="requestedType">Type thats being requested</param>
        /// <returns>UnityEngine.Object instance or null</returns>
        public delegate T AssetLoader<T>(ManifestDef bundleDef, AssetBundle assetBundle, string assetName, Type requestedType) where T : UnityEngine.Object;

        /// <summary>
        /// Name of the injected asset
        /// </summary>
        public string AssetName { get; private set; }

        /// <summary>
        /// Type of the injected asset
        /// </summary>
        public Type AssetType { get; private set; }

        /// <summary>
        /// Loader delegate for the injected asset
        /// </summary>
        public AssetLoader Loader { get; private set; }


    }
}