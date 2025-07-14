using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppSystem;
using Il2CppSystem.Reflection;
using UnityEngine;

namespace FeralTweaks.Managers
{
    public abstract class InjectedManagersContainer : Il2CppSystem.Object
    {
        private static GameObject rootContainer;
        internal System.Collections.Generic.List<RegisteredManager> managers = new System.Collections.Generic.List<RegisteredManager>();
        private static System.Collections.Generic.List<string> injectedTypes = new System.Collections.Generic.List<string>();

        internal static void InjectManagerContainerTypeIfNeeded<T>() where T : InjectedManagersContainer
        {
            // Handle fields
            System.Type t = typeof(T);
            foreach (System.Reflection.FieldInfo field in ((System.Reflection.TypeInfo)t).DeclaredFields)
            {
                // Check field type
                if (field.FieldType.Namespace + "." +field.FieldType.Name == typeof(Il2CppReferenceField<>).Namespace + "." + typeof(Il2CppReferenceField<>).Name)
                {
                    // Get subtype
                    if (field.FieldType.GenericTypeArguments[0].IsAssignableTo(typeof(FeralTweaksManagerBase)))
                    {
                        // Register
                        string tName2 = field.FieldType.GenericTypeArguments[0].FullName;
                        if (!injectedTypes.Contains(tName2))
                        {
                            injectedTypes.Add(tName2);
                            ClassInjector.RegisterTypeInIl2Cpp(field.FieldType.GenericTypeArguments[0]);
                        }
                    }
                }
            }

            string tName = typeof(T).FullName;
            if (!injectedTypes.Contains(tName))
            {
                injectedTypes.Add(tName);
                ClassInjector.RegisterTypeInIl2Cpp<T>();
            }
        }

        internal class RegisteredManager
        {
            public FeralTweaksManagerBase manager;
            public FeralTweaksManagerLoadRule[] loadRules;
            public GameObject gameObject;
            public FieldInfo field;
            public string fieldName;
        }

        protected InjectedManagersContainer(nint pointer) : base(pointer)
        {
        }

        /// <summary>
        /// Called to set up managers, creates 
        /// </summary>
        /// <typeparam name="T">Manager type</typeparam>
        /// <param name="fieldName">Manager field name (must be present in this class, FT will redirect the Core to this class's field)</param>
        /// <returns>Manager game object (with disabled manager attached)</returns>
        protected GameObject SetupManager<T>(string fieldName) where T : FeralTweaksManagerBase
        {
            // Check if registered
            RegisteredManager mgr = managers.Find(t => t.fieldName == fieldName);
            if (mgr != null)
            {
                // Check type
                if (mgr.manager.GetType().IsAssignableFrom(typeof(T)))
                    return mgr.gameObject;
                throw new System.ArgumentException("Manager with field " + fieldName + " was already registered but with an incompatible type.");
            }

            // Register if needed
            string tName = typeof(T).FullName;
            if (!injectedTypes.Contains(tName))
            {
                injectedTypes.Add(tName);
                ClassInjector.RegisterTypeInIl2Cpp<T>();
            }

            // Get field
            FieldInfo field;
            try
            {
                field = Il2CppType.From(GetType()).GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
                if (field == null || !field.FieldType.IsAssignableFrom(Il2CppType.Of<T>()))
                    throw new System.Exception();
            }
            catch
            {
                throw new System.ArgumentException("Could not find field " + fieldName + " in " + GetType().Name + " (please make sure the type matches the manager, otherwise it will not load)");
            }

            // Create gameobject
            GameObject obj = new GameObject();
            obj.SetActive(false);
            if (rootContainer == null)
            {
                rootContainer = new GameObject("FT_InjectedManagersCache");
                GameObject.DontDestroyOnLoad(rootContainer);
            }
            obj.transform.parent = rootContainer.transform;

            // Add manager, rename, move to DontDestroyOnLoad and disable manager
            T manager = (T) obj.AddComponent(field.FieldType);
            manager.name = field.FieldType.Name;
            manager.enabled = false;
            obj.name = manager.name;

            // Register
            mgr = new RegisteredManager();
            manager.Setup(this, obj, mgr);
            mgr.manager = manager;
            mgr.gameObject = obj;
            mgr.field = field;
            mgr.fieldName = fieldName;
            managers.Add(mgr);

            // Assign field
            ((Il2CppReferenceField<T>)GetType().GetField(fieldName).GetValue(this)).Value = manager;

            // Return
            return obj;
        }

        /// <summary>
        /// Called to set up all managers
        /// </summary>
        [HideFromIl2Cpp]
        public abstract void Setup();
    }
}