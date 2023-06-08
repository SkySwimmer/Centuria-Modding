using System;
using System.IO;
using System.Runtime.InteropServices;
using DiscordRPC;
using FeralTweaks;
using FeralTweaks.Mods;
using FeralTweaks.Networking;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace FeralDiscordRpcMod
{
    public class RpcMod : NetworkedFeralTweaksMod
    {
        private static DiscordRpcClient client;
        private static string clientid = "1115933633967050812";

        public override void Init()
        {
            //AddModHandshakeRequirementForSelf(Version);
        }

        public override void PostInit()
        {
            // Check platform
            if (OperatingSystem.IsWindows())
            {
                bool wine = false;
                try
                {
                    // Check for wine
                    IntPtr ptr = NativeLibrary.Load("ntdll.dll");
                    NativeLibrary.GetExport(ptr, "wine_get_version");
                    wine = true;
                }
                catch
                {
                    // Real windows
                    client = new DiscordRpcClient(clientid);
                }
                if (wine)
                {
                    // Check
                    if (!File.Exists(ModBaseDirectory + "/winepipebridge.dll.so"))
                    {
                        LogError("Unable to load wine compatibility layer for Rich Presence!");
                        client = new DiscordRpcClient(clientid);
                    }
                    else
                    {
                        // Wine
                        NativeLibrary.Load(Path.GetFullPath(ModBaseDirectory + "/winepipebridge.dll.so"));
                        client = new DiscordRpcClient(clientid, client: new WineUnixPipeClient());
                    }
                }
            }
            else
            {
                client = new DiscordRpcClient(clientid);
            }

            // Init RPC logging
            client.Logger = new ModLogger() { mod = this };

            // Bind updates
            client.OnReady += (sender, e) =>
            {
                LogInfo("Received Ready from user " + e.User.Username);
            };
            client.OnPresenceUpdate += (sender, e) =>
            {
                LogInfo("Received Update! " + e.Presence);
            };

            // Init
            client.Initialize();

            // Register classes
            ClassInjector.RegisterTypeInIl2Cpp<RPC>();

            // Create object for the mod
            GameObject obj = new GameObject();
            obj.name = "~RPC";
            obj.AddComponent<RPC>();
            GameObject.DontDestroyOnLoad(obj);

            // Set presence
            client.SetPresence(new RichPresence()
            {
                Details = "Loading the game...",
                Assets = new DiscordRPC.Assets()
                {
                    LargeImageKey = "logo"
                }
            });
        }

        public class RPC : UnityEngine.MonoBehaviour
        {
            public RPC() { }
            public RPC(IntPtr ptr) : base(ptr) { }

            public void Update()
            {
                //client.Invoke();
            }

            public void OnApplicationQuit()
            {
                // Close client
                client.Dispose();
            }
        }
    }
}