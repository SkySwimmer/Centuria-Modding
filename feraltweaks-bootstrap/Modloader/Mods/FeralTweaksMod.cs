using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FeralTweaks.Logging;
using FeralTweaksBootstrap;
using FeralTweaksBootstrap.Detour;

namespace FeralTweaks.Mods
{
    /// <summary>
    /// FeralTweaks Mod Abstract
    /// </summary>
    public abstract class FeralTweaksMod
    {
        /// <summary>
        /// Raw injector delegate
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="clsName">Class name</param>
        /// <param name="clsPointer">Class pointer</param>
        /// <param name="objPointer">Object pointer</param>
        /// <param name="methodPointer">Method object pointer</param>
        /// <param name="methodParametersPointer">Method parameters pointer</param>
        /// <param name="originalMethod">Original method call pointer</param>
        /// <returns>Detour to execute or null if invalid</returns>
        public delegate RuntimeInvokeDetour RawInjectionHandler(string methodName, string clsName, IntPtr clsPointer, IntPtr objPointer, IntPtr methodPointer, IntPtr methodParametersPointer, RuntimeInvokeDetour originalMethod);

        internal int _priority = 0;
        internal List<string> _depends = new List<string>();
        internal List<string> _optDepends = new List<string>();
        internal List<string> _conflicts = new List<string>();
        internal List<string> _loadBefore = new List<string>();
        internal List<RawInjectionHandler> _rawDetours = new List<RawInjectionHandler>();
        internal Dictionary<string, string> _dependencyVersions = new Dictionary<string, string>();
        private static List<Assembly> modAssemblies = new List<Assembly>();
        private bool locked;
        private string baseFolder;

        internal string _id;
        internal string _version;

        private Logger logger;

        public Logger Logger
        {
            get
            {
                return logger;
            }
        }
        
        internal void Initialize(string baseFolder, Assembly assembly)
        {
            if (!Regex.Match(ID, "^[0-9A-Za-z._,]+$").Success)
                throw new ArgumentException("Invalid mod ID: " + ID);
            modAssemblies.Add(assembly);
            Define();
            this.baseFolder = baseFolder;
            locked = true;
            logger = Logger.GetLogger(ID);
        }

        /// <summary>
        /// Logs an info message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void LogInfo(string message)
        {
            logger.Info(message);
        }

        /// <summary>
        /// Logs a debug message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void LogDebug(string message)
        {
            logger.Debug(message);
        }

