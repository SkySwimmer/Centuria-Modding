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
using FeralTweaks;
using FeralTweaks.Networking;

namespace feraltweaks
{
    public class FeralTweaks : FeralTweaksMod
    {
        public static int ProtocolVersion = 2;

        protected override void Define()
        {
        }

        internal static List<Func<bool>> threadActions = new List<Func<bool>>();
        internal static List<Func<bool>> uiRepeatingActions = new List<Func<bool>>();
        internal static List<Action> uiActions = new List<Action>();

        internal static void ScheduleDelayedAction(Func<bool> act)
        {
            lock (threadActions)
                threadActions.Add(act);
        }

        public static void ScheduleDelayedActionForUnity(Func<bool> act)
        {
            lock (uiRepeatingActions)
                uiRepeatingActions.Add(act);
        }

        public static void ScheduleDelayedActionForUnity(Action act)
        {
            lock (uiActions)
                uiActions.Add(act);
        }

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
        public static int EncryptedChat = -1; // -1 = unset, 0 = false, 1 = true
        public static int EncryptedVoiceChat = -1; // -1 = unset, 0 = false, 1 = true

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
                    lock (threadActions)
                        actions = threadActions.ToArray();

                    // Handle actions
                    foreach (Func<bool> ac in actions)
                    {
                        if (ac == null || ac())
                            lock (threadActions)
                                threadActions.Remove(ac);
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
                FeralTweaks.WriteDefaultConfig();
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
            if (PatchConfig.ContainsKey("OverrideProtocolVersion"))
                ProtocolVersion = int.Parse(PatchConfig["OverrideProtocolVersion"]);

            // Load environment
            if (PatchConfig.ContainsKey("ServerEnvironment"))
                LoadServerEnvironment(PatchConfig["ServerEnvironment"]);

            // Start action thread
            StartActionThread();

            // Patch with harmony
            LogInfo("Applying patches...");
            ApplyPatch(typeof(DisplayNameManagerPatches));
            ApplyPatch(typeof(BaseDefPatch));
            ApplyPatch(typeof(CoreChartDataManagerPatch));
            ApplyPatch(typeof(UI_Window_AccountCreationPatch));
            ApplyPatch(typeof(UI_Window_ChangeDisplayNamePatch));
            ApplyPatch(typeof(UI_Window_ResetPasswordPatch));
            ApplyPatch(typeof(UI_Window_TradeItemQuantityPatch));
            ApplyPatch(typeof(UI_Window_OkPopupPatch));
            ApplyPatch(typeof(UI_Window_YesNoPopupPatch));
            ApplyPatch(typeof(WindUpdraftPatch));
            ApplyPatch(typeof(LoginLogoutPatches));
            ApplyPatch(typeof(CoreBundleManager2Patch));
            ApplyPatch(typeof(WorldObjectManagerPatch));
            ApplyPatch(typeof(UI_VersionPatch));
            ApplyPatch(typeof(MessageRouterPatch));
            ApplyPatch(typeof(ChatPatches));
            ApplyPatch(typeof(DOTweenAnimatorPatch));
            ApplyPatch(typeof(GlobalSettingsManagerPatch));
            ApplyPatch(typeof(BundlePatches));
            ApplyPatch(typeof(InitialLoadingPatches));
            ApplyPatch(typeof(DecreePatches));
            ApplyPatch(typeof(InventoryPatches));
            ApplyPatch(typeof(ActionWheelPatches));
            ApplyPatch(typeof(ActorScalingPatch));

            // Scan mods for assets
            LogInfo("Scanning for mod assets...");
            foreach (FeralTweaksMod mod in FeralTweaksLoader.GetLoadedMods())
            {
                if (mod.ModBaseDirectory != null)
                {
                    // Check files
                    if (Directory.Exists(mod.ModBaseDirectory + "/assetbundles"))
                    {
                        LogDebug("Finding assets in mod '" + mod.ID + "'...");

                        // Find files
                        DirectoryInfo dir = new DirectoryInfo(mod.ModBaseDirectory + "/assetbundles");
                        foreach (FileInfo file in dir.GetFiles("*.unity3d", SearchOption.AllDirectories))
                        {
                            // Get path
                            string filePath = Path.GetRelativePath(dir.FullName, file.FullName).Replace(Path.DirectorySeparatorChar, '/');
                            string bundleId = filePath.Replace("/", "_").Remove(filePath.LastIndexOf(".unity3d"));

                            // Log
                            LogInfo("Found asset for '" + bundleId + "', file path: " + file.FullName);
                            BundlePatches.AssetBundlePaths[bundleId] = file.FullName;
                        }
                    }
                }
            }

            // Check command line
            LogInfo("Processing command line arguments...");
            int handoffPort = 0;
            int i = 0;
            if (!Environment.GetEnvironmentVariables().Contains("DOORSTOP_EXECUTABLE_ARGS"))
            {
                foreach (string arg in Environment.GetCommandLineArgs())
                {
                    if (arg == "--launcher-handoff" && i + 1 < Environment.GetCommandLineArgs().Length)
                    {
                        handoffPort = int.Parse(Environment.GetCommandLineArgs()[i + 1]);
                    }
                    i++;
                }
            }
            else
            {
                // MacOS workaround
                string[] args = Environment.GetEnvironmentVariables()["DOORSTOP_EXECUTABLE_ARGS"].ToString().Split(" ");
                foreach (string arg in args)
                {
                    if (arg == "--launcher-handoff" && i + 1 < args.Length)
                    {
                        handoffPort = int.Parse(args[i + 1]);
                    }
                    i++;
                }
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
                                        LoadServerEnvironment(args);
                                        break;
                                    }
                                case "autologin":
                                    {
                                        if (args == "")
                                            LogError("Error: missing argument for autologin: token");
                                        else
                                        {
                                            AutoLoginToken = args;
                                            LogInfo("Enabled autologin.");
                                        }
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
                                                LogInfo("Loaded chart patch: " + file);
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

                                                    LogInfo("Configuration updated: " + key + " = " + value);
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
                LogInfo("Launcher disconnected, launching game...");
            }
            else
                LogInfo("No command line parameters received for launcher handoff, starting regularly...");
        }

