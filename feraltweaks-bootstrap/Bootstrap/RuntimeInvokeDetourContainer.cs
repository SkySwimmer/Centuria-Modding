using FeralTweaks;
using FeralTweaksBootstrap.Detour;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using FeralTweaks.Logging.Impl;
using Logger = FeralTweaks.Logging.Logger;
using System.IO;
using System.Reflection;

namespace FeralTweaksBootstrap
{
    /// <summary>
    /// Runtime invoke delegate
    /// </summary>
    /// <param name="method">Method pointer</param>
    /// <param name="obj">Object pointer</param>
    /// <param name="parameters">Parameter pointer</param>
    /// <param name="exc">Exception pointer</param>
    /// <returns>Result pointer</returns>
    public delegate IntPtr RuntimeInvokeDetour(IntPtr method, IntPtr obj, IntPtr parameters, IntPtr exc);

    internal class RuntimeInvokeDetourContainer : DetourContainer<RuntimeInvokeDetour>
    {
        private static Logger unityLogger;
  
        public override RuntimeInvokeDetour run()
        {
            return (method, obj, parameters, except) =>
            {
                string cls = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(IL2CPP.il2cpp_method_get_class(method)));
                string methodName = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_method_get_name(method));
                if (methodName == "Internal_ActiveSceneChanged")
                {
                    // Wrap up and unhook
                    IntPtr res = Original(method, obj, parameters, except);
                    Unhook();

                    // Init logging
                    if (Bootstrap.logUnityToConsole || Bootstrap.logUnityToFile)
                    {
                        if (!Bootstrap.logUnityToConsole)
                            unityLogger = new FileLoggerImpl("Unity");
                        else if (!Bootstrap.logUnityToFile)
                            unityLogger = new ConsoleLoggerImpl("Unity");
                        else
                            unityLogger = Logger.GetLogger("Unity");

                        // Add logger
                        ClassInjector.RegisterTypeInIl2Cpp<LogHandler>();

                        // Create FTL container
                        GameObject objC = new GameObject();
                        objC.name = "~FTL";
                        objC.AddComponent<LogHandler>();
                        GameObject.DontDestroyOnLoad(objC);
                    }

                    // Finish loading
                    FeralTweaksLoader.LoadFinish();
     
                    // Load all assemblies
                    FeralTweaksLoader.LogInfo("Loading assemblies...");
                    foreach (FileInfo file in new DirectoryInfo("FeralTweaks/cache/assemblies").GetFiles("*.dll"))
                    {
                        FeralTweaksLoader.LogDebug("Loading assembly: " + file.Name);
                        Assembly.Load(file.Name.Replace(".dll", ""));
                    }

                    // Log finish
                    FeralTweaksLoader.LogInfo("FTL is ready! Launching game... Launching " + Application.productName + " " + Application.version + " (unity version: " + Application.unityVersion + ")");
                    try
                    {
                        Console.Title = Application.productName + " " + Application.version + " (FTL " + FeralTweaksLoader.VERSION + ")";
                    }
                    catch
                    {
                    }

                    // Return result
                    return res;
                }
                return Original(method, obj, parameters, except);
            };
        }

        private class LogHandler : UnityEngine.MonoBehaviour
        {
            public LogHandler() { }
            public LogHandler(IntPtr ptr) : base(ptr) { }

            public void HandleLog(string logString, string stackTrace, LogType type)
            {
                if (stackTrace != null)
                {
                    stackTrace = stackTrace.Trim();
                    while (stackTrace.Contains("\n\n"))
                        stackTrace = stackTrace.Replace("\n\n", "\n");
                }

                switch (type)
                {
                    case LogType.Error:
                        {
                            string msg = logString + (stackTrace == null || stackTrace == "" ? "" : (logString.Contains("\n") ? "\nStacktrace:" : "") + "\n  at: " + stackTrace.Replace("\n", "\n  at: "));
                            if (msg.Contains("\n"))
                                msg = msg + "\n";
                            unityLogger.Error(msg);
                            break;
                        }
                    case LogType.Assert:
                        {
                            string msg = logString;
                            if (msg.Contains("\n"))
                                msg = msg + "\n";
                            unityLogger.Debug(msg);
                        }
                        break;
                    case LogType.Warning:
                        {
                            string msg = logString + (stackTrace == null || stackTrace == "" ? "" : (logString.Contains("\n") ? "\nStacktrace:" : "") + "\n  at: " + stackTrace.Replace("\n", "\n  at: "));
                            if (msg.Contains("\n"))
                                msg = msg + "\n";
                            unityLogger.Warn(msg);
                            break;
                        }
                    case LogType.Log:
                        {
                            string msg = logString;
                            if (msg.Contains("\n"))
                                msg = msg + "\n";
                            unityLogger.Info(msg);
                            break;
                        }
                    case LogType.Exception:
                        {
                            string msg = logString + (stackTrace == null || stackTrace == "" ? "" : (logString.Contains("\n") ? "\nStacktrace:" : "") + "\n  at: " + stackTrace.Replace("\n", "\n  at: "));
                            if (msg.Contains("\n"))
                                msg = msg + "\n";
                            unityLogger.Error(msg);
                            break;
                        }
                }
            }

            public void Awake()
            {
                Application.add_logMessageReceivedThreaded(new Application.LogCallback(this, GetIl2CppType().GetMethod("HandleLog").MethodHandle.Value));
            }
        }
    }
}