using System;
using FeralTweaks.Profiler.Profiling;

namespace FeralTweaks.Profiler.API
{
    /// <summary>
    /// FeralTweaks Profiler Frame
    /// </summary>
    public abstract class ProfilerFrame
    {
        // FIXME implement fully

        /// <summary>
        /// Closes the profiler frame
        /// </summary>
        public abstract void CloseFrame();

        /// <summary>
        /// Retrieves the runtime instance of the profiler frame
        /// </summary>
        /// <exception cref="ProfilerDisabledException">Thrown if accessed while the profiler is not enabled</exception> 
        public abstract RuntimeProfilerFrame Runtime { get; }

    }
}