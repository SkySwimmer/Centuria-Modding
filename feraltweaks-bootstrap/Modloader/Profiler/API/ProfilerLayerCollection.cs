using System;
using System.Collections.Generic;
using System.Reflection;
using FeralTweaks.Logging;
using FeralTweaks.Profiler.Profiling;

namespace FeralTweaks.Profiler.API
{
    /// <summary>
    /// FeralTweaks Profiler Layer Collection class
    /// </summary>
    public abstract class ProfilerLayerCollection
    {
        internal List<ProfilerLayer> _layers = new List<ProfilerLayer>();
        internal string _modID;
        internal bool _isModded;

        /// <summary>
        /// Registers a profiler layer
        /// </summary>
        /// <param name="id">Layer ID</param>
        /// <param name="name">Layer name</param>
        /// <param name="defaultDurationWarningThresholdMs">The maximum duration for each frame before warning or highlighting should be performed (-1 to disable)</param>
        /// <param name="defaultShouldThresholdWarn">If when the threshold is exceeded, warnings should be logged</param>
        protected void RegisterLayer(string id, string name, int defaultDurationWarningThresholdMs, bool defaultShouldThresholdWarn)
        {
            RegisterLayer(id, name, defaultDurationWarningThresholdMs, defaultShouldThresholdWarn, false);
        }

        /// <summary>
        /// Registers a profiler layer
        /// </summary>
        /// <param name="id">Layer ID</param>
        /// <param name="name">Layer name</param>
        /// <param name="defaultDurationWarningThresholdMs">The maximum duration for each frame before warning or highlighting should be performed (-1 to disable)</param>
        /// <param name="defaultShouldThresholdWarn">If when the threshold is exceeded, warnings should be logged</param>
        /// <param name="actAsFrame">If the profiler layer should act as a frame during profiling collection</param>
        protected void RegisterLayer(string id, string name, int defaultDurationWarningThresholdMs, bool defaultShouldThresholdWarn, bool actAsFrame)
        {
            if (_isModded && !id.StartsWith(_modID + "."))
                throw new ArgumentException("Layer ID is not qualified enough, please start the layer ID with `" + _modID + ".`");

            // Register
            ProfilerLayer layer = new ProfilerLayer(id, name, defaultDurationWarningThresholdMs, defaultShouldThresholdWarn, actAsFrame);
            layer._collection = this;
            _layers.Add(layer);
            Logger.GetLogger("Profiler").Debug("Collection provided layer " + id + " with name " + name);
        }

        internal void SetupAllLayer(string modID, bool isMod)
        {
            _modID = modID;
            _isModded = isMod;
            
            // Setup primary
            SetupLayers();

            // Setup reflective
            foreach (FieldInfo field in ((TypeInfo)GetType()).DeclaredFields)
            {
                if (!field.IsPublic)
                    continue;
                RegisterLayerAttribute attr = field.GetCustomAttribute<RegisterLayerAttribute>();
                string id = field.GetValue(field.IsStatic ? null : this).ToString();
                RegisterLayer(id, attr.Name, attr.DurationWarningThresholdMs, attr.ShouldThresholdWarn, attr.ActAsFrame);
            }
            foreach (PropertyInfo field in ((TypeInfo)GetType()).DeclaredProperties)
            {
                if (!field.GetMethod.IsPublic)
                    continue;
                RegisterLayerAttribute attr = field.GetCustomAttribute<RegisterLayerAttribute>();
                string id = field.GetMethod.Invoke(field.GetMethod.IsStatic ? null : this, new object[0]).ToString();
                RegisterLayer(id, attr.Name, attr.DurationWarningThresholdMs, attr.ShouldThresholdWarn, attr.ActAsFrame);
            }
        }

        /// <summary>
        /// Called to set up profiler layers
        /// </summary>
        public abstract void SetupLayers();
    }
}