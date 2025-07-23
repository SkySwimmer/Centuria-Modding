using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FeralTweaks.Profiler.Profiling;

namespace FeralTweaks.Profiler.Internal
{
    internal class ThreadLinkedObject
    {
        private static Dictionary<int, ThreadLinkedObject> objects = new Dictionary<int, ThreadLinkedObject>();
        private Thread thread;

        static ThreadLinkedObject()
        {
            // Start cleanup
            Thread cleanup = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        // Go through objects
                        ThreadLinkedObject[] objs;
                        lock(objects)
                        {
                            objs = objects.Values.ToArray();
                        }

                        // Go through all threads
                        foreach (ThreadLinkedObject o in objs)
                        {
                            if (!o.thread.IsAlive)
                            {
                                // Remove
                                lock(objects)
                                {
                                    objects.Remove(o.thread.ManagedThreadId);
                                }

                                // Call exit
                                try
                                {
                                    o.OnExit();
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Rerun
                        continue;
                    }

                    Thread.Sleep(100);
                }
            });
            cleanup.IsBackground = true;
            cleanup.Name = "Profiler cleanup";
            cleanup.Start();
        }

        public static ThreadLinkedObject ForThread(Thread thread)
        {
            if (objects.ContainsKey(thread.ManagedThreadId))
                return objects[thread.ManagedThreadId];

            // Retrieve
            lock (objects)
            {
                // Check
                if (objects.ContainsKey(thread.ManagedThreadId))
                    return objects[thread.ManagedThreadId];

                // Create
                ThreadLinkedObject o = new ThreadLinkedObject();
                o.Start();
                o.thread = thread;
                objects[thread.ManagedThreadId] = o;

                // Return
                return o;
            }
        }

        public RuntimeProfilerFrames ProfilerFramesInstance { get; private set; }

        public void Start()
        {
            // Start
            ProfilerFramesInstance = new RuntimeProfilerFrames(this);
        }

        public void OnExit()
        {
            // Thread exited
        }
    }
}