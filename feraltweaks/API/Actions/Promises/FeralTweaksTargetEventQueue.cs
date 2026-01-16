namespace FeralTweaks.Actions
{
    /// <summary>
    /// Types of target event queues for action callbacks
    /// </summary>
    public enum FeralTweaksTargetEventQueue
    {
        /// <summary>
        /// Automatic detection of event queue
        /// </summary>
        Automatic,

        /// <summary>
        /// Run callback on the Unity event queue
        /// </summary>
        Unity,

        /// <summary>
        /// Run callback on the FeralTweaks event queue
        /// </summary>
        FeralTweaks,

        /// <summary>
        /// Synchronize to the action making the callback, no event queue
        /// </summary>
        OnAction
    }
}