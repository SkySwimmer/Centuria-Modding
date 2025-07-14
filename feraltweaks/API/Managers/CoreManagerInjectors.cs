using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem.Reflection;

namespace FeralTweaks.Managers
{
    public class CoreManagerInjectors
    {
        /// <summary>
        /// Core injected managers
        /// </summary>
        public static CoreManagerInjectors Core = new CoreManagerInjectors();

        /// <summary>
        /// SplashCore injected managers
        /// </summary>
        public static CoreManagerInjectors SplashCore = new CoreManagerInjectors();

        private static bool _inited;

        private static void Init()
        {
            if (_inited)
                return;
            _inited = true;
            ClassInjector.RegisterTypeInIl2Cpp<FeralTweaksManagerBase>();
        }

        private List<InjectedManagersContainer> containers = new List<InjectedManagersContainer>();
        internal List<ManagerData> loadOrder = new List<ManagerData>();
        internal Dictionary<string, ManagerData> managerDatas = new Dictionary<string, ManagerData>();
        private bool built = false;
        private bool building = false;
        private object lck = new object();

        internal bool NeedsBuilding { get { return !built; } }

        internal void WipeBuiltManagers()
        {
            built = false;
            building = false;
            managerDatas.Clear();
            loadOrder.Clear();
        }

        /// <summary>
        /// Registers manager containers to inject into the game
        /// </summary>
        /// <typeparam name="T">Container type</typeparam>
        public void RegisterManagerContainer<T>() where T : InjectedManagersContainer
        {
            // Init
            Init();

            // Check
            if (built || building)
                throw new InvalidOperationException("Unable to register managers after the game has inited, please use ModPreInit or ModEarlyInit to register managers");

            // Inject
            InjectedManagersContainer.InjectManagerContainerTypeIfNeeded<T>();

            // Ignore if present
            if (containers.Any(t => t.GetType().FullName == typeof(T).FullName))
                return;

            // Instantiate
            T container;
            try
            {
                container = (T)typeof(T).GetConstructor(new Type[0]).Invoke(new object[0]);
            }
            catch
            {
                throw new ArgumentException("Could not invoke empty constructor on type " + typeof(T).Name + "!");
            }

            // Register
            containers.Add(container);
            container.Setup();
        }

        internal class ManagerData
        {
            public string id;
            public ManagerBase manager;
            public ManagerBase originalManager;
            public FeralTweaksManagerLoadRule[] rules = new FeralTweaksManagerLoadRule[0];
            public InjectedManagersContainer container;
            public FieldInfo field;

            public bool loadFirst;
            public bool loadLast;
            public int priority;
            public List<string> loadBefore = new List<string>();
            public List<string> dependsOn = new List<string>();
            public Dictionary<string, Il2CppSystem.Type> dependsTypes = new Dictionary<string, Il2CppSystem.Type>();
        }

