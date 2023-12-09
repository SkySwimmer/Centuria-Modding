using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DiscordRPC;
using FeralTweaks;
using FeralTweaks.Mods;
using FeralTweaks.Networking;
using Il2CppInterop.Runtime.Injection;
using Newtonsoft.Json;
using UnityEngine;
using HarmonyLib;
using feraltweaks.Patches.AssemblyCSharp;
using TMPro;
using System.Threading;
using Server;

namespace FeralDiscordRpcMod
{
    public class RpcMod : NetworkedFeralTweaksMod
    {
        private class JoinReq
        {
            public string playerID;
            public string tpSecret;
        }

        private static JoinReq pendingJoinRequest;
        private static string prevSecret;
        private static string currentSecret;
        private static long secretGenTime;
        private static System.Random rnd = new System.Random();
        private static Dictionary<string, RpcJoinPlayerResultPacket> pendingResults = new Dictionary<string, RpcJoinPlayerResultPacket>();

        private static string tpSecret;
        private static string joinExe;
        private static string partyID;
        private static DiscordRpcClient client;
        private static string clientid = "1115933633967050812";
        private static long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        private static Conf config;

        public override void Init()
        {
            // Register packets
            RegisterPacket(new RpcJoinPlayerRequestPacket());
            RegisterPacket(new RpcJoinPlayerResultPacket());
        }

        private class BtnConf
        {
            public string label;
            public string url;
        }

        private class Conf
        {
            public int pipe = -1;
            public bool joiningEnabled;
            public string joinExecutableLinux;
            public string joinExecutableWindows;
            public string joinExecutableOSX;
            public string joinExecutableAndroid;
            public string joinExecutableIOS;
            public bool disableAskToJoin;
            public int partySize;
            public BtnConf[] buttons;
        }

        public override void PostInit()
        {
            // Find config
            if (!File.Exists(ConfigDir + "/config.json"))
            {
                Directory.CreateDirectory(ConfigDir);
                File.WriteAllText(ConfigDir + "/config.json", "{\n"
                    + "    \"pipe\": -1,\n"
                    + "    \"joiningEnabled\": " + (Environment.GetEnvironmentVariable("CENTURIA_LAUNCHER_PATH") != null ? "true" : "false") + ",\n"
                    + "    \"disableAskToJoin\": false,"
                    + "    \"joinExecutableWindows\": \"\",\n"
                    + "    \"joinExecutableLinux\": \"\",\n"
                    + "    \"joinExecutableOSX\": \"\",\n"
                    + "    \"joinExecutableIOS\": \"\",\n"
                    + "    \"joinExecutableAndroid\": \"\",\n"
                    + "    \"partySize\": 10,\n"
                    + "    \"buttons\": []\n"
                    + "}\n");
            }

            // Load config
            config = JsonConvert.DeserializeObject<Conf>(File.ReadAllText(ConfigDir + "/config.json"));
            if (config.partySize <= 1)
            {
                LogWarn("Invalid party size in config: " + config.partySize + ", resetting to 10");
                config.partySize = 10;
                File.WriteAllText(ConfigDir + "/config.json", JsonConvert.SerializeObject(config, Formatting.Indented));
            }

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
                    client = new DiscordRpcClient(clientid, config.pipe);
                }
                if (wine)
                {
                    // Check
                    if (!File.Exists(ModBaseDirectory + "/winepipebridge.dll.so"))
                    {
                        LogError("Unable to load wine compatibility layer for Rich Presence!");
                        client = new DiscordRpcClient(clientid, config.pipe);
                    }
                    else
                    {
                        // Wine
                        NativeLibrary.Load(Path.GetFullPath(ModBaseDirectory + "/winepipebridge.dll.so"));
                        client = new DiscordRpcClient(clientid, config.pipe, client: new WineUnixPipeClient());
                    }
                    joinExe = config.joinExecutableLinux;
                }
                else
                {
                    joinExe = config.joinExecutableWindows;
                }
            }
            else
            {
                client = new DiscordRpcClient(clientid);
                joinExe = config.joinExecutableLinux;
                if (OperatingSystem.IsAndroid())
                    joinExe = config.joinExecutableAndroid;
                else if (OperatingSystem.IsIOS())
                    joinExe = config.joinExecutableIOS;
                else if (OperatingSystem.IsMacOS())
                    joinExe = config.joinExecutableOSX;
            }

