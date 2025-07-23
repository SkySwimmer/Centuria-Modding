using System;
using System.Threading;

namespace FeralTweaks.Actions.Internal.AsyncTasks
{
    /// <summary>
    /// Async Task Container
    /// </summary>
    public class AsyncTask
    {
        private Action _action;
        private bool _run;
        
        /// <summary>
        /// Creates a task container
        /// </summary>
        /// <param name="action">Action assigned to the container</param>
        public AsyncTask(Action action)
        {
            _action = action;
        }

        internal void Run()
        {
            try
            {
                _action();
            }
            finally
            {
                _run = true;
            }
        }

        /// <summary>
        /// Checks if the task has been run already
        /// </summary>
        public bool HasRun
        {
            get
            {
                return _run;
            }
        }

        /// <summary>
        /// Waits for the task to finish running
        /// </summary>
        public void Block()
        {
            while (!_run)
                Thread.Sleep(1);
        }

    }
}