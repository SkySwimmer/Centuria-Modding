using FeralTweaks.BundleInjection;
using Il2CppInterop.Runtime;

namespace feraltweaks.Patches.Bundles
{
    public class AnimationEventsBundleHook : BundleHook
    {
        public override string BundlePath => "Animation/FeralAnimationEvents";

        public override void Setup()
        {
            // Hook assets
            HookAsset("assets/bundledassets/animation/feralanimationevents/feralanimationevents.asset", Il2CppType.Of<FeralAnimationEvents>(), (def, bundle, an, at, asset) =>
            {
                // Asset loaded, inject into it

                // Load animation event list
                FeralAnimationEvents events = asset.Cast<FeralAnimationEvents>();
                
                
            });
        }
    }
}