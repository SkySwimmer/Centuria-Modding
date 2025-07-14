using FeralTweaks.Actions;
using FeralTweaks.Managers;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class SplashCoreManagersContainer : InjectedManagersContainer
    {
        public SplashCoreManagersContainer() : base(ClassInjector.DerivedConstructorPointer<SplashCoreManagersContainer>())
        {
			ClassInjector.DerivedConstructorBody(this);
        }

        public Il2CppReferenceField<FadeWaitManager> fadeWaitManager;
        public Il2CppReferenceField<LoadFirstManager> loadFirstManager;

        public override void Setup()
        {
            SetupManager<FadeWaitManager>(nameof(fadeWaitManager));
            SetupManager<LoadFirstManager>(nameof(loadFirstManager));
        }
    }
}