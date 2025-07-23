using System;
using System.Collections.Generic;

namespace FeralTweaks.Profiler.API
{
    /// <summary>
    /// Attribute for automatic layer collection detection
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RegisterLayersAttribute : Attribute
    {
    }
}