            // Check env var
            if (Environment.GetEnvironmentVariable("CENTURIA_LAUNCHER_PATH") != null)
            {
                string pth = Environment.GetEnvironmentVariable("CENTURIA_LAUNCHER_PATH");
                if (File.Exists(pth))
                    joinExe = pth;
            }

            // Init RPC logging
            client.Logger = new ModLogger() { mod = this };

            // Bind updates
            client.OnReady += (sender, e) =>
            {
                LogInfo("Received Ready from user " + e.User.Username);
                if (client.CurrentPresence != null)
                {
                    RichPresence pr = client.CurrentPresence.Clone();
                    if (pr.Timestamps == null)
                        pr.Timestamps = new Timestamps();
                    pr.Timestamps.Start = DateTime.UtcNow;
                    client.SetPresence(pr);
                }
            };
            client.OnPresenceUpdate += (sender, e) =>
            {
                LogDebug("Received Update! " + JsonConvert.SerializeObject(e.Presence));
            };
            client.OnJoinRequested += (sender, e) =>
            {
                if (!config.disableAskToJoin)
                {
                    FeralTweaks.Actions.FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                    {
                        // Show popup
                        try
                        {
                            UI_Window_YesNoPopup.CloseWindow();
                        }
                        catch { }
                        UI_Window_YesNoPopup.OpenWindow(e.User.Username + " wishes to join your game.", "Discord user " + e.User.Username + " requested to join your party and game, accept join request?", "Accept", "Deny", (Il2CppSystem.Action<bool>)new System.Action<bool>(res =>
                        {
                            client.Respond(e, res);
                        }));
                    });
                }
            };
            client.OnJoin += (sender, e) =>
            {
                try
                {
                    // Parse secret
                    Dictionary<string, string> sec = JsonConvert.DeserializeObject<Dictionary<string, string>>(e.Secret);
                    if (sec != null && sec.ContainsKey("id") && sec.ContainsKey("pid") && sec.ContainsKey("sc"))
                    {
                        string partyID = sec["id"];
                        string playerID = sec["pid"];
                        string secret = sec["sc"];

                        // Run async
                        Task.Run(() =>
                        {
                            // Check if the teleport can be performed
                            bool valid = false;
                            bool wait = false;
                            if (NetworkManager.instance != null && NetworkManager.instance._uuid != null && NetworkManager.instance._serverConnection != null)
                            {
                                // Already ingame
                                valid = true;
                                if (UI_ProgressScreen.instance.IsVisible)
                                    wait = true; // Switching
                            }
                            else if (feraltweaks.FeralTweaks.IsAutoLogin)
                            {
                                // Autologin
                                valid = true;
                                wait = true;
                            }

                            // Perform teleport
                            if (valid)
                            {
                                // Check if server has RPC
                                if (FeralTweaksServer.IsModLoaded("discordrpc"))
                                {
                                    // Prepare to send packet
                                    while (pendingResults.ContainsKey(playerID))
                                        Thread.Sleep(100);
                                    lock (pendingResults)
                                        pendingResults[playerID] = null;

                                    // Verify secret, send packet to server
                                    RpcJoinPlayerRequestPacket pkt = new RpcJoinPlayerRequestPacket();
                                    pkt.playerID = playerID;
                                    pkt.partyID = partyID;
                                    pkt.secret = secret;
                                    GetMessenger().SendPacket(pkt);

                                    // Wait for response
                                    RpcJoinPlayerResultPacket res = null;
                                    for (int i = 0; i < 100; i++)
                                    {
                                        lock (pendingResults)
                                        {
                                            res = pendingResults[playerID];
                                            if (res != null)
                                                break;
                                        }
                                        Thread.Sleep(100);
                                    }
                                    lock (pendingResults)
                                        pendingResults.Remove(playerID);
                                    string tpSecret = null;
                                    if (res != null && res.success)
                                        tpSecret = res.secret;
                                } else
                                    tpSecret = null;

                                // Set party
                                RpcMod.partyID = partyID;
                                RichPresence pr = client.CurrentPresence;
                                client.SetPresence(pr.Clone().WithParty(new Party()
                                {
                                    ID = partyID,
                                    Max = (pr.Party == null ? config.partySize : pr.Party.Max),
                                    Size = 1
                                }));

                                // Teleport
                                if (!wait)
                                {
                                    // Run tp now
                                    FeralTweaks.Actions.FeralTweaksActionManager.ScheduleDelayedActionForUnity(() => TeleportToPlayer(playerID, tpSecret));
                                }
                                else
                                {
                                    // Wait for world switch
                                    pendingJoinRequest = new JoinReq()
                                    {
                                        tpSecret = tpSecret,
                                        playerID = playerID
                                    };
                                }
                            }
                        });
                    }
                }
                catch
                {

                }
            };

            // Setup
            client.Subscribe(EventType.Join | EventType.JoinRequest);
            if (config.joiningEnabled && joinExe != null && File.Exists(joinExe))
                client.RegisterUriScheme(executable: joinExe);

            // Init
            client.Initialize();

            // Register classes
            ClassInjector.RegisterTypeInIl2Cpp<RPC>();

            // Set presence
            List<Button> btns = new List<Button>();
            foreach (BtnConf btn in config.buttons)
            {
                btns.Add(new Button()
                {
                    Label = btn.label,
                    Url = btn.url
                });
            }
            RichPresence pr = new RichPresence()
            {
                Details = "Preparing the game...",
                Assets = new DiscordRPC.Assets()
                {
                    LargeImageKey = "logo"
                },

                Buttons = btns.ToArray(),
                Timestamps = new Timestamps()
                {
                    Start = DateTime.UtcNow
                }
            };
            client.SetPresence(pr);

            // Create object for the mod
            GameObject obj = new GameObject();
            GameObject.DontDestroyOnLoad(obj);
            obj.name = "~RPC";
            obj.AddComponent<RPC>();
            Harmony.CreateAndPatchAll(GetType());
        }

        private static void TeleportToPlayer(string playerID, string tpSecret)
        {
            // Show loading window
            try
            {
                UI_Window_OkPopup.CloseWindow();
            }
            catch { }
            connectPopup = true;
            UI_Window_LoadingRegistrationWebApp.OpenWindow();

            // Wait
            FeralTweaks.Actions.FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
            {
                if (WindowManager.ExistsOrIsLoading("UI_Window_LoadingRegistrationWebApp"))
                    return false;

                RpcMod.tpSecret = tpSecret;
                RelationshipManager.instance.GoToPlayer(playerID);
                return true;
            });
        }

        private static bool taskRunning = false;
        private static bool connectPopup = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkManager), "SendServerMessage", new Type[] { typeof(INetMessageWriter) })]
        public static void SendPacket(INetMessageWriter message)
        {
            // Check
            if (message.Cmd == XtCmd.RelationshipFollowerJumpToRoom.Cmd())
            {
                // Check tp secret
                if (tpSecret != null)
                {
                    // Write secret
                    message.WriteBool(true);
                    message.WriteString(tpSecret);
                    tpSecret = null;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_ProgressScreen), "Hide")]
        public static void Hide()
        {
            FeralTweaks.Actions.FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
            {
                if (UI_ProgressScreen.instance.IsVisibleOrFading)
                    return false;

                FeralTweaks.Actions.FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                {
                    // Run action
                    if (pendingJoinRequest != null)
                    {
                        if (NetworkManager.instance == null || NetworkManager.instance._uuid == null || NetworkManager.instance._serverConnection == null)
                        {
                            pendingJoinRequest = null;
                            return;
                        }
                        if (RoomManager.instance.CurrentLevelDef.levelType == ELevelType.TUTORIAL)
                            return;
                        TeleportToPlayer(pendingJoinRequest.playerID, pendingJoinRequest.tpSecret);
                        pendingJoinRequest = null;
                    }
                });
                return true;
            });
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_LoadingRegistrationWebApp), "OnOpen")]
        public static void OpenLoadingPopupPatch(UI_Window_LoadingRegistrationWebApp __instance)
        {
            if (!connectPopup)
                return;
            connectPopup = false;
            GameObject body = GetChild(__instance.gameObject, "Body_Popup");
            GameObject header = GetChild(body, "Header_Popup");
            GameObject text = GetChild(header, "Text_Header");
            GameObject text2 = GetChild(body, "Text_Message");
            TMP_Text textHeader = text.GetComponent<TMP_Text>();
            TMP_Text textMsg = text2.GetComponent<TMP_Text>();
            textHeader.text = "Joining player...";
            textMsg.text = "Joining player from invite...";
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_ProgressScreen), "Update")]
        public static void Update()
        {
            // Update presence if 5 seconds passed
            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start >= 1000)
            {
                if (taskRunning)
                    return;
                taskRunning = true;
                Task.Run(() =>
                {
                    // Update
                    RichPresence pr = client.CurrentPresence.Clone();

                    // Update party
                    if (NetworkManager.instance == null || NetworkManager.instance._uuid == null || NetworkManager.instance._serverConnection == null)
                    {
                        pr.Party = null;
                        pr.Secrets = null;
                        partyID = null;
                        currentSecret = null;
                        prevSecret = null;
                    }
                    else if (config.joiningEnabled && joinExe != null && File.Exists(joinExe))
                    {
                        // Create party
                        if (partyID == null)
                            partyID = Guid.NewGuid().ToString();
                        int size = 1;
                        if (pr.Party != null)
                            size = pr.Party.Size;
                        pr.Party = new Party()
                        {
                            ID = partyID,
                            Max = config.partySize,
                            Size = size
                        };

                        // Regenerate secret if needed
                        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - secretGenTime > (30 * 60 * 1000))
                        {
                            // Regenerate
                            prevSecret = currentSecret;
                            currentSecret = "";
                            for (int i = 0; i < 28; i++)
                                currentSecret += (char)rnd.Next('0', 'Z');
                            secretGenTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }

                        // Build secret
                        Dictionary<string, string> secretPayload = new Dictionary<string, string>();
                        secretPayload["id"] = pr.Party.ID;
                        secretPayload["pid"] = NetworkManager.instance._uuid;
                        secretPayload["sc"] = currentSecret;
                        string secret = JsonConvert.SerializeObject(secretPayload);

                        // Set secrets
                        pr.Secrets = new Secrets()
                        {
                            JoinSecret = secret
                        };
                    }

                    // Update location
                    if (UI_ProgressScreen.instance == null || UI_ProgressScreen.instance.IsVisibleOrFading || WindowManager.instance == null || RoomManager.instance == null || RoomManager.instance.CurrentLevelDefID == null || RoomManager.instance.CurrentLevelDefID == "")
                    {
                        pr.Details = "In loading screen";
                        pr.Assets = new DiscordRPC.Assets()
                        {
                            LargeImageKey = "logo",
                            LargeImageText = "Fer.al - Loading Screen"
                        };
                    }
                    else
                    {
                        // Find map
                        if (WindowManager.ExistsOrIsLoading("UI_Window_Login"))
                        {
                            pr.Details = "In title screen";
                            pr.Assets = new DiscordRPC.Assets()
                            {
                                LargeImageKey = "logo",
                                LargeImageText = "Fer.al - Title Screen"
                            };
                        }
                        else
                        {
                            switch (RoomManager.instance.CurrentLevelDefID)
                            {
                                case "820":
                                    {
                                        pr.Details = "Exploring City Fera";
                                        pr.Assets = new DiscordRPC.Assets()
                                        {
                                            SmallImageKey = "logo",
                                            LargeImageKey = "cf",
                                            LargeImageText = "Fer.al - City Fera"
                                        };
                                        break;
                                    }
                                case "1689":
                                    {
                                        pr.Details = "In a sanctuary";
                                        pr.Assets = new DiscordRPC.Assets()
                                        {
                                            SmallImageKey = "logo",
                                            LargeImageKey = "sanc",
                                            LargeImageText = "Fer.al - Sanctuary"
                                        };
                                        break;
                                    }
                                case "2364":
                                    {
                                        pr.Details = "Exploring the Blood Tundra";
                                        pr.Assets = new DiscordRPC.Assets()
                                        {
                                            SmallImageKey = "logo",
                                            LargeImageKey = "bt",
                                            LargeImageText = "Fer.al - Blood Tundra"
                                        };
                                        break;
                                    }
                                case "9687":
                                    {
                                        pr.Details = "Exploring Lakeroot Valley";
                                        pr.Assets = new DiscordRPC.Assets()
                                        {
                                            SmallImageKey = "logo",
                                            LargeImageKey = "lv",
                                            LargeImageText = "Fer.al - Lakeroot Valley"
                                        };
                                        break;
                                    }
                                case "2147":
                                    {
                                        pr.Details = "Exploring Mugmyre Marsh";
                                        pr.Assets = new DiscordRPC.Assets()
                                        {
                                            SmallImageKey = "logo",
                                            LargeImageKey = "mm",
                                            LargeImageText = "Fer.al - Mugmyre Marsh"
                                        };
                                        break;
                                    }
                                case "1825":
                                    {
                                        pr.Details = "Exploring Shattered Bay";
                                        pr.Assets = new DiscordRPC.Assets()
                                        {
                                            SmallImageKey = "logo",
                                            LargeImageKey = "sb",
                                            LargeImageText = "Fer.al - Shattered Bay"
                                        };
                                        break;
                                    }
                                case "3273":
                                    {
                                        pr.Details = "Exploring Sunken Thicket";
                                        pr.Assets = new DiscordRPC.Assets()
                                        {
                                            SmallImageKey = "logo",
                                            LargeImageKey = "st",
                                            LargeImageText = "Fer.al - Sunken Thicket"
                                        };
                                        break;
                                    }
                                case "2619":
                                    {
                                        pr.Details = "Visiting Twigla's Workshop";
                                        pr.Assets = new DiscordRPC.Assets()
                                        {
                                            SmallImageKey = "logo",
                                            LargeImageKey = "sb",
                                            LargeImageText = "Fer.al - Twigla's Workshop"
                                        };
                                        break;
                                    }
                                case "7790":
                                    {
                                        pr.Details = "Visiting Latchkey's Lab";
                                        pr.Assets = new DiscordRPC.Assets()
                                        {
                                            SmallImageKey = "logo",
                                            LargeImageKey = "st",
                                            LargeImageText = "Fer.al - Latchkey's Lab"
                                        };
                                        break;
                                    }
                                case "1718":
                                    {
                                        pr.Details = "Visiting Centuria";
                                        pr.Assets = new DiscordRPC.Assets()
                                        {
                                            SmallImageKey = "logo",
                                            LargeImageKey = "cf",
                                            LargeImageText = "Fer.al - Centuria"
                                        };
                                        break;
                                    }
                                case "1717":
                                    {
                                        pr.Details = "Visiting To Dye For";
                                        pr.Assets = new DiscordRPC.Assets()
                                        {
                                            SmallImageKey = "logo",
                                            LargeImageKey = "cf",
                                            LargeImageText = "Fer.al - To Dye For"
                                        };
                                        break;
                                    }
                                case "1716":
                                    {
                                        pr.Details = "Visiting Fera Fashions";
                                        pr.Assets = new DiscordRPC.Assets()
                                        {
                                            SmallImageKey = "logo",
                                            LargeImageKey = "cf",
                                            LargeImageText = "Fer.al - Fera Fashions"
                                        };
                                        break;
                                    }
                            }
                        }
                    }
                    client.SetPresence(pr);

                    // Update timestamp
                    start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    taskRunning = false;
                });
            }
        }

        public class RPC : UnityEngine.MonoBehaviour
        {
            public RPC() { }
            public RPC(IntPtr ptr) : base(ptr) { }

            public void OnApplicationQuit()
            {
                // Close client
                client.Dispose();
            }
        }

        private static GameObject GetChild(GameObject parent, string name)
        {
            if (name.Contains("/"))
            {
                string pth = name.Remove(name.IndexOf("/"));
                string ch = name.Substring(name.IndexOf("/") + 1);
                foreach (GameObject obj in GetChildren(parent))
                {
                    if (obj.name == pth)
                    {
                        GameObject t = GetChild(obj, ch);
                        if (t != null)
                            return t;
                    }
                }
                return null;
            }
            Transform tr = parent.transform;
            Transform[] trs = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in trs)
            {
                if (t.name == name && t.parent == tr.gameObject.transform)
                {
                    return t.gameObject;
                }
            }
            return null;
        }

        private static GameObject[] GetChildren(GameObject parent)
        {
            Transform tr = parent.transform;
            List<GameObject> children = new List<GameObject>();
            Transform[] trs = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform trCh in trs)
            {
                if (trCh.parent == tr.gameObject.transform)
                    children.Add(trCh.gameObject);
            }
            return children.ToArray();
        }

        internal void HandleJoinRequest(RpcJoinPlayerRequestPacket packet)
        {
            // Check party id
            if (partyID == packet.partyID && currentSecret != null)
            {
                // Check secret
                if (packet.secret == currentSecret || (prevSecret != null && packet.secret == prevSecret))
                {
                    // Send response
                    RpcJoinPlayerResultPacket pkt2 = new RpcJoinPlayerResultPacket();
                    pkt2.playerID = packet.playerID;
                    pkt2.success = true;
                    GetMessenger().SendPacket(pkt2);
                    return;
                }
            }

            // Send response
            RpcJoinPlayerResultPacket pkt = new RpcJoinPlayerResultPacket();
            pkt.playerID = packet.playerID;
            pkt.success = false;
            GetMessenger().SendPacket(pkt);
        }

        internal void HandleJoinResult(RpcJoinPlayerResultPacket packet)
        {
            lock (pendingResults)
            {
                if (pendingResults.ContainsKey(packet.playerID))
                    pendingResults[packet.playerID] = packet;
            }
        }
    }
}