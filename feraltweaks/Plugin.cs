using HarmonyLib;
using HarmonyLib.Tools;
using feraltweaks.Patches.AssemblyCSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Text;
using Server;
using UnityEngine;
using Random = System.Random;
using LitJson;
using FeralTweaks.Mods;

namespace feraltweaks
{
    public class Plugin : FeralTweaksMod
    {
        public const int ProtocolVersion = 1;

        public override string ID => "feraltweaks";
        public override string Version => "beta-1.0.0";

        protected override void Define()
        {
        }

        public static List<Func<bool>> actions = new List<Func<bool>>();
        public static List<Action> uiActions = new List<Action>();
        public static Dictionary<string, string> Patches = new Dictionary<string, string>();
        public static Dictionary<string, string> PatchConfig = new Dictionary<string, string>();
        public static string AutoLoginToken = null;

        public static bool ShowWorldJoinChatUnreadPopup;

        public static string DirectorAddress = null;
        public static string APIAddress = null;
        public static string ChatHost = null;
        public static string VoiceChatHost = null;
        public static int GamePort = -1;
        public static int VoiceChatPort = -1;
        public static int ChatPort = -1;
        public static int BlueboxPort = -1;
        public static int EncryptedGame = -1; // -1 = unset, 0 = false, 1 = true

        // Error message container for when login fails and the server includes a feraltweaks message field in the response
        public static string LoginErrorMessage = null;

        private static void StartActionThread()
        {
            // Start action thread
            Thread th = new Thread(() =>
            {
                while (true)
                {
                    Func<bool>[] actions;
                    
                    // Its not pretty but i had a concurrent modification exception once while using ToArray()
                    // Lists are not thread-safe so have to use toarray() until it doesnt fail
                    while(true)
                    {
                        try
                        {
                            actions = Plugin.actions.ToArray();
                            break;
                        }
                        catch { }
                    }

                    // Handle actions
                    foreach (Func<bool> ac in actions)
                    {
                        if (ac())
                            Plugin.actions.Remove(ac);
                    }

                    Thread.Sleep(10);
                }
            });
            th.IsBackground = true;
            th.Name = "FeralTweaks Action Thread";
            th.Start();
        }

