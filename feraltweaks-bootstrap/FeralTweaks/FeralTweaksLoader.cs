using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FeralTweaks.Mods;

namespace FeralTweaks 
{
    /// <summary>
    /// FeralTweaks Modloader Type
    /// </summary>
    public static class FeralTweaksLoader
    {
        private const string VERSION = "v1.0.0-alpha-a2";
        private static StreamWriter LogWriter;
        private static List<FeralTweaksMod> mods;

        /// <summary>
        /// Retrieves all loaded mods
        /// </summary>
        /// <returns>Array of FeralTweaksMod instances</returns>
        public static FeralTweaksMod[] GetLoadedMods()
        {
            return mods.ToArray();
        }

        /// <summary>
        /// Checks if a mod is loaded by its ID
        /// </summary>
        /// <param name="id">Mod ID</param>
        /// <returns>True if loaded, false otherwise</returns>
        public static bool IsModLoaded(string id)
        {
            return mods.Any(t => t.ID == id);
        }

        /// <summary>
        /// Retrieves loaded mods by ID
        /// </summary>
        /// <param name="id">Mod ID</param>
        /// <returns>FeralTweaksMod instance</returns>
        public static FeralTweaksMod GetLoadedMod(string id)
        {
            if (IsModLoaded(id))
                return mods.Find(t => t.ID == id);
            throw new ArgumentException("Specified mod is not loaded");
        }

        /// <summary>
        /// Retrieves loaded mods by type
        /// </summary>
        /// <returns>FeralTweaksMod instance</returns>
        public static T GetLoadedMod<T>() where T : FeralTweaksMod
        {
            if (mods.Any(t => t is T))
                return (T)mods.Find(t => t is T);
            throw new ArgumentException("Specified mod is not loaded");
        }

