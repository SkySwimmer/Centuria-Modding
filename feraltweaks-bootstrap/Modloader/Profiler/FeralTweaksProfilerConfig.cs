using System;
using FeralTweaks.Logging;

namespace FeralTweaks.Profiler.Profiling
{
    /// <summary>
    /// Profiler configuration
    /// </summary>
    internal class FeralTweaksProfilerConfig
    {
        public bool enable = false;
        public bool enableOnDebugger = true;

        public bool automaticallyStartProfiling = false;
        public bool automaticallyStartTracking = false;
    }

    internal class FeralTweaksProfilerLayerConfig
    {
        public string name;
        public int durationWarningThresholdMs;
        public bool shouldThresholdWarn;
    }
}