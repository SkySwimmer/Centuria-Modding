using FeralTweaks.Profiler.API;
using FeralTweaks.Profiler.Internal;

namespace FeralTweaks
{
    [RegisterLayers]
    internal class BaseProfilerLayers : ProfilerLayerCollection
    {
        [RuntimeInvokeUnityProfilingHook("Awake")]
        [RegisterLayer("Behaviour Awake", 100, true, true)]
        public const string UNITYENGINE_LIFECYCLE_AWAKE = "unityengine.lifecycle.awake";
        
        [RuntimeInvokeUnityProfilingHook("OnEnable")]
        [RegisterLayer("Behaviour OnEnable", 100, true, true)]
        public const string UNITYENGINE_LIFECYCLE_ONENABLE = "unityengine.lifecycle.onenable";

        [RuntimeInvokeUnityProfilingHook("OnDisable")]
        [RegisterLayer("Behaviour OnDisable", 100, true, true)]
        public const string UNITYENGINE_LIFECYCLE_ONDISABLE = "unityengine.lifecycle.ondisable";
        
        [RuntimeInvokeUnityProfilingHook("Reset")]
        [RegisterLayer("Behaviour Reset", 100, true, true)]
        public const string UNITYENGINE_LIFECYCLE_RESET = "unityengine.lifecycle.reset";

        [RuntimeInvokeUnityProfilingHook("Start")]
        [RegisterLayer("Behaviour Start", 100, true, true)]
        public const string UNITYENGINE_LIFECYCLE_START = "unityengine.lifecycle.start";

        [RuntimeInvokeUnityProfilingHook("FixedUpdate")]
        [RegisterLayer("Behaviour FixedUpdate", 100, true, true)]
        public const string UNITYENGINE_LIFECYCLE_FIXEDUPDATE = "unityengine.lifecycle.fixedupdate";

        [RuntimeInvokeUnityProfilingHook("Update")]
        [RegisterLayer("Behaviour Update", 100, true, true)]
        public const string UNITYENGINE_LIFECYCLE_UPDATE = "unityengine.lifecycle.update";

        [RuntimeInvokeUnityProfilingHook("LateUpdate")]
        [RegisterLayer("Behaviour LateUpdate", 100, true, true)]
        public const string UNITYENGINE_LIFECYCLE_LATEUPDATE = "unityengine.lifecycle.lateupdate";

        [RuntimeInvokeUnityProfilingHook("OnDestroy")]
        [RegisterLayer("Behaviour OnDestroy", 100, true, true)]
        public const string UNITYENGINE_LIFECYCLE_ONDESTROY = "unityengine.lifecycle.ondestroy";

        [RegisterLayer("Coroutine Execution", 100, true, true)]
        public const string UNITYENGINE_COROUTINES = "unityengine.coroutines"; // FIXME: implement profiling for this

        public override void SetupLayers()
        {
        }
    }
}