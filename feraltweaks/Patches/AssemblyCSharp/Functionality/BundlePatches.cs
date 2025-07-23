using FeralTweaks;
using FeralTweaks.BundleInjection;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using WW.Waiters;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public static class BundlePatches
    {
        public static Dictionary<string, string> AssetBundlePaths = new Dictionary<string, string>();
        public static Dictionary<string, string> AssetBundleRelativePaths = new Dictionary<string, string>();
        public static Dictionary<string, ManifestDef> AddedManifestDefs = new Dictionary<string, ManifestDef>();

        private static Dictionary<string, ManifestDef> ManifestsByCacheFile = new Dictionary<string, ManifestDef>();
        private static FT_BundleCacher cacherIl2cppSafe;
        private static bool patched;

        public class FT_BundleCacher : MonoBehaviour
        {
            public FT_BundleCacher() : base()
            { }

            public FT_BundleCacher(System.IntPtr pointer) : base(pointer)
            { }

            public Il2CppSystem.Collections.Generic.Dictionary<string, AssetBundleCreateRequest> assetBundleLoadCache = new Il2CppSystem.Collections.Generic.Dictionary<string, AssetBundleCreateRequest>();
        }

        // FIXME: support full assetbundle API
        // FIXME: make sure to store loaded bundles, unity might break down if the same bundle is loaded twice without unload
        // FIXME: this class is a fucking mess, need full reimplementation

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AssetBundle), "LoadFromFile", new Type[] { typeof(string) })]
        public static bool LoadFromFile(string path, ref AssetBundle __result)
        {
            // FIXME: implement hook
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AssetBundle), "LoadFromFileAsync", new Type[] { typeof(string) })]
        public static bool LoadFromFileAsync(string path, ref AssetBundleCreateRequest __result)
        {
            // Fix for 10-XXX, since we cant patch the bundle manager, we'll patch unity instead
            // The bug is caused by concurrent loading of asset bundles, we will block that and return the currently loading bundle instead
            // FIXME: doesnt work, may be caused by concurrent calls to LoadFromFile until its closed???? not sure

            // Check if in cache
            path = Path.GetFullPath(path);
            lock (cacherIl2cppSafe.assetBundleLoadCache)
            {
                if (cacherIl2cppSafe.assetBundleLoadCache.ContainsKey(path))
                {
                    // Return from cache
                    if (!cacherIl2cppSafe.assetBundleLoadCache[path].WasCollected)
                    {
                        __result = cacherIl2cppSafe.assetBundleLoadCache[path];
                        return false;
                    }
                }
            }

            // Allow unity to load
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AssetBundle), "LoadFromFileAsync", new Type[] { typeof(string) })]
        public static void LoadFromFileAsyncPost(string path, ref AssetBundleCreateRequest __result)
        {
            // Fix for 10-XXX, since we cant patch the bundle manager, we'll patch unity instead
            // The bug is caused by concurrent loading of asset bundles, we will block that and return the currently loading bundle instead
            // FIXME: doesnt work

            // This part of the patch also hooks into the final loaded file

            // Check if in cache
            path = Path.GetFullPath(path);
            bool wasComplete = false;
            lock (cacherIl2cppSafe.assetBundleLoadCache)
            {
                if (!cacherIl2cppSafe.assetBundleLoadCache.ContainsKey(path))
                {
                    // Add to cache
                    cacherIl2cppSafe.assetBundleLoadCache[path] = __result;

                    // Hook complete
                    wasComplete = __result.isDone;
                    if (!wasComplete)
                    {
                        __result.add_completed((Il2CppSystem.Action<AsyncOperation>)new Action<AsyncOperation>(op =>
                        {
                            // FIXME: may hook too late
                            AssetBundle bundle = op.Cast<AssetBundleCreateRequest>().assetBundle;
                            OnLoadComplete(bundle, path);
                        }));
                    }
                }
            }
            if (wasComplete)
            {
                OnLoadComplete(__result.assetBundle, path);
            }
        }

        private static void OnLoadComplete(AssetBundle bundle, string path)
        {
            // Remove from cache
            lock (cacherIl2cppSafe.assetBundleLoadCache)
            {
                cacherIl2cppSafe.assetBundleLoadCache.Remove(path);
            }

            // Find def
            if (ManifestsByCacheFile.ContainsKey(Path.GetFileNameWithoutExtension(bundle.name).ToLower()))
            {
                // Get def
                ManifestDef def = ManifestsByCacheFile[Path.GetFileNameWithoutExtension(bundle.name).ToLower()];

                // Get bundle def name without platform
                string bundleDefName = def.defName;
                string plat = CoreBundleManager.GetSimplePlatformFromRunTime().ToString().ToLower();
                if (bundleDefName.ToLower().StartsWith("/" + plat + "/"))
                    bundleDefName = bundleDefName.Substring(("/" + plat + "/").Length);
                bundleDefName = bundleDefName.ToLower();
                if (!bundleDefName.StartsWith("/"))
                    bundleDefName = "/" + bundleDefName;
                if (!bundleDefName.EndsWith("/"))
                    bundleDefName = bundleDefName + "/";

                // Asset bundle load hooks
                // Find compatible hooks
                foreach (BundleHook hook in BundleHook.GetRegisteredBundleHooks())
                {
                    // Get name
                    string defNameHook = hook.BundlePath.ToLower().Replace("_", "/");
                    if (!defNameHook.StartsWith("/"))
                        defNameHook = "/" + defNameHook;
                    if (!defNameHook.EndsWith("/"))
                        defNameHook = defNameHook + "/";

                    // Check if valid for this def
                    if (defNameHook == bundleDefName)
                    {
                        // Valid
                        // Call hook
                        try
                        {
                            hook.OnBundleLoaded(def, bundle);
                        }
                        catch (Exception e)
                        {
                            FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogError("An error occurred while running asset bundle hooks!\nException: " + e + ":\n" + e.StackTrace);
                        }
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AssetBundle), "LoadAssetAsync", new Type[] { typeof(string), typeof(Il2CppSystem.Type) })]
        public static void LoadAssetAsyncPost(AssetBundle __instance, string name, Type type, ref AssetBundleRequest __result)
        {
            // Check complete
            if (__result.isDone)
            {
                // Call onload
                OnLoadAssetFrom(__instance, name, __result.asset);
            }
            else
            {
                // Hook complete
                __result.add_completed((Il2CppSystem.Action<AsyncOperation>)new Action<AsyncOperation>(op =>
                {
                    // Call onload
                    // FIXME: may hook too late
                    OnLoadAssetFrom(__instance, name, op.Cast<AssetBundleRequest>().asset);
                }));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AssetBundle), "LoadAsset", new Type[] { typeof(string), typeof(Il2CppSystem.Type) })]
        public static void LoadAssetPost(AssetBundle __instance, string name, Type type, ref UnityEngine.Object __result)
        {
            // Call onload
            OnLoadAssetFrom(__instance, name, __result);
        }

        private static void OnLoadAssetFrom(AssetBundle bundle, string assetName, UnityEngine.Object asset)
        {
            // Find def
            if (ManifestsByCacheFile.ContainsKey(Path.GetFileNameWithoutExtension(bundle.name).ToLower()))
            {
                // Get def
                ManifestDef def = ManifestsByCacheFile[Path.GetFileNameWithoutExtension(bundle.name).ToLower()];

                // Get bundle def name without platform
                string bundleDefName = def.defName;
                string plat = CoreBundleManager.GetSimplePlatformFromRunTime().ToString().ToLower();
                if (bundleDefName.ToLower().StartsWith("/" + plat + "/"))
                    bundleDefName = bundleDefName.Substring(("/" + plat + "/").Length);
                bundleDefName = bundleDefName.ToLower();
                if (!bundleDefName.StartsWith("/"))
                    bundleDefName = "/" + bundleDefName;
                if (!bundleDefName.EndsWith("/"))
                    bundleDefName = bundleDefName + "/";

                // Get def file name
                string defFileName = def.defName;
                if (defFileName.EndsWith("/"))
                    defFileName = defFileName.Substring(0, defFileName.LastIndexOf("/"));

                // Check asset name
                if (Path.GetFileNameWithoutExtension(assetName) == "_" + Path.GetFileName(defFileName).ToLower() + "multibundle")
                {
                    // Inject into asset
                    // Load asset list object
                    GameObject multiBundle = asset.Cast<GameObject>();
                    MultiBundle bundleManifest = multiBundle.GetComponent<MultiBundle>();

                    // Add mod assets
                    foreach (string defID in AddedManifestDefs.Keys)
                    {
                        // Load assets into multi bundle
                        // FIXME: it can be done better. dont load all.
                        AssetBundle sourceBundle = AssetBundle.LoadFromFile(AssetBundlePaths[defID]);

                        // Find helper
                        string[] assets = sourceBundle.GetAllAssetNames();
                        if (assets.Any(t => Path.GetFileNameWithoutExtension(t).ToLower() == "ft_multibundlehelper"))
                        {
                            // FIXME: make sure unity-generated bundles work with this, it was tested via a custom made via UABEA
                            TextAsset helperAs = null;
                            UnityEngine.Object[] asl = sourceBundle.LoadAllAssets();
                            foreach (UnityEngine.Object assetObj in asl)
                            {
                                TextAsset tAs = assetObj.TryCast<TextAsset>();
                                if (tAs != null && Path.GetFileNameWithoutExtension(tAs.name.ToLower()) == "ft_multibundlehelper")
                                {
                                    helperAs = tAs;
                                    break;
                                }
                            }
                            if (helperAs != null)
                            {
                                // There is a helper file, use it
                                Dictionary<string, string> helper = new Dictionary<string, string>();
                                foreach (string line in helperAs.text.Replace("\r", "").Split("\n"))
                                {
                                    if (line == "" || line.StartsWith("#") || !line.Contains(": "))
                                        continue;
                                    string key = line.Remove(line.IndexOf(": "));
                                    string value = line.Substring(line.IndexOf(": ") + 2);
                                    helper[key] = value;
                                }

                                // Verify helper bundle name
                                if (!helper.ContainsKey("Multi-Bundle-Name"))
                                {
                                    // Error
                                    sourceBundle.Unload(false);
                                    FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogError("Unable to inject " + defID + " into multi-bundle asset archives, its helper document is missing a Multi-Bundle-Name field.");
                                    continue;
                                }

                                // Verify name
                                if (helper["Multi-Bundle-Name"].ToLower() != Path.GetFileName(defFileName).ToLower())
                                {
                                    sourceBundle.Unload(false);
                                    continue; // Skip
                                }

                                // Verify helper
                                if (!helper.ContainsKey("Bundle-ID"))
                                {
                                    // Error
                                    sourceBundle.Unload(false);
                                    FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogError("Unable to inject " + defID + " into multi-bundle asset archives, its helper document is missing a Bundle-ID field.");
                                    continue;
                                }
                                if (!helper.ContainsKey("Asset-Name"))
                                {
                                    // Error
                                    sourceBundle.Unload(false);
                                    FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogError("Unable to inject " + defID + " into multi-bundle asset archives, its helper document is missing a Asset-Name field.");
                                    continue;
                                }
                                if (!helper.ContainsKey("Asset-Type"))
                                {
                                    // Error
                                    sourceBundle.Unload(false);
                                    FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogError("Unable to inject " + defID + " into multi-bundle asset archives, its helper document is missing a Asset-Type field.");
                                    continue;
                                }

                                // Bundle ID
                                string bundleID = helper["Bundle-ID"];
                                if (!bundleID.StartsWith("/"))
                                    bundleID = "/" + bundleID;
                                if (!bundleID.EndsWith("/"))
                                    bundleID = bundleID + "/";
                                bundleID = bundleID.ToLower();

                                // Source asset name and type
                                string sourceAssetName = helper["Asset-Name"];
                                string assetTypeName = helper["Asset-Type"];
                                Type typeInterop = null;
                                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                                {
                                    try
                                    {
                                        typeInterop = asm.GetType(assetTypeName);
                                        if (typeInterop != null)
                                            break;
                                    }
                                    catch { }
                                }
                                if (typeInterop == null || !typeInterop.IsAssignableTo(typeof(UnityEngine.Object)))
                                {
                                    // Error
                                    sourceBundle.Unload(false);
                                    FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogError("Unable to inject " + defID + " into multi-bundle asset archives, the Asset-Type field does not point to a valid UnityEngine.Object type.");
                                    continue;
                                }
                                Il2CppSystem.Type type = Il2CppType.From(typeInterop);

                                // Find source asset
                                UnityEngine.Object assetInst = sourceBundle.LoadAsset(sourceAssetName, type);
                                if (assetInst == null)
                                    assetInst = sourceBundle.LoadAllAssets().Where(t => t.name.ToLower() == sourceAssetName.ToLower() && type.IsAssignableFrom(t.GetIl2CppType())).FirstOrDefault();
                                if (assetInst == null)
                                {
                                    // Error
                                    sourceBundle.Unload(false);
                                    FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogError("Unable to inject " + defID + " into multi-bundle asset archives, the source asset could not be found in the source bundle.");
                                    continue;
                                }

                                // Find entry in multi-bundle
                                MultiBundleEntry entry = null;
                                foreach (MultiBundleEntry en in bundleManifest.assets)
                                {
                                    // Check entry
                                    if (en.bundleID.ToLower() == bundleID.ToLower())
                                    {
                                        entry = en;
                                        break;
                                    }
                                }

                                // Create entry if needed
                                if (entry == null)
                                {
                                    // Create and add
                                    entry = new MultiBundleEntry();
                                    entry.bundleID = bundleID;
                                    bundleManifest.assets.Add(entry);
                                }

                                // Update
                                entry.asset = assetInst;
                            }
                            sourceBundle.Unload(false);
                        }
                        else
                            sourceBundle.Unload(true);
                    }
                }

                // Asset bundle load hooks
                // Find compatible hooks
                foreach (BundleHook hook in BundleHook.GetRegisteredBundleHooks())
                {
                    // Get name
                    string defNameHook = hook.BundlePath.ToLower().Replace("_", "/");
                    if (!defNameHook.StartsWith("/"))
                        defNameHook = "/" + defNameHook;
                    if (!defNameHook.EndsWith("/"))
                        defNameHook = defNameHook + "/";

                    // Check if valid for this def
                    if (defNameHook == bundleDefName)
                    {
                        // Valid
                        // Call asset hooks
                        foreach (AssetHook ahook in hook.GetAssetHooks())
                        {
                            try
                            {
                                // Check asset hook
                                if (ahook.AssetName.ToLower() == assetName.ToLower() && ahook.AssetType.IsAssignableFrom(asset.GetIl2CppType()))
                                {
                                    // Call hook
                                    ahook.Hook(def, bundle, assetName, asset.GetIl2CppType(), asset);
                                }
                            }
                            catch (Exception e)
                            {
                                FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogError("An error occurred while running asset load hooks!\nException: " + e + ":\n" + e.StackTrace);
                            }
                        }
                    }
                }        
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AssetBundle), "GetAllAssetNames")]
        public static void GetAllAssetNames(AssetBundle __instance, ref Il2CppStringArray __result)
        {
            // Inject assets
            // FIXME: support
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AssetBundle), "LoadAllAssets", new Type[] { typeof(Il2CppSystem.Type) })]
        public static void LoadAllAssets(AssetBundle __instance, Il2CppSystem.Type type, ref Il2CppReferenceArray<UnityEngine.Object> __result)
        {
            // Inject assets
            // FIXME: support
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AssetBundle), "LoadAllAssetsAsync", new Type[] { typeof(Il2CppSystem.Type) })]
        public static void LoadAllAssetsAsync(AssetBundle __instance, Type type, ref AssetBundleRequest __result)
        {
            // Inject assets into result
            // FIXME: support
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AssetBundle), "LoadAsset", new Type[] { typeof(string), typeof(Il2CppSystem.Type) })]
        public static bool LoadAsset(AssetBundle __instance, string name, Type type, ref UnityEngine.Object __result)
        {
            // Inject assets
            // FIXME: support

            // Allow load
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AssetBundle), "LoadAssetAsync", new Type[] { typeof(string), typeof(Il2CppSystem.Type) })]
        public static void LoadAssetAsync(AssetBundle __instance, string name, Type type, ref AssetBundleRequest __result)
        {
            // Inject assets into result
            // FIXME: support
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WaitController), "Update")]
        public static void Update()
        {
            // Prevent cached charts from removing
            if (CoreChartDataManager.coreInstance == null)
                return;
            ManifestChartData chart = CoreChartDataManager.coreInstance.manifestChartData;
            foreach (ManifestDef def in AddedManifestDefs.Values)
            {
                if (!chart._parsedDefsByID.ContainsKey(def.defID))
                    chart._parsedDefsByID.Add(def.defID, def); // FIXME: just change lastAccessTime
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CoreChartDataManager), "SetChartObjectInstances")]
        public static void SetChartObjectInstances()
        {
            if (patched)
                return;
            patched = true;

            // Inject
            ClassInjector.RegisterTypeInIl2Cpp<FT_BundleCacher>();
            GameObject obj = new GameObject();
            obj.name = "FT_BundleCacher";
            GameObject.DontDestroyOnLoad(obj);
            cacherIl2cppSafe = obj.AddComponent<FT_BundleCacher>();

            // Get chart
            FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogInfo("Patching bundle manifest chart...");
            ManifestChartData chart = CoreChartDataManager.coreInstance.manifestChartData;

            // Go through all defs
            foreach (string asset in AssetBundlePaths.Keys)
            {
                // Find existing def, if none create one
                ManifestDef def = null;
                foreach (ManifestDef defI in chart.defList)
                {
                    if (defI.defID == asset)
                    {
                        def = defI;
                        break;
                    }
                }

                // Create def if needed
                if (def == null)
                {
                    FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogInfo("Creating bundle def: " + asset + "...");
                    def = new ManifestDef();
                    chart.defList.Add(def);
                    chart._parsedDefsByID.Add(asset, def);
                    AddedManifestDefs[asset] = def;
                }
                else
                    FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogInfo("Patching bundle def: " + asset + "...");

                // Update def
                def.fileName = asset;
                def.defID = asset;
                def._downloadURL = new Uri(AssetBundlePaths[asset]).AbsoluteUri;
                def.hash = new DateTimeOffset(File.GetLastWriteTimeUtc(AssetBundlePaths[asset])).ToUnixTimeMilliseconds().ToString();
                def.defName = "/" + AssetBundleRelativePaths[asset] + "/";
                def.lowerDefName = "/" + AssetBundleRelativePaths[asset].ToLower() + "/";
                FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogDebug("Def ID: " + def.defID);
                FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogDebug("Def file name: " + def.fileName);
                FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogDebug("Def timestamp: " + def.hash);
                FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogDebug("Def name: " + def.defName);
                FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogDebug("Lower def name: " + def.lowerDefName);
            }

            // Populate index
            foreach (ManifestDef defI in chart.defList)
            {
                ManifestsByCacheFile[defI.defID.ToLower()] = defI;
            }
        }

    }
}