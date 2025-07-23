using FeralTweaks.Profiler.API;

namespace FeralTweaks.Profiler.Profiling
{
    /// <summary>
    /// FeralTweaks Profiler Layer (mostly configuration)
    /// </summary>
    public class ProfilerLayer
    {
        private string _id;
        internal string _name;
        internal int _durationWarningThresholdMs;
        internal bool _shouldThresholdWarn;
        internal bool _actAsFrame;
        internal ProfilerLayerCollection _collection;

        /// <summary>
        /// Quick access to the value to disable warning time fields
        /// </summary>
        public const int DISALBE_WARNING_VALUE = -1;

        internal ProfilerLayer(string id, string name, int durationWarningThresholdMs, bool shouldThresholdWarn, bool actAsFrame)
        {
            this._id = id;
            this._name = name;
            this._durationWarningThresholdMs = durationWarningThresholdMs;
            this._shouldThresholdWarn = shouldThresholdWarn;
            this._actAsFrame = actAsFrame;
        }
        
        /// <summary>
        /// Retrieves the layer ID
        /// </summary>
        public string ID
        {
            get
            {
                return _id;
            }
        }
        
        /// <summary>
        /// Retrieves the layer name
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// Retrieves the maximum duration (in milliseconds) of frames and captures during this profiler layer, any that exceed this limit will be logged should logging be enabled, or highlighted 
        /// </summary>
        public int DurationWarningThreshold
        {
            get
            {
                return _durationWarningThresholdMs;
            }
        }

        /// <summary>
        /// Retrieves whether passing the threshold should log a profiler warning
        /// </summary>
        public bool ShouldThresholdWarn
        {
            get
            {
                return _shouldThresholdWarn;
            }
        }
    }
}