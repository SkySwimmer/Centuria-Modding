using FeralTweaks.Actions;
using FeralTweaks.Managers;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem.Collections;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class LoadFinishManager : FeralTweaksManagerBase
    {
        public LoadFinishManager() : base(ClassInjector.DerivedConstructorPointer<LoadFinishManager>())
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        public LoadFinishManager(nint pointer) : base(pointer)
        {
        }

        [FTManagerSetInstance]
        public static LoadFinishManager instance { get; internal set; }

        [FTManagerSetInstance]
        public static LoadFinishManager coreInstance { get; internal set; }

        [HideFromIl2Cpp]
        protected override void SetupLoadRules(LoadRuleBuilder ruleBuilder)
        {
            ruleBuilder.AddLoadPriorityRule(int.MinValue + 1); // Make sure this has the LOWEST priority, just note that MinValue results in a small one unless its + 1
            ruleBuilder.AddLoadLastRule();
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
                    // Post-load
                    // FIXME: implement fully
                    ctx = ctx;
                });
            });
        }
    }
}