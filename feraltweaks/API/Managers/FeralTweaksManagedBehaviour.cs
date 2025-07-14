using System.Collections.Generic;
using Il2CppInterop.Runtime.Injection;
using System;
using Il2CppInterop.Runtime;
using System.Linq;

namespace FeralTweaks.Managers
{
    /// <summary>
    /// FeralTweaks managed behaviour class, a MonoBehaviour managed by a Feral game Manager, for use with <see cref="FeralTweaks.Managers.FeralTweaksManagedBehaviour"/> as well as any vanilla <see cref="ManagerBase"/> instance, annotate with <see cref="FeralTweaks.Managers.ManagedBehaviourFTManagerAttribute"/> to automatically register to a manager.
    /// 
    /// Note: when implementing, do NOT override Awake, OnEnable, Start, Update or OnDestroy, instead use MStart, MAwake, MStart, MUpdate and MOnDestroy like the vanilla game does, otherwise core logic will be lost
    /// </summary>
    public abstract class FeralTweaksManagedBehaviour : ManagedBehaviour
    {
        private ManagerBase controllingManager; // Current owner
        private ManagerBase originalOwnedManager; // Original owner
        private List<ManagerBase> linkedManagers = new List<ManagerBase>(); // Any managers linked to this behaviour via non-takeover interception

        static FeralTweaksManagedBehaviour()
        {
            ClassInjector.RegisterTypeInIl2Cpp<FeralTweaksManagedBehaviour>();
        }

        protected FeralTweaksManagedBehaviour() : base()
        {
            throw new ArgumentException("Unable to infer parameterless constructor automatically, please make sure injected managed behaviours define their own constructors with proper derived pointers");
        }

        protected FeralTweaksManagedBehaviour(nint pointer) : base(pointer)
        {
        }

        public override void Awake()
        {
            // Assign
            originalOwnedManager = base.Manager;

            // Set defaults
            controllingManager = originalOwnedManager;

            // Call base calls
            SetInstanceInternal();
            UAwake();
        }

        internal void ChangeOwner(ManagerBase newOwner)
        {
            controllingManager = newOwner;
            _manager = controllingManager;
        }

        internal void AddLinkedManager(ManagerBase linkedMgr)
        {
            linkedManagers.Add(linkedMgr);
        }

        internal void ResetLinksFully()
        {
            // Reset ownership
            controllingManager = originalOwnedManager;
            _manager = controllingManager;
            linkedManagers.Clear();
        }

        /// <summary>
        /// Called when the behaviour is registered
        /// </summary>
        /// <param name="owningManager">Owning manager (the actual manager that owns it, could be different from the target due to injection)</param>
        /// <param name="originalOwner">Original owner (the target owner)</param>
        public virtual void OnRegisteredToManager(ManagerBase owningManager, ManagerBase originalOwner)
        {
        }

        /// <summary>
        /// Called when the behaviour is linked to a manager (also called for interception, not only takeover)
        /// </summary>
        /// <param name="manager">Manager that was linked to</param>
        public virtual void OnLinkedToManager(ManagerBase manager)
        {
        }

        /// <summary>
        /// Called when the behaviour is unregistered
        /// </summary>
        /// <param name="owningManager">Owning manager (the actual manager that owns it, could be different from the target due to injection)</param>
        /// <param name="originalOwner">Original owner (the target owner)</param>
        public virtual void OnUnregisteredFromManager(ManagerBase owningManager, ManagerBase originalOwner)
        {
        }

        /// <summary>
        /// Called when the behaviour is unlinked from a manager (also called for interception, not only takeover)
        /// </summary>
        /// <param name="manager">Manager that was unlinked from</param>
        public virtual void OnUnlinkedFromManager(ManagerBase manager)
        {
        }

