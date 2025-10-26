using System;

namespace FeralTweaks.Actions
{
    /// <summary>
    /// FeralTweaks callback system, a way to easily create callback wrappers on desired event queues, used by the type FeralTweaksAction and FeralTweaksPromise, usable separately as well
    /// </summary>
    public static class FeralTweaksCallbacks
    {
        private static FeralTweaksTargetEventQueue QueueBasedOnThread()
        {
            // Get thread
            int tId = Environment.CurrentManagedThreadId;

            // Compare
            if (FeralTweaksActions.actionThread.ManagedThreadId == tId)
                return FeralTweaksTargetEventQueue.FeralTweaks;
            else if (FeralTweaksActions.unityThread.ManagedThreadId == tId)
                return FeralTweaksTargetEventQueue.Unity;
            else
                return FeralTweaksTargetEventQueue.OnAction;
        }

        /// <summary>
        /// Creates a callback wrapper
        /// </summary>
        /// <param name="queue">Desired event queue</param>
        /// <param name="callback">Function to wrap</param>
        /// <returns>Wrapped function</returns>
        public static Action CreateQueuedWrapper(FeralTweaksTargetEventQueue queue, Action callback)
        {
            FeralTweaksTargetEventQueue target = queue;
            if (target == FeralTweaksTargetEventQueue.Automatic)
                target = QueueBasedOnThread();
            return () =>
            {
                // Check type
                FeralTweaksTargetEventQueue currentQueue = QueueBasedOnThread();
                if (target == FeralTweaksTargetEventQueue.OnAction || currentQueue == target)
                {
                    // Call directly
                    callback();
                    return;
                }

                // Queue
                if (target == FeralTweaksTargetEventQueue.Unity)
                    FeralTweaksActions.Unity.Oneshot(callback);
                else if (target == FeralTweaksTargetEventQueue.FeralTweaks)
                    FeralTweaksActions.EventQueue.Oneshot(callback);
            };
        }

        /// <summary>
        /// Creates a callback wrapper, automatically determining the target event queue by comparing the current thread
        /// </summary>
        /// <param name="callback">Function to wrap</param>
        /// <returns>Wrapped function</returns>
        public static Action CreateQueuedWrapper(Action callback)
        {
            return CreateQueuedWrapper(FeralTweaksTargetEventQueue.Automatic, callback);
        }
        
        /// <summary>
        /// Creates a callback wrapper
        /// </summary>
        /// <typeparam name="T">Callback parameter type</typeParam>
        /// <param name="queue">Desired event queue</param>
        /// <param name="callback">Function to wrap</param>
        /// <returns>Wrapped function</returns>
        public static Action<T> CreateQueuedWrapper<T>(FeralTweaksTargetEventQueue queue, Action<T> callback)
        {
            FeralTweaksTargetEventQueue target = queue;
            if (target == FeralTweaksTargetEventQueue.Automatic)
                target = QueueBasedOnThread();
            return (value) =>
            {
                // Check type
                FeralTweaksTargetEventQueue currentQueue = QueueBasedOnThread();
                if (target == FeralTweaksTargetEventQueue.OnAction || currentQueue == target)
                {
                    // Call directly
                    callback(value);
                    return;
                }

                // Queue
                if (target == FeralTweaksTargetEventQueue.Unity)
                    FeralTweaksActions.Unity.Oneshot(() => callback(value));
                else if (target == FeralTweaksTargetEventQueue.FeralTweaks)
                    FeralTweaksActions.EventQueue.Oneshot(() => callback(value));
            };
        }

        /// <summary>
        /// Creates a callback wrapper, automatically determining the target event queue by comparing the current thread
        /// </summary>
        /// <typeparam name="T">Callback parameter type</typeParam>
        /// <param name="callback">Function to wrap</param>
        /// <returns>Wrapped function</returns>
        public static Action<T> CreateQueuedWrapper<T>(Action<T> callback)
        {
            return CreateQueuedWrapper(FeralTweaksTargetEventQueue.Automatic, callback);
        }
    }
}