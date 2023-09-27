using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FeralTweaks.Mods;
using Newtonsoft.Json;
using UnityExplorer;
using UnityExplorer.Config;

namespace FtlSupportWrappers.UnityExplorer
{
    public class FtlConfigHandler : ConfigHandler
    {
        private string configDir;
        private Dictionary<string, string> config = new Dictionary<string, string>();

        public FtlConfigHandler(string configDir)
        {
            this.configDir = configDir;
            LoadConfig();
        }

        public override void Init()
        {
        }

        public override void LoadConfig()
        {
            // Read config
            if (File.Exists(configDir + "/unityexplorer.json"))
                config = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(configDir + "/unityexplorer.json"));
        }

        public override void SaveConfig()
        {
            Directory.CreateDirectory(configDir);
            File.WriteAllText(configDir + "/unityexplorer.json", JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        public override void RegisterConfigElement<T>(ConfigElement<T> element)
        {
            // Set entry
            if (!config.ContainsKey(element.Name))
                config[element.Name] = JsonConvert.SerializeObject(element.Value);
            else
                element.Value = JsonConvert.DeserializeObject<T>(config[element.Name]);
        }

        public override T GetConfigValue<T>(ConfigElement<T> element)
        {
            // Find entry
            if (config.ContainsKey(element.Name))
            {
                T val = JsonConvert.DeserializeObject<T>(config[element.Name]);
                element.Value = val;
                return val;
            }
			throw new IOException("Could not find entry: " + element.Name);
        }

        public override void SetConfigValue<T>(ConfigElement<T> element, T value)
        {
            // Set entry
            config[element.Name] = JsonConvert.SerializeObject(value);
            SaveConfig();
        }
    }
}