        private void LoadServerEnvironment(string args)
        {
            // Parse environment
            string[] payload = args.Split(" ");
            if (payload.Length == 0)
                LogError("Error: missing argument(s) for server environment: [directorhost] [apihost] [chathost] [chatport] [gameport] [voicehost] [voiceport] [blueboxport] [encryptedgame: true/false] [encryptedchat: true/false] [encryptedvoicechat: true/false]");
            try
            {
                if (payload.Length >= 1)
                {
                    DirectorAddress = payload[0];
                    LogInfo("Director: " + DirectorAddress);
                }
                if (payload.Length >= 2)
                {
                    APIAddress = payload[1];
                    LogInfo("API: " + APIAddress);
                }
                if (payload.Length >= 3)
                {
                    ChatHost = payload[2];
                    LogInfo("Chat: " + ChatHost);
                }
                if (payload.Length >= 4)
                {
                    ChatPort = int.Parse(payload[3]);
                    LogInfo("Chat port: " + ChatPort);
                }
                if (payload.Length >= 5)
                {
                    GamePort = int.Parse(payload[4]);
                    LogInfo("Game port: " + GamePort);
                }
                if (payload.Length >= 6)
                {
                    VoiceChatHost = payload[5];
                    LogInfo("Voice chat: " + VoiceChatHost);
                }
                if (payload.Length >= 7)
                {
                    VoiceChatPort = int.Parse(payload[6]);
                    LogInfo("Voice chat port: " + VoiceChatPort);
                }
                if (payload.Length >= 8)
                {
                    BlueboxPort = int.Parse(payload[7]);
                    LogInfo("Bluebox port: " + BlueboxPort);
                }
                if (payload.Length >= 9)
                {
                    EncryptedGame = payload[8].ToLower() == "true" ? 1 : 0;
                    LogInfo("Encryped game server: " + (EncryptedGame == 1));
                }
                if (payload.Length >= 10)
                {
                    EncryptedChat = payload[9].ToLower() == "true" ? 1 : 0;
                    LogInfo("Encryped chat server: " + (EncryptedChat == 1));
                }
                if (payload.Length >= 11)
                {
                    EncryptedVoiceChat = payload[10].ToLower() == "true" ? 1 : 0;
                    LogInfo("Encryped voice chat server: " + (EncryptedVoiceChat == 1));
                }
                if (payload.Length >= 12)
                {
                    PatchConfig["GameAssetsProd"] = payload[11];
                    LogInfo("Game Assets Prod: " + payload[11]);
                }
                if (payload.Length >= 13)
                {
                    PatchConfig["GameAssetsStage"] = payload[12];
                    LogInfo("Game Assets Stage: " + payload[12]);
                }
                if (payload.Length >= 14)
                {
                    PatchConfig["GameAssetsDev"] = payload[13];
                    LogInfo("Game Assets Dev: " + payload[13]);
                }
                if (payload.Length >= 15)
                {
                    PatchConfig["GameAssetsShared"] = payload[14];
                    LogInfo("Game Assets Shared Prod: " + payload[14]);
                }
                if (payload.Length >= 16)
                {
                    PatchConfig["GameAssetsStageShared"] = payload[15];
                    LogInfo("Game Assets Shared Stage: " + payload[15]);
                }
                if (payload.Length >= 17)
                {
                    PatchConfig["GameAssetsDevShared"] = payload[16];
                    LogInfo("Game Assets Shared Dev: " + payload[16]);
                }
            }
            catch
            {
                LogError("Error: invalid server environment arguments, expected: [directorhost] [apihost] [chathost] [chatport] [gameport] [voicehost] [voiceport] [blueboxport] [encryptedgame: true/false] [encryptedchat: true/false]");
            }
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
            File.WriteAllText(FeralTweaksLoader.GetLoadedMod<FeralTweaks>().ConfigDir + "/settings.props", "DisableUpdraftAudioSuppressor=false\nAllowNonEmailUsernames=false\nFlexibleDisplayNames=false\nUserNameRegex=^[\\w%+\\.-]+@(?:[a-zA-Z0-9-]+[\\.{1}])+[a-zA-Z]{2,}$\nDisplayNameRegex=^[0-9A-Za-z\\-_. ]+\nUserNameMaxLength=320\nDisplayNameMaxLength=16\nTradeItemLimit=99\nVersionLabel=${global:7358}\\n${game:version} (${game:build})\nEnableGroupChatTab=false\nJiggleResourceInteractions=false\nCityFeraMovingRocks=false\nCityFeraTeleporterSFX=false\nGameAssetsProd=https://emuferal.ddns.net/feralassets/\nGameAssetsStage=https://emuferal.ddns.net/feralassetsstage/\nGameAssetsDev=https://emuferal.ddns.net/feralassetsdev/\n");
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
                        if ((FeralTweaks.PatchConfig.ContainsKey("OverrideReplicate-" + msg.ObjectId) && FeralTweaks.PatchConfig["OverrideReplicate-" + msg.ObjectId].ToLower() == "true") || (FeralTweaks.PatchConfig.ContainsKey("EnableReplication") && FeralTweaks.PatchConfig["EnableReplication"].ToLower() == "true" && (!FeralTweaks.PatchConfig.ContainsKey("OverrideReplicate-" + msg.ObjectId) || FeralTweaks.PatchConfig["OverrideReplicate-" + msg.ObjectId].ToLower() != "false")))
                        {
                            // Remove manually
                            ScheduleDelayedActionForUnity(() =>
                            {
                                global::FeralTweaks.FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogInfo("Destroying object: " + msg.ObjectId);
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
                        if ((FeralTweaks.PatchConfig.ContainsKey("OverrideReplicate-" + msg.ObjectId) && FeralTweaks.PatchConfig["OverrideReplicate-" + msg.ObjectId].ToLower() == "true") || (FeralTweaks.PatchConfig.ContainsKey("EnableReplication") && FeralTweaks.PatchConfig["EnableReplication"].ToLower() == "true" && (!FeralTweaks.PatchConfig.ContainsKey("OverrideReplicate-" + msg.ObjectId) || FeralTweaks.PatchConfig["OverrideReplicate-" + msg.ObjectId].ToLower() != "false")))
                        {
                            // Move manually
                            ScheduleDelayedActionForUnity(() =>
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
                                        if (KeepAlive.instance != null)
                                        {
                                            KeepAlive.instance._elapsedTime = 0f;
                                            KeepAlive.instance._sendKeepAliveMessageNextSendInterval = false;
                                            KeepAlive.instance._warningSent = false;
                                        }
                                        NetworkManager.instance._serverConnection.Disconnect();
                                        if (NetworkManager.instance._chatServiceConnection.IsConnected)
                                            NetworkManager.instance._chatServiceConnection.Disconnect();
                                        NetworkManager.DisconnectReason = DisconnectReason.Unknown;
                                        NetworkManager.instance._serverConnection = null;
                                        NetworkManager.instance._chatServiceConnection = null;
                                        NetworkManager.instance._jwt = null;
                                    }

                                    // Show window and log out
                                    ScheduleDelayedAction(() =>
                                    {
                                        // Show window
                                        ScheduleDelayedActionForUnity(() =>
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
                                    ScheduleDelayedActionForUnity(() =>
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
                                    ScheduleDelayedActionForUnity(() =>
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
                                    ScheduleDelayedActionForUnity(() =>
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
                                    ScheduleDelayedActionForUnity(() =>
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
                                    ScheduleDelayedActionForUnity(() =>
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
                                    ScheduleDelayedActionForUnity(() =>
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
                                    ScheduleDelayedActionForUnity(() =>
                                    {
                                        try
                                        {
                                            UI_Window_YesNoPopup.CloseWindow();
                                        }
                                        catch { }
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
                            case "displaynameupdate":
                                {
                                    // Display name update

                                    string userID = reader.ReadString();
                                    string displayName = reader.ReadString();
                                    if (UserManager.instance != null)
                                    {
                                        UserInfo i = UserManager.instance._users.GetByUUID(userID);
                                        if (i != null)
                                        {
                                            i.Name = displayName;

                                            // Update avatar
                                            updateAvi(i);
                                        }

                                        // Update self if needed
                                        if (UserManager.instance._me != null && UserManager.instance._me.UUID == userID)
                                        {
                                            UserManager.instance._me.Name = displayName;

                                            // Update avatar
                                            updateAvi(UserManager.instance._me);

                                            // Update HUD
                                            UI_Window_HUD hud = CoreWindowManager.GetWindow<UI_Window_HUD>();
                                            if (hud != null)
                                            {
                                                foreach (UI_PlayerStats bar in hud.gameObject.GetComponentsInChildren<UI_PlayerStats>())
                                                {
                                                    bar.RefreshName();
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                            default:
                                {
                                    FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogError("Unhandled FeralTweaks packet: " + id + ": " + reader);
                                    break;
                                }
                        }
                        return true;
                    }
            }

            // Mod packets
            if (id.StartsWith("mod:"))
            {
                // Find mod
                string mod = id.Substring(4);
                FeralTweaksMod md = FeralTweaksLoader.GetLoadedMod(mod);
                if (md != null && md is IModNetworkHandler)
                {
                    IModNetworkHandler handler = (IModNetworkHandler)md;
                    id = reader.ReadString();
                    if (!handler.GetMessenger().HandlePacket(id, reader))
                        FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogError("Unhandled mod packet: " + id + ": " + reader);
                    return true;
                }
            }

            return false;
        }

        private static void updateAvi(UserInfo info)
        {
            AvatarBase av = info.Avatar;
            if (av != null && av._bubble != null)
            {
                av._bubble._namebarText.text = info.Name;
            }
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
                            if (ChatManager.instance._unreadConversations == null)
                                ChatManager.instance._unreadConversations = new Il2CppSystem.Collections.Generic.List<string>();
                            foreach (string convo in convos)
                                ChatManager.instance._unreadConversations.Add(convo);
                            ShowWorldJoinChatUnreadPopup = true;
                            break;
                        }
                    case "typing":
                        // Update display name
                        lock (ChatPatches.typingStatusDisplayNames)
                        {
                            ChatPatches.typingStatusDisplayNames[(string)packet["uuid"]] = (string)packet["displayName"];
                        }

                        // Typing status
                        lock (ChatPatches.typingStatuses)
                        {
                            if (!ChatPatches.typingStatuses.ContainsKey((string)packet["conversationId"]))
                                ChatPatches.typingStatuses[(string)packet["conversationId"]] = new Dictionary<string, long>();
                            ChatPatches.typingStatuses[(string)packet["conversationId"]][(string)packet["uuid"]] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        break;
                    default:
                        {
                            FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogError("Unhandled FeralTweaks chat packet: " + id + ": " + JsonMapper.ToJson(packet));
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
    }
}
