using System;
using System.Collections.Generic;
using System.Diagnostics;
using FeralTweaks.Logging;

namespace FeralTweaks.Actions
{
    /// <summary>
    /// FeralTweaks Promise Interface
    /// </summary>
    /// <typeparam name="T">Promise result type</typeparam>
    public abstract class FeralTweaksPromise<T>
    {
        private List<Action<T>> _onCompleteHandlers = new List<Action<T>>();
        private List<Action<Exception>> _onErrorHandlers = new List<Action<Exception>>();


        /// <summary>
        /// Checks if the promise completed
        /// </summary>
        public abstract bool HasCompleted { get; }
        
        /// <summary>
        /// Checks if the promise has errored
        /// </summary>
        public abstract bool HasErrored { get; }

        /// <summary>
        /// Retrieves the exception should one be present
        /// </summary>
        /// <returns>Exception instance or null</returns>
        public abstract Exception GetException();

        /// <summary>
        /// Retrieves the promise result
        /// 
        /// <para>Note: this does NOT await the action, use AwaitResult() instead to await the action result</para>
        /// </summary>
        /// <returns>Function result or null</returns>
        public abstract T GetResult();


        /// <summary>
        /// Called to add complete handlers
        /// </summary>
        /// <param name="handler">Handler to add</param>
        protected abstract void ProcessAddCompleteHandler(Action<T> handler);

        /// <summary>
        /// Called to add error handlers
        /// </summary>
        /// <param name="handler">Handler to add</param>
        protected abstract void ProcessAddErrorHandler(Action<Exception> handler);

        /// <summary>
        /// Adds the handler internally
        /// </summary>
        /// <param name="handler">Handler to add</param>
        protected virtual void ProcessAddCompleteHandlerInternal(Action<T> handler)
        {
            lock (_onCompleteHandlers)
                _onCompleteHandlers.Add(handler);
        }

        /// <summary>
        /// Adds the handler internally
        /// </summary>
        /// <param name="handler">Handler to add</param>
        protected virtual void ProcessAddErrorHandlerInternal(Action<Exception> handler)
        {
            lock (_onErrorHandlers)
                _onErrorHandlers.Add(handler);
        }

        /// <summary>
        /// Clears all handlers
        /// </summary>
        protected void ClearHandlers()
        {
            _onCompleteHandlers.Clear();
            _onErrorHandlers.Clear();
        }

        /// <summary>
        /// Runs the OnComplete event
        /// </summary>
        /// <param name="value">Result value</param>
        protected void RunOnComplete(T value)
        {
            List<Action<T>> acL = new List<Action<T>>();
            lock (_onCompleteHandlers)
            {
                acL.AddRange(_onCompleteHandlers);
                _onCompleteHandlers.Clear();
            }
            foreach (Action<T> ac in acL)
            {
                try
                {
                    ac(value);
                }
                catch (Exception e)
                {
                    Logger.GetLogger("ActionManager").Error("An exception was thrown while running an OnComplete action", e);
                    if (Debugger.IsAttached)
                        throw;
                }
            }
        }

        /// <summary>
        /// Runs the OnError event
        /// </summary>
        /// <param name="exception">Exception that was thrown</param>
        protected void RunOnError(Exception exception)
        {
            List<Action<Exception>> acL = new List<Action<Exception>>();
            lock (_onErrorHandlers)
            {
                acL.AddRange(_onErrorHandlers);
                _onCompleteHandlers.Clear();
            }
            foreach (Action<Exception> ac in acL)
            {
                try
                {
                    ac(exception);
                }
                catch (Exception e)
                {
                    Logger.GetLogger("ActionManager").Error("An exception was thrown while running an OnError action", e);
                    if (Debugger.IsAttached)
                        throw;
                }
            }
        }


        /// <summary>
        /// Adds an on complete handler
        /// </summary>
        /// <param name="handler">Handler to add</param>
        public FeralTweaksPromise<T> OnComplete(Action<T> handler)
        {
            handler = FeralTweaksCallbacks.CreateQueuedWrapper(handler);
            ProcessAddCompleteHandler(handler);
            return this;
        }

        /// <summary>
        /// Adds an on complete handler
        /// </summary>
        /// <param name="handler">Handler to add</param>
        public FeralTweaksPromise<T> OnComplete(Action handler)
        {
            handler = FeralTweaksCallbacks.CreateQueuedWrapper(handler);
            ProcessAddCompleteHandler((val) => handler());
            return this;
        }

        /// <summary>
        /// Adds an on error handler
        /// </summary>
        /// <param name="handler">Error handler to add</param>
        public FeralTweaksPromise<T> OnError(Action<Exception> handler)
        {
            handler = FeralTweaksCallbacks.CreateQueuedWrapper(handler);
            ProcessAddErrorHandler(handler);
            return this;
        }

        /// <summary>
        /// Adds an on complete handler
        /// </summary>
        /// <param name="handler">Error handler to add</param>
        public FeralTweaksPromise<T> OnError(Action handler)
        {
            handler = FeralTweaksCallbacks.CreateQueuedWrapper(handler);
            ProcessAddErrorHandler((val) => handler());
            return this;
        }

        /// <summary>
        /// Adds an on complete handler
        /// </summary>
        /// <param name="queue">Target event queue to run the handler on</param>
        /// <param name="handler">Handler to add</param>
        public FeralTweaksPromise<T> OnComplete(FeralTweaksTargetEventQueue queue, Action<T> handler)
        {
            handler = FeralTweaksCallbacks.CreateQueuedWrapper(queue, handler);
            ProcessAddCompleteHandler(handler);
            return this;
        }

        /// <summary>
        /// Adds an on complete handler
        /// </summary>
        /// <param name="queue">Target event queue to run the handler on</param>
        /// <param name="handler">Handler to add</param>
        public FeralTweaksPromise<T> OnComplete(FeralTweaksTargetEventQueue queue, Action handler)
        {
            handler = FeralTweaksCallbacks.CreateQueuedWrapper(queue, handler);
            ProcessAddCompleteHandler((val) => handler());
            return this;
        }

        /// <summary>
        /// Adds an on error handler
        /// </summary>
        /// <param name="queue">Target event queue to run the handler on</param>
        /// <param name="handler">Error handler to add</param>
        public FeralTweaksPromise<T> OnError(FeralTweaksTargetEventQueue queue, Action<Exception> handler)
        {
            handler = FeralTweaksCallbacks.CreateQueuedWrapper(queue, handler);
            ProcessAddErrorHandler(handler);
            return this;
        }

        /// <summary>
        /// Adds an on complete handler
        /// </summary>
        /// <param name="queue">Target event queue to run the handler on</param>
        /// <param name="handler">Error handler to add</param>
        public FeralTweaksPromise<T> OnError(FeralTweaksTargetEventQueue queue, Action handler)
        {
            handler = FeralTweaksCallbacks.CreateQueuedWrapper(queue, handler);
            ProcessAddErrorHandler((val) => handler());
            return this;
        }
    }
}