using System;

namespace FeralTweaks.Profiler.Profiling
{
    /// <summary>
    /// Exception thrown when the profiler is disabled and user code is attempting to interact with the profiler
    /// </summary>
    public class ProfilerDisabledException : InvalidOperationException
    {
        public ProfilerDisabledException() : base("The profiler is not enabled")
        {
        }

        public ProfilerDisabledException(string message) : base(message)
        {
        }
    }
}