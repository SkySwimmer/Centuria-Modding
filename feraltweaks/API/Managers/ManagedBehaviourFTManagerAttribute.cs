using System;

namespace FeralTweaks.Managers
{
    /// <summary>
    /// Attribute used to find the target manager of a ManagedBehaviour instance
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ManagedBehaviourFTManagerAttribute : Attribute
    {
        public string ManagerName { get; private set; }
        
        public ManagedBehaviourFTManagerAttribute(string name)
        {
            ManagerName = name;
        }
        
        public ManagedBehaviourFTManagerAttribute(Type type)
        {
            ManagerName = type.FullName;
        }
    }
}