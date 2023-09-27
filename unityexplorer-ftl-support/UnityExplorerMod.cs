using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FeralTweaks.Mods;
using UnityExplorer;
using UnityExplorer.Config;
using HarmonyLib;
using UniverseLib;
using System.Reflection;

namespace FtlSupportWrappers.UnityExplorer
{
    public class UnityExplorerMod : FeralTweaksMod, IExplorerLoader
    {
        public static UnityExplorerMod Instance;
        private ConfigHandler confHandler;

        public override void Init()
        {
            // Init
            LogInfo("Initializing UnityExplorer support...");
            confHandler = new FtlConfigHandler(ConfigDir);
            Instance = this;
        }

        public override void PostInit()
        {
            // Run mod
            LogInfo("Starting UnityExplorer support...");
            ExplorerCore.Init(this);
        }

        public string ExplorerFolderDestination => ConfigDir;

        public string ExplorerFolderName => "data";

        public string UnhollowedModulesFolder => "FeralTweaks/cache/assemblies";

        public ConfigHandler ConfigHandler => confHandler;

        public Action<object> OnLogMessage => (msg) => LogInfo(msg.ToString());

        public Action<object> OnLogWarning => (msg) => LogWarn(msg.ToString());

        public Action<object> OnLogError => (msg) => LogError(msg.ToString());
    }
}
