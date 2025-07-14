using Il2CppSystem;
using UnityEngine;

namespace FeralTweaks.Managers
{
    public class FeralTweaksManagerBehaviourInterceptionRule
    {
        public FeralTweaksManagerBehaviourInterceptionRule(Type target, FeralTweaksManagerBehaviourInterceptionRuleType type, FeralTweaksManagerBehaviourInterceptionMethod method)
        {
            Target = target;
            RuleType = type;
            Method = method;
        }

        public Type Target { get; private set; }
        public FeralTweaksManagerBehaviourInterceptionRuleType RuleType { get; private set; }
        public FeralTweaksManagerBehaviourInterceptionMethod Method { get; private set; }
    }

    public enum FeralTweaksManagerBehaviourInterceptionRuleType
    { 
        ALLOFMANAGER,
        ALLOFBEHAVIOUR
    }

    public enum FeralTweaksManagerBehaviourInterceptionMethod
    {
        /// <summary>
        /// Takes over behaviours, registering them to the injected manager instead of the original manager
        /// </summary>
        TAKEOVER,

        /// <summary>
        /// Links with behaviours, registering them to the original manager but linking them to the injected manager so the injected manager can interact with it
        /// </summary>
        LINK
    }
}