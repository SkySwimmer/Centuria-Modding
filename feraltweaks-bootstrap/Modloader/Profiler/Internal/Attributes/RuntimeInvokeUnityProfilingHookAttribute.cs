using System;
using System.Collections.Generic;

namespace FeralTweaks.Profiler.Internal
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal class RuntimeInvokeUnityProfilingHookAttribute : Attribute
    {
        public string MethodName { get; private set; }

        public RuntimeInvokeUnityProfilingHookAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}