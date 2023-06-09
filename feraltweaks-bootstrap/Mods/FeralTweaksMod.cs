using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FeralTweaks.Logging;

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
        
        internal void Initialize(string baseFolder)
        {
            if (!Regex.Match(ID, "^[0-9A-Za-z._,]+$").Success)
                throw new ArgumentException("Invalid mod ID: " + ID);
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
