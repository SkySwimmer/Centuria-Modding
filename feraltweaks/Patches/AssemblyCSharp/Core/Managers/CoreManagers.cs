using FeralTweaks.Actions;
using FeralTweaks.Managers;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class CoreManagersContainer : InjectedManagersContainer
    {
        public CoreManagersContainer() : base(ClassInjector.DerivedConstructorPointer<CoreManagersContainer>())
        {
			ClassInjector.DerivedConstructorBody(this);
        }

        public Il2CppReferenceField<LoadFinishManager> loadFinishManager;
        public Il2CppReferenceField<TestManager> testManager;

        public override void Setup()
        {
            SetupManager<TestManager>(nameof(testManager)); // FIXME: remove
            SetupManager<LoadFinishManager>(nameof(loadFinishManager));
        }
    }
}