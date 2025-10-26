using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

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
        /// <typeparam name="T">Promise result type</typeParam>
        /// <returns>FeralTweaksPromiseController instance with a FeralTweaksPromise instance attached</returns>
        public static FeralTweaksPromiseController<T> CreatePromise<T>()
        {
            return new FeralTweaksPromiseController<T>();
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
            public InternalPromise(out Action<T> promiseCallback, out Action<Exception> promiseErrorCallback)
            {
                promiseCallback = res =>
                {
                    RunOnComplete(res);
                    ClearHandlers();
                };
                promiseErrorCallback = error =>
                {
                    RunOnError(error);
                    ClearHandlers();
                };
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