using System;
using System.Collections.Generic;

namespace FeralTweaks.Profiler.API
{
    /// <summary>
    /// Attribute for automatic layer registration
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class RegisterLayerAttribute : Attribute
    {
        public string Name { get; private set; }
        public int DurationWarningThresholdMs { get; private set; }
        public bool ShouldThresholdWarn { get; private set; }
        public bool ActAsFrame { get; private set; }

        /// <summary>
        /// Registers a profiler layer
        /// </summary>
        /// <param name="name">Layer name</param>
        /// <param name="defaultDurationWarningThresholdMs">The maximum duration for each frame before warning or highlighting should be performed (-1 to disable)</param>
        /// <param name="defaultShouldThresholdWarn">If when the threshold is exceeded, warnings should be logged</param>
        /// <param name="actAsFrame">If the profiler layer should act as a frame during profiling collection</param>
        public RegisterLayerAttribute(string name, int defaultDurationWarningThresholdMs, bool defaultShouldThresholdWarn, bool actAsFrame)
        {
            Name = name;
            DurationWarningThresholdMs = defaultDurationWarningThresholdMs;
            ShouldThresholdWarn = defaultShouldThresholdWarn;
            ActAsFrame = actAsFrame;
        }

        /// <summary>
        /// Registers a profiler layer
        /// </summary>
        /// <param name="name">Layer name</param>
        /// <param name="defaultDurationWarningThresholdMs">The maximum duration for each frame before warning or highlighting should be performed (-1 to disable)</param>
        /// <param name="defaultShouldThresholdWarn">If when the threshold is exceeded, warnings should be logged</param>
        public RegisterLayerAttribute(string name, int defaultDurationWarningThresholdMs, bool defaultShouldThresholdWarn)
        {
            Name = name;
            DurationWarningThresholdMs = defaultDurationWarningThresholdMs;
            ShouldThresholdWarn = defaultShouldThresholdWarn;
            ActAsFrame = false;
        }
    }
}