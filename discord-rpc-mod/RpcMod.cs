using System;
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
        public override void Init()
        {
            //AddModHandshakeRequirementForSelf(Version);
        }
		
        public override void PostInit()
        {
            // Init RPC
            client = new DiscordRpcClient("1115933633967050812");
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
            client.SetPresence(new RichPresence() {
                Details= "Loading the game...",
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
