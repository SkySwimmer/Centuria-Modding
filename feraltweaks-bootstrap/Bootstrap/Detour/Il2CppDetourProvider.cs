using System;
using Il2CppInterop.Runtime.Injection;

namespace FeralTweaksBootstrap
{
    public class Il2CppInteropDetourProvider : IDetourProvider
    {
        public Il2CppInterop.Runtime.Injection.IDetour Create<TDelegate>(nint original, TDelegate target) where TDelegate : Delegate
        {
            return new Il2CppInteropDetour<TDelegate>(original, target);
        }
    }
}