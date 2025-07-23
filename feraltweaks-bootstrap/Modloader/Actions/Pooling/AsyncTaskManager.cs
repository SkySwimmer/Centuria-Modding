using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FeralTweaks.Actions.Internal.AsyncTasks
{
    /// <summary>
    /// Async Task Manager - Alternate thread pooler
    /// </summary>
    public static class AsyncTaskManager
    {
        private static List<AsyncTaskThread> _threads = new List<AsyncTaskThread>();
        private static List<AsyncTask> _queuedTasks = new List<AsyncTask>();

        internal static AsyncTask ObtainNext()
        {
            lock (_queuedTasks)
            {
                if (_queuedTasks.Count == 0)
                    return null;
                AsyncTask tsk = _queuedTasks[0];
                _queuedTasks.RemoveAt(0);
                return tsk;
            }
        }

        /// <summary>
        /// Runs a action asynchronously
        /// </summary>
        /// <param name="action">Action to run</param>
        /// <returns>AsyncTask</returns>
        public static AsyncTask RunAsync(Action action)
        {
            AsyncTask tsk = new AsyncTask(action);

            // Check if any thread is available
            // If not, start a new one
            lock (_threads)
            {
                AsyncTaskThread[] threads;
                while (true)
                {
                    try
                    {
                        threads = _threads.ToArray();
                        break;
                    }
                    catch { }
                }

                // Check if any thread is available
                if (!threads.Any(t => t.IsAvailable))
                {
                    // Start new thread
                    AsyncTaskThread th = new AsyncTaskThread();
                    _threads.Add(th);
                    Thread thI = new Thread(() =>
                    {
                        // Start
                        th.Run();

                        // End
                        lock (_threads)
                            _threads.Remove(th);
                    });
                    thI.Name = "FeralTweaks Async Task Thread";
                    thI.IsBackground = true;
                    thI.Start();
                }
            }

            // Queue task
            lock (_queuedTasks)
            {
                _queuedTasks.Add(tsk);
            }

            // Return
            return tsk;
        }
    }
}