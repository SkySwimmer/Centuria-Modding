using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using FeralTweaks.Logging;

namespace FeralTweaks.Actions
{
    /// <summary>
    /// FeralTweaks action interface
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    public class FeralTweaksAction<T> : FeralTweaksPromise<T>
    {
        internal bool _denyContinueCall;

        private FeralTweaksActionType _type;
        private bool _hasCompleted;
        private bool _hasRun;
        private bool _hasRunI;
        private bool _awaitIsSafe; // Overrides await safety should the manager have taken nested synchronity into account

        private object _lock = new object();
        private object _lockFullComplete = new object();

        private T _cResult = default(T);
        private Exception _ex;

        protected class RunHandlerInfo
        {
            public Action<T> action;
            public bool runOnError;
            public bool persist;
        }

        private List<RunHandlerInfo> _onRunHandlers = new List<RunHandlerInfo>();

        private Func<FeralTweaksActionExecutionContext<T>, T> _action;

        private long _timeStart = -1;
        private long _millisWait = -1;

        private int _interval = 0;
        private int _limit = 1;

        private int _cInterval = 0;
        private int _cCount = 0;

        private bool _cancel;

        internal FeralTweaksAction(Func<FeralTweaksActionExecutionContext<T>, T> ac, FeralTweaksActionType type, bool awaitIsSafe, long millisWait, int interval, int limit)
        {
            _action = ac;
            _type = type;
            _awaitIsSafe = awaitIsSafe;
            _millisWait = millisWait;
            _interval = interval;
            _limit = limit;
        }

        /// <summary>
        /// Retrieves if the action was cancelled
        /// </summary>
        public bool WasCancelled
        {
            get
            {
                return _cancel;
            }
        }

        /// <summary>
        /// Retrieves how often the action has ticked (caps at maxint)
        /// </summary>
        public int TickCount
        {
            get
            {
                return _cCount;
            }
        }

        /// <summary>
        /// Retrieves the action tick interval (how many ticks before each time the action is run)
        /// </summary>
        public int ActionInterval
        {
            get
            {
                return _interval;
            }
        }

        /// <summary>
        /// Retrieves how often the action can run
        /// </summary>
        public int ActionLimit
        {
            get
            {
                return _limit;
            }
        }

        /// <summary>
        /// Retrieves the amount of remaining ticks before the action stops running
        /// </summary>
        public int TicksRemaining
        {
            get
            {
                if (_limit == -1)
                    return -1;
                return _limit - _cCount;
            }
        }

        /// <summary>
        /// Retrieves the amount of ticks that remain before the action is run
        /// </summary>
        public int TicksBeforeStart
        {
            get
            {
                if (_interval <= 0 || _cCount >= _limit)
                    return 0;
                return _interval - _cInterval;
            }
        }

        /// <summary>
        /// Checks how many milliseconds are left before the action is invoked
        /// </summary>
        public long TimeRemainingBeforeInvoke
        {
            get
            {
                if (_millisWait == -1)
                    return -1;
                long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (time - _timeStart > _millisWait)
                    return 0;
                return _millisWait - (time - _timeStart);
            }
        }

        /// <summary>
        /// Checks if the action completed
        /// </summary>
        public override bool HasCompleted
        {
            get
            {
                return _hasCompleted;
            }
        }

        /// <summary>
        /// Checks if the action has errored
        /// </summary>
        public override bool HasErrored
        {
            get
            {
                return _ex != null;
            }
        }

        /// <summary>
        /// Checks if the action ran at least once
        /// </summary>
        public bool HasRun
        {
            get
            {
                return _hasRun;
            }
        }

        /// <summary>
        /// Cancels the action
        /// 
        /// <para>Note: Cancel only works if the action has not run yet or is a repeating action, it cannot end execution</para>
        /// </summary>
        public void Cancel()
        {
            _cancel = true;
        }

        /// <summary>
        /// Retrieves the exception should one be present
        /// </summary>
        /// <returns>Exception instance or null</returns>
        public override Exception GetException()
        {
            return _ex;
        }

        /// <summary>
        /// Retrieves the function result
        /// 
        /// <para>Note: this does NOT await the action, use AwaitResult() instead to await the action result</para>
        /// </summary>
        /// <returns>Function result or null</returns>
        public override T GetResult()
        {
            return _cResult;
        }

        /// <summary>
        /// Waits for the function to finish a single tick
        /// 
        /// <para>Note: this call is to await the next tick that becomes available, meaning even if it has completed once, this will block on each call until the next tick finishes</para>
        /// <para>Further note: its unreliable to retrieve results using AwaitTick() and then GetResult(), please use AwaitNextResult() to reliably await the next result otherwise race conditions might occur</para>
        /// </summary>
        /// <returns>True if successful, false if the function has thrown an exception</returns>
        /// <exception cref="InvalidOperationException">If the current thread context is unsafe to await in</exception>
        public bool AwaitTick()
        {
            // Check safety
            if (!_awaitIsSafe && !FeralTweaksActions.IsAwaitSafeOnCurrentThread(_type))
                throw new InvalidOperationException("AwaitTick() call is unsafe in current context, as it would lock up the active action thread");

            bool hasReceivedRun = false;
            ProcessAddRunHandler(new RunHandlerInfo()
            {
                action = (val) => hasReceivedRun = true,
                persist = false,
                runOnError = true
            });
            lock (_lock)
            {
                lock (_lockFullComplete)
                {
                    // Check completed
                    if (_hasCompleted)
                        return _ex != null;
                }

                // Check result
                if (hasReceivedRun)
                    return _ex != null;

                // Wait
                while (!_hasRunI || !hasReceivedRun)
                    Monitor.Wait(_lock);
            }

            // Check exception
            return _ex != null;
        }

        /// <summary>
        /// Waits for the function to finish completely
        /// </summary>
        /// <returns>True if successful, false if the function has thrown an exception</returns>
        /// <exception cref="InvalidOperationException">If the current thread context is unsafe to await in</exception>
        public bool AwaitComplete()
        {
            // Check safety
            if (!_awaitIsSafe && !FeralTweaksActions.IsAwaitSafeOnCurrentThread(_type))
                throw new InvalidOperationException("AwaitComplete() call is unsafe in current context, as it would lock up the active action thread");

            lock (_lockFullComplete)
            {
                // Check completed
                if (_hasCompleted)
                    return _ex != null;

                // Wait
                while (!_hasCompleted)
                    Monitor.Wait(_lockFullComplete);
            }

            // Check exception
            return _ex != null;
        }

        /// <summary>
        /// Waits for the function to finish a single tick and retrieves the result
        /// 
        /// <para>Note: this call is to await the next tick that becomes available, meaning even if it has completed once, this will block on each call until the next tick finishes</para>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if the current thread context is unsafe to await in</exception>
        /// <exception cref="TargetInvocationException">Thrown if the target method causes an exception</exception>
        public T AwaitNextResult()
        {
            // Check safety
            if (!_awaitIsSafe && !FeralTweaksActions.IsAwaitSafeOnCurrentThread(_type))
                throw new InvalidOperationException("AwaitNextResult() call is unsafe in current context, as it would lock up the active action thread");

            lock (_lock)
            {
                lock (_lockFullComplete)
                {
                    // Check completed
                    if (_hasCompleted)
                        return GetResult();
                }
            }

            T res = default(T);
            bool hasReceivedRun = false;
            ProcessAddRunHandler(new RunHandlerInfo()
            {
                action = (val) =>
                {
                    res = val;
                    hasReceivedRun = true;
                },
                persist = false,
                runOnError = true
            });
            lock (_lock)
            {
                lock (_lockFullComplete)
                {
                    // Check completed
                    if (_hasCompleted && _ex != null)
                        throw new TargetInvocationException("Target function has thrown an exception", _ex); // Throw exception
                    else if (_hasCompleted)
                        return _cResult;
                }

                // Check result
                if (hasReceivedRun)
                    return res;

                // Wait
                while (!_hasRunI || !hasReceivedRun)
                    Monitor.Wait(_lock);
            }

            // Check exception
            if (_ex != null)
                throw new TargetInvocationException("Target function has thrown an exception", _ex); // Throw exception

            // Return
            return res;
        }

        /// <summary>
        /// Waits for the function to finish completely
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if the current thread context is unsafe to await in</exception>
        /// <exception cref="TargetInvocationException">Thrown if the target method causes an exception</exception>
        public T AwaitCompleteResult()
        {
            // Check safety
            if (!_awaitIsSafe && !FeralTweaksActions.IsAwaitSafeOnCurrentThread(_type))
                throw new InvalidOperationException("AwaitCompleteResult() call is unsafe in current context, as it would lock up the active action thread");

            lock (_lock)
            {
                lock (_lockFullComplete)
                {
                    // Check completed
                    if (_hasCompleted)
                        return GetResult();
                }
            }

            // Wait
            AwaitComplete();

            // Check exception
            if (_ex != null)
                throw new TargetInvocationException("Target function has thrown an exception", _ex); // Throw exception

            // Return
            return _cResult;
        }


        /// <summary>
        /// Adds an on run handler
        /// </summary>
        /// <param name="handler">Handler to add</param>
        /// <param name="persist">Controls if the handler should remain in the handler list after its run</param>
        public FeralTweaksAction<T> OnRun(Action<T> handler, bool persist = false)
        {
            handler = FeralTweaksCallbacks.CreateQueuedWrapper(handler);
            ProcessAddRunHandler(new RunHandlerInfo()
            {
                action = handler,
                persist = persist
            });
            return this;
        }

        /// <summary>
        /// Adds an on run handler
        /// </summary>
        /// <param name="handler">Handler to add</param>
        /// <param name="persist">Controls if the handler should remain in the handler list after its run</param>
        public FeralTweaksAction<T> OnRun(Action handler, bool persist = false)
        {
            handler = FeralTweaksCallbacks.CreateQueuedWrapper(handler);
            ProcessAddRunHandler(new RunHandlerInfo()
            {
                action = (val) => handler(),
                persist = persist
            });
            return this;
        }

        /// <summary>
        /// Adds an on run handler
        /// </summary>
        /// <param name="queue">Target event queue to run the handler on</param>
        /// <param name="handler">Handler to add</param>
        /// <param name="persist">Controls if the handler should remain in the handler list after its run</param>
        public FeralTweaksAction<T> OnRun(FeralTweaksTargetEventQueue queue, Action<T> handler, bool persist = false)
        {
            handler = FeralTweaksCallbacks.CreateQueuedWrapper(queue, handler);
            ProcessAddRunHandler(new RunHandlerInfo()
            {
                action = handler,
                persist = persist
            });
            return this;
        }

        /// <summary>
        /// Adds an on run handler
        /// </summary>
        /// <param name="queue">Target event queue to run the handler on</param>
        /// <param name="handler">Handler to add</param>
        /// <param name="persist">Controls if the handler should remain in the handler list after its run</param>
        public FeralTweaksAction<T> OnRun(FeralTweaksTargetEventQueue queue, Action handler, bool persist = false)
        {
            handler = FeralTweaksCallbacks.CreateQueuedWrapper(queue, handler);
            ProcessAddRunHandler(new RunHandlerInfo()
            {
                action = (val) => handler(),
                persist = persist
            });
            return this;
        }

        /// <summary>
        /// Adds an on complete handler
        /// </summary>
        /// <param name="handler">Handler to add</param>
        public new FeralTweaksAction<T> OnComplete(Action<T> handler)
        {
            return (FeralTweaksAction<T>)base.OnComplete(handler);
        }

        /// <summary>
        /// Adds an on complete handler
        /// </summary>
        /// <param name="handler">Handler to add</param>
        public new FeralTweaksAction<T> OnComplete(Action handler)
        {
            return (FeralTweaksAction<T>)base.OnComplete(handler);
        }

        /// <summary>
        /// Adds an on error handler
        /// </summary>
        /// <param name="handler">Error handler to add</param>
        public new FeralTweaksAction<T> OnError(Action<Exception> handler)
        {
            return (FeralTweaksAction<T>)base.OnError(handler);
        }

        /// <summary>
        /// Adds an on complete handler
        /// </summary>
        /// <param name="handler">Error handler to add</param>
        public new FeralTweaksAction<T> OnError(Action handler)
        {
            return (FeralTweaksAction<T>)base.OnError(handler);
        }

        /// <summary>
        /// Adds an on complete handler
        /// </summary>
        /// <param name="queue">Target event queue to run the handler on</param>
        /// <param name="handler">Handler to add</param>
        public new FeralTweaksAction<T> OnComplete(FeralTweaksTargetEventQueue queue, Action<T> handler)
        {
            return (FeralTweaksAction<T>)base.OnComplete(queue, handler);
        }

        /// <summary>
        /// Adds an on complete handler
        /// </summary>
        /// <param name="queue">Target event queue to run the handler on</param>
        /// <param name="handler">Handler to add</param>
        public new FeralTweaksAction<T> OnComplete(FeralTweaksTargetEventQueue queue, Action handler)
        {
            return (FeralTweaksAction<T>)base.OnComplete(queue, handler);
        }

        /// <summary>
        /// Adds an on error handler
        /// </summary>
        /// <param name="queue">Target event queue to run the handler on</param>
        /// <param name="handler">Error handler to add</param>
        public new FeralTweaksAction<T> OnError(FeralTweaksTargetEventQueue queue, Action<Exception> handler)
        {
            return (FeralTweaksAction<T>)base.OnError(queue, handler);
        }

        /// <summary>
        /// Adds an on complete handler
        /// </summary>
        /// <param name="queue">Target event queue to run the handler on</param>
        /// <param name="handler">Error handler to add</param>
        public new FeralTweaksAction<T> OnError(FeralTweaksTargetEventQueue queue, Action handler)
        {
            return (FeralTweaksAction<T>)base.OnError(queue, handler);
        }

        /// <summary>
        /// Adds the handler internally
        /// </summary>
        /// <param name="handler">Handler to add</param>
        protected virtual void ProcessAddRunHandlerInternal(RunHandlerInfo handler)
        {
            lock (_onRunHandlers)
                _onRunHandlers.Add(handler);
        }

        /// <summary>
        /// Called to add run handlers
        /// </summary>
        /// <param name="handler">Handler to add</param>
        protected virtual void ProcessAddRunHandler(RunHandlerInfo handler)
        {
            bool runNow = false;
            lock (_lock)
            {
                if (!_hasCompleted)
                {
                    ProcessAddRunHandlerInternal(handler);
                }
                else
                    runNow = true;
            }
            if (runNow && _ex == null)
                handler.action(_cResult);
        }

        /// <inheritdoc/>
        protected override void ProcessAddCompleteHandler(Action<T> handler)
        {
            bool runNow = false;
            lock (_lockFullComplete)
            {
                if (!_hasCompleted)
                {
                    ProcessAddCompleteHandlerInternal(handler);
                }
                else
                    runNow = true;
            }
            if (runNow && _ex == null)
                handler(_cResult);
        }

        /// <inheritdoc/>
        protected override void ProcessAddErrorHandler(Action<Exception> handler)
        {
            bool runNow = false;
            lock (_lockFullComplete)
            {
                if (!_hasCompleted)
                {
                    ProcessAddErrorHandlerInternal(handler);
                }
                else
                    runNow = true;
            }
            if (runNow && _ex != null)
                handler(_ex);
        }


        /// <summary>
        /// Runs the OnRun event
        /// </summary>
        /// <param name="value">Result value</param>
        /// <param name="isCrash">True if called because of an error, false if called normally</param>
        protected void RunOnRun(T value, bool isCrash)
        {
            // Run actions
            List<RunHandlerInfo> acL = new List<RunHandlerInfo>();
            List<RunHandlerInfo> acRemoveList = new List<RunHandlerInfo>();
            lock (_onRunHandlers)
            {
                acL.AddRange(_onRunHandlers);
                _onRunHandlers.RemoveAll(t => !t.persist);
            }
            foreach (RunHandlerInfo ac in acL)
            {
                try
                {
                    if (!isCrash || ac.runOnError)
                        ac.action(value);
                }
                catch (Exception e)
                {
                    Logger.GetLogger("ActionManager").Error("An exception was thrown while running an OnRun action", e);
                    if (Debugger.IsAttached)
                        throw;
                }
            }
        }


        internal bool Tick()
        {
            // Check cancel
            if (_cancel)
            {
                // End task
                Complete(default(T), true);
                return false;
            }

            // Check start time and assign if needed
            if (_timeStart == -1 && _millisWait != -1)
                _timeStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Check if below start interval
            if (_millisWait != -1 && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _timeStart < _millisWait)
            {
                // Wait
                return true;
            }

            // Check interval
            if (_interval > 0 && _cInterval++ < _interval)
            {
                // Wait
                return true;
            }

            // Run action
            // FIXME: profiler
            FeralTweaksActionExecutionContext<T> ctx = new FeralTweaksActionExecutionContext<T>(this);
            T res = default(T);
            bool crashed = false;
            try
            {
                lock (_lock)
                {
                    _hasRunI = false;
                }
                res = _action(ctx);
            }
            catch (Exception e)
            {
                crashed = true;

                // Crash
                Crash(e);

                // Log exception
                Logger.GetLogger("ActionManager").Error("An exception was thrown while running scheduled action", e);
                if (Debugger.IsAttached)
                    throw;
            }

            // Check continue
            if (!crashed && ctx._doContinue && !ctx._doBreak)
            {
                // Run complete
                Complete(res, false);

                // Continue
                return true;
            }

            // Reset interval
            _cInterval = 0;

            // Increase count
            if (_cCount != int.MaxValue)
                _cCount++;

            // Update start time at end of execution
            if (_millisWait != -1)
                _timeStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Check crash
            if (crashed)
            {
                // End
                return false;
            }

            // Run complete
            Complete(res, (_limit != -1 && _cCount >= _limit) || ctx._doBreak);

            // Success
            if ((_limit != -1 && _cCount >= _limit) || ctx._doBreak)
                return false; // End
            return true;
        }


        private void Complete(T res, bool allFinished)
        {
            if (allFinished)
            {
                // Mark done
                lock (_lockFullComplete)
                {
                    _cResult = res;
                    _hasCompleted = true;
                }

                // Run actions
                RunOnComplete(res);

                // Release
                lock (_lockFullComplete)
                {
                    Monitor.PulseAll(_lockFullComplete);
                }
            }

            // Mark done
            lock (_lock)
            {
                _cResult = res;
                _hasRun = true;
                _hasRunI = true;
            }

            // Run actions
            RunOnRun(res, false);

            // Release
            lock (_lock)
            {
                Monitor.PulseAll(_lock);
            }

            // Check
            if (allFinished)
            {
                // Clear
                ClearHandlers();
                _onRunHandlers.Clear();
            }
        }

        private void Crash(Exception ex)
        {
            // Mark done
            lock (_lockFullComplete)
            {
                _cResult = default(T);
                _ex = ex;
                _hasCompleted = true;
            }
            
            // Run actions
            RunOnError(ex);

            // Release
            lock (_lockFullComplete)
            {
                Monitor.PulseAll(_lockFullComplete);
            }

            // Mark done
            lock(_lock)
            {
                _cResult = default(T);
                _ex = ex;
                _hasRun = true;
                _hasRunI = true;
            }

            // Run actions
            RunOnRun(default(T), true);

            // Release
            lock (_lock)
            {
                Monitor.PulseAll(_lock);
            }

            // Clear
            ClearHandlers();
            _onRunHandlers.Clear();
        }


        private FeralTweaksActionAwaiter<T> awaiter;

        /// <summary>
        /// Retrieves the action awaiter
        /// </summary>
        /// <returns>FeralTweaksActionAwaiter instance</returns>
        public FeralTweaksActionAwaiter<T> GetAwaiter()
        {
            if (awaiter == null)
                awaiter = new FeralTweaksActionAwaiter<T>(this);
            return awaiter;
        }
    }

    /// <summary>
    /// Action awaiter
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    public class FeralTweaksActionAwaiter<T> : ICriticalNotifyCompletion
    {
        private FeralTweaksAction<T> action;

        /// <summary>
        /// Checks if the action completed
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                return action.HasCompleted;
            }
        }

        internal FeralTweaksActionAwaiter(FeralTweaksAction<T> action)
        {
            this.action = action;
        }

        /// <summary>
        /// Awaits function completion
        /// </summary>
        /// <returns>Action result</returns>
        public T GetResult()
        {
            return action.AwaitCompleteResult();
        }

        /// <inheritdoc/>
        public void OnCompleted(Action continuation)
        {
            action.OnComplete(FeralTweaksTargetEventQueue.OnAction, continuation);
            action.OnError(FeralTweaksTargetEventQueue.OnAction, continuation);
        }

        /// <inheritdoc/>
        public void UnsafeOnCompleted(Action continuation)
        {
            action.OnComplete(FeralTweaksTargetEventQueue.OnAction, continuation);
            action.OnError(FeralTweaksTargetEventQueue.OnAction, continuation);
        }
    }
}