        /// <summary>
        /// Logs a trace message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void LogTrace(string message)
        {
            logger.Trace(message);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void LogWarn(string message)
        {
            logger.Warn(message);
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void LogError(string message)
        {
            logger.Error(message);
        }

        /// <summary>
        /// Logs a fatal error message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void LogFatal(string message)
        {
            logger.Fatal(message);
        }
        
        /// <summary>
        /// Mod ID
        /// </summary>
        public virtual string ID
        {
            get
            {
                return _id;
            }
        }

        /// <summary>
        /// Mod version
        /// </summary>
        public virtual string Version
        {
            get
            {
                return _version;
            }
        }

        /// <summary>
        /// Retrieves all mod assemblies
        /// </summary>
        public Assembly[] Assemblies
        {
            get
            {
                return modAssemblies.ToArray();
            }
        }

        /// <summary>
        /// Mod cache directory, only available during init, null during preinit
        /// </summary>
        public string CacheDir { get; internal set; }

        /// <summary>
        /// Mod config directory, only available during init, null during preinit
        /// </summary>
        public string ConfigDir { get; internal set; }

        /// <summary>
        /// Retrieves the directory containing mod files, <b>may return null depending on how the mod loaded.</b>
        /// </summary>
        public string ModBaseDirectory
        {
            get
            {
                return baseFolder;
            }
        }

        /// <summary>
        /// Defines mod dependencies and information
        /// </summary>
        protected virtual void Define() { }

        /// <summary>
        /// Defines the mod loading priority
        /// </summary>
        /// <param name="priority">Mod load priority, higher loads earlier</param>
        protected void DefinePriority(int priority)
        {
            if (locked)
                throw new ArgumentException("Locked registry");
            _priority = priority;
        }

        /// <summary>
        /// Defines dependency versions (applies to both dependencies and optional dependencies)
        /// </summary>
        /// <param name="id">Dependency ID</param>
        /// <param name="version">Dependency version (start with '>=', '>', '&lt;', '&lt;=' or '!=' to define minimal/maximal versions, '&amp;' allows for multiple version rules, '||' functions as the OR operator, spaces are stripped during parsing)</param>
        protected void DefineDependencyVersion(string id, string version)
        {
            if (locked)
                throw new ArgumentException("Locked registry");
            _dependencyVersions[id] = version;
        }

        /// <summary>
        /// Defines a dependency
        /// </summary>
        /// <param name="id">Mod ID</param>
        protected void DefineDependency(string id)
        {
            if (locked)
                throw new ArgumentException("Locked registry");
            if (!_depends.Contains(id))
                _depends.Add(id);
        }

        /// <summary>
        /// Defines a optional dependency
        /// </summary>
        /// <param name="id">Mod ID</param>
        protected void DefineOptionalDependency(string id)
        {
            if (locked)
                throw new ArgumentException("Locked registry");
            if (!_optDepends.Contains(id))
                _optDepends.Add(id);
        }

        /// <summary>
        /// Defines a mod ID that must load AFTER this mod
        /// </summary>
        /// <param name="id">Mod ID</param>
        [Obsolete("Use DefineLoadBefore instead, this method is incorrectly named")]
        protected void DefineLoadAfter(string id)
        {
            DefineLoadBefore(id);
        }

        /// <summary>
        /// Defines a mod ID that must load AFTER this mod
        /// </summary>
        /// <param name="id">Mod ID</param>
        protected void DefineLoadBefore(string id)
        {
            if (locked)
                throw new ArgumentException("Locked registry");
            if (!_loadBefore.Contains(id))
                _loadBefore.Add(id);
        }

        /// <summary>
        /// Defines which mods cannot load while having this mod loaded
        /// </summary>
        /// <param name="id">Mod ID</param>
        protected void DefineConflict(string id)
        {
            if (locked)
                throw new ArgumentException("Locked registry");
            if (!_conflicts.Contains(id))
                _conflicts.Add(id);
        }

        /// <summary>
        /// Registers a low-level runtime invoke detour
        /// </summary>
        /// <param name="handler">Injection handler (a method called to compare if the injected method is compatible)</param>
        protected void RegisterLowlevelRuntimeInvokeDetour(RawInjectionHandler handler)
        {
            if (!_rawDetours.Contains(handler))
                _rawDetours.Add(handler);
        }

        /// <summary>
        /// Removes a low-level runtime invoke detour
        /// </summary>
        /// <param name="handler">Injection handler to remove</param>
        protected void DeregisterLowlevelRuntimeInvokeDetour(RawInjectionHandler handler)
        {
            if (_rawDetours.Contains(handler))
                _rawDetours.Remove(handler);
        }
        
        /// <summary>
        /// Controls if harmony patches are allowed to be called directly
        /// </summary>
        public virtual bool AllowHarmonyDirectInvoke
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Controls if harmony patches are allowed to be applied at any load phase
        /// </summary>
        public virtual bool AllowHarmonyAtAllPhases
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Earliest load phase event, called when the loader prepares all mods (called just after the modloader finished gathering all mods)
        /// 
        /// <para>Note: harmony is blocked outside of mod ApplyPatch methods, please make sure to use FeralTweaksMod.ApplyPatch() to apply harmony patches, alternatively, override the internal flag AllowHarmonyDirectInvoke to alter this behaviour</para>
        /// </summary>
        public virtual void LoaderPreInit() { }

        /// <summary>
        /// Mod pre-initialization event, called when mods are pre-initialized (called before unity is loaded)
        /// 
        /// <para>Note: harmony is blocked outside of mod ApplyPatch methods, please make sure to use FeralTweaksMod.ApplyPatch() to apply harmony patches, alternatively, override the internal flag AllowHarmonyDirectInvoke to alter this behaviour</para>
        /// </summary>
        public virtual void ModPreInit() { }

        /// <summary>
        /// (Compatibility layer) Pre-initializes the mod
        /// </summary>
        [Obsolete("Use the new loading phases instead, for PreInit, use ModPreInit (pre-unity) or ModEarlyInit (after-unity)")]
        public virtual void PreInit() { }

        /// <summary>
        /// Mod unity pre-init event, called just after unity has become available
        /// 
        /// <para>Note: harmony is blocked during this phase to conserve load times, please use ModPreInit to define early core patches, or ModInit and later to define patches that can safely load after the game initializes, override the internal flag AllowHarmonyAtAllPhases to alter this behaviour</para>
        /// <para>Further note: as this is called during a unity frame update, any call in this method should be light on resources, any call that takes longer than 100ms will trigger a profiler warning and must instead be done via ... for smooth mod loading in all environments</para> // FIXME: add alternative
        /// </summary>
        public virtual void UnityPreInit() { }

        /// <summary>
        /// Mod early init event, called when the mod is going through the early initializaiton phase (called after unity has become available)
        /// 
        /// <para>Note: harmony is blocked during this phase to conserve load times, please use ModPreInit to define early core patches, or ModInit and later to define patches that can safely load after the game initializes, override the internal flag AllowHarmonyAtAllPhases to alter this behaviour</para>
        /// <para>Further note: as this is called during a unity frame update, any call in this method should be light on resources, any call that takes longer than 100ms will trigger a profiler warning and must instead be done via ... for smooth mod loading in all environments</para> // FIXME: add alternative
        /// </summary>
        public virtual void ModEarlyInit() { }

        /// <summary>
        /// (Compatibility layer) Initializes the mod
        /// </summary>
        [Obsolete("Use the new loading phases instead, for Init, use ModPreInit (pre-unity), ModEarlyInit (after-unity but early) or ModInit (intended phase for init)")]
        public virtual void Init() { }

        /// <summary>
        /// Game pre-init event, called when the game is going through pre-initialization
        /// 
        /// <para>Note: harmony is blocked outside of mod ApplyPatch methods, please make sure to use FeralTweaksMod.ApplyPatch() to apply harmony patches, alternatively, override the internal flag AllowHarmonyDirectInvoke to alter this behaviour</para>
        /// <para>Further note: as this is called during a unity frame update, any call in this method should be light on resources, any call that takes longer than 100ms will trigger a profiler warning and must instead be done via ... for smooth mod loading in all environments</para> // FIXME: add alternative
        /// </summary>
        public virtual void GamePreInit() { }

        /// <summary>
        /// Mod init event, called when the mod is being initialized, this phase is when patches are intended to be applied
        /// 
        /// <para>Note: harmony is blocked outside of mod ApplyPatch methods, please make sure to use FeralTweaksMod.ApplyPatch() to apply harmony patches, alternatively, override the internal flag AllowHarmonyDirectInvoke to alter this behaviour</para>
        /// <para>Further note: as this is called during a unity frame update, any call in this method should be light on resources, any call that takes longer than 100ms will trigger a profiler warning and must instead be done via ... for smooth mod loading in all environments</para> // FIXME: add alternative
        /// </summary>
        public virtual void ModInit() { }

        /// <summary>
        /// (Compatibility layer) Post-initializes the mod
        /// </summary>
        [Obsolete("Use the new loading phases instead, for PostInit, use ModPostInit")]
        public virtual void PostInit() { }

        /// <summary>
        /// Game init event, called when the game is going through main initialization
        /// 
        /// <para>Note: harmony is blocked outside of mod ApplyPatch methods, please make sure to use FeralTweaksMod.ApplyPatch() to apply harmony patches, alternatively, override the internal flag AllowHarmonyDirectInvoke to alter this behaviour</para>
        /// <para>Further note: as this is called during a unity frame update, any call in this method should be light on resources, any call that takes longer than 100ms will trigger a profiler warning and must instead be done via ... for smooth mod loading in all environments</para> // FIXME: add alternative
        /// </summary>
        public virtual void GameInit() { }

        /// <summary>
        /// Mod post-init event, called when all mods are being post-initialized at the end of the loading chain
        /// 
        /// <para>Note: harmony is blocked outside of mod ApplyPatch methods, please make sure to use FeralTweaksMod.ApplyPatch() to apply harmony patches, alternatively, override the internal flag AllowHarmonyDirectInvoke to alter this behaviour</para>
        /// <para>Further note: as this is called during a unity frame update, any call in this method should be light on resources, any call that takes longer than 100ms will trigger a profiler warning and must instead be done via ... for smooth mod loading in all environments</para> // FIXME: add alternative
        /// </summary>
        public virtual void ModPostInit() { }

        /// <summary>
        /// Game post-init event, called at the very end of the game initialization process
        /// 
        /// <para>Note: harmony is blocked outside of mod ApplyPatch methods, please make sure to use FeralTweaksMod.ApplyPatch() to apply harmony patches, alternatively, override the internal flag AllowHarmonyDirectInvoke to alter this behaviour</para>
        /// <para>Further note: as this is called during a unity frame update, any call in this method should be light on resources, any call that takes longer than 100ms will trigger a profiler warning and must instead be done via ... for smooth mod loading in all environments</para> // FIXME: add alternative
        /// </summary>
        public virtual void GamePostInit() { }

        /// <summary>
        /// Mod finalization event, called when all mods have finished their loading chains
        /// 
        /// <para>Note: harmony is blocked outside of mod ApplyPatch methods, please make sure to use FeralTweaksMod.ApplyPatch() to apply harmony patches, alternatively, override the internal flag AllowHarmonyDirectInvoke to alter this behaviour</para>
        /// <para>Further note: as this is called during a unity frame update, any call in this method should be light on resources, any call that takes longer than 300ms will trigger a profiler warning and must instead be done via ... for smooth mod loading in all environments</para> // FIXME: add alternative
        /// </summary>
        public virtual void ModFinalizeLoad() { }

        /// <summary>
        /// (Compatibility layer) Called at the very end of the mod loading process
        /// </summary>
        [Obsolete("Use the new loading phases instead, for FinalizeLoad, use ModFinalizeLoad")]
        public virtual void FinalizeLoad() { }

        /// <summary>
        /// Game finalization event, called after the game finalizes
        /// 
        /// <para>Note: harmony is blocked during this phas, override the internal flag AllowHarmonyAtAllPhases to alter this behaviour</para>
        /// <para>Further note: as this is called during a unity frame update, any call in this method should be light on resources, any call that takes longer than 300ms will trigger a profiler warning and must instead be done via ... for smooth mod loading in all environments</para> // FIXME: add alternative
        /// </summary>
        public virtual void GameFinalizeLoad() { }
    }
}
