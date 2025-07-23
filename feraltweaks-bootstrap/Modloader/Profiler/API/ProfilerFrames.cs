using System;
using System.Threading;
using FeralTweaks.Profiler.Internal;
using FeralTweaks.Profiler.Profiling;

namespace FeralTweaks.Profiler.API
{
    /// <summary>
    /// FeralTweaks Profiler Frame API
    /// </summary>
    public abstract class ProfilerFrames
    {
        /// <summary>
        /// Retrieves the frame interface of the current thread
        /// </summary>
        public static ProfilerFrames OfCurrentThread => ForThread(Thread.CurrentThread);

        /// <summary>
        /// Retrieves the frame interface of the specified thread
        /// </summary>
        /// <param name="thread">Thread for which to retrieve the frame interface</param>
        /// <returns>ProfilerFrames instance</returns>
        public static ProfilerFrames ForThread(Thread thread)
        {
            if (!FeralTweaksProfiler.IsEnabled)
                return new NoOpProfilerFrames();

            // Retrieve
            ThreadLinkedObject obj = ThreadLinkedObject.ForThread(thread);
            return obj.ProfilerFramesInstance;
        }

        /// <summary>
        /// Opens a new profiler thread
        /// </summary>
        /// <param name="layerId">Profiler layer ID</param>
        /// <param name="frameId">Profiler frame ID</param>
        /// <param name="frameName">Profiler frame name</param>
        /// <returns>ProfilerFrame instance</returns>
        public abstract ProfilerFrame OpenFrame(string layerId, string frameId, string frameName);

        /// <summary>
        /// Retrieves the runtime instance of the profiler frames interface
        /// </summary>
        /// <exception cref="ProfilerDisabledException">Thrown if accessed while the profiler is not enabled</exception> 
        public abstract RuntimeProfilerFrames Runtime { get; }
    }
}