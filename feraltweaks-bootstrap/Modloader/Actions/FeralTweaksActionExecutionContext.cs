using System;

namespace FeralTweaks.Actions
{
    /// <summary>
    /// FeralTweaks action execution context
    /// </summary>
    public class FeralTweaksActionExecutionContext<T>
    {
        private FeralTweaksAction<T> _action;
        internal bool _doContinue;
        internal bool _doBreak;

        internal FeralTweaksActionExecutionContext(FeralTweaksAction<T> action)
        {
            _action = action;
        }

        /// <summary>
        /// Tells the action to run again next tick (make sure to use return Continue(value))
        /// </summary>
        /// <param name="value">Value to return</param>
        /// <returns>Return value</returns>
        public T Continue(T value)
        {
            if (_action._denyContinueCall)
                throw new InvalidOperationException("Unable to use Continue() in the current context");
            _doContinue = true;
            return value;
        }

        /// <summary>
        /// Tells the action to run again next tick (make sure to use return Continue())
        /// </summary>
        /// <returns>Return value (default)</returns>
        public T Continue()
        {
            return Continue(default(T));
        }

        /// <summary>
        /// Tells the action to break and not run further (make sure to use return Break(value))
        /// </summary>
        /// <param name="value">Value to return</param>
        /// <returns>Return value</returns>
        public T Break(T value)
        {
            _doBreak = true;
            return value;
        }

        /// <summary>
        /// Tells the action to break and not run further (make sure to use return Break())
        /// </summary>
        /// <returns>Return value (default)</returns>
        public T Break()
        {
            return Break(default(T));
        }

        public class ActionExecutionContext
        {
            private FeralTweaksActionType type;
            internal ActionExecutionContext(FeralTweaksActionType type)
            {
                this.type = type;
            }

            /// <summary>
            /// Registeres a oneshot action to run on event tick without return type
            /// </summary>
            /// <param name="action">Action to schedule</param>
            /// <returns>FeralTweaksAction instance</returns>
            public FeralTweaksAction<object> ExecuteAction(Action action)
            {
                return ExecuteAction(ctx => action());
            }

            /// <summary>
            /// Registeres a oneshot action to run on event tick without return type
            /// </summary>
            /// <param name="action">Action to schedule</param>
            /// <returns>FeralTweaksAction instance</returns>
            public FeralTweaksAction<object> ExecuteAction(Action<FeralTweaksActionExecutionContext<object>> action)
            {
                return ExecuteAction<object>(ctx =>
                {
                    action(ctx);
                    return null;
                });
            }

            /// <summary>
            /// Registeres a oneshot action to run on event tick
            /// </summary>
            /// <param name="action">Action to schedule</param>
            /// <returns>FeralTweaksAction instance</returns>
            public FeralTweaksAction<object> ExecuteAction(Func<bool> action)
            {
                return ExecuteAction<object>(ctx =>
                {
                    bool val = action();
                    return !val ? ctx.Continue() : null;
                });
            }

            /// <summary>
            /// Registeres a oneshot action to run on event tick
            /// </summary>
            /// <param name="action">Action to schedule</param>
            /// <returns>FeralTweaksAction instance</returns>
            public FeralTweaksAction<T2> ExecuteAction<T2>(Func<FeralTweaksActionExecutionContext<T2>, T2> action)
            {
                return FeralTweaksActions.ScheduleAction(action, type, true, -1, -1, 1);
            }

            /// <summary>
            /// Runs a coroutine as an action
            /// </summary>
            /// <param name="coroutine">Coroutine initializer</param>
            /// <returns>FeralTweaksAction instance</returns>
            public FeralTweaksAction<UnityEngine.Coroutine> ExecuteCoroutine(System.Action<FTCoroutine.CoroutineBuilder> coroutine)
            {
                // Check
                if (type != FeralTweaksActionType.UNITY && !FeralTweaksActions.IsAwaitSafeOnCurrentThread(FeralTweaksActionType.UNITY))
                {
                    // Unsafe
                    throw new InvalidOperationException("Unable to use ExecuteCoroutine in the current context, ExecuteCoroutine cannot safely be run on non-unity event queues when scheduling from a unity coroutine, as awaiting from unity would cause a deadlock");
                }
                bool coroutineFinished = false;
                UnityEngine.Coroutine routine = null;
                return FeralTweaksActions.ScheduleAction<UnityEngine.Coroutine>(ctx =>
                {
                    // Start unity coroutine on unity thread if needed
                    if (routine == null)
                    {
                        ctx.GetUnityEventQueue().ExecuteAction(() =>
                        {
                            // Starts the coroutine
                            // Should the RunCoroutine call have been on unity thread, GetUnityEventQueue().ExecuteAction will run right away
                            routine = FeralTweaksActions.updateHandler.StartCoroutine(FeralTweaksCoroutines.InjectAtTail(FeralTweaksCoroutines.CreateNew(t =>
                            {
                                t.Execute(() =>
                                {
                                    coroutineFinished = true;
                                });
                            }), FeralTweaksCoroutines.CreateNew(coroutine)));
                        }).AwaitComplete();
                    }

                    // Tick
                    if (!coroutineFinished)
                        return ctx.Continue();

                    // Finish
                    return routine;
                }, type, false, -1, -1, 1);
            }

