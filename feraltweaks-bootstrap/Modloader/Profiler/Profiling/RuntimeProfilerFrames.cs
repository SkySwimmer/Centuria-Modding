using System;
using System.Diagnostics;
using FeralTweaks.Profiler.API;
using FeralTweaks.Profiler.Internal;

namespace FeralTweaks.Profiler.Profiling
{
    /// <summary>
    /// FeralTweaks Profiler Frame API
    /// </summary>
    public class RuntimeProfilerFrames : ProfilerFrames
    {
        /// <inheritdoc/>
        public override RuntimeProfilerFrames Runtime => this;

        private ThreadLinkedObject _thObj;

        internal RuntimeProfilerFrames(ThreadLinkedObject thObj)
        {
            _thObj = thObj;
        }

        private RuntimeProfilerFrame _current;
        private RuntimeProfilerFrame _last;
        private RuntimeProfilerFrame _lastOrig;

        /// <inheritdoc/>
        public override ProfilerFrame OpenFrame(string layerId, string frameId, string frameName)
        {
            return OpenFrame(layerId, frameId, frameName, false, new StackTrace(true).GetFrame(1));
        }

        private ProfilerFrame OpenFrame(string layerId, string frameId, string frameName, bool skipFrame, StackFrame stackInfo)
        {
            // Find layer
            ProfilerLayer layer = ProfilerLayers.GetLayerById(layerId);

            // Prepare parent
            RuntimeProfilerFrame parent = _current;
            bool needToCloseParent = false;

            // Check layer
            if (layer._actAsFrame && !skipFrame)
            {
                // Find layer frame
                bool needCreateFrame = true;
                RuntimeProfilerFrame cFrame = parent;
                while (cFrame != null)
                {
                    // Check if layer frame
                    if (cFrame.isLayerFrame)
                    {
                        // Its a layer frame
                        // We dont have to open a new frame if we are still in the same layer frame

                        // Check ID
                        if (cFrame.FrameId == layer.ID)
                            needCreateFrame = false; // If the frame doesnt match the id, this wont be false
                        break;
                    }

                    // Go to parent
                    cFrame = cFrame.Parent;
                }

                // Check result
                if (needCreateFrame)
                {
                    // Check current frame's last child
                    if (_current != null)
                    {
                        RuntimeProfilerFrame[] frames = _current.Children;
                        if (frames.Length > 0)
                        {
                            // Check last
                            RuntimeProfilerFrame last = frames[frames.Length - 1];
                            if (last.isLayerFrame)
                            {
                                // Its a layer frame

                                // Check ID
                                if (last.FrameId == layer.ID)
                                {
                                    // No need to create a frame
                                    needCreateFrame = false;

                                    // Make parent
                                    parent = last;
                                    needToCloseParent = true;

                                    // Reopen this frame
                                    last.isOpen = true;
                                    last.end = -1;
                                    _current = last;
                                }
                            }
                        }
                    }

                    // Check result
                    if (needCreateFrame)
                    {
                        // Still no success
                        // Check last, if its a layer frame
                        if (_current == null && _last != null && _last.isLayerFrame)
                        {
                            // Its a layer frame

                            // Check ID
                            if (_last.FrameId == layer.ID)
                            {
                                // No need to create a frame
                                needCreateFrame = false;

                                // Make parent
                                parent = _last;

                                // Deal with closing recovery
                                parent.recoverLastStatements = true;
                                parent.recoverLastStatementsLast = _lastOrig;

                                // Reopen this frame
                                parent.end = -1;
                                parent.isOpen = true;
                                _last = _lastOrig; // Restore last statement to correct object
                                _current = parent; // Make current
                                needToCloseParent = true;
                            }
                        }
                    }
                }

                // Check result
                if (needCreateFrame)
                {
                    // Create parent frame
                    parent = (RuntimeProfilerFrame)OpenFrame(layer.ID, layer.ID, layer.Name, true, new StackTrace(true).GetFrame(0));
                    parent.isLayerFrame = true;
                    needToCloseParent = true;
                }
            }

            // Create frame
            RuntimeProfilerFrame frame = new RuntimeProfilerFrame(parent, this, _thObj, layer, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), layerId, frameId, frameName);
            frame.parentNeedsClosing = needToCloseParent;
            frame.stackInfo = stackInfo;

            // Add to parent if needed
            if (parent != null)
                parent.children.Add(frame);

            // Make current
            _current = frame;

            // Return
            return frame;
        }

        internal void OnCloseFrame(RuntimeProfilerFrame frame)
        {
            // Check current
            while (_current != frame)
            {
                // Uh oh
                FeralTweaksProfiler.Logger.Error("Profiler frame " + _current.FrameId + " of layer " + _current.LayerId + " was never closed! Closing it reluctantly...");
                FeralTweaksProfiler.Logger.Error("Note: unclosed frames will result in profiling accuracy loss");
                FeralTweaksProfiler.Logger.Error("Frame was opened at " + _current.stackInfo.GetMethod().DeclaringType.FullName + "." + _current.stackInfo.GetMethod().Name + (_current.stackInfo.GetFileName() != null ? " in " + _current.stackInfo.GetFileName() + ":" + _current.stackInfo.GetFileLineNumber() + ":" + _current.stackInfo.GetFileColumnNumber() : ""));
                _current.CloseFrame();
            }

            // Move current to last if needed
            if (frame.Parent == null)
            {
                _lastOrig = _last;
                _last = _current;

                // FIXME: a full root finished
                // FIXME: it should be captured
            }

            // Set parent as current
            _current = frame.Parent; 
            
            // Restore
            if (frame.recoverLastStatements)
                _lastOrig = frame.recoverLastStatementsLast;
            frame.recoverLastStatements = false;
            frame.recoverLastStatementsLast = null;

            // Close parent if needed
            if (frame.parentNeedsClosing)
                frame.Parent.CloseFrame();

            // FIXME: implement, make sure to move the current to last if needed, and original last to original last
        }

        /// <summary>
        /// Checks if a current frame is present
        /// </summary>
        public bool HasCurrentFrame
        {
            get
            {
                return _current != null;
            }
        }

        /// <summary>
        /// Checks if a last frame is present
        /// </summary>
        public bool HasLastFrame
        {
            get
            {
                return _last != null;
            }
        }

        /// <summary>
        /// Retrieves the current profiler frame
        /// </summary>
        public RuntimeProfilerFrame CurrentFrame
        {
            get
            {
                return _current;
            }
        }

        /// <summary>
        /// Retrieves the last profiler frame
        /// </summary>
        public RuntimeProfilerFrame LastFrame
        {
            get
            {
                return _last;
            }
        }
    }
}