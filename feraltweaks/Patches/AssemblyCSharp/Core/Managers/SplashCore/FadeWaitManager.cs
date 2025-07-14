using FeralTweaks.Actions;
using FeralTweaks.Managers;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem.Collections;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class FadeWaitManager : FeralTweaksManagerBase
    {
        public FadeWaitManager() : base(ClassInjector.DerivedConstructorPointer<FadeWaitManager>())
        {
			ClassInjector.DerivedConstructorBody(this);
        }

        public FadeWaitManager(nint pointer) : base(pointer)
        {
        }

        [FTManagerSetInstance]
        public static FadeWaitManager instance { get; internal set; }

        [FTManagerSetInstance]
        public static FadeWaitManager coreInstance { get; internal set; }

        [HideFromIl2Cpp]
        protected override void SetupLoadRules(LoadRuleBuilder ruleBuilder)
        {
            ruleBuilder.AddLoadPriorityRule(int.MaxValue);
            ruleBuilder.AddLoadFirstRule();
        }

        [HideFromIl2Cpp]
        protected override void SetupBehaviourInterceptionRules(BehaviourInterceptionRuleBuilder ruleBuilder)
        {
            // No need to intercept any behaviours
        }

        [HideFromIl2Cpp]
        protected override void SetupGameObject(GameObject gameObject)
        {
            // We dont need any special gameobject content
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
                    if (!InitialLoadScreenFadein._doneLoadingPatch)
                        return ctx.Continue();
                    return ctx.Return(new WaitForSeconds(1f));
                });
            });
        }
    }
}