            /// <summary>
            /// Runs a coroutine as an action
            /// </summary>
            /// <param name="coroutine">Coroutine to run</param>
            /// <returns>FeralTweaksAction instance</returns>
            public FeralTweaksAction<UnityEngine.Coroutine> ExecuteCoroutine(System.Collections.IEnumerator coroutine)
            {
                // Check
                if (type != FeralTweaksActionType.UNITY && !FeralTweaksActions.IsAwaitSafeOnCurrentThread(FeralTweaksActionType.UNITY))
                {
                    // Unsafe
                    throw new InvalidOperationException("Unable to use ExecuteCoroutine in the current context, ExecuteCoroutine cannot safely be run on non-unity event queues when scheduling from a unity coroutine, as awaiting from unity would cause a deadlock");
                }
                bool coroutineFinished = false;
                UnityEngine.Coroutine routine = null;
                return FeralTweaksActions.ScheduleAction<UnityEngine.Coroutine>(ctx =>
                {
                    // Start unity coroutine on unity thread if needed
                    if (routine == null)
                    {
                        ctx.GetUnityEventQueue().ExecuteAction(() =>
                        {
                            // Starts the coroutine
                            // Should the RunCoroutine call have been on unity thread, GetUnityEventQueue().ExecuteAction will run right away
                            routine = FeralTweaksActions.updateHandler.StartCoroutine(FeralTweaksCoroutines.InjectAtTail(FeralTweaksCoroutines.CreateNew(t =>
                            {
                                t.Execute(() =>
                                {
                                    coroutineFinished = true;
                                });
                            }), FeralTweaksCoroutines.CreateNew(coroutine)));
                        }).AwaitComplete();
                    }

                    // Tick
                    if (!coroutineFinished)
                        return ctx.Continue();

                    // Finish
                    return routine;
                }, type, false, -1, -1, 1);
            }

            /// <summary>
            /// Runs a coroutine as an action
            /// </summary>
            /// <param name="coroutine">Coroutine to run</param>
            /// <returns>FeralTweaksAction instance</returns>
            public FeralTweaksAction<UnityEngine.Coroutine> ExecuteCoroutine(Il2CppSystem.Collections.IEnumerator coroutine)
            {
                // Check
                if (type != FeralTweaksActionType.UNITY && !FeralTweaksActions.IsAwaitSafeOnCurrentThread(FeralTweaksActionType.UNITY))
                {
                    // Unsafe
                    throw new InvalidOperationException("Unable to use ExecuteCoroutine in the current context, ExecuteCoroutine cannot safely be run on non-unity event queues when scheduling from a unity coroutine, as awaiting from unity would cause a deadlock");
                }
                bool coroutineFinished = false;
                UnityEngine.Coroutine routine = null;
                return FeralTweaksActions.ScheduleAction<UnityEngine.Coroutine>(ctx =>
                {
                    // Start unity coroutine on unity thread if needed
                    if (coroutine == null)
                    {
                        ctx.GetUnityEventQueue().ExecuteAction(() =>
                        {
                            // Starts the coroutine
                            // Should the RunCoroutine call have been on unity thread, GetUnityEventQueue().ExecuteAction will run right away
                            routine = FeralTweaksActions.updateHandler.StartCoroutine(FeralTweaksCoroutines.InjectAtTail(FeralTweaksCoroutines.CreateNew(t =>
                            {
                                t.Execute(() =>
                                {
                                    coroutineFinished = true;
                                });
                            }), coroutine));
                        }).AwaitComplete();
                    }

                    // Tick
                    if (!coroutineFinished)
                        return ctx.Continue();

                    // Finish
                    return routine;
                }, type, false, -1, -1, 1);
            }            
        }

        private ActionExecutionContext unityCtx = new ActionExecutionContext(FeralTweaksActionType.UNITY);
        private ActionExecutionContext eventQueueCtx = new ActionExecutionContext(FeralTweaksActionType.SYNC);
        private ActionExecutionContext asyncCtx = new ActionExecutionContext(FeralTweaksActionType.ASYNC);

        /// <summary>
        /// Retrieves the inline unity event queue
        /// 
        /// <para>Note: keep in mind nested Continue() calls might not work in some contexts, this mostly happens when on the same queue, as when on the same queue as the parent, ExecuteAction will run them as nested actions, disabling Continue()</para>
        /// </summary>
        /// <returns>ActionExecutionContext instance</returns>
        public ActionExecutionContext GetUnityEventQueue()
        {
            return unityCtx;
        }

        /// <summary>
        /// Retrieves the inline unity event queue
        /// 
        /// <para>Note: keep in mind nested Continue() calls might not work in some contexts, this mostly happens when on the same queue, as when on the same queue as the parent, ExecuteAction will run them as nested actions, disabling Continue()</para>
        /// </summary>
        /// <returns>ActionExecutionContext instance</returns>
        public ActionExecutionContext GetSyncEventQueue()
        {
            return eventQueueCtx;
        }

        /// <summary>
        /// Retrieves the inline unity event queue
        /// 
        /// <para>Note: keep in mind nested Continue() calls might not work in some contexts, this mostly happens when on the same queue, as when on the same queue as the parent, ExecuteAction will run them as nested actions, disabling Continue()</para>
        /// </summary>
        /// <returns>ActionExecutionContext instance</returns>
        public ActionExecutionContext GetAsyncEventQueue()
        {
            return asyncCtx;
        }

        /// <summary>
        /// Retrieves the action instance
        /// </summary>
        public FeralTweaksAction<T> Action
        {
            get
            {
                return _action;
            }
        }
    }
}