        internal void BuildIfNeeded(ManagerBase[] sourceManagers)
        {
            // Init
            Init();
            lock (lck)
            {
                if (built)
                    return;
                building = true;

                // Build a new list of managers of the game combined with the managers registered
                List<ManagerData> allManagers = new List<ManagerData>();
                ManagerBase last = null;
                foreach (ManagerBase mgr in sourceManagers)
                {
                    // Create rule if needed
                    FeralTweaksManagerLoadRule[] rules = new FeralTweaksManagerLoadRule[0];
                    if (last != null)
                        rules = new FeralTweaksManagerLoadRule[] { new FeralTweaksManagerLoadRule(last.GetIl2CppType(), FeralTweaksManagerLoadRuleType.DEPENDSON, 0) }; // Make sure the base load chain remains intact
                    allManagers.Add(new ManagerData() { manager = mgr, rules = rules });
                    last = mgr;
                }
                foreach (InjectedManagersContainer cont in containers)
                {
                    foreach (InjectedManagersContainer.RegisteredManager mgr in cont.managers)
                    {
                        allManagers.Add(new ManagerData() { manager = mgr.manager, rules = mgr.loadRules, field = mgr.field, container = cont });
                    }
                }

                // Set up data
                foreach (ManagerData data in allManagers)
                {
                    // Set ID
                    data.id = data.manager.GetIl2CppType().FullName;

                    // LoadBefore and LoadAfter
                    foreach (FeralTweaksManagerLoadRule rule in data.rules)
                    {
                        if (rule.RuleType == FeralTweaksManagerLoadRuleType.LOADBEFORE)
                            data.loadBefore.Add(rule.TargetManager.FullName);
                        else if (rule.RuleType == FeralTweaksManagerLoadRuleType.DEPENDSON)
                        {
                            data.dependsOn.Add(rule.TargetManager.FullName);
                            data.dependsTypes[rule.TargetManager.FullName] = rule.TargetManager;
                        }
                    }

                    // Set priority and loadfirst
                    foreach (FeralTweaksManagerLoadRule rule in data.rules)
                    {
                        if (rule.RuleType == FeralTweaksManagerLoadRuleType.LOADPRIORITY && (rule.RuleValue > data.priority || data.priority == 0))
                            data.priority = rule.RuleValue;
                        else if (rule.RuleType == FeralTweaksManagerLoadRuleType.LOADFIRST)
                            data.loadFirst = true;
                        else if (rule.RuleType == FeralTweaksManagerLoadRuleType.LOADLAST)
                            data.loadLast = true;
                    }
                }

                // Sort by priority
                allManagers = new List<ManagerData>(allManagers.OrderBy(t => -t.priority));

                // Build load order
                List<ManagerData> loadOrder = new List<ManagerData>();
                List<string> loading = new List<string>();

                // First do all LoadFirst 
                foreach (ManagerData mgr in allManagers)
                {
                    if (mgr.loadFirst)
                        AddToLoadOrder(mgr, loading, loadOrder, allManagers);
                }

                // Then load the rest
                foreach (ManagerData mgr in allManagers)
                {
                    if (CheckHasLoadBefore(mgr, allManagers))
                        continue; // Skip anything with a loadbefore statement until loaded most managers, so loadbefore really happens just before the target manager
                    if (!mgr.loadLast && !CheckDepsLoadLast(mgr, allManagers)) // Skip any loadlast instances and any depending on a loadlast instance
                        AddToLoadOrder(mgr, loading, loadOrder, allManagers);
                }

                // Then load rest 
                foreach (ManagerData mgr in allManagers)
                {
                    if (CheckHasLoadBefore(mgr, allManagers))
                        continue; // Skip anything with a loadbefore statement until loaded most managers, so loadbefore really happens just before the target manager
                    if (!CheckDepsLoadLast(mgr, allManagers)) // Skip any dependent on loadlast entries
                        AddToLoadOrder(mgr, loading, loadOrder, allManagers);
                }

                // Then load rest 
                foreach (ManagerData mgr in allManagers)
                    AddToLoadOrder(mgr, loading, loadOrder, allManagers);

                // Strip instances from non-ft managers to prevent garbage collection trouble
                foreach (ManagerData mgr in loadOrder)
                {
                    if (!(mgr.manager is FeralTweaksManagerBase))
                        mgr.manager = null;
                    else
                        mgr.originalManager = mgr.manager;
                }

                // Store load order as IDs
                this.loadOrder = loadOrder;

                // Save instances by ID
                Dictionary<string, ManagerData> managerDatas = new Dictionary<string, ManagerData>();
                foreach (ManagerData mgr in loadOrder)
                    managerDatas[mgr.id] = mgr;
                this.managerDatas = managerDatas;

                // Mark done
                built = true;
            }
        }

        private bool CheckDepsLoadLast(ManagerData mgr, List<ManagerData> managers)
        {
            // Check dependencies
            foreach (string dep in mgr.dependsOn)
            {
                if (managers.Any(t => t.id == dep))
                {
                    // Check if loadlast
                    ManagerData depD = managers.Find(t => t.id == dep);
                    if (depD.loadLast)
                        return true;
                    else
                        return CheckDepsLoadLast(depD, managers);
                }
            }
            return false;
        }

        private bool CheckHasLoadBefore(ManagerData mgr, List<ManagerData> managers)
        {
            // Checl loadBefore
            if (mgr.loadBefore.Count != 0)
                return true;

            // Check dependencies
            foreach (string dep in mgr.dependsOn)
            {
                if (managers.Any(t => t.id == dep))
                {
                    // Check if loadlast
                    ManagerData depD = managers.Find(t => t.id == dep);
                    return CheckDepsLoadLast(depD, managers);
                }
            }
            return false;
        }

        private void AddToLoadOrder(ManagerData mgr, List<string> loading, List<ManagerData> loadOrder, List<ManagerData> managers)
        {
            // Skip double loads
            if (loading.Contains(mgr.id))
                return;
            loading.Add(mgr.id);

            // Load dependencies first
            foreach (string dep in mgr.dependsOn)
            {
                if (managers.Any(t => t.id == dep))
                {
                    // Run for dependency
                    AddToLoadOrder(managers.Find(t => t.id == dep), loading, loadOrder, managers);
                }
            }

            // Check load-before of other managers
            foreach (ManagerData md in managers)
            {
                if (md.loadBefore.Contains(mgr.id))
                {
                    // Load this one first
                    AddToLoadOrder(md, loading, loadOrder, managers);
                }
            }

            // Add to list
            loadOrder.Add(mgr);
        }

        internal ManagerBase[] InjectManagers(ManagerBase[] sourceList)
        {
            // Build
            BuildIfNeeded(sourceList);

            // Transform list
            List<ManagerBase> lst = new List<ManagerBase>();
            
            // Go through load order
            foreach (ManagerData manager in loadOrder)
            {
                ManagerBase mgrI = manager.manager;
                if (mgrI == null)
                {
                    // Try map
                    mgrI = sourceList.Where(t => t.GetIl2CppType().FullName == manager.id).FirstOrDefault();
                }

                // Check
                if (mgrI != null)
                    lst.Add(mgrI);
            }

            // Return
            return lst.ToArray();
        }
    }
}