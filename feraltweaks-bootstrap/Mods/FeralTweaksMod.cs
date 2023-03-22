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
        internal List<string> _depends = new List<string>();
        internal List<string> _optDepends = new List<string>();
        internal List<string> _conflicts = new List<string>();
        internal List<string> _loadAfter = new List<string>();
        private StreamWriter LogWriter;
        private bool locked;
        
        internal void Initialize()
        {
            if (!Regex.Match(ID, "^[0-9A-Za-z._,]+$").Success)
                throw new ArgumentException("Invalid mod ID: " + ID);
            Define();
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
        /// Defines mod dependencies and information
        /// </summary>
        protected abstract void Define();

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
        protected void DefineLoadAfter(string id)
        {
            if (locked)
                throw new ArgumentException("Locked registry");
            if (!_loadAfter.Contains(id))
                _loadAfter.Add(id);
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
