using FeralTweaks.Actions;
using FeralTweaks.Managers;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem.Collections;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class LoadFirstManager : FeralTweaksManagerBase
    {
        public LoadFirstManager() : base(ClassInjector.DerivedConstructorPointer<LoadFirstManager>())
        {
			ClassInjector.DerivedConstructorBody(this);
        }

        public LoadFirstManager(nint pointer) : base(pointer)
        {
        }

        [FTManagerSetInstance]
        public static LoadFirstManager instance { get; internal set; }

        [FTManagerSetInstance]
        public static LoadFirstManager coreInstance { get; internal set; }

        [HideFromIl2Cpp]
        protected override void SetupLoadRules(LoadRuleBuilder ruleBuilder)
        {
            ruleBuilder.AddLoadFirstRule();
            ruleBuilder.AddDependsOnRule<FadeWaitManager>();
            ruleBuilder.AddLoadPriorityRule(int.MaxValue);
        }

        [HideFromIl2Cpp]
        protected override void SetupBehaviourInterceptionRules(BehaviourInterceptionRuleBuilder ruleBuilder)
        {
            // No need to intercept any behaviours
        }

        [HideFromIl2Cpp]
        protected override void SetupGameObject(GameObject gameObject)
        {
            // No need for any special gameobject stuff
        }

        public override bool HasInitCoroutine()
        {
            return true;
        }

        public override IEnumerator InitCoroutine()
        {
            return FeralTweaksCoroutines.CreateNew(t =>
            {
                t.Execute(ctx =>
                {
                    // FIXME: implement fully
                    ctx = ctx;
                });
            });
        }
    }
}