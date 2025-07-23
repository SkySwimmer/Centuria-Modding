using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FeralTweaks.Logging;
using FeralTweaks.Profiler.API;

namespace FeralTweaks.Profiler.Profiling
{
    /// <summary>
    /// FeralTweaks Profiler Layer (mostly configuration)
    /// </summary>
    public class ProfilerLayers
    {
        internal static Dictionary<string, ProfilerLayer> _layers = new Dictionary<string, ProfilerLayer>();

        internal static void RegisterLayer(ProfilerLayer layer)
        {
            if (!Regex.Match(layer.ID, "^[0-9A-Za-z_.]+$").Success)
            {
                Logger.GetLogger("Profiler").Fatal("Profiler initialization error: Profiler layer " + layer.ID + " has an invalid ID, only IDs consisting of alphanumeric characters, periods and underscores are allowed.");
                Environment.Exit(1);
            }
            if (_layers.ContainsKey(layer.ID))
            {
                Logger.GetLogger("Profiler").Fatal("Profiler initialization error: Profiler layer " + layer.ID + " was already registered.\n\nOriginal registerd by " + GetLayerById(layer.ID)._collection._modID + " (collection " + GetLayerById(layer.ID)._collection.GetType().FullName + ")\nReregister attempt by " + layer._collection._modID + " (collection " + layer._collection.GetType().FullName + ")");
                Environment.Exit(1);
            }
            Logger.GetLogger("Profiler").Info("Registered profiler layer " + layer.ID + " with name " + layer.Name + "!");
            _layers[layer.ID] = layer;
        }
        
        /// <summary>
        /// Retrieves all registered profiler layers
        /// </summary>
        /// <exception cref="ProfilerDisabledException">Thrown if accessed while the profiler is not enabled</exception> 
        public static ProfilerLayer[] AllLayers
        {
            get
            {
                if (!FeralTweaksProfiler.IsEnabled)
                    throw new ProfilerDisabledException();
                return _layers.Values.ToArray();
            }
        }

        /// <summary>
        /// Retrieves profile layers by ID
        /// </summary>
        /// <param name="id">Profile layer ID</param>
        /// <returns>ProfilerLayer instance </returns>
        /// <exception cref="ProfilerDisabledException">Thrown if accessed while the profiler is not enabled</exception> 
        /// <exception cref="ArgumentException">Thrown if the layer is not recognized</exception>
        public static ProfilerLayer GetLayerById(string id)
        {
            if (!FeralTweaksProfiler.IsEnabled)
                throw new ProfilerDisabledException();
            if (!_layers.TryGetValue(id, out ProfilerLayer val))
                throw new ArgumentException("Profiler layer not recognized: " + id);
            return val;
        }
    }
}