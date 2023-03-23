using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FeralTweaks.Mods
{
    /// <summary>
    /// FeralTweaks Mod Abstract
    /// </summary>
    public abstract class FeralTweaksMod
    {
        internal int _priority = 0;
        internal List<string> _depends = new List<string>();
        internal List<string> _optDepends = new List<string>();
        internal List<string> _conflicts = new List<string>();
        internal List<string> _loadBefore = new List<string>();
        internal Dictionary<string, string> _dependencyVersions = new Dictionary<string, string>();
        private StreamWriter LogWriter;
        private bool locked;
        private string baseFolder;

        internal void Initialize(string baseFolder)
        {
            if (!Regex.Match(ID, "^[0-9A-Za-z._,]+$").Success)
                throw new ArgumentException("Invalid mod ID: " + ID);
            Define();
            this.baseFolder = baseFolder;
            locked = true;
            LogWriter = new StreamWriter("FeralTweaks/logs/" + ID + ".log");
            LogWriter.AutoFlush = true;
        }

        /// <summary>
        /// Logs a debug message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void LogDebug(string message)
        {
            if (!FeralTweaksLoader.DebugLoggingEnabled)
                return;
            LogWriter.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [DBG] " + message);
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [DBG] [Loader] " + message);
        }

        /// <summary>
        /// Logs an info message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void LogInfo(string message)
        {
            LogWriter.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [INF] " + message);
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [INF] [" + ID + "] " + message);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void LogWarn(string message)
        {
            LogWriter.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [WRN] " + message);
            Console.Error.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [WRN] [" + ID + "] " + message);
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void LogError(string message)
        {
            LogWriter.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [ERR] " + message);
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [ERR] [" + ID + "] " + message);
        }

        /// <summary>
        /// Mod ID
        /// </summary>
        public abstract string ID { get; }

        /// <summary>
        /// Mod version
        /// </summary>
        public abstract string Version { get; }

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
        /// <param name="version">Dependency version (start with '>=', '>', '&lt;', '&lt;=' or '!=' to define minimal/maximal versions, '&amp;' allows for multiple version rules, spaces are stripped during parsing)</param>
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
        /// Pre-initializes the mod
        /// </summary>
        public virtual void PreInit() { }

        /// <summary>
        /// Initializes the mod
        /// </summary>
        public abstract void Init();

        /// <summary>
        /// Post-initializes the mod
        /// </summary>
        public virtual void PostInit() { }
    }
}
