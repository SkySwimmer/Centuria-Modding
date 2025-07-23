using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using FeralTweaks.Logging;
using FeralTweaks.Profiler.API;
using Newtonsoft.Json;

namespace FeralTweaks.Profiler.Profiling
{
    /// <summary>
    /// Profiler base type
    /// </summary>
    public static class FeralTweaksProfiler
    {
        private static bool _enabled;
        private static bool _isRunning;
        private static bool _isTracking;
        private static Logger _logger;

        private static string _profilerBaseDir;
        private static string _profilerConfigDir;

        private static FeralTweaksProfilerConfig _config;

        internal static void SetupProfiler()
        {
            // Called to set up the profiler
            _logger = FeralTweaks.Logging.Logger.GetLogger("Profiler");
            _logger.Debug("Preparing profiler...");
            
            // Setup
            _logger.Debug("Setting up folders...");
            _profilerBaseDir = "FeralTweaks/profiling";
            _profilerConfigDir = "FeralTweaks/config/profiling";
            Directory.CreateDirectory(_profilerConfigDir);

            // Setup configuration
            if (!File.Exists(_profilerConfigDir + "/profiler.json"))
            {
                // Save
                _logger.Debug("Loading default configuration...");
                _config = new FeralTweaksProfilerConfig();
                _logger.Debug("Saving default configuration...");
                File.WriteAllText(_profilerConfigDir + "/profiler.json", JsonConvert.SerializeObject(_config, Formatting.Indented));
            }
            else
            {
                // Load
                _logger.Debug("Loading profiler configuration...");
                _config = JsonConvert.DeserializeObject<FeralTweaksProfilerConfig>(File.ReadAllText(_profilerConfigDir + "/profiler.json"));
            }

            // Enable if needed
            _logger.Debug("Checking enabled state...");
            if (_config.enable || (_config.enableOnDebugger && Debugger.IsAttached))
            {
                // Enable
                _logger.Info("Profiler is enabled! Preparing profiler...");
                _enabled = true;
            }
            else
            {
                // Dont enable
                _logger.Info("Profiler is not active, exiting profiler...");
                return;
            }

            // Create profiler dir
            _logger.Debug("Setting up profiler folder...");
            Directory.CreateDirectory(_profilerBaseDir);

            // Load layers
            _logger.Info("Registering all profiler layers...");
            LoadLayersFor(typeof(FeralTweaksProfiler).Assembly, false, "FTL Base");
            FeralTweaksLoader.RunForMods(mod =>
            {
                // Load
                _logger.Debug("Registering profiling layers of mod " + mod.ID + "...");
                foreach (Assembly asm in mod.Assemblies)
                    LoadLayersFor(asm, true, mod.ID);
            });

            // Load layer configuraitons
            Directory.CreateDirectory(_profilerConfigDir + "/layers");
            _logger.Info("Setting up layer configurations...");
            foreach (ProfilerLayer layer in ProfilerLayers.AllLayers)
            {
                // Load config
                _logger.Debug("Checking config state for " + layer.ID + "...");
                FeralTweaksProfilerLayerConfig config;
                if (!File.Exists(_profilerConfigDir + "/layers/" + layer.ID + ".json"))
                {
                    // Save
                    _logger.Debug("Loading default configuration for " + layer.ID + "...");
                    config = new FeralTweaksProfilerLayerConfig();
                    config.name = layer.Name;
                    config.durationWarningThresholdMs = layer.DurationWarningThreshold;
                    config.shouldThresholdWarn = layer.ShouldThresholdWarn;
                    _logger.Info("Saving default configuration for " + layer.ID + "...");
                    File.WriteAllText(_profilerConfigDir + "/layers/" + layer.ID + ".json", JsonConvert.SerializeObject(config, Formatting.Indented));
                }
                else
                {
                    // Load
                    _logger.Info("Loading layer configuration of " + layer.ID + "...");
                    config = JsonConvert.DeserializeObject<FeralTweaksProfilerLayerConfig>(File.ReadAllText(_profilerConfigDir + "/layers/" + layer.ID + ".json"));
                    if (config.name == null)
                        throw new ArgumentException("Layer configuration " + layer.ID + " is missing important fields");
                }

                // Apply
                layer._name = config.name;
                layer._durationWarningThresholdMs = config.durationWarningThresholdMs;
                layer._shouldThresholdWarn = config.shouldThresholdWarn;
            }

            // Move on
            // FIXME
        }

        private static void LoadLayersFor(Assembly assembly, bool isMod, string modID)
        {
            _logger.Debug("Loading layers for " + assembly.GetName() + "...");
            foreach (Type t in assembly.GetTypes())
            {
                if (typeof(ProfilerLayerCollection).IsAssignableFrom(t))
                {
                    // Check attribute
                    if (t.GetCustomAttribute<RegisterLayersAttribute>() != null)
                    {
                        // Load
                        _logger.Debug("Loading layers container type " + t.FullName + "...");
                        ProfilerLayerCollection col = null;
                        try
                        {
                            col = (ProfilerLayerCollection)t.GetConstructor(new Type[0]).Invoke(new object[0]);
                            if (col == null)
                                throw new Exception();
                        }
                        catch
                        {
                            _logger.Fatal("Profiler initialization error: Profiler layer collection " + t.FullName + " does not have an accessile parameterless constructor!");
                            Environment.Exit(1);
                        }

                        // Setup
                        _logger.Debug("Setting up layers in collection " + t.FullName + "...");
                        col.SetupAllLayer(modID, isMod);

                        // Register all
                        _logger.Debug("Registering layers of collection " + t.FullName + "...");
                        foreach (ProfilerLayer layer in col._layers)
                        {
                            ProfilerLayers.RegisterLayer(layer);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the profiler logger
        /// </summary>
        public static Logger Logger
        {
            get
            {
                return _logger;
            }
        }

        /// <summary>
        /// Checks if the profiler is enabled
        /// </summary>
        public static bool IsEnabled
        {
            get
            {
                return _enabled;
            }
        }

        /// <summary>
        /// Checks if the profiler is running
        /// </summary>
        public static bool IsRunning
        {
            get
            {
                return _isRunning;
            }
        }

        /// <summary>
        /// Checks if the profiler is tracking
        /// </summary>
        public static bool IsTracking
        {
            get
            {
                return _isTracking;
            }
        }
    }
}