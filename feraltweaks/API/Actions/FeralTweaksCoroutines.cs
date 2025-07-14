using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem.Collections;

namespace FeralTweaks.Actions
{
    /// <summary>
    /// Coroutine injection system, usage example:
    /// 
    /// <code>
    /// [HarmonyPostfix]
    /// [HarmonyPatch(typeof(ObjectExample), "RunCoroutine")]
    /// private static void RunCoroutine(ObjectExample __instance, ref Il2CppSystem.Collections.IEnumerator __result)
    /// {
    ///     __result = FeralTweaksCoroutines.InjectAtStart(__result, FeralTweaksCoroutines.CreateNew(builder => {
    ///          builder.Execute(() => { __instance.SomeCall(); })
    ///     });
    /// }
    /// </code> 
    /// </summary>
    public static class FeralTweaksCoroutines
    {
        private static bool _inited;
        private static void Init()
        {
            if (_inited)
                return;
            Il2CppInterfaceCollection col = new Il2CppInterfaceCollection(new System.Type[] { typeof(IEnumerator) });
            ClassInjector.RegisterTypeInIl2Cpp<FTCoroutine>(new RegisterTypeOptions()
            {
                Interfaces = col
            });
            _inited = true;
        }

        /// <summary>
        /// Injects a coroutine at the start of another coroutine
        /// </summary>
        /// <param name="coroutine">Coroutine to inject at the start of the target</param>
        /// <param name="target">Target coroutine</param>
        /// <returns>Altered coroutine</returns>
        public static IEnumerator InjectAtHead(IEnumerator coroutine, IEnumerator target)
        {
            Init();
            return CreateNew(t =>
            {
                t.Execute(coroutine);
                t.Execute(target);
            });
        }

        /// <summary>
        /// Injects a coroutine at the end of another coroutine
        /// </summary>
        /// <param name="coroutine">Coroutine to inject at the end of the target</param>
        /// <param name="target">Target coroutine</param>
        /// <returns>Altered coroutine</returns>
        public static IEnumerator InjectAtTail(IEnumerator coroutine, IEnumerator target)
        {
            Init();
            return CreateNew(t =>
            {
                t.Execute(target);
                t.Execute(coroutine);
            });
        }

        /// <summary>
        /// Injects a coroutine at the start of another coroutine
        /// </summary>
        /// <param name="coroutine">Coroutine to inject at the start of the target</param>
        /// <param name="target">Target coroutine</param>
        /// <returns>Altered coroutine</returns>
        public static IEnumerator InjectAtHead(System.Collections.IEnumerator coroutine, IEnumerator target)
        {
            Init();
            return CreateNew(t =>
            {
                t.Execute(coroutine);
                t.Execute(target);
            });
        }

        /// <summary>
        /// Injects a coroutine at the end of another coroutine
        /// </summary>
        /// <param name="coroutine">Coroutine to inject at the end of the target</param>
        /// <param name="target">Target coroutine</param>
        /// <returns>Altered coroutine</returns>
        public static IEnumerator InjectAtTail(System.Collections.IEnumerator coroutine, IEnumerator target)
        {
            Init();
            return CreateNew(t =>
            {
                t.Execute(target);
                t.Execute(coroutine);
            });
        }

        /// <summary>
        /// Creates a new coroutine
        /// </summary>
        /// <param name="func">Coroutine to initialize</param>
        /// <returns>Altered coroutine</returns>
        public static IEnumerator CreateNew(System.Action<FTCoroutine.CoroutineBuilder> func)
        {
            Init();
            FTCoroutine.CoroutineBuilder b = new FTCoroutine.CoroutineBuilder();
            func(b);
            return CastFT(new FTCoroutine(b));
        }

        /// <summary>
        /// Creates a new coroutine from a managed coroutine
        /// </summary>
        /// <param name="func">Coroutine instance</param>
        /// <returns>Altered coroutine</returns>
        public static IEnumerator CreateNew(System.Collections.IEnumerator func)
        {
            Init();
            FTCoroutine.CoroutineBuilder b = new FTCoroutine.CoroutineBuilder();
            b.Execute(func);
            return CastFT(new FTCoroutine(b));
        }

        internal static IEnumerator CastFT(FTCoroutine coroutine)
        {
            return new IEnumerator(coroutine.Pointer);
        }
    }
}