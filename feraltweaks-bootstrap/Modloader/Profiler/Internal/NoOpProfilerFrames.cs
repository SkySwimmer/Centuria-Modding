using System;
using System.Threading;
using FeralTweaks.Profiler.API;
using FeralTweaks.Profiler.Profiling;

namespace FeralTweaks.Profiler.Internal
{
    internal class NoOpProfilerFrames : ProfilerFrames
    {
        // No-operation implementation

        public override ProfilerFrame OpenFrame(string layerId, string frameId, string frameName)
        {
            return new NoOpProfilerFrame();
        }

        public override RuntimeProfilerFrames Runtime => throw new ProfilerDisabledException();

    }

    internal class NoOpProfilerFrame : ProfilerFrame
    {
        // No-operation implementation

        private bool isOpen = true;

        public override RuntimeProfilerFrame Runtime => throw new ProfilerDisabledException();

        public override void CloseFrame()
        {
            if (!isOpen)
                throw new InvalidOperationException("Frame is already closed");
        }
    }
}