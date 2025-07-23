using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using Il2CppSystem.Collections.Generic;
using static FeralTweaks.Managers.InjectedManagersContainer;
using UnityEngine;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Linq;

namespace FeralTweaks.Managers
{
    /// <summary>
    /// FeralTweaks base manager class, for use with <see cref="FeralTweaks.Managers.CoreManagerInjectors"/>
    /// 
    /// <para>Note: when implementing, do NOT override Awake, Start, Update or OnDestroy, instead use MAwake, MStart, MStart, MUpdate and MOnDestroy like the vanilla game does, otherwise core logic will be lost</para>
    /// </summary>
    public abstract class FeralTweaksManagerBase : ManagerBase
    {
        internal InjectedManagersContainer container;
        internal GameObject setupGameObject;

        internal List<ManagedBehaviour> registeredBehavioursBackend;
        internal List<ManagedBehaviour> registeredDisableBehavioursBackend;

        internal FeralTweaksManagerBehaviourInterceptionRule[] interceptionRules;

        internal System.Collections.Generic.List<ManagedBehaviour> linkedBehaviours = new System.Collections.Generic.List<ManagedBehaviour>();


        protected FeralTweaksManagerBase() : base()
        {
            throw new ArgumentException("Unable to infer parameterless constructor automatically, please make sure injected managers define their own constructors with proper derived pointers");
        }

        protected FeralTweaksManagerBase(nint pointer) : base(pointer)
        {
        }

        public void Awake()
        {
            registeredBehavioursBackend = new List<ManagedBehaviour>();
            registeredDisableBehavioursBackend = new List<ManagedBehaviour>();
            MAwake();
        }

        public virtual void MAwake()
        { }

        [HideFromIl2Cpp]
        internal void Setup(InjectedManagersContainer container, GameObject setupGameObject, RegisteredManager mgr)
        {
            SetupGameObject(setupGameObject);
            this.container = container;
            this.setupGameObject = setupGameObject;

            LoadRuleBuilder b = new LoadRuleBuilder();
            SetupLoadRules(b);
            mgr.loadRules = b.rules.ToArray();

            BehaviourInterceptionRuleBuilder b2 = new BehaviourInterceptionRuleBuilder();
            SetupBehaviourInterceptionRules(b2);
            interceptionRules = b2.rules.ToArray();
        }

        protected class BehaviourInterceptionRuleBuilder
        {
            internal System.Collections.Generic.List<FeralTweaksManagerBehaviourInterceptionRule> rules = new System.Collections.Generic.List<FeralTweaksManagerBehaviourInterceptionRule>();

            /// <summary>
            /// Adds a intercecption rule based on behaviour type
            /// </summary>
            /// <typeparam name="T">Behaviour type to register the interception rule for</typeparam>
            public void AddInterceptOfBehaviourTypeRule<T>() where T : ManagedBehaviour
            {
                rules.Add(new FeralTweaksManagerBehaviourInterceptionRule(Il2CppType.Of<T>(), FeralTweaksManagerBehaviourInterceptionRuleType.ALLOFBEHAVIOUR, FeralTweaksManagerBehaviourInterceptionMethod.LINK));
            }

            /// <summary>
            /// Adds a intercecption rule to intercept all behaviours of a specific manager
            /// </summary>
            /// <typeparam name="T">Manager type to register the interception rule for</typeparam>
            public void AddInterceptOfManagerRule<T>() where T : ManagerBase
            {
                rules.Add(new FeralTweaksManagerBehaviourInterceptionRule(Il2CppType.Of<T>(), FeralTweaksManagerBehaviourInterceptionRuleType.ALLOFMANAGER, FeralTweaksManagerBehaviourInterceptionMethod.LINK));
            }

            /// <summary>
            /// Adds a intercecption rule based on behaviour type
            /// </summary>
            /// <typeparam name="T">Behaviour type to register the interception rule for</typeparam>
            /// <param name="method">Interception method to use, use TAKEOVER for full object takeover (would switch the manager its using), use LINK for a simple linkup</param>
            public void AddInterceptOfBehaviourTypeRule<T>(FeralTweaksManagerBehaviourInterceptionMethod method) where T : ManagedBehaviour
            {
                rules.Add(new FeralTweaksManagerBehaviourInterceptionRule(Il2CppType.Of<T>(), FeralTweaksManagerBehaviourInterceptionRuleType.ALLOFBEHAVIOUR, method));
            }

            /// <summary>
            /// Adds a intercecption rule to intercept all behaviours of a specific manager
            /// </summary>
            /// <typeparam name="T">Manager type to register the interception rule for</typeparam>
            /// <param name="method">Interception method to use, use TAKEOVER for full object takeover (would switch the manager its using), use LINK for a simple linkup</param>
            public void AddInterceptOfManagerRule<T>(FeralTweaksManagerBehaviourInterceptionMethod method) where T : ManagerBase
            {
                rules.Add(new FeralTweaksManagerBehaviourInterceptionRule(Il2CppType.Of<T>(), FeralTweaksManagerBehaviourInterceptionRuleType.ALLOFMANAGER, method));
            }
        }

        protected class LoadRuleBuilder
        {
            internal System.Collections.Generic.List<FeralTweaksManagerLoadRule> rules = new System.Collections.Generic.List<FeralTweaksManagerLoadRule>();