        /// <summary>
        /// Logs an info message
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void LogInfo(string message)
        {
            LogWriter.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [INF] " + message);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void LogWarn(string message)
        {
            LogWriter.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [WRN] " + message);
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void LogError(string message)
        {
            LogWriter.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [ERR] " + message);
        }

        private static void Logger_MessageReceived(object sender, HarmonyLib.Tools.Logger.LogEventArgs e)
        {
            switch (e.LogChannel)
            {
                case HarmonyLib.Tools.Logger.LogChannel.Info:
                    LogInfo(e.Message);
                    break;
                case HarmonyLib.Tools.Logger.LogChannel.Warn:
                    LogWarn(e.Message);
                    break;
                case HarmonyLib.Tools.Logger.LogChannel.Error:
                    LogError(e.Message);
                    break;
                case HarmonyLib.Tools.Logger.LogChannel.Debug:
                    break;
                case HarmonyLib.Tools.Logger.LogChannel.IL:
                    break;
            }
        }

        internal static void LoadFinish()
        {
            // Log
            LogInfo("Initial loading completed!");
            LogInfo("Post-initializing mods...");
            RunForMods(mod =>
            {
                LogInfo("Post-initializing mod: " + mod.ID);
                mod.PostInit();
            });
        }

        internal static void Start()
        {
            // Set up log
            LogWriter = new StreamWriter("FeralTweaks/logs/loader.log");
            LogWriter.AutoFlush = true;

            // Log
            LogInfo("Preparing...");
            LogInfo("FeralTweaks Loader version " + VERSION + " initializing...");
            HarmonyLib.Tools.Logger.MessageReceived += Logger_MessageReceived;

            // Log and prepare
            LogInfo("Preparing to load mods...");
            Directory.CreateDirectory("FeralTweaks/mods");
            Directory.CreateDirectory("FeralTweaks/config");
            mods = new List<FeralTweaksMod>();

            // Discover mods
            LogInfo("Discovering mods...");
            List<string> modDirs = new List<string>();
            foreach (FileInfo mod in new DirectoryInfo("FeralTweaks/mods").GetFiles("*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    // Add mod assembly folder to resolution if not done yet
                    string dir = mod.DirectoryName;
                    if (!modDirs.Contains(dir))
                    {
                        // Add folder to assembly resolution
                        AppDomain.CurrentDomain.AssemblyResolve += (s, args) =>
                        {
                            // Attempt to resolve
                            AssemblyName nm = new AssemblyName(args.Name);
                            if (File.Exists(dir + "/" + nm.Name + ".dll"))
                            {
                                return Assembly.LoadFile(Path.GetFullPath(dir + "/" + nm.Name + ".dll"));
                            }
                            return null;
                        };
                        modDirs.Add(dir);
                    }

                    // Load assembly
                    Assembly asm = Assembly.Load(mod.Name.Remove(mod.Name.LastIndexOf(".dll")));

                    // Find mod types
                    LoadModsFrom(asm);
                }
                catch (Exception e)
                {
                    LogError("Failed to load mod file: " + mod.Name + ": " + e);
                }
            }

            // Load mods
            LogInfo("Discovered " + mods.Count + " mods.");
            LogInfo("Loading mods...");
            RunForMods(mod =>
            {
                LogInfo("Loading mod: " + mod.ID + ", version " + mod.Version + "...");
                mod.PreInit();
            });

            // Initialize mods
            LogInfo("Initializing mods...");
            RunForMods(mod =>
            {
                // Log
                LogInfo("Initializing mod: " + mod.ID);

                // Create config and cache folder
                Directory.CreateDirectory("FeralTweaks/config/" + mod.ID);
                Directory.CreateDirectory("FeralTweaks/cache/" + mod.ID);
                mod.ConfigDir = Path.GetFullPath("FeralTweaks/config/" + mod.ID);
                mod.CacheDir = Path.GetFullPath("FeralTweaks/cache/" + mod.ID);

                // Init
                mod.Init();
            });
        }

        private static void LoadModsFrom(Assembly asm)
        {
            foreach (Type t in asm.GetTypes())
            {
                if (t.IsAssignableTo(typeof(FeralTweaksMod)) && !t.IsAbstract)
                {
                    try
                    {
                        // Attempt to load type
                        ConstructorInfo constr = t.GetConstructor(new Type[0]);
                        if (constr == null)
                            throw new ArgumentException("No empty constructor");
                        FeralTweaksMod inst = (FeralTweaksMod)constr.Invoke(new object[0]);
                        try
                        {
                            // Attempt to load mod instance
                            inst.Initialize();

                            // Find existing mod
                            if (IsModLoaded(inst.ID))
                                throw new ArgumentException("Duplicate mod detected! ID: " + inst.ID + " was loaded twice!");
                            mods.Add(inst);
                        }
                        catch (Exception e)
                        {
                            LogError("Failed to load mod: " + inst.ID + ": " + e);
                        }
                    }
                    catch (Exception e)
                    {
                        LogError("Failed to load mod: " + t.FullName + ": " + e);
                    }
                }
            }
        }

        private static void RunForMods(Action<FeralTweaksMod> ex)
        {
            List<string> loading = new List<string>();
            foreach (FeralTweaksMod mod in mods)
            {
                RunFor(mod, loading, ex);
            }
        }

        private static void RunFor(FeralTweaksMod mod, List<string> loading, Action<FeralTweaksMod> ex)
        {
            // Skip double loads
            if (loading.Contains(mod.ID))
                return;
            loading.Add(mod.ID);

            // Load dependencies first
            foreach (string dep in mod._depends)
            {
                if (!IsModLoaded(dep))
                {
                    // Failure
                    LogError("Unable to load mod " + mod.ID + ", missing dependency mod: " + dep);
                    Environment.Exit(1);
                    return;
                }

                // Run for dependency
                RunFor(GetLoadedMod(dep), loading, ex);
            }

            // Load optional dependencies after that
            foreach (string dep in mod._optDepends)
            {
                if (IsModLoaded(dep))
                {
                    // Run for dependency
                    RunFor(GetLoadedMod(dep), loading, ex);
                }
            }

            // Check conflicts
            foreach (string conflict in mod._conflicts)
            {
                if (IsModLoaded(conflict))
                {
                    // Failure
                    LogError("Mod conflict! Unable to load mod " + mod.ID + " as it conflicts with " + conflict + "!");
                    Environment.Exit(1);
                    return;
                }
            }

            // Check load-after of other mods
            foreach (FeralTweaksMod md in mods)
            {
                if (md._loadAfter.Contains(mod.ID))
                {
                    // Load this one first
                    RunFor(md, loading, ex);
                }
            }

            // Load
            ex(mod);
        }
    }
}