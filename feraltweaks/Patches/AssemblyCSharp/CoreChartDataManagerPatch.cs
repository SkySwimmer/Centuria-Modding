using HarmonyLib;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Threading;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class CoreChartDataManagerPatch
    {
        private class MirrorList<T, T2> : List<T> where T2 : T where T : BaseDef
        {
            private List<T2> delegateList;
            private bool loading = true;
            public MirrorList(List<T2> delegateList)
            {
                this.delegateList = delegateList;
                foreach (T obj in delegateList)
                    Add(obj);
                loading = false;
            }

            public override void Add(T item)
            {
                base.Add(item);
                if (!loading)
                    delegateList.Add((T2)item);
            }

        }

        private delegate BaseDef DefCreator();

        public static Dictionary<string, BaseDef> DefCache = new Dictionary<string, BaseDef>();
        private static bool patched;
        private static bool safeToLoad;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CraftableItemChartData), "CreateDef")]
        public static void CreateDef(CraftableItemChartData __instance)
        {
            safeToLoad = true;
            SetChartObjectInstances();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocalizationChartData), "Get")]
        public static bool Get(string inDefID, string inDefault, ref LocalizationChartData __instance, ref string __result)
        {
            if (DefCache.ContainsKey(inDefID))
            {
                __result = DefCache[inDefID].GetComponent<LocalizationDefComponent>().LocalizedString;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocalizationChartData), "GetString")]
        public static bool GetString(string inDefID, ref string __result)
        {
            if (DefCache.ContainsKey(inDefID))
            {
                __result = DefCache[inDefID].GetComponent<LocalizationDefComponent>().LocalizedString;
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CoreChartDataManager), "SetChartObjectInstances")]
        public static void SetChartObjectInstances()
        {
            if (!safeToLoad)
                return;
            if (patched)
                return;
            patched = true;

            // Okay time to load over the original game
            FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogInfo("Loading chart patches...");

            // Check
            if (!Plugin.PatchConfig.ContainsKey("DisableClientChartPatches") || Plugin.PatchConfig["DisableClientChartPatches"] != "True")
            {
                // Create patch directory
                Directory.CreateDirectory(FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().ConfigDir + "/chartpatches");

                // Read patches
                foreach (FileInfo file in new DirectoryInfo(FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().ConfigDir + "/chartpatches").GetFiles("*.cdpf", SearchOption.AllDirectories))
                {
                    string patch = File.ReadAllText(file.FullName).Replace("\t", "    ").Replace("\r", "");
                    ApplyPatch(patch, file.Name);
                }
            }

            // Other patches
            foreach ((string patch, string fileName) in Plugin.Patches)
            {
                ApplyPatch(patch, fileName);
            }
        }

        private static void ApplyPatch(string patch, string fileName)
        {
            // Parse patch
            FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogInfo("Loading patch: " + fileName);
            bool inPatchBlock = false;
            bool inNewDefBlock = false;
            string defID = "";
            string defData = "";
            ChartDataObject chart = null;
            List<BaseDef> defs = null;
            DefCreator defCreator = () => new BaseDef();
            foreach (string line in patch.Split('\n'))
            {
                if (!inPatchBlock && !inNewDefBlock)
                {
                    if (line == "" || line.StartsWith("//") || line.StartsWith("#"))
                        continue;

                    // Check command
                    System.Collections.Generic.List<string> args = new System.Collections.Generic.List<string>(line.Split(" "));
                    if (args.Count <= 1)
                    {
                        FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogError("Invalid command: " + line + " found while parsing " + fileName);
                        break;
                    }
                    else
                    {
                        string cmd = args[0];
                        args.RemoveAt(0);

                        bool error = false;
                        switch (cmd)
                        {
                            case "setchart":
                                {
                                    string chartName = args[0];
                                    switch (chartName)
                                    {
                                        case "WorldObjectChart":
                                            {
                                                chart = ChartDataManager.instance.worldObjectChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.worldObjectChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.worldObjectChartData.defList;
                                                break;
                                            }
                                        case "LocalizationChart":
                                            {
                                                chart = ChartDataManager.instance.localizationChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.localizationChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = new MirrorList<BaseDef, LocalizationDef>(ChartDataManager.instance.localizationChartData.defList);
                                                defCreator = () => new LocalizationDef();
                                                break;
                                            }
                                        case "ActorAttachNodeChart":
                                            {
                                                chart = ChartDataManager.instance.actorAttachNodeChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.actorAttachNodeChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.actorAttachNodeChartData.defList;
                                                break;
                                            }
                                        case "CalendarChart":
                                            {
                                                chart = ChartDataManager.instance.calendarChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.calendarChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.calendarChartData.defList;
                                                break;
                                            }
                                        case "DialogChart":
                                            {
                                                chart = ChartDataManager.instance.dialogChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.dialogChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.dialogChartData.defList;
                                                break;
                                            }
                                        case "ActorScaleGroupChart":
                                            {
                                                chart = ChartDataManager.instance.actorScaleGroupChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.actorScaleGroupChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.actorScaleGroupChartData.defList;
                                                break;
                                            }
                                        case "ActorAttachNodeGroupChart":
                                            {
                                                chart = ChartDataManager.instance.actorAttachNodeGroupChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.actorAttachNodeGroupChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.actorAttachNodeGroupChartData.defList;
                                                break;
                                            }
                                        case "ActorNPCChart":
                                            {
                                                chart = ChartDataManager.instance.actorNPCChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.actorNPCChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.actorNPCChartData.defList;
                                                break;
                                            }
                                        case "ActorBodyPartNodeChart":
                                            {
                                                chart = ChartDataManager.instance.actorBodyPartNodeChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.actorBodyPartNodeChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.actorBodyPartNodeChartData.defList;
                                                break;
                                            }
                                        case "WorldSurfaceChart":
                                            {
                                                chart = ChartDataManager.instance.worldSurfaceChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.worldSurfaceChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.worldSurfaceChartData.defList;
                                                break;
                                            }
                                        case "URLChart":
                                            {
                                                chart = ChartDataManager.instance.urlChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.urlChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.urlChartData.defList;
                                                break;
                                            }
                                        case "CraftableItemChart":
                                            {
                                                chart = ChartDataManager.instance.craftableItemChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.craftableItemChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.craftableItemChartData.defList;
                                                break;
                                            }
                                        case "AudioChart":
                                            {
                                                chart = ChartDataManager.instance.audioChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.audioChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.audioChartData.defList;
                                                break;
                                            }
                                        case "HarvestPointChart":
                                            {
                                                chart = ChartDataManager.instance.harvestPointChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.harvestPointChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.harvestPointChartData.defList;
                                                break;
                                            }
                                        case "QuestChart":
                                            {
                                                chart = ChartDataManager.instance.questChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.questChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.questChartData.defList;
                                                break;
                                            }
                                        case "ObjectiveChart":
                                            {
                                                chart = ChartDataManager.instance.objectiveChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.objectiveChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.objectiveChartData.defList;
                                                break;
                                            }
                                        case "TaskChart":
                                            {
                                                chart = ChartDataManager.instance.taskChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.taskChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.taskChartData.defList;
                                                break;
                                            }
                                        case "InteractableChart":
                                            {
                                                chart = ChartDataManager.instance.interactableChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.interactableChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.interactableChartData.defList;
                                                break;
                                            }
                                        case "BundleIDChart":
                                            {
                                                chart = ChartDataManager.instance.bundleIDChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.bundleIDChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.bundleIDChartData.defList;
                                                break;
                                            }
                                        case "BundlePackChart":
                                            {
                                                chart = ChartDataManager.instance.bundlePackChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.bundlePackChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.bundlePackChartData.defList;
                                                break;
                                            }
                                        case "ShopContentChart":
                                            {
                                                chart = ChartDataManager.instance.shopContentChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.shopContentChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.shopContentChartData.defList;
                                                break;
                                            }
                                        case "ShopChart":
                                            {
                                                chart = ChartDataManager.instance.shopChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.shopChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.shopChartData.defList;
                                                break;
                                            }
                                        case "LootChart":
                                            {
                                                chart = ChartDataManager.instance.lootChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.lootChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = new MirrorList<BaseDef, LootDef>(ChartDataManager.instance.lootChartData.defList);
                                                defCreator = () => new LootDef();
                                                break;
                                            }
                                        case "NetworkedObjectsChart":
                                            {
                                                chart = ChartDataManager.instance.networkedObjectsChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.networkedObjectsChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.networkedObjectsChartData.defList;
                                                break;
                                            }
                                        case "ColorChart":
                                            {
                                                chart = ChartDataManager.instance.colorChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.colorChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = new MirrorList<BaseDef, ColorDef>(ChartDataManager.instance.colorChartData.defList);
                                                defCreator = () => new ColorDef();
                                                break;
                                            }
                                        case "GlobalChart":
                                            {
                                                chart = ChartDataManager.instance.globalChartData;

                                                // Let it load
                                                while (chart == null)
                                                {
                                                    chart = ChartDataManager.instance.globalChartData;
                                                    Thread.Sleep(100);
                                                }
                                                defs = ChartDataManager.instance.globalChartData.defList;
                                                break;
                                            }
                                        default:
                                            {
                                                FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogError("Invalid command: " + line + " found while parsing " + fileName + ": chart not recognized");
                                                error = true;
                                                break;
                                            }
                                    }
                                    break;
                                }
                            case "cleardef":
                                {
                                    if (chart == null)
                                    {
                                        FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogError("Invalid command: " + line + " found while parsing " + fileName + ": no active chart set");
                                        error = true;
                                        break;
                                    }
                                    FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogInfo("Clear def: " + args[0]);
                                    BaseDef def = chart.GetDef(args[0]);
                                    if (def == null)
                                        FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogError("Error! Definition not found!");
                                    else
                                    {
                                        def._components._components.Clear();
                                        DefCache[args[0]] = def;
                                    }
                                    break;
                                }
                            case "patch":
                                {
                                    if (chart == null)
                                    {
                                        FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogError("Invalid command: " + line + " found while parsing " + fileName + ": no active chart set");
                                        error = true;
                                        break;
                                    }
                                    inPatchBlock = true;
                                    defData = "";
                                    defID = args[0];
                                    break;
                                }
                            case "def":
                                {
                                    if (chart == null)
                                    {
                                        FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogError("Invalid command: " + line + " found while parsing " + fileName + ": no active chart set");
                                        error = true;
                                        break;
                                    }
                                    inNewDefBlock = true;
                                    defData = "";
                                    defID = args[0];
                                    break;
                                }
                            default:
                                {
                                    FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogError("Invalid command: " + line + " found while parsing " + fileName);
                                    error = true;
                                    break;
                                }
                        }
                        if (error)
                            break;
                    }
                }
                else
                {
                    string l = line;
                    if (l == "endpatch" && inPatchBlock)
                    {
                        // Apply patch
                        inPatchBlock = false;
                        string chartPatch = defData;
                        defData = "";

                        // Get def
                        FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogInfo("Patching " + defID + " in chart " + chart.ChartName);
                        BaseDef def = chart.GetDef(defID, true);
                        if (def == null)
                            FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogError("Error! Definition not found!");
                        else
                        {
                            PatchDef(chartPatch, def);
                            DefCache[defID] = def;
                        }
                        continue;
                    }
                    else if (l == "enddef" && inNewDefBlock)
                    {
                        // Apply patch
                        inPatchBlock = false;
                        string chartPatch = defData;
                        defData = "";

                        // Get def
                        FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogInfo("Creating " + defID + " in chart " + chart.ChartName);
                        BaseDef def = chart.GetDef(defID, true);
                        if (def != null)
                            FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogError("Error! Chart definition already exists");
                        else
                        {
                            def = defCreator();
                            def.LoadDataJSON(chartPatch);
                            DefCache[defID] = def;
                            defs.Add(def);
                        }
                        continue;
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        if (!l.StartsWith(" "))
                            break;
                        l = l.Substring(1);
                    }
                    if (defData == "")
                        defData = l;
                    else
                        defData += l + "\n";
                }
            }
        }

        private static void PatchDef(string chartPatch, BaseDef def)
        {
            if (def == null)
                return;
            // Patch
            Dictionary<Type, List<ComponentBase>> components = new Dictionary<Type, List<ComponentBase>>();
            if (def._components != null && def._components._components != null)
            {
                foreach (Type t in def._components._components.Keys)
                {
                    components[t] = def._components._components[t];
                }
                def._components._components.Clear();
            }
            def.LoadDataJSON(chartPatch);

            if (def._components != null && def._components._components != null)
            {
                foreach (Type t in components.Keys)
                {
                    List<ComponentBase> lst = new List<ComponentBase>();
                    if (def._components._components.ContainsKey(t))
                        lst = def._components._components[t];
                    else
                        def._components._components[t] = lst;
                    foreach (ComponentBase comp in components[t])
                    {
                        lst.Add(comp);
                    }
                }
            }
        }
    }
}
