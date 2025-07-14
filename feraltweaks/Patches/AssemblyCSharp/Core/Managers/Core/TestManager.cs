using FeralTweaks.Actions;
using FeralTweaks.Managers;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem.Collections;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class TestManager : FeralTweaksManagerBase
    {
        static TestManager()
        {
            ClassInjector.RegisterTypeInIl2Cpp<TestBehaviour>();
        }

        public TestManager() : base(ClassInjector.DerivedConstructorPointer<TestManager>())
        {
			ClassInjector.DerivedConstructorBody(this);
        }

        public TestManager(nint pointer) : base(pointer)
        {
        }

        [ManagedBehaviourFTManager(typeof(TestManager))]
        public class TestBehaviour : FeralTweaksManagedBehaviour
        {
            public TestBehaviour() : base(ClassInjector.DerivedConstructorPointer<TestBehaviour>())
            {
                ClassInjector.DerivedConstructorBody(this);
            }

            public TestBehaviour(nint pointer) : base(pointer)
            {
            }
            
            public override void MAwake()
            {
                GetType();
            }

            public override void MOnEnable()
            {
                GetType();
            }

            public override void MStart()
            {
                GetType();
            }

            public override void MStartAfterLocal()
            {
                GetType();
            }

            public override void MOnDisable()
            {
                GetType();
            }

            public override void MOnDestroy()
            {
                GetType();
            }

            public override void MUpdate()
            {
                GetType();
            }
        }

        [FTManagerSetInstance]
        public static TestManager instance { get; internal set; }

        [FTManagerSetInstance]
        public static TestManager coreInstance { get; internal set; }

        [HideFromIl2Cpp]
        protected override void SetupLoadRules(LoadRuleBuilder ruleBuilder)
        {
            ruleBuilder.AddLoadBeforeRule<LoadFinishManager>();
        }

        [HideFromIl2Cpp]
        protected override void SetupGameObject(GameObject gameObject)
        {
            // No need for any special gameobject stuff
        }

        [HideFromIl2Cpp]
        protected override void SetupBehaviourInterceptionRules(BehaviourInterceptionRuleBuilder ruleBuilder)
        {
            // ruleBuilder.AddInterceptOfManagerRule<WorldObjectManager>();
            // ruleBuilder.AddInterceptOfBehaviourTypeRule<FeralAudioEmitter>(FeralTweaksManagerBehaviourInterceptionMethod.TAKEOVER);
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
                    ctx = ctx;

                    // Create test
                    GameObject test = new GameObject("test");
                    test.AddComponent<TestBehaviour>();
                    GameObject.DontDestroyOnLoad(test);
                    test = test;

                    // FIXME: remove manager
                });
            });
        }
    }
}