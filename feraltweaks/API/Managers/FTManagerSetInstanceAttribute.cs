using System;

namespace FeralTweaks.Managers
{
    /// <summary>
    /// Attribute used to automatically set static instance properties of managers
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class FTManagerSetInstanceAttribute : Attribute
    {
    }
}