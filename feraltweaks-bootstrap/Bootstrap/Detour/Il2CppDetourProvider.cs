using System;
using Il2CppInterop.Runtime.Injection;

namespace FeralTweaksBootstrap
{
    public class Il2CppDetourProvider : IDetourProvider
    {
        public Il2CppInterop.Runtime.Injection.IDetour Create<TDelegate>(nint original, TDelegate target) where TDelegate : Delegate
        {
            return new Il2CppDetour<TDelegate>(original, target);
        }
    }
}