            /// <summary>
            /// Adds a load priority rule
            /// </summary>
            /// <param name="value">Load priority</param>
            public void AddLoadPriorityRule(int value)
            {
                rules.Add(new FeralTweaksManagerLoadRule(null, FeralTweaksManagerLoadRuleType.LOADPRIORITY, value));
            }

            /// <summary>
            /// Adds a loadfirst rule
            /// </summary>
            public void AddLoadFirstRule()
            {
                rules.Add(new FeralTweaksManagerLoadRule(null, FeralTweaksManagerLoadRuleType.LOADFIRST, 0));
            }

            /// <summary>
            /// Adds a loadlast rule
            /// </summary>
            public void AddLoadLastRule()
            {
                rules.Add(new FeralTweaksManagerLoadRule(null, FeralTweaksManagerLoadRuleType.LOADLAST, 0));
            }

            /// <summary>
            /// Adds a loadbefore rule
            /// </summary>
            public void AddLoadBeforeRule<T>() where T : ManagerBase
            {
                rules.Add(new FeralTweaksManagerLoadRule(Il2CppType.Of<T>(), FeralTweaksManagerLoadRuleType.LOADBEFORE, 0));
            }

            /// <summary>
            /// Adds a dependson rule
            /// </summary>
            public void AddDependsOnRule<T>() where T : ManagerBase
            {
                rules.Add(new FeralTweaksManagerLoadRule(Il2CppType.Of<T>(), FeralTweaksManagerLoadRuleType.DEPENDSON, 0));
            }
        }

        /// <summary>
        /// Called to set up load rules
        /// </summary>
        /// <param name="ruleBuilder">Load rule builder</param>
        [HideFromIl2Cpp]
        protected abstract void SetupLoadRules(LoadRuleBuilder ruleBuilder);

        /// <summary>
        /// Called to set up behaviour interception rules
        /// </summary>
        /// <param name="ruleBuilder">Behaviour interception rule builder</param>
        [HideFromIl2Cpp]
        protected abstract void SetupBehaviourInterceptionRules(BehaviourInterceptionRuleBuilder ruleBuilder);

        /// <summary>
        /// Called to set up the manager game object
        /// </summary>
        /// <param name="gameObject">Manager game object to configure</param>
        [HideFromIl2Cpp]
        protected abstract void SetupGameObject(GameObject gameObject);

        /// <summary>
        /// Retrieves all linked behaviours (both directly registered and interception link)
        /// </summary>
        /// <returns>Array of ManagedBehaviour instances</returns>
        [HideFromIl2Cpp]
        public ManagedBehaviour[] GetAllLinkedBehaviours()
        {
            return GetAllLinkedBehaviours<ManagedBehaviour>();
        }

        /// <summary>
        /// Retrieves all linked behaviours of a specific type (both directly registered and interception link)
        /// </summary>
        /// <typeparam name="T">Behaviour type</typeparam>
        /// <returns>Array of ManagedBehaviour instances</returns>
        [HideFromIl2Cpp]
        public T[] GetAllLinkedBehaviours<T>() where T : ManagedBehaviour
        {
            return GetAllLinkedBehaviours(Il2CppType.Of<T>()).Select(t => t.Cast<T>()).ToArray();
        }

        /// <summary>
        /// Retrieves all linked behaviours of a specific type (both directly registered and interception link)
        /// </summary>
        /// <param name="type">Behaviour type</param>
        /// <returns>Array of ManagedBehaviour instances</returns>
        [HideFromIl2Cpp]
        public ManagedBehaviour[] GetAllLinkedBehaviours(Il2CppSystem.Type type)
        {
            System.Collections.Generic.List<ManagedBehaviour> behaviours = new System.Collections.Generic.List<ManagedBehaviour>();

            // Collect behaviours
            foreach (ManagedBehaviour linked in linkedBehaviours)
            {
                // Check
                if (type.IsAssignableFrom(linked.GetIl2CppType()))
                    behaviours.Add(linked);
            }

            return behaviours.ToArray();
        }

        /// <summary>
        /// Called a the behaviour is registered
        /// </summary>
        /// <param name="behaviour">Behaviour that was registered</param>
        public virtual void OnRegisterBehaviourToManager(ManagedBehaviour behaviour)
        {
        }

        /// <summary>
        /// Called when a behaviour is linked to a manager (also called for interception, not only takeover)
        /// </summary>
        /// <param name="behaviour">Behaviour that was linked</param>
        public virtual void OnLinkBehaviourToManager(ManagedBehaviour behaviour)
        {
        }

        /// <summary>
        /// Called when a behaviour is unregistered
        /// </summary>
        /// <param name="behaviour">Behaviour that was unregistered</param>
        public virtual void OnUnregisterBehaviourFromManager(ManagedBehaviour behaviour)
        {
        }

        /// <summary>
        /// Called when a behaviour is unlinked from a manager (also called for interception, not only takeover)
        /// </summary>
        /// <param name="behaviour">Behaviour that was unlinked</param>
        public virtual void OnUnlinkBehaviourFromManager(ManagedBehaviour behaviour)
        {
        }

    }
}