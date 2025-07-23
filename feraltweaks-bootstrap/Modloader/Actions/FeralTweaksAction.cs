using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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
    public class FeralTweaksAction<T>
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

        private List<Action<T>> onCompleteActions = new List<Action<T>>();
        private List<Action<T>> onRunActions = new List<Action<T>>();

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
        public bool HasCompleted
        {
            get
            {
                return _hasCompleted;
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
        /// Checks if the action has errored
        /// </summary>
        public bool HasErrored
        {
            get
            {
                return _ex != null;
            }
        }

        /// <summary>
        /// Adds actions to run when the task completes a tick (removed each tick)
        /// </summary>
        /// <param name="action">Action to run</param>
        public void AddOnRun(Action action)
        {
            bool runNow = false;
            lock (_lock)
            {
                if (!_hasCompleted)
                {
                    lock (onRunActions)
                    {
                        onRunActions.Add(t => action());
                    }
                }
                else
                    runNow = true;
            }
            if (runNow)
                action();
        }


        /// <summary>
        /// Adds actions to run when the task completes a tick (removed each tick)
        /// </summary>
        /// <param name="action">Action to run</param>
        public void AddOnRun(Action<T> action)
        {
            bool runNow = false;
            lock (_lock)
            {
                if (!_hasCompleted)
                {
                    lock (onRunActions)
                    {
                        onRunActions.Add(action);
                    }
                }
                else
                    runNow = true;
            }
            if (runNow)
                action(_cResult);
        }

        /// <summary>
        /// Adds actions to run when the task completes (note: called only after all tasks finish, use AddOnRun to add a task executed every run)
        /// </summary>
        /// <param name="action">Action to run</param>
        public void AddOnComplete(Action action)
        {
            bool runNow = false;
            lock (_lockFullComplete)
            {
                if (!_hasCompleted)
                {
                    lock (onCompleteActions)
                    {
                        onCompleteActions.Add(t => action());
                    }
                }
                else
                    runNow = true;
            }
            if (runNow)
                action();
        }

        /// <summary>
        /// Adds actions to run when the task completes (note: called only after all tasks finish, use AddOnRun to add a task executed every run)
        /// </summary>
        /// <param name="action">Action to run</param>
        public void AddOnComplete(Action<T> action)
        {
            bool runNow = false;
            lock (_lockFullComplete)
            {
                if (!_hasCompleted)
                {
                    lock (onCompleteActions)
                    {
                        onCompleteActions.Add(action);
                    }
                }
                else
                    runNow = true;
            }
            if (runNow)
                action(_cResult);
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
        public Exception GetException()
        {
            return _ex;
        }

        /// <summary>
        /// Retrieves the function result
        /// 
        /// <para>Note: this does NOT await the action, use AwaitResult() instead to await the action result</para>
        /// </summary>
        /// <returns>Function result or null</returns>
        public T GetResult()
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
            AddOnRun(() => hasReceivedRun = true);
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
            AddOnRun(t =>
            {
                res = t;
                hasReceivedRun = true;
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
                // Run actions
                List<Action<T>> acL2 = new List<Action<T>>();
                lock (onCompleteActions)
                {
                    acL2.AddRange(onCompleteActions);
                    onCompleteActions.Clear();
                }
                foreach (Action<T> ac in acL2)
                {
                    try
                    {
                        ac(res);
                    }
                    catch (Exception e)
                    {
                        Logger.GetLogger("ActionManager").Error("An exception was thrown while running an OnComplete action", e);
                        if (Debugger.IsAttached)
                            throw;
                    }
                }

                // Release
                lock (_lockFullComplete)
                {
                    _cResult = res;
                    _hasCompleted = true;
                    Monitor.PulseAll(_lockFullComplete);
                }
            }

            // Run actions
            List<Action<T>> acL = new List<Action<T>>();
            lock (onRunActions)
            {
                acL.AddRange(onRunActions);
                onRunActions.Clear();
            }
            foreach (Action<T> ac in acL)
            {
                try
                {
                    ac(default(T));
                }
                catch (Exception e)
                {
                    Logger.GetLogger("ActionManager").Error("An exception was thrown while running an OnRun action", e);
                    if (Debugger.IsAttached)
                        throw;
                }
            }

            // Release
            lock (_lock)
            {
                _cResult = res;
                _hasRun = true;
                _hasRunI = true;
                Monitor.PulseAll(_lock);
            }
        }

        private void Crash(Exception ex)
        {
            // Run actions
            List<Action<T>> acL2 = new List<Action<T>>();
            lock (onCompleteActions)
            {
                acL2.AddRange(onCompleteActions);
                onCompleteActions.Clear();
            }
            foreach (Action<T> ac in acL2)
            {
                try
                {
                    ac(default(T));
                }
                catch (Exception e)
                {
                    Logger.GetLogger("ActionManager").Error("An exception was thrown while running an OnComplete action", e);
                    if (Debugger.IsAttached)
                        throw;
                }
            }

            // Release
            lock (_lockFullComplete)
            {
                _cResult = default(T);
                _ex = ex;
                _hasCompleted = true;
                Monitor.PulseAll(_lockFullComplete);
            }

            // Run actions
            List<Action<T>> acL = new List<Action<T>>();
            lock (onRunActions)
            {
                acL.AddRange(onRunActions);
                onRunActions.Clear();
            }
            foreach (Action<T> ac in acL)
            {
                try
                {
                    ac(default(T));
                }
                catch (Exception e)
                {
                    Logger.GetLogger("ActionManager").Error("An exception was thrown while running an OnRun action", e);
                    if (Debugger.IsAttached)
                        throw;
                }
            }

            // Release
            lock (_lock)
            {
                _cResult = default(T);
                _ex = ex;
                _hasRun = true;
                _hasRunI = true;
                Monitor.PulseAll(_lock);
            }
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
            action.AddOnComplete(continuation);
        }

        /// <inheritdoc/>
        public void UnsafeOnCompleted(Action continuation)
        {
            action.AddOnComplete(continuation);
        }
    }
}