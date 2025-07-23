using FeralTweaks.Managers;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using System.Linq;
using System.Collections.Generic;
using Il2CppSystem.Reflection;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes;
using FeralTweaks.Logging;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Il2CppSystem.Runtime.Serialization;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class CorePatches
    {
        // FIXME: implement profiling for managers
        // FIXME: implement profiling for managedbehaviours (by overriding the base game function)
        
        private static bool _inited = false;
        private static bool _initedG = false;
        private static void Init()
        {
            if (_inited)
                return;
            _inited = true;

            // Initialize
            CoreManagerInjectors.SplashCore.RegisterManagerContainer<SplashCoreManagersContainer>();
            CoreManagerInjectors.Core.RegisterManagerContainer<CoreManagersContainer>();
        }

        private static void InitGuard()
        {
            if (_initedG)
                return;
            _initedG = true;
            ClassInjector.RegisterTypeInIl2Cpp<CoreDestroyGuard>();
        }

        private static T ConvertToActualType<T>(Il2CppSystem.Object input) where T : Il2CppSystem.Object
        {
            Il2CppSystem.Object obj = Il2CppSystem.Convert.ChangeType(input.Cast<T>(), input.GetIl2CppType());
            if (!(obj is T))
            {
                // Try il2cpp cast
                obj = obj.Cast<T>();
                obj = Il2CppSystem.Convert.ChangeType(input.Cast<T>(), input.GetIl2CppType());
                if (!(obj is T)) // Give up
                    obj = obj.Cast<T>();
            }
            return (T) obj;
        }

        public class CoreDestroyGuard : UnityEngine.MonoBehaviour
        {
            public CoreDestroyGuard() { }
            public CoreDestroyGuard(System.IntPtr ptr) : base(ptr) { }
            
            public CoreBase targetCore;
            public CoreManagerInjectors targetInjectors;
            
            public void OnDestroy()
            {
                // On destroy
                
                // Clean up injected managers
                foreach (CoreManagerInjectors.ManagerData mgr in targetInjectors.loadOrder)
                {
                    bool wasOriginal = false;
                    if (mgr.manager != null)
                    {
                        if (mgr.originalManager != null && mgr.manager == mgr.originalManager)
                            wasOriginal = true;
                        mgr.manager.enabled = false;
                        mgr.field.SetValue(mgr.container, null);
                        InjectedManagersContainer.RegisteredManager mgrC = mgr.container.managers.FirstOrDefault(t =>
                        {
                            string id = t.manager.GetIl2CppType().FullName;
                            if (id == mgr.id)
                                return true;
                            return false;
                        });
                        if (mgrC != null)
                            mgr.container.managers.Remove(mgrC);
                        Destroy(mgr.manager.gameObject);
                        mgr.manager = null;
                    }
                    if (mgr.originalManager != null && !wasOriginal)
                    {
                        mgr.originalManager.enabled = false;
                        mgr.field.SetValue(mgr.container, null);
                        InjectedManagersContainer.RegisteredManager mgrC = mgr.container.managers.FirstOrDefault(t =>
                        {
                            string id = t.manager.GetIl2CppType().FullName;
                            if (id == mgr.id)
                                return true;
                            return false;
                        });
                        if (mgrC != null)
                            mgr.container.managers.Remove(mgrC);
                        Destroy(mgr.originalManager.gameObject);
                        mgr.originalManager = null;
                    }
                }
                targetInjectors.WipeBuiltManagers();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ManagerBase), "Update")]
        public static bool Update(ref ManagerBase __instance)
        {
            // Init
            Init();

            // Check type
            FeralTweaksManagerBase ftMgr = __instance.TryCast<FeralTweaksManagerBase>();
            if (ftMgr == null)
                return true;

            // Reimplement update
            if (ftMgr._inited && ftMgr.loaded && CoreBase<Core>.Loaded)
            {
                // Validate behaviours
                ftMgr.ValidateRegisteredBehaviours();

                // Go through managed behaviours and wake each
                foreach (ManagedBehaviour behaviour in ftMgr.registeredBehaviours)
                {
                    if (!behaviour.managedAwoken && behaviour.gameObject.activeInHierarchy && behaviour.enabled)
                    {
                        try
                        {
                            behaviour.AwakeInternal();
                        }
                        catch (System.Exception e)
                        {
                            Logger.GetLogger("Interop").Error("Uncaught exception during ManagedBehaviour AwakeInternal!", e);
                            if (Debugger.IsAttached)
                                throw;
                        }
                    }
                }

                // Now enable each
                foreach (ManagedBehaviour behaviour in ftMgr.registeredBehaviours)
                {
                    if (!behaviour.managedEnabled && behaviour.gameObject.activeInHierarchy && behaviour.enabled)
                    {
                        try
                        {
                            behaviour.OnEnableInternal();
                        }
                        catch (System.Exception e)
                        {
                            Logger.GetLogger("Interop").Error("Uncaught exception during ManagedBehaviour OnEnableInternal!", e);
                            if (Debugger.IsAttached)
                                throw;
                        }
                    }
                }

                // Now start each
                foreach (ManagedBehaviour behaviour in ftMgr.registeredBehaviours)
                {
                    if (!behaviour.managedStarted && behaviour.gameObject.activeInHierarchy && behaviour.enabled)
                    {
                        try
                        {
                            behaviour.StartInternal();
                        }
                        catch (System.Exception e)
                        {
                            Logger.GetLogger("Interop").Error("Uncaught exception during ManagedBehaviour StartInternal!", e);
                            if (Debugger.IsAttached)
                                throw;
                        }
                    }
                }

                // Avatar local start
                if (Avatar_Local.instance != null && Avatar_Local.instance.BuildState == ActorBuildState.Built)
                {
                    foreach (ManagedBehaviour behaviour in ftMgr.registeredBehaviours)
                    {
                        if (!behaviour.managedStartedAfterLocal && behaviour.gameObject.activeInHierarchy && behaviour.enabled)
                        {
                            try
                            {
                                behaviour.StartAfterLocalInternal();
                            }
                            catch (System.Exception e)
                            {
                                Logger.GetLogger("Interop").Error("Uncaught exception during ManagedBehaviour StartAfterLocalInternal!", e);
                                if (Debugger.IsAttached)
                                    throw;
                            }
                        }
                    }
                }

                // Disabled
                foreach (ManagedBehaviour behaviour in ftMgr.registeredDisableBehaviours)
                {
                    if (!behaviour.managedDisabled)
                    {
                        try
                        {
                            behaviour.OnDisableInternal();
                        }
                        catch (System.Exception e)
                        {
                            Logger.GetLogger("Interop").Error("Uncaught exception during ManagedBehaviour OnDisableInternal!", e);
                            if (Debugger.IsAttached)
                                throw;
                        }
                    }
                }
                if (ftMgr.registeredDisableBehaviours.Count != 0)
                    ftMgr.registeredDisableBehaviours.Clear();

                // Update internal
                ftMgr.UpdateInternal();
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ManagerBase), "UpdateInternal")]
        public static bool UpdateInternal(ref ManagerBase __instance)
        {
            // Init
            Init();

            // Check type
            FeralTweaksManagerBase ftMgr = __instance.TryCast<FeralTweaksManagerBase>();
            if (ftMgr == null)
                return true;

            // Reimplement update
            if (ftMgr._inited && ftMgr.loaded && CoreBase<Core>.Loaded)
            {
                // Update
                ftMgr.MUpdate();

                // Call update for behaviours
                foreach (ManagedBehaviour behaviour in ftMgr.registeredBehaviours)
                {
                    if (behaviour.managedAwoken && behaviour.managedEnabled && behaviour.managedStarted && behaviour.gameObject.activeInHierarchy && behaviour.enabled)
                    {
                        try
                        {
                            behaviour.UpdateInternal();
                        }
                        catch (System.Exception e)
                        {
                            Logger.GetLogger("Interop").Error("Uncaught exception during ManagedBehaviour AwakeInternal!", e);
                            if (Debugger.IsAttached)
                                throw;
                        }
                    }
                }
            }
            return false;
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ManagerBase), "ValidateRegisteredBehaviours")]
        private static bool ValidateRegisteredBehaviours(ref ManagerBase __instance)
        {
            // Init
            Init();

            // Check type
            FeralTweaksManagerBase ftMgr = __instance.TryCast<FeralTweaksManagerBase>();
            if (ftMgr == null)
                return true;

            // Reimplement
            while (ftMgr.registeredBehavioursBackend.ToArray().Any(t => t == null))
                ftMgr.registeredBehavioursBackend.RemoveAt(ftMgr.registeredBehavioursBackend.ToArray().IndexOf(null));
            while (ftMgr.registeredDisableBehavioursBackend.ToArray().Any(t => t == null))
                ftMgr.registeredDisableBehavioursBackend.RemoveAt(ftMgr.registeredDisableBehavioursBackend.ToArray().IndexOf(null));
            return false;
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ManagerBase), "RegisterManagedBehaviour")]
        public static bool RegisterManagedBehaviour(ManagerBase __instance, ManagedBehaviour inManagedBehaviour)
        {
            // Init
            Init();

            // Cast properly
            __instance = ConvertToActualType<ManagerBase>(__instance);
            inManagedBehaviour = ConvertToActualType<ManagedBehaviour>(inManagedBehaviour);

            // Cast to FT managed behaviour
            FeralTweaksManagedBehaviour ftBehaviour = inManagedBehaviour.TryCast<FeralTweaksManagedBehaviour>();
            if (ftBehaviour != null)
                ftBehaviour.ResetLinksFully();

            // Find all managers
            ManagerBase targetManager = __instance;
            System.Collections.Generic.List<ManagerBase> linkedManagers = new System.Collections.Generic.List<ManagerBase>();
            
            // Get injectors
            CoreManagerInjectors injectors = CoreManagerInjectors.Core;

            // Go through managers
            foreach (CoreManagerInjectors.ManagerData mgrD in injectors.loadOrder)
            {
                if (mgrD.manager != null)
                {
                    FeralTweaksManagerBase mgr = mgrD.manager.TryCast<FeralTweaksManagerBase>();
                    if (mgr != null)
                    {
                        // Go through rules
                        foreach (FeralTweaksManagerBehaviourInterceptionRule rule in mgr.interceptionRules)
                        {
                            // Check
                            if ((rule.RuleType == FeralTweaksManagerBehaviourInterceptionRuleType.ALLOFBEHAVIOUR && rule.Target.IsAssignableFrom(inManagedBehaviour.GetIl2CppType())) || (rule.RuleType == FeralTweaksManagerBehaviourInterceptionRuleType.ALLOFMANAGER && rule.Target.IsAssignableFrom(targetManager.GetIl2CppType())))
                            {
                                // Link
                                linkedManagers.Add(mgr);

                                // Check method
                                if (rule.Method == FeralTweaksManagerBehaviourInterceptionMethod.TAKEOVER)
                                {
                                    // Takeover
                                    // The last manager that attempts takeover is always the manager that receives the instance
                                    __instance = mgr;
                                    inManagedBehaviour._manager = mgr;
                                    if (ftBehaviour != null)
                                        ftBehaviour.ChangeOwner(mgr);
                                }
                            }
                        }
                    }
                }
            }

            // Add to link for owner
            if (!linkedManagers.Contains(__instance))
                linkedManagers.Add(__instance);

            // Go through linked managers and call event
            if (ftBehaviour != null)
            {
                foreach (ManagerBase linked in linkedManagers)
                {
                    // Call link in behaviour
                    ftBehaviour.OnLinkedToManager(linked);
                }
            }

            // Add to behaviour (should be run after the linking events are called to match behaviour of unlinking)
            if (ftBehaviour != null)
            {
                foreach (ManagerBase linked in linkedManagers)
                {
                    // Add
                    ftBehaviour.AddLinkedManager(linked);
                }
            }

            // Call link for managers
            foreach (ManagerBase linked in linkedManagers)
            {
                FeralTweaksManagerBase ftM = linked.TryCast<FeralTweaksManagerBase>();
                if (ftM != null)
                {
                    // Call link
                    ftM.OnLinkBehaviourToManager(inManagedBehaviour);
                }
            }

            // Add to linked managers (should be run after the linking events are called to match behaviour of unlinking)
            foreach (ManagerBase linked in linkedManagers)
            {
                FeralTweaksManagerBase ftM = linked.TryCast<FeralTweaksManagerBase>();
                if (ftM != null)
                {
                    // Call link
                    ftM.linkedBehaviours.Add(inManagedBehaviour);
                }
            }

            // Check type
            FeralTweaksManagerBase ftMgr = __instance.TryCast<FeralTweaksManagerBase>();
            if (ftMgr == null)
            {
                if (__instance != targetManager)
                {
                    // Make sure to install into the right manager
                    if (!__instance._registeredBehaviours.Contains(inManagedBehaviour))
                    {
                        __instance._registeredBehaviours.Add(inManagedBehaviour);
                        __instance._registeredBehaviourNames.Add(inManagedBehaviour.gameObject.name);
                    }
                    return false;
                }
                return true;
            }

            // Call on register
            ftMgr.OnRegisterBehaviourToManager(inManagedBehaviour);

            // Call register for behaviour
            if (ftBehaviour != null)
                ftBehaviour.OnRegisteredToManager(ftMgr, targetManager);

            // Register
            if (!ftMgr.registeredBehaviours.Contains(inManagedBehaviour))
                ftMgr.registeredBehaviours.Add(inManagedBehaviour);
            return false;
        }
         
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ManagerBase), "UnregisterManagedBehaviour")]
        public static bool UnregisterManagedBehaviour(ManagerBase __instance, ManagedBehaviour inManagedBehaviour)
        {
            // Init
            Init();

            // Cast properly
            __instance = ConvertToActualType<ManagerBase>(__instance);
            inManagedBehaviour = ConvertToActualType<ManagedBehaviour>(inManagedBehaviour);

            // Cast to FT managed behaviour
            FeralTweaksManagedBehaviour ftBehaviour = inManagedBehaviour.TryCast<FeralTweaksManagedBehaviour>();
            if (ftBehaviour != null)
                ftBehaviour.ResetLinksFully();
                
            // Get original manager
            ManagerBase targetManager = GetManagerFromBehaviourAtributes(inManagedBehaviour);
            inManagedBehaviour._manager = targetManager;

            // Find all managers
            System.Collections.Generic.List<ManagerBase> linkedManagers = new System.Collections.Generic.List<ManagerBase>();

            // Get injectors
            CoreManagerInjectors injectors = CoreManagerInjectors.Core;

            // Go through managers
            foreach (CoreManagerInjectors.ManagerData mgrD in injectors.loadOrder)
            {
                if (mgrD.manager != null)
                {
                    FeralTweaksManagerBase mgr = mgrD.manager.TryCast<FeralTweaksManagerBase>();
                    if (mgr != null)
                    {
                        // Go through rules
                        foreach (FeralTweaksManagerBehaviourInterceptionRule rule in mgr.interceptionRules)
                        {
                            // Check
                            if ((rule.RuleType == FeralTweaksManagerBehaviourInterceptionRuleType.ALLOFBEHAVIOUR && rule.Target.IsAssignableFrom(inManagedBehaviour.GetIl2CppType())) || (rule.RuleType == FeralTweaksManagerBehaviourInterceptionRuleType.ALLOFMANAGER && rule.Target.IsAssignableFrom(targetManager.GetIl2CppType())))
                            {
                                // Add to link list
                                linkedManagers.Add(mgr);
                            }
                        }
                    }
                }
            }

            // Add to link for owner
            if (!linkedManagers.Contains(__instance))
                linkedManagers.Add(__instance);

            // Go through linked managers and call unlink
            if (ftBehaviour != null)
            {
                foreach (ManagerBase linked in linkedManagers)
                {
                    // Call link in behaviour
                    ftBehaviour.OnUnlinkedFromManager(linked);
                }
            }

            // Call unlink for managers
            foreach (ManagerBase linked in linkedManagers)
            {
                FeralTweaksManagerBase ftM = linked.TryCast<FeralTweaksManagerBase>();
                if (ftM != null)
                {
                    // Call link
                    ftM.OnUnlinkBehaviourFromManager(inManagedBehaviour);
                }
            }

            // Remove from linked managers (should be run after the linking events are called to match behaviour of unlinking)
            foreach (ManagerBase linked in linkedManagers)
            {
                FeralTweaksManagerBase ftM = linked.TryCast<FeralTweaksManagerBase>();
                if (ftM != null)
                {
                    // Call link
                    ftM.linkedBehaviours.Remove(inManagedBehaviour);
                }
            }

            // Check type
            FeralTweaksManagerBase ftMgr = __instance.TryCast<FeralTweaksManagerBase>();
            if (ftMgr == null)
                return true;

            // Call on unregister
            ftMgr.OnUnregisterBehaviourFromManager(inManagedBehaviour);

            // Call unregister for behaviour
            if (ftBehaviour != null)
                ftBehaviour.OnUnregisteredFromManager(ftMgr, ftBehaviour.OriginalManager);

            // Unregister
            if (ftMgr.registeredBehaviours.Contains(inManagedBehaviour))
            {
                ftMgr.registeredBehaviours.Remove(inManagedBehaviour);
                if (!ftMgr.registeredDisableBehaviours.Contains(inManagedBehaviour))
                    ftMgr.registeredDisableBehaviours.Add(inManagedBehaviour);
            }
            return false;
        }
         
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ManagedBehaviour), "UnregisterWithManager")]
        public static bool UnregisterWithManager(ref ManagedBehaviour __instance)
        {
            // Init
            Init();

            // Check type
            if (__instance.Manager == null)
                return true;
                
            // Execute manually
            __instance.managedRegistered = false;
            __instance.Manager.UnregisterManagedBehaviour(__instance);
            return false;
        }
         
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ManagedBehaviour), "OnDisable")]
        public static bool OnDisable(ref ManagedBehaviour __instance)
        {
            // Init
            Init();

            // Reimplement to fix accessviolation caused by the original breaking when injection

            // Check reset/quit
            if (CoreBase<Core>.Quitting || CoreBase<Core>.IsResetting)
                return false; // Skip
            
            // Check registered
            if (__instance.managedRegistered && __instance.Manager != null && __instance.Manager.registeredBehaviours.Contains(__instance))
            {
                __instance.UnregisterWithManager();
                return false;
            }

            // Lets allow the original
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ManagerBase), "registeredBehaviours", MethodType.Getter)]
        public static bool RegisteredBehaviours(ref ManagerBase __instance, ref Il2CppSystem.Collections.Generic.List<ManagedBehaviour> __result)
        {
            // Init
            Init();

            // Check type
            FeralTweaksManagerBase ftMgr = __instance.TryCast<FeralTweaksManagerBase>();
            if (ftMgr == null)
                return true;

            // Rework
            __result = ftMgr.registeredBehavioursBackend;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ManagerBase), "registeredDisableBehaviours", MethodType.Getter)]
        public static bool RegisteredDisableBehaviours(ref ManagerBase __instance, ref Il2CppSystem.Collections.Generic.List<ManagedBehaviour> __result)
        {
            // Init
            Init();

            // Check type
            FeralTweaksManagerBase ftMgr = __instance.TryCast<FeralTweaksManagerBase>();
            if (ftMgr == null)
                return true;

            // Rework
            __result = ftMgr.registeredDisableBehavioursBackend;
            return false;
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ManagedBehaviour), "Manager", MethodType.Getter)]
        public static bool GetManager(ref ManagedBehaviour __instance, ref ManagerBase __result)
        {
            // Check
            if (__instance._manager != null)
            {
                // Return existing
                __result = __instance._manager;
                return false;
            }

            // Get
            // Get manager
            __instance._manager = GetManagerFromBehaviourAtributes(__instance);
            __result = __instance._manager;

            // Done
            return false;
        }

        private static ManagerBase GetManagerFromBehaviourAtributes(ManagedBehaviour __instance)
        {
            // Select manager
            string behaviourId = __instance.GetType().FullName; // FT
            if (behaviourId == "ManagedBehaviour")
                behaviourId = __instance.GetIl2CppType().FullName; // Base
            string managerName = ManagedBehaviour._managerNameDict.GetExistingEntryOrNull(behaviourId);
            if (managerName == null)
            {
                // Find manager based on attributes

                // First, check FT attributes
                foreach (System.Attribute attr in __instance.GetType().GetCustomAttributes(true))
                {
                    if (attr is ManagedBehaviourFTManagerAttribute)
                    {
                        ManagedBehaviourFTManagerAttribute ftMgrAttr = (ManagedBehaviourFTManagerAttribute)attr;
                        managerName = ftMgrAttr.ManagerName;
                    }
                }

                // Check if success
                if (managerName == null)
                {
                    // If not, check vanilla  attributes via Il2Cpp
                    foreach (Object attr in __instance.GetIl2CppType().GetCustomAttributes(true))
                    {
                        ManagedBehaviourManagerAttribute atC = attr.TryCast<ManagedBehaviourManagerAttribute>();
                        if (atC != null)
                            managerName = atC.managerName;
                    }
                }

                // Fallback
                if (managerName == null)
                    managerName = "PlatformManager";
                ManagedBehaviour._managerNameDict[behaviourId] = managerName;
            }

            // Get
            return ManagerBase.GetInstanceForManagerName(managerName);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ManagerBase), "AddInstanceToDictInternal")]
        public static bool AddInstanceToDictInternal(ref ManagerBase __instance)
        {
            // Init
            Init();

            // Check type
            FeralTweaksManagerBase ftMgr = __instance.TryCast<FeralTweaksManagerBase>();
            if (ftMgr == null)
                return true;

            // Reimplement
            Type t = ftMgr.GetIl2CppType();
            while (t.FullName != Il2CppType.Of<FeralTweaksManagerBase>().FullName)
            {
                // Get name
                string name = t.FullName;

                // Assign
                ManagerBase._instanceDictionary[name] = ftMgr;

                // Select parent
                t = t.BaseType;
            }
            return false;
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ManagerBase), "UnsetInstance")]
        public static bool UnsetInstance(ref ManagerBase __instance)
        {
            // Init
            Init();

            // Check type
            FeralTweaksManagerBase ftMgr = __instance.TryCast<FeralTweaksManagerBase>();
            if (ftMgr == null)
                return true;

            // Unset instance fields
            foreach (System.Reflection.PropertyInfo prop in ftMgr.GetType().GetProperties())
            {
                if (prop.SetMethod != null && prop.SetMethod.IsStatic && prop.PropertyType != null && prop.PropertyType.IsAssignableFrom(ftMgr.GetType()) && prop.CustomAttributes != null && prop.CustomAttributes.Any(t => t.AttributeType.IsAssignableTo(typeof(FTManagerSetInstanceAttribute))))
                {
                    object cVal = prop.GetValue(null);
                    if (cVal != null && cVal.GetType().IsAssignableFrom(ftMgr.GetType()))
                    {
                        // Set
                        prop.SetMethod.Invoke(null, new object[] { null });
                    }
                }
            }

            // Reimplement rest
            Type t = ftMgr.GetIl2CppType();
            while (t.FullName != Il2CppType.Of<FeralTweaksManagerBase>().FullName)
            {
                // Get name
                string name = t.FullName;

                // Check
                if (ManagerBase._instanceDictionary.ContainsKey(name))
                    ManagerBase._instanceDictionary.Remove(name);

                // Select parent
                t = t.BaseType;
            }
            return false;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ManagerBase), "SetInstance")]
        public static void SetInstance(ref ManagerBase __instance)
        {
            // Init
            Init();

            // Check type
            FeralTweaksManagerBase ftMgr = __instance.TryCast<FeralTweaksManagerBase>();
            if (ftMgr == null)
                return;

            // Set instance fields
            foreach (System.Reflection.PropertyInfo prop in ftMgr.GetType().GetProperties())
            {
                if (prop.SetMethod != null && prop.SetMethod.IsStatic && prop.PropertyType != null && prop.PropertyType.IsAssignableFrom(ftMgr.GetType()) && prop.CustomAttributes != null && prop.CustomAttributes.Any(t => t.AttributeType.IsAssignableTo(typeof(FTManagerSetInstanceAttribute))))
                {
                    // Set
                    prop.SetMethod.Invoke(null, new object[] { ftMgr });
                }
            }

            // Rename object to keep things simple
            __instance.gameObject.name = ftMgr.GetType().Name;

            // Enable
            ftMgr.enabled = true;
            ftMgr.gameObject.SetActive(true);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CoreBase<SplashCore>), "GetManagerField")]
        public static bool GetManagerField(ref Il2CppSystem.Object __instance, ManagerBase inManager, ref FieldInfo __result)
        {
            // Init
            Init();

            // Check core type
            Core core = __instance.TryCast<Core>();
            SplashCore splashCore = __instance.TryCast<SplashCore>();
            if (core != null)
                __instance = core;
            if (splashCore != null)
                __instance = splashCore;

            // Build if needed
            CoreManagerInjectors injectors = null;
            if (core != null)
                injectors = CoreManagerInjectors.Core;
            else if (splashCore != null)
                injectors = CoreManagerInjectors.SplashCore;
            else
                return true;
            if (injectors.NeedsBuilding)
            {
                Il2CppReferenceArray<FieldInfo> arr = core == null ? CoreBase<SplashCore>.ManagerFields : CoreBase<Core>.ManagerFields;
                ManagerBase[] mgrs = arr.Where(t => Il2CppType.Of<ManagerBase>().IsAssignableFrom(t.FieldType)).Select(t =>
                {
                    Il2CppSystem.Object val = t.GetValue(core);
                    if (val != null)
                        return val.Cast<ManagerBase>();
                    return null;
                }).Where(t => t != null).ToArray();
                injectors.BuildIfNeeded(mgrs);
            }

            // Find manager
            string id = inManager.GetIl2CppType().FullName;
            CoreManagerInjectors.ManagerData mgr = injectors.managerDatas.GetValueOrDefault(id);
            if (mgr != null)
            {
                // Check FT
                if (mgr.manager != null && mgr.manager is FeralTweaksManagerBase)
                {
                    // Return field
                    __result = mgr.field;
                    return false;
                }
                
                // Check core (needed workaround else the game crashes, because of generic type shenanigans)
                if (core != null)
                {
                    // Find in core
                    FieldInfo[] fields = Core.ManagerFields;
                    foreach (FieldInfo field in fields)
                    {
                        if (field.FieldType.FullName == id)
                        {
                            // Return field
                            __result = field;
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private static bool TryInjector(Core core, SplashCore splashCore, Type inType, ref ManagerBase __result, CoreManagerInjectors injectors)
        {
            // Find manager
            string id = inType.FullName;
            CoreManagerInjectors.ManagerData mgr = injectors.managerDatas.GetValueOrDefault(id);
            if (mgr != null)
            {
                // Check FT
                if (mgr.manager != null && mgr.manager is FeralTweaksManagerBase)
                {
                    // Return field
                    __result = mgr.manager;
                    return false;
                }
                
                // Check core (needed workaround else the game crashes, because of generic type shenanigans)
                if (core != null)
                {
                    // Find in core
                    FieldInfo[] fields = Core.ManagerFields;
                    foreach (FieldInfo field in fields)
                    {
                        if (field.FieldType.FullName == id)
                        {
                            // Return field
                            __result = field.GetValue(core).Cast<ManagerBase>();
                            return false;
                        }
                    }
                }
                
                // Check splashcore
                if (splashCore != null)
                {
                    // Find in core
                    FieldInfo[] fields = SplashCore.ManagerFields;
                    foreach (FieldInfo field in fields)
                    {
                        if (field.FieldType.FullName == id)
                        {
                            // Return field
                            __result = field.GetValue(splashCore).Cast<ManagerBase>();
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CoreBase<SplashCore>), "GetManagerInstance", new System.Type[] { typeof(Type) })]
        public static bool GetManagerInstance(Type inType, ref ManagerBase __result)
        {
            // FIXME: due to no access to generic types, theres a lot of limitation here, we cant determine which core we are on, so we have to check both for a match

            // Init
            Init();

            // Check core type
            Core core = null;
            SplashCore splashCore = null;
            if (SplashCore._loaded)
                splashCore = SplashCore.instance;
            if (Core._loaded)
                core = Core.instance;
            if (core == null && splashCore == null)
                return true;

            // Run for both if possible
            if (core != null)
            {
                // Core is prioritized, if a match is found in the core, its returned

                // Build if needed
                CoreManagerInjectors injectors = CoreManagerInjectors.Core;
                if (injectors.NeedsBuilding)
                {
                    Il2CppReferenceArray<FieldInfo> arr = CoreBase<Core>.ManagerFields;
                    ManagerBase[] mgrs = arr.Where(t => Il2CppType.Of<ManagerBase>().IsAssignableFrom(t.FieldType)).Select(t =>
                    {
                        Il2CppSystem.Object val = t.GetValue(core);
                        if (val != null)
                            return val.Cast<ManagerBase>();
                        return null;
                    }).Where(t => t != null).ToArray();
                    injectors.BuildIfNeeded(mgrs);
                }

                // Try run
                if (!TryInjector(core, splashCore, inType, ref __result, injectors))
                    return false;
            }
            if (splashCore != null)
            {
                CoreManagerInjectors injectors = CoreManagerInjectors.SplashCore;
                if (injectors.NeedsBuilding)
                {
                    Il2CppReferenceArray<FieldInfo> arr = CoreBase<SplashCore>.ManagerFields;
                    ManagerBase[] mgrs = arr.Where(t => Il2CppType.Of<ManagerBase>().IsAssignableFrom(t.FieldType)).Select(t =>
                    {
                        Il2CppSystem.Object val = t.GetValue(core);
                        if (val != null)
                            return val.Cast<ManagerBase>();
                        return null;
                    }).Where(t => t != null).ToArray();
                    injectors.BuildIfNeeded(mgrs);
                }

                // Try run
                if (!TryInjector(core, splashCore, inType, ref __result, injectors))
                    return false;
            }
            
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FieldInfo), "SetValue", new System.Type[] { typeof(Il2CppSystem.Object), typeof(Il2CppSystem.Object) })]
        public static void SetValue(ref FieldInfo __instance, ref Il2CppSystem.Object obj, ref Il2CppSystem.Object value)
        {
            // Init
            Init();

            // Check null
            if (obj == null)
                return;

            // Check core type
            Core core = obj.TryCast<Core>();
            SplashCore splashCore = obj.TryCast<SplashCore>();
            if (core != null)
                obj = core;
            if (splashCore != null)
                obj = splashCore;
            if (core == null && splashCore == null)
                return;

            // Get injectors
            CoreManagerInjectors injectors = null;
            if (core != null)
                injectors = CoreManagerInjectors.Core;
            else if (splashCore != null)
                injectors = CoreManagerInjectors.SplashCore;
            else
                return;
            if (!injectors.NeedsBuilding)
            {
                // Compare type
                string id = __instance.FieldType.FullName;
                CoreManagerInjectors.ManagerData mgr = injectors.managerDatas.GetValueOrDefault(id);
                if (mgr != null)
                {
                    // Check FT
                    if (mgr.manager != null && mgr.manager is FeralTweaksManagerBase)
                    {
                        // Replace field object so there will not be an assignment error 
                        obj = mgr.container;

                        // Update
                        FeralTweaksManagerBase nMgr = value.Cast<FeralTweaksManagerBase>();
                        if (nMgr != mgr.manager)
                        {
                            // Initialize
                            nMgr.container = mgr.container;
                            nMgr.setupGameObject = nMgr.gameObject;
                            nMgr.interceptionRules = mgr.manager.Cast<FeralTweaksManagerBase>().interceptionRules;

                            // Change value
                            if (mgr.manager != nMgr)
                            {
                                // Set
                                mgr.manager = nMgr;

                                // Destroy old and clean up
                                if (mgr.originalManager != null)
                                {
                                    mgr.originalManager.enabled = false;
                                    mgr.field.SetValue(mgr.container, null);
                                    InjectedManagersContainer.RegisteredManager mgrC = mgr.container.managers.FirstOrDefault(t =>
                                    {
                                        string id = t.manager.GetIl2CppType().FullName;
                                        if (id == mgr.id)
                                            return true;
                                        return false;
                                    });
                                    if (mgrC != null)
                                        mgr.container.managers.Remove(mgrC);
                                    UnityEngine.GameObject.Destroy(mgr.originalManager.gameObject);
                                    mgr.originalManager = null;
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CoreBase<SplashCore>), "GetManagerList")]
        public static void GetManagerList(ref Il2CppSystem.Object __instance, ref Il2CppSystem.Collections.Generic.IEnumerable<ManagerBase> __result)
        {
            // Init
            Init();

            // Init
            InitGuard();

            // Check core type
            Core core = __instance.TryCast<Core>();
            SplashCore splashCore = __instance.TryCast<SplashCore>();
            if (core != null)
                __instance = core;
            if (splashCore != null)
                __instance = splashCore;

            // Inject guard
            if (core != null)
            {
                CoreDestroyGuard guard = core.gameObject.GetComponent<CoreDestroyGuard>();
                if (guard == null)
                {
                    guard = core.gameObject.AddComponent<CoreDestroyGuard>();
                    guard.targetCore = core;
                    guard.targetInjectors = CoreManagerInjectors.Core;
                }
            }
            else if (splashCore != null)
            {
                CoreDestroyGuard guard = splashCore.gameObject.GetComponent<CoreDestroyGuard>();
                if (guard == null)
                {
                    guard = splashCore.gameObject.AddComponent<CoreDestroyGuard>();
                    guard.targetCore = splashCore;
                    guard.targetInjectors = CoreManagerInjectors.SplashCore;
                }
            }

            // Convert list
            ManagerBase[] mgrs = core == null ? new Il2CppSystem.Collections.Generic.List<ManagerBase>(__result).ToArray().Where(t => t != null).ToArray() : CoreBase<Core>.ManagerFields.Where(t => Il2CppType.Of<ManagerBase>().IsAssignableFrom(t.FieldType)).Select(t =>
            {
                Il2CppSystem.Object val = t.GetValue(core);
                if (val != null)
                    return val.Cast<ManagerBase>();
                return null;
            }).Where(t => t != null).ToArray();

            // Alter list
            if (core != null)
                mgrs = CoreManagerInjectors.Core.InjectManagers(mgrs);
            else if (splashCore != null)
                mgrs = CoreManagerInjectors.SplashCore.InjectManagers(mgrs);

            // Convert back
            __result = new Il2CppSystem.Collections.Generic.List<ManagerBase>(new Il2CppReferenceArray<ManagerBase>(mgrs).Cast<Il2CppSystem.Collections.Generic.IEnumerable<ManagerBase>>()).Cast<Il2CppSystem.Collections.Generic.IEnumerable<ManagerBase>>();
        }
    }
}