        public override void Init()
        {
            // Load config
            LogInfo("Loading configuration...");
            Directory.CreateDirectory(ConfigDir);
            if (!File.Exists(ConfigDir + "/settings.props"))
            {
                LogInfo("Writing defaults...");
                Plugin.WriteDefaultConfig();
            }
            else
            {
                LogInfo("Processing data...");
                foreach (string line in File.ReadAllLines(ConfigDir + "/settings.props"))
                {
                    if (line == "" || line.StartsWith("#") || !line.Contains("="))
                        continue;
                    string key = line.Remove(line.IndexOf("="));
                    string value = line.Substring(line.IndexOf("=") + 1);
                    PatchConfig[key] = value;
                }
            }
            LogInfo("Configuration loaded.");

            // Start action thread
            StartActionThread();

            // Patch with harmony
            LogInfo("Applying patches...");
            ApplyPatch(typeof(BaseDefPatch));
            ApplyPatch(typeof(CoreChartDataManagerPatch));
            ApplyPatch(typeof(UI_Window_AccountCreationPatch));
            ApplyPatch(typeof(UI_Window_ChangeDisplayNamePatch));
            ApplyPatch(typeof(UI_Window_ResetPasswordPatch));
            ApplyPatch(typeof(UI_Window_TradeItemQuantityPatch));
            ApplyPatch(typeof(UI_Window_OkPopupPatch));
            ApplyPatch(typeof(UI_Window_YesNoPopupPatch));
            ApplyPatch(typeof(WWTcpClientPatch));
            ApplyPatch(typeof(WindUpdraftPatch));
            ApplyPatch(typeof(LoginLogoutPatches));
            ApplyPatch(typeof(CoreBundleManager2Patch));
            ApplyPatch(typeof(WorldObjectManagerPatch));
            ApplyPatch(typeof(UI_VersionPatch));
            ApplyPatch(typeof(MessageRouterPatch));
            ApplyPatch(typeof(ChatPatches));
            ApplyPatch(typeof(HttpRequestPatch));
            ApplyPatch(typeof(DOTweenAnimatorPatch));

            // Check command line
            LogInfo("Processing command line arguments...");
            int handoffPort = 0;
            int i = 0;
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg == "--launcher-handoff" && i + 1 < Environment.GetCommandLineArgs().Length)
                {
                    handoffPort = int.Parse(Environment.GetCommandLineArgs()[i + 1]);
                }
                i++;
            }
            if (handoffPort != 0)
            {
                LogInfo("Connecting to launcher...");
                TcpClient client = null;
                try
                {
                    client = new TcpClient("127.0.0.1", handoffPort);
                }
                catch
                {
                    LogError("Failed to connect to the launcher!");
                }
                if (client != null)
                {
                    LogInfo("Connected to the launcher, processing...");
                    try
                    {
                        StreamReader rd = new StreamReader(client.GetStream());
                        while (true)
                        {
                            string args = "";
                            string command = rd.ReadLine();
                            if (command == "end")
                                break;
                            if (command.Contains(" "))
                            {
                                args = command.Substring(command.IndexOf(" ") + 1);
                                command = command.Remove(command.IndexOf(" "));
                            }
                            switch (command)
                            {
                                case "serverenvironment":
                                    {
                                        // Parse environment
                                        string[] payload = args.Split(" ");
                                        if (payload.Length == 0)
                                            LogError("Error: missing argument(s) for serverenvironment: [directorhost] [apihost] [chathost] [chatport] [gameport] [voicehost] [voiceport] [blueboxport] [encryptedgame: true/false]");
                                        if (payload.Length >= 1)
                                            DirectorAddress = payload[0];
                                        if (payload.Length >= 2)
                                            APIAddress = payload[1];
                                        if (payload.Length >= 3)
                                            ChatHost = payload[2];
                                        if (payload.Length >= 4)
                                            ChatPort = int.Parse(payload[3]);
                                        if (payload.Length >= 5)
                                            GamePort = int.Parse(payload[4]);
                                        if (payload.Length >= 6)
                                            VoiceChatHost = payload[5];
                                        if (payload.Length >= 7)
                                            VoiceChatPort = int.Parse(payload[6]);
                                        if (payload.Length >= 8)
                                            BlueboxPort = int.Parse(payload[7]);
                                        if (payload.Length >= 9)
                                            EncryptedGame = payload[8].ToLower() == "true" ? 1 : 0;
                                        break;
                                    }
                                case "autologin":
                                    {
                                        if (args == "")
                                            LogError("Error: missing argument for autologin: token");
                                        else
                                            AutoLoginToken = args;
                                        break;
                                    }
                                case "chartpatch":
                                    {
                                        if (args == "")
                                            LogError("Error: missing argument for patchchart: patch-data");
                                        else
                                        {
                                            // Process data
                                            try
                                            {
                                                string patch = Encoding.UTF8.GetString(Convert.FromBase64String(args));
                                                string file = patch.Remove(patch.IndexOf("::"));
                                                patch = patch.Substring(patch.IndexOf("::") + 2);
                                                Patches[patch] = file;
                                            }
                                            catch
                                            {
                                                LogError("Error: invalid patch data for patchchart");
                                            }
                                        }
                                        break;
                                    }
                                case "config":
                                    {
                                        if (args == "")
                                            LogError("Error: missing argument for config: configuration-data");
                                        else
                                        {
                                            try
                                            {
                                                // Process data
                                                string config = Encoding.UTF8.GetString(Convert.FromBase64String(args)).Replace("\r", "");
                                                foreach (string line in config.Split('\n'))
                                                {
                                                    if (line == "" || line.StartsWith("#") || !line.Contains("="))
                                                        continue;
                                                    string key = line.Remove(line.IndexOf("="));
                                                    string value = line.Substring(line.IndexOf("=") + 1);
                                                    PatchConfig[key] = value;
                                                }
                                            }
                                            catch
                                            {
                                                LogError("Error: invalid configuration data for config");
                                            }
                                        }
                                        break;
                                    }
                            }
                        }
                    }
                    catch { }
                    client.Close();
                }
            }
            else
                LogInfo("No command line parameters received for launcher handoff, starting regularly...");
        }

        private void ApplyPatch(Type type)
        {
            LogInfo("Applying patch: " + type.FullName);
            Harmony.CreateAndPatchAll(type);
        }

        /// <summary>
        /// Writes the default configuration
        /// </summary>
        public static void WriteDefaultConfig()
        {
            File.WriteAllText(FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().ConfigDir + "/settings.props", "DisableUpdraftAudioSuppressor=false\nAllowNonEmailUsernames=false\nFlexibleDisplayNames=false\nUserNameRegex=^[\\w%+\\.-]+@(?:[a-zA-Z0-9-]+[\\.{1}])+[a-zA-Z]{2,}$\nDisplayNameRegex=^[0-9A-Za-z\\-_. ]+\nUserNameMaxLength=320\nDisplayNameMaxLength=16\nTradeItemLimit=99\nVersionLabel=${global:7358}\\n${game:version} (${game:build})\nEnableGroupChatTab=false\nJiggleResourceInteractions=false\nCityFeraMovingRocks=false\nCityFeraTeleporterSFX=false\nHttpDnsOverrides=game-assets.fer.al:23.218.218.148\n");
        }

        /// <summary>
        /// Called when the client receives packet
        /// </summary>
        /// <param name="id">Packet ID</param>
        /// <param name="reader">Packet reader</param>
        /// <returns>True if handled by feraltweaks, false otherwise</returns>
        public static bool HandlePacket(string id, INetMessageReader reader)
        {
            switch (id)
            {
                case "od":
                    {
                        // Object delete
                        WorldObjectDeleteMessage msg = new WorldObjectDeleteMessage(reader);
                        msg.RouteInfo = NetworkManager.Router._table[id];

                        // Check replication settings
                        if ((Plugin.PatchConfig.ContainsKey("OverrideReplicate-" + msg.ObjectId) && Plugin.PatchConfig["OverrideReplicate-" + msg.ObjectId].ToLower() == "true") || (Plugin.PatchConfig.ContainsKey("EnableReplication") && Plugin.PatchConfig["EnableReplication"].ToLower() == "true" && (!Plugin.PatchConfig.ContainsKey("OverrideReplicate-" + msg.ObjectId) || Plugin.PatchConfig["OverrideReplicate-" + msg.ObjectId].ToLower() != "false")))
                        {
                            // Remove manually
                            Plugin.uiActions.Add(() =>
                            {
                                FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogInfo("Destroying object: " + msg.ObjectId);
                                if (WorldObjectManager.instance._objects._objectsById.ContainsKey(msg.ObjectId))
                                {
                                    WorldObject obj = WorldObjectManager.instance._objects._objectsById[msg.ObjectId];
                                    try
                                    {
                                        ActorNPCSpawner npcSpawner = GetNpcSpawnerFrom(obj);
                                        if (npcSpawner != null && npcSpawner.ActorBase != null)
                                            npcSpawner.ActorBase.Delete();
                                    }
                                    catch
                                    {
                                        // Either unity or il2cpp would have goofed
                                    }
                                    obj.Delete();
                                    WorldObjectManager.instance._objects._objectsById.Remove(msg.ObjectId);
                                }
                            });
                            return true;
                        }

                        NetworkManager.Router.OnMessage(msg, NetworkManager.Router._queuedMessages);
                        return true;
                    }
                case "ou":
                    {
                        // Object update
                        WorldObjectMoveMessage msg = new WorldObjectMoveMessage(reader);
                        msg.RouteInfo = NetworkManager.Router._table[id];

                        // Check replication settings
                        if ((Plugin.PatchConfig.ContainsKey("OverrideReplicate-" + msg.ObjectId) && Plugin.PatchConfig["OverrideReplicate-" + msg.ObjectId].ToLower() == "true") || (Plugin.PatchConfig.ContainsKey("EnableReplication") && Plugin.PatchConfig["EnableReplication"].ToLower() == "true" && (!Plugin.PatchConfig.ContainsKey("OverrideReplicate-" + msg.ObjectId) || Plugin.PatchConfig["OverrideReplicate-" + msg.ObjectId].ToLower() != "false")))
                        {
                            // Move manually
                            Plugin.uiActions.Add(() =>
                            {
                                if (WorldObjectManager.instance._objects._objectsById.ContainsKey(msg.ObjectId))
                                {
                                    WorldObject obj = WorldObjectManager.instance._objects._objectsById[msg.ObjectId];
                                    try
                                    {
                                        ActorNPCSpawner npcSpawner = GetNpcSpawnerFrom(obj);
                                        if (npcSpawner != null && npcSpawner.ActorBase != null)
                                            npcSpawner.ActorBase.OnMoveMessage(msg);
                                    }
                                    catch
                                    {
                                        // Either unity or il2cpp would have goofed
                                    }
                                    obj.OnMoveMessage(msg);
                                }
                            });
                            return true;
                        }

                        NetworkManager.Router.OnMessage(msg, NetworkManager.Router._queuedMessages);
                        return true;
                    }
                case "mod:ft":
                    {
                        // Feraltweaks packet
                        id = reader.ReadString();
                        switch (id)
                        {
                            case "disconnect":
                                {
                                    // Disconnect

                                    // Read packet
                                    string title = reader.ReadString();
                                    string message = reader.ReadString();
                                    string button = reader.ReadString();

                                    // Disconnect
                                    LoginLogoutPatches.loggingOut = true;
                                    if (NetworkManager.instance._serverConnection.IsConnected)
                                    {
                                        NetworkManager.instance._serverConnection.Disconnect();
                                        if (NetworkManager.instance._chatServiceConnection.IsConnected)
                                            NetworkManager.instance._chatServiceConnection.Disconnect();
                                        NetworkManager.instance._serverConnection = null;
                                        NetworkManager.instance._chatServiceConnection = null;
                                        NetworkManager.instance._jwt = null;
                                    }

                                    // Show window and log out
                                    actions.Add(() =>
                                    {
                                        // Show window
                                        uiActions.Add(() =>
                                        {
                                            if (UI_ProgressScreen.instance.IsVisible)
                                                UI_ProgressScreen.instance.Hide();
                                            UI_Window_OkErrorPopup.OpenWindow(ChartDataManager.instance.localizationChartData.Get(title, title), ChartDataManager.instance.localizationChartData.Get(message, message), ChartDataManager.instance.localizationChartData.Get(button, button));
                                            UI_Window_OkPopupPatch.SingleTimeOkButtonAction = () =>
                                            {
                                                LoginLogoutPatches.loggingOut = false;
                                                LoginLogoutPatches.doLogout = true;
                                                CoreSharedUtils.CoreReset(SplashError.NONE, ErrorCode.None);
                                            };
                                        });

                                        return true;
                                    });
                                    break;
                                }
                            case "notification":
                                {
                                    // Notification

                                    // Read packet
                                    string message = reader.ReadString();
                                    string icon = null;
                                    if (reader.ReadBool())
                                        icon = reader.ReadString();

                                    // Handle packet
                                    uiActions.Add(() =>
                                    {
                                        NotificationManager.instance.AddNotification(new Notification(ChartDataManager.instance.localizationChartData.Get(message, message), icon));
                                    });

                                    break;
                                }
                            case "sysnotification":
                                {
                                    // Notification

                                    // Read packet
                                    string message = reader.ReadString();
                                    string icon = null;
                                    if (reader.ReadBool())
                                        icon = reader.ReadString();

                                    // Handle packet
                                    uiActions.Add(() =>
                                    {
                                        NotificationManager.instance.AddSystemNotification(new Notification(ChartDataManager.instance.localizationChartData.Get(message, message), icon));
                                    });

                                    break;
                                }
                            case "critnotification":
                                {
                                    // Notification

                                    // Read packet
                                    string message = reader.ReadString();
                                    string icon = null;
                                    if (reader.ReadBool())
                                        icon = reader.ReadString();

                                    // Handle packet
                                    uiActions.Add(() =>
                                    {
                                        NotificationManager.instance.AddCriticalNotification(new Notification(ChartDataManager.instance.localizationChartData.Get(message, message), icon));
                                    });

                                    break;
                                }
                            case "gpnotification":
                                {
                                    // Notification

                                    // Read packet
                                    string message = reader.ReadString();
                                    string icon = null;
                                    if (reader.ReadBool())
                                        icon = reader.ReadString();

                                    // Handle packet
                                    uiActions.Add(() =>
                                    {
                                        NotificationManager.instance.AddGameplayNotification(new Notification(ChartDataManager.instance.localizationChartData.Get(message, message), icon));
                                    });

                                    break;
                                }
                            case "errorpopup":
                                {
                                    // Error popup

                                    // Read packet
                                    string title = reader.ReadString();
                                    string message = reader.ReadString();

                                    // Handle packet
                                    uiActions.Add(() =>
                                    {
                                        UI_Window_OkErrorPopup.OpenWindow(ChartDataManager.instance.localizationChartData.Get(title, title), ChartDataManager.instance.localizationChartData.Get(message, message));
                                    });
                                    break;
                                }
                            case "okpopup":
                                {
                                    // Regular popup

                                    // Read packet
                                    string title = reader.ReadString();
                                    string message = reader.ReadString();

                                    // Handle packet
                                    uiActions.Add(() =>
                                    {
                                        UI_Window_OkPopup.OpenWindow(ChartDataManager.instance.localizationChartData.Get(title, title), ChartDataManager.instance.localizationChartData.Get(message, message));
                                    });
                                    break;
                                }
                            case "yesnopopup":
                                {
                                    // Yes/no popup

                                    // Read packet
                                    string popupID = reader.ReadString(); // ID for callbacks
                                    string title = reader.ReadString();
                                    string message = reader.ReadString();
                                    string yesBtn = reader.ReadString();
                                    string noBtn = reader.ReadString();

                                    // Handle packet
                                    uiActions.Add(() =>
                                    {
                                        UI_Window_YesNoPopup.OpenWindow(ChartDataManager.instance.localizationChartData.Get(title, title), ChartDataManager.instance.localizationChartData.Get(message, message),
                                            ChartDataManager.instance.localizationChartData.Get(yesBtn, yesBtn), ChartDataManager.instance.localizationChartData.Get(noBtn, noBtn));
                                        UI_Window_YesNoPopupPatch.SingleTimeNoButtonAction = () =>
                                        {
                                            // Send response
                                            XtWriter wr = new XtWriter(XtCmd.FacilitatorSetBusy);
                                            wr.Cmd = "mod:ft";
                                            wr.WriteString("yesnopopup");
                                            wr.WriteString(popupID);
                                            NetworkManager.instance._serverConnection.Send(wr.WriteBool(false));
                                        };
                                        UI_Window_YesNoPopupPatch.SingleTimeYesButtonAction = () =>
                                        {
                                            // Send response
                                            XtWriter wr = new XtWriter(XtCmd.FacilitatorSetBusy);
                                            wr.Cmd = "mod:ft";
                                            wr.WriteString("yesnopopup");
                                            wr.WriteString(popupID);
                                            NetworkManager.instance._serverConnection.Send(wr.WriteBool(true));
                                        };
                                    });
                                    break;
                                }
                            default:
                                {
                                    FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogError("Unhandled FeralTweaks packet: " + id + ": " + reader);
                                    break;
                                }
                        }
                        return true;
                    }
            }
            return false;
        }

        /// <summary>
        /// Handles chat packets
        /// </summary>
        /// <param name="evt">Packet ID</param>
        /// <param name="packet">Packet payload</param>
        /// <returns>True if handled by feraltweaks, false otherwise</returns>
        public static bool HandleChatPacket(string evt, JsonData packet)
        {
            if (evt.StartsWith("feraltweaks."))
            {
                string id = evt.Substring("feraltweaks.".Length);
                switch (id)
                {
                    case "unreadconversations":
                        {
                            // Add unreads
                            Il2CppSystem.Collections.Generic.List<string> convos = JsonMapper.ToObject<Il2CppSystem.Collections.Generic.List<string>>(JsonMapper.ToJson(packet["conversations"])); 
                            foreach (string convo in convos)
                                ChatManager.instance._unreadConversations.Add(convo);
                            ShowWorldJoinChatUnreadPopup = true;
                            break;
                        }
                    default:
                        {
                            FeralTweaks.FeralTweaksLoader.GetLoadedMod<Plugin>().LogError("Unhandled FeralTweaks chat packet: " + id + ": " + JsonMapper.ToJson(packet));
                            break;
                        }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Retrieves ActorNPCSpawner instances by world object
        /// </summary>
        /// <param name="obj">WorldObject of the NPC</param>
        /// <returns>ActorBase instance or null</returns>
        public static ActorNPCSpawner GetNpcSpawnerFrom(WorldObject obj)
        {
            NetworkedObjectInfo info = obj.gameObject.GetComponent<NetworkedObjectInfo>();
            if (info.actorType == NetworkedObjectInfo.EActorType.npc)
            {
                // NPCS will not move by simply calling OnMoveMessage, we need to get the actual NPC object
                GameObject current = obj.gameObject;
                while (true)
                {
                    ActorNPCSpawner spawner = current.GetComponent<ActorNPCSpawner>();
                    if (spawner != null)
                    {
                        // Found the npc spawner
                        return spawner;
                    }
                    if (current.transform.parent == null)
                    {
                        // No result, fallback to default behaviour
                        break;
                    }
                    current = current.transform.parent.gameObject;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Checks if a dns override is present
        /// </summary>
        /// <param name="host">Host string</param>
        /// <returns>True if a override is present, false otherwise</returns>
        public static bool HasDnsOverride(string host)
        {
            string overrides = PatchConfig.GetValueOrDefault("HttpDnsOverride", "game-assets.fer.al:23.218.218.148").Replace(" ", "");
            foreach (string ov in overrides.Split(","))
            {
                string domain = ov.Remove(ov.IndexOf(":"));
                if (domain == host)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Retrieves host overrides
        /// </summary>
        /// <param name="host">Host string</param>
        /// <returns>IP string</returns>
        public static string GetDnsOverride(string host)
        {
            string overrides = PatchConfig.GetValueOrDefault("HttpDnsOverride", "game-assets.fer.al:23.218.218.148").Replace(" ", "");
            foreach (string ov in overrides.Split(","))
            {
                string domain = ov.Remove(ov.IndexOf(":"));
                string address = ov.Substring(ov.IndexOf(":") + 1);
                if (domain == host)
                    return address;
            }
            throw new ArgumentException("No override for " + host);
        }
    }
}