        /// <summary>
        /// Retrieves the active manager
        /// 
        /// Note: its recommended to use GetLinkedManager&amp;lt;T&gt;() instead to make sure the behaviour uses the intended manager instead of a manager that took control, use GetLinkedManager() to retrieve any of the ManagerBase class (including the current owner), and GetLinkedManager&amp;lt;T&gt;() to retrieve based on type
        /// </summary>
        public new ManagerBase Manager
        {
            get
            {
                // Active manager
                return controllingManager;
            }
        }

        /// <summary>
        /// Retrieves the original owning manager, itll always be assigned to the manager initially used as parent
        /// 
        /// Note: its recommended to use GetLinkedManager&amp;lt;T&gt;() instead to make sure the behaviour uses the intended manager instead of a manager that took control, use GetLinkedManager() to retrieve any of the ManagerBase class (including the current owner), and GetLinkedManager&amp;lt;T&gt;() to retrieve based on type
        /// </summary>
        public ManagerBase OriginalManager
        {
            get
            {
                // Original owner only
                return originalOwnedManager;
            }
        }

        /// <summary>
        /// Retrieves the current active manager
        /// </summary>
        /// <returns>ManagerBase instance</returns>
        public ManagerBase GetLinkedManager()
        {
            return GetLinkedManager<ManagerBase>();
        }

        /// <summary>
        /// Retrieves the current active manager of the first matching type
        /// </summary>
        /// <typeparam name="T">Manager type</typeparam>
        /// <returns>Manager instance or null</returns>
        public T GetLinkedManager<T>() where T : ManagerBase
        {
            return (T)GetLinkedManager(Il2CppType.Of<T>());
        }

        /// <summary>
        /// Retrieves the current active manager of the first matching type
        /// </summary>
        /// <type name="type">Manager type</type>
        /// <returns>Manager instance or null</returns>
        public ManagerBase GetLinkedManager(Il2CppSystem.Type type)
        {
            // Check owner
            if (type.IsAssignableFrom(controllingManager.GetIl2CppType()))
                return controllingManager;

            // Any linked manager
            foreach (ManagerBase mgr in linkedManagers)
            {
                // Check compatible
                if (type.IsAssignableFrom(mgr.GetIl2CppType()))
                    return mgr;
            }

            // Original
            if (type.IsAssignableFrom(originalOwnedManager.GetIl2CppType()))
                return originalOwnedManager;

            // Fail
            return null;
        }

        /// <summary>
        /// Retrieves all linked managers
        /// </summary>
        /// <returns>All linked manager instances</returns>
        public ManagerBase[] GetAllLinkedManagers()
        {
            return GetAllLinkedManagers<ManagerBase>();
        }

        /// <summary>
        /// Retrieves all linked managers of a type
        /// </summary>
        /// <typeparam name="T">Manager type</typeparam>
        /// <returns>Array of linked managers matching the type</returns>
        public T[] GetAllLinkedManagers<T>() where T : ManagerBase
        {
            return GetAllLinkedManagers(Il2CppType.Of<T>()).Select(t => t.Cast<T>()).ToArray();
        }

        /// <summary>
        /// Retrieves all linked managers of a type
        /// </summary>
        /// <param name="type">Manager type</param>
        /// <returns>Array of linked managers matching the type</returns>
        public ManagerBase[] GetAllLinkedManagers(Il2CppSystem.Type type)
        {
            List<ManagerBase> mgrs = new List<ManagerBase>();
            
            // Check current
            if (type.IsAssignableFrom(controllingManager.GetIl2CppType()))
                mgrs.Add(controllingManager);
        
            // Add linked
            foreach (ManagerBase mgr in linkedManagers)
            {
                // Check compatible
                if (type.IsAssignableFrom(mgr.GetIl2CppType()) && !mgrs.Any(t => t.Pointer == mgr.Pointer))
                    mgrs.Add(mgr);
            }

            // Add original
            if (type.IsAssignableFrom(originalOwnedManager.GetIl2CppType()) && !mgrs.Any(t => t.Pointer == originalOwnedManager.Pointer))
                mgrs.Add(originalOwnedManager);

            return mgrs.ToArray();
        }

    }
}