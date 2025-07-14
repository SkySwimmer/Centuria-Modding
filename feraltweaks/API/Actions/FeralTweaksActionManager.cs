using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using FeralTweaks.Logging;

namespace FeralTweaks.Actions
{
    /// <summary>
    /// Action scheduling system
    /// </summary>
    public static class FeralTweaksActionManager
    {
        private static List<Func<bool>> threadActions = new List<Func<bool>>();
        private static List<Func<bool>> uiRepeatingActions = new List<Func<bool>>();
        private static List<Action> uiActions = new List<Action>();

        internal static void CallUpdate()
        {
            Func<bool>[] actionsUA;
            lock (uiRepeatingActions)
                actionsUA = uiRepeatingActions.ToArray();
            foreach (Func<bool> ac in actionsUA)
            {
                try
                {
                    if (ac == null || ac())
                        lock (uiRepeatingActions)
                            uiRepeatingActions.Remove(ac);
                }
                catch (Exception e)
                { 
                    lock (uiRepeatingActions)
                        uiRepeatingActions.Remove(ac);
                    
                    // Log error
                    Logger.GetLogger("Interop").Error("An exception occurred while handling an on-unity update action", e);
                }
            }

            Action[] actionsU;
            lock (uiActions)
                actionsU = uiActions.ToArray();
            foreach (Action ac in actionsU)
            {
                lock (uiActions)
                    uiActions.Remove(ac);
                if (ac != null)
                {
                    try
                    {
                        ac();
                    }
                    catch (Exception e)
                    {                        
                        // Log error
                        Logger.GetLogger("Interop").Error("An exception occurred while handling an on-unity update action", e);
                    }
                }
            }
        }

        internal static void StartActionThread()
        {
            // Start action thread
            Thread th = new Thread(() =>
            {
                while (true)
                {
                    Func<bool>[] actions;
                    lock (threadActions)
                        actions = threadActions.ToArray();

                    // Handle actions
                    foreach (Func<bool> ac in actions)
                    {
                        if (ac == null || ac())
                            lock (threadActions)
                                threadActions.Remove(ac);
                    }

                    Thread.Sleep(10);
                }
            });
            th.IsBackground = true;
            th.Name = "FeralTweaks Action Thread";
            th.Start();
        }

        /// <summary>
        /// Schedules actions that are asynchronously to Unity, NOTE THIS CANNOT INTERACT WITH IL2CPP DIRECTLY
        /// </summary>
        /// <param name="act">Action to schedule</param>
        public static void ScheduleDelayedNonUnityAction(Func<bool> act)
        {
            lock (threadActions)
                threadActions.Add(act);
        }

        /// <summary>
        /// Schedules loop-routine actions for the next frame updates that continue to run until true is returned
        /// </summary>
        /// <param name="act">Action to schedule</param>
        public static void ScheduleDelayedActionForUnity(Func<bool> act)
        {
            lock (uiRepeatingActions)
                uiRepeatingActions.Add(act);
        }

        /// <summary>
        /// Schedules single-time actions that are run on the next frame update
        /// </summary>
        /// <param name="act">Action to schedule</param>
        public static void ScheduleDelayedActionForUnity(Action act)
        {
            lock (uiActions)
                uiActions.Add(act);
        }

    }
}