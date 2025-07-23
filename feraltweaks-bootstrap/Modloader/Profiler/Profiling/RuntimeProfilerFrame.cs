using System;
using System.Collections.Generic;
using System.Diagnostics;
using FeralTweaks.Profiler.API;
using FeralTweaks.Profiler.Internal;
using MonoMod.Core.Platforms;

namespace FeralTweaks.Profiler.Profiling
{
    /// <summary>
    /// FeralTweaks Profiler Frame
    /// </summary>
    public class RuntimeProfilerFrame : ProfilerFrame
    {
        /// <inheritdoc/>
        public override RuntimeProfilerFrame Runtime => this;

        private RuntimeProfilerFrames framesCol;
        private ThreadLinkedObject thObj;
        private ProfilerLayer layer;

        internal bool isLayerFrame;
        internal bool parentNeedsClosing;
        internal bool recoverLastStatements;
        internal RuntimeProfilerFrame recoverLastStatementsLast;

        private RuntimeProfilerFrame parent;
        internal List<RuntimeProfilerFrame> children = new List<RuntimeProfilerFrame>();

        private long start;
        internal long end = -1;
        internal bool isOpen;
        private string layerId;
        private string frameId;
        private string frameName;
        internal StackFrame stackInfo;

        internal RuntimeProfilerFrame(RuntimeProfilerFrame parent, RuntimeProfilerFrames framesCol, ThreadLinkedObject thObj, ProfilerLayer layer, long start, string layerId, string frameId, string frameName)
        {
            this.parent = parent;
            this.framesCol = framesCol;
            this.thObj = thObj;
            this.layer = layer;
            this.start = start;
            this.layerId = layerId;
            this.frameId = frameId;
            this.frameName = frameName;
            this.isOpen = true;
        }

        /// <inheritdoc/>
        public override void CloseFrame()
        {
            // Close prepare
            if (!isOpen)
                throw new InvalidOperationException("Frame is already closed");
            end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Close
            isOpen = false;

            // Handle close on collection
            framesCol.OnCloseFrame(this);
        }

        /// <summary>
        /// Retrieves the timestamp the frame was opened at
        /// </summary>
        public long OpenedAt => start;

        /// <summary>
        /// Retrieves the timestamp the frame was closed at (-1 if not closed yet)
        /// </summary>
        public long ClosedAt => end;

        /// <summary>
        /// Retrieves the frame duration (how long the frame has been running, or if closed, how long it has run)
        /// </summary>
        public long Duration
        {
            get
            {
                long end;
                if (IsOpen)
                    end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                else
                    end = ClosedAt;
                return end - OpenedAt;
            }
        }

        /// <summary>
        /// Checks if the frame is open
        /// </summary>
        public bool IsOpen => isOpen;

        /// <summary>
        /// Frame layer ID
        /// </summary>
        public string LayerId => layerId;

        /// <summary>
        /// Frame ID
        /// </summary>
        public string FrameId => frameId;

        /// <summary>
        /// Frame human-readable name
        /// </summary>
        public string FrameName => frameName;

        /// <summary>
        /// Retrieves the profiler layer
        /// </summary>
        public ProfilerLayer Layer => layer;

        /// <summary>
        /// Retrieves the parent frame (returns null if there is no parent frame)
        /// </summary>
        public RuntimeProfilerFrame Parent => parent;

        /// <summary>
        /// Retrieves all child frames
        /// </summary>
        public RuntimeProfilerFrame[] Children => children.ToArray();

        // FIXME: captures
        // FIXME: metadata
    }
}