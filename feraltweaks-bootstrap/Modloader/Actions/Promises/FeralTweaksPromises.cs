using System;
using System.Reflection;
using System.Threading;

namespace FeralTweaks.Actions
{
    /// <summary>
    /// FeralTweaks promise system
    /// </summary>
    public static class FeralTweaksPromises
    {
        /// <summary>
        /// Creates a new promise
        /// </summary>
        /// <returns>FeralTweaksPromiseController instance with a FeralTweaksPromise instance attached</returns>
        public static FeralTweaksPromiseController CreatePromise()
        {
            return new FeralTweaksPromiseController();
        }

        /// <summary>
        /// Creates a new promise
        /// </summary>
        /// <typeparam name="T">Promise result type</typeparam>
        /// <returns>FeralTweaksPromiseController instance with a FeralTweaksPromise instance attached</returns>
        public static FeralTweaksPromiseController<T> CreatePromise<T>()
        {
            return new FeralTweaksPromiseController<T>();
        }


        /// <summary>
        /// Creates a promise from a managed Task
        /// </summary>
        /// <typeparam name="T">Promise type</typeparam>
        /// <param name="task">Task to bind to</param>
        /// <returns>FeralTweaksPromise instance</returns>
        public static FeralTweaksPromise<T> CreatePromiseFrom<T>(System.Threading.Tasks.Task<T> task)
        {
            return CreatePromiseFrom(task.GetAwaiter());
        }

        /// <summary>
        /// Creates a promise from a managed Awaiter
        /// </summary>
        /// <typeparam name="T">Promise type</typeparam>
        /// <param name="awaiter">Task awaiter to bind to</param>
        /// <returns>FeralTweaksPromise instance</returns>
        public static FeralTweaksPromise<T> CreatePromiseFrom<T>(System.Runtime.CompilerServices.TaskAwaiter<T> awaiter)
        {
            FeralTweaksPromiseController<T> controller = CreatePromise<T>();
            awaiter.OnCompleted(() =>
            {
                // Call complete
                controller.CallComplete(awaiter.GetResult());
            });
            return controller.GetPromise();
        }

        /// <summary>
        /// Creates a promise from a unmanaged il2cpp Task
        /// </summary>
        /// <typeparam name="T">Promise type</typeparam>
        /// <param name="task">Task to bind to</param>
        /// <returns>FeralTweaksPromise instance</returns>
        public static FeralTweaksPromise<T> CreatePromiseFrom<T>(Il2CppSystem.Threading.Tasks.Task<T> task)
        {
            return CreatePromiseFrom(task.GetAwaiter());
        }

        /// <summary>
        /// Creates a promise from a unmanaged il2cpp Awaiter
        /// </summary>
        /// <typeparam name="T">Promise type</typeparam>
        /// <param name="awaiter">Task awaiter to bind to</param>
        /// <returns>FeralTweaksPromise instance</returns>
        public static FeralTweaksPromise<T> CreatePromiseFrom<T>(Il2CppSystem.Runtime.CompilerServices.TaskAwaiter<T> awaiter)
        {
            FeralTweaksPromiseController<T> controller = CreatePromise<T>();
            awaiter.OnCompleted(new Action(() =>
            {
                // Call complete
                controller.CallComplete(awaiter.GetResult());
            }));
            return controller.GetPromise();
        }
    }

    /// <summary>
    /// FeralTweaks Promise Controller - Object managing a specific Promise instance
    /// </summary>
    public class FeralTweaksPromiseController
    {
        private FeralTweaksPromiseController<object> delegateController = new FeralTweaksPromiseController<object>();

        /// <summary>
        /// Retrieves the promise
        /// </summary>
        /// <returns>FeralTweaksPromise instance</returns>
        public FeralTweaksPromise<object> GetPromise()
        {
            return delegateController.GetPromise();
        }

        /// <summary>
        /// Calls the promise complete event
        /// </summary>
        public void CallComplete()
        {
            delegateController.CallComplete(null);
        }

        /// <summary>
        /// Calls the promise error event
        /// </summary>
        /// <param name="exception">Exception to send</param>
        public void CallError(Exception exception)
        {
            delegateController.CallError(exception);
        }
    }

    /// <summary>
    /// FeralTweaks Promise Controller - Object managing a specific Promise instance
    /// </summary>
    public class FeralTweaksPromiseController<T>
    {
        private InternalPromise _promise;
        private Action<T> _promiseCallback;
        private Action<Exception> _promiseErrorCallback;

        internal FeralTweaksPromiseController()
        {
            _promise = new InternalPromise(out _promiseCallback, out _promiseErrorCallback);
        }

        private class InternalPromise : FeralTweaksPromise<T>
        {
            private bool _hasCompleted;
            private Exception _ex;
            private T _cResult = default(T);
            private object _lockFullComplete = new object();

            public InternalPromise(out Action<T> promiseCallback, out Action<Exception> promiseErrorCallback)
            {
                promiseCallback = res =>
                {
                    lock (_lockFullComplete)
                    {
                        _cResult = res;
                        _hasCompleted = true;
                    }
                    RunOnComplete(res);
                    ClearHandlers();

                    // Release
                    lock (_lockFullComplete)
                    {
                        Monitor.PulseAll(_lockFullComplete);
                    }
                };
                promiseErrorCallback = error =>
                {
                    lock (_lockFullComplete)
                    {
                        _ex = error;
                        _cResult = default(T);
                        _hasCompleted = true;
                    }
                    RunOnError(error);
                    ClearHandlers();

                    // Release
                    lock (_lockFullComplete)
                    {
                        Monitor.PulseAll(_lockFullComplete);
                    }
                };
            }

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

            public override bool HasCompleted
            {
                get
                {
                    return _hasCompleted;
                }
            }

            public override bool HasErrored
            {
                get
                {
                    return _ex != null;
                }
            }

            public override Exception GetException()
            {
                return _ex;
            }

            public override T GetResult()
            {
                return _cResult;
            }

            public override T AwaitResult()
            {
                lock (_lockFullComplete)
                {
                    // Check exception
                    if (_ex != null)
                        throw new TargetInvocationException("Target function has thrown an exception", _ex); // Throw exception

                    // Check completed
                    if (_hasCompleted)
                        return GetResult();

                    // Wait
                    while (!_hasCompleted)
                        Monitor.Wait(_lockFullComplete);
                }

                // Check exception
                if (_ex != null)
                    throw new TargetInvocationException("Target function has thrown an exception", _ex); // Throw exception

                // Check completed
                if (_hasCompleted)
                    return GetResult();
                return default(T);
            }
        }

        /// <summary>
        /// Retrieves the promise
        /// </summary>
        /// <returns>FeralTweaksPromise instance</returns>
        public FeralTweaksPromise<T> GetPromise()
        {
            return _promise;
        }

        /// <summary>
        /// Calls the promise complete event
        /// </summary>
        /// <param name="result">Result object</param>
        public void CallComplete(T result)
        {
            _promiseCallback(result);
        }

        /// <summary>
        /// Calls the promise error event
        /// </summary>
        /// <param name="exception">Exception to send</param>
        public void CallError(Exception exception)
        {
            _promiseErrorCallback(exception);
        }
    }
}