using Il2CppSystem;
using UnityEngine;

namespace FeralTweaks.Managers
{
    public class FeralTweaksManagerLoadRule
    {
        public FeralTweaksManagerLoadRule(Type targetManager, FeralTweaksManagerLoadRuleType type, int value)
        {
            TargetManager = targetManager;
            RuleType = type;
            RuleValue = value;
        }

        public Type TargetManager { get; private set; }
        public FeralTweaksManagerLoadRuleType RuleType { get; private set; }
        public int RuleValue { get; private set; } = 0;
    }

    public enum FeralTweaksManagerLoadRuleType
    { 
        LOADPRIORITY,
        LOADBEFORE,
        DEPENDSON,
        LOADLAST,
        LOADFIRST
    }
}