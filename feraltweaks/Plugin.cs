using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.Logging;
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

namespace feraltweaks
{
    [BepInProcess("Fer.al.exe")]
    [BepInProcess("Feral.exe")]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        public static List<Func<bool>> actions = new List<Func<bool>>();
        public static List<Action> uiActions = new List<Action>();
        public static Dictionary<string, string> Patches = new Dictionary<string, string>();
        public static Dictionary<string, string> PatchConfig = new Dictionary<string, string>();
        public static string AutoLoginToken = null;

        public static string DirectorAddress = null;
        public static string APIAddress = null;
        public static string ChatHost = null;
        public static string VoiceChatHost = null;
        public static string BlueboxHost = null;
        public static int GamePort = -1;
        public static int VoiceChatPort = -1;
        public static int ChatPort = -1;
        public static int BlueboxPort = -1;
        public static int EncryptedGame = -1; // -1 = unset, 0 = false, 1 = true

        // Error message container for when login fails and the server includes a feraltweaks message field in the response
        public static string LoginErrorMessage = null;

        public static ManualLogSource logger;

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

        public override void Load()
        {
            logger = Log;

            // Load config
            logger.LogInfo("Loading configuration...");
            Directory.CreateDirectory(Paths.ConfigPath + "/feraltweaks");
            if (!File.Exists(Paths.ConfigPath + "/feraltweaks/settings.props"))
            {
                logger.LogInfo("Writing defaults...");
                Plugin.WriteDefaultConfig();
            }
            else
            {
                logger.LogInfo("Processing data...");
                foreach (string line in File.ReadAllLines(Paths.ConfigPath + "/feraltweaks/settings.props"))
                {
                    if (line == "" || line.StartsWith("#") || !line.Contains("="))
                        continue;
                    string key = line.Remove(line.IndexOf("="));
                    string value = line.Substring(line.IndexOf("=") + 1);
                    PatchConfig[key] = value;
                }
            }
            logger.LogInfo("Configuration loaded.");

            // Start action thread
            StartActionThread();

            // Patch with harmony
            Log.LogInfo("Applying patches...");
            Harmony.CreateAndPatchAll(typeof(BaseDefPatch));
            Harmony.CreateAndPatchAll(typeof(CoreChartDataManagerPatch));
            Harmony.CreateAndPatchAll(typeof(UI_Window_AccountCreationPatch));
            Harmony.CreateAndPatchAll(typeof(UI_Window_ChangeDisplayNamePatch));
            Harmony.CreateAndPatchAll(typeof(UI_Window_ResetPasswordPatch));
            Harmony.CreateAndPatchAll(typeof(UI_Window_TradeItemQuantityPatch));
            Harmony.CreateAndPatchAll(typeof(UI_Window_OkPopupPatch));
            Harmony.CreateAndPatchAll(typeof(UI_Window_YesNoPopupPatch));
            Harmony.CreateAndPatchAll(typeof(WWTcpClientPatch));
            Harmony.CreateAndPatchAll(typeof(WindUpdraftPatch));
            Harmony.CreateAndPatchAll(typeof(LoginLogoutPatches));
            Harmony.CreateAndPatchAll(typeof(CoreBundleManager2Patch));
            Harmony.CreateAndPatchAll(typeof(WorldObjectManagerPatch));
            Harmony.CreateAndPatchAll(typeof(MessageRouterPatch));
            Harmony.CreateAndPatchAll(typeof(UI_VersionPatch));

            // Handle command line
            Log.LogInfo("Waiting for commands from launcher...");
            Random rnd = new Random();
            int port = 0;
            TcpListener listener;
            while (true)
            {
                port = rnd.Next(10000, short.MaxValue);
                try
                {
                    listener = new TcpListener(IPAddress.Loopback, port);
                    listener.Start();
                    break;
                }
                catch
                {
                }
            }

            bool done = false;
            bool wait = false;
            Log.LogInfo("Waiting for commands on port " + port + "...");
            File.WriteAllText("launcherhandoff." + Process.GetCurrentProcess().Id + ".port", port.ToString());
            Task.Run(() => {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    wait = true;

                    // Read commands
                    StreamReader rd = new StreamReader(client.GetStream());
                    Log.LogInfo("Launcher connected, processing...");
                    try
                    {
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
                                            Log.LogError("Error: missing argument(s) for serverenvironment: [directorhost] [apihost] [chathost] [chatport] [gameport] [voicehost] [voiceport] [blueboxhost] [blueboxport] [encryptedgame: true/false]");
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
                                            BlueboxHost = payload[7];
                                        if (payload.Length >= 9)
                                            BlueboxPort = int.Parse(payload[8]);
                                        if (payload.Length >= 10)
                                            EncryptedGame = payload[9].ToLower() == "true" ? 1 : 0;
                                        break;
                                    }
                                case "autologin":
                                    {
                                        if (args == "")
                                            Log.LogError("Error: missing argument for autologin: token");
                                        else
                                            AutoLoginToken = args;
                                        break;
                                    }
                                case "chartpatch":
                                    {
                                        if (args == "")
                                            Log.LogError("Error: missing argument for patchchart: patch-data");
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
                                                Log.LogError("Error: invalid patch data for patchchart");
                                            }
                                        }
                                        break;
                                    }
                                case "config":
                                    {
                                        if (args == "")
                                            Log.LogError("Error: missing argument for config: configuration-data");
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
                                                Log.LogError("Error: invalid configuration data for config");
                                            }
                                        }
                                        break;
                                    }
                            }
                        }
                    }
                    catch { }
                    client.Close();

                    done = true;
                }
                catch
                {
                }
            });
            for (int i = 0; i < 10 && !done && !wait; i++)
                Thread.Sleep(1000);
            while (wait && !done)
                Thread.Sleep(100);
            listener.Stop();
            if (!done)
                Log.LogInfo("Timed out while waiting for commands, starting regularly.");
            File.Delete("launcherhandoff." + Process.GetCurrentProcess().Id + ".port");
        }

        /// <summary>
        /// Writes the default configuration
        /// </summary>
        public static void WriteDefaultConfig()
        {
            File.WriteAllText(Paths.ConfigPath + "/feraltweaks/settings.props", "DisableUpdraftAudioSuppressor=false\nAllowNonEmailUsernames=false\nFlexibleDisplayNames=false\nUserNameRegex=^[\\w%+\\.-]+@(?:[a-zA-Z0-9-]+[\\.{1}])+[a-zA-Z]{2,}$\nDisplayNameRegex=^[0-9A-Za-z\\-_. ]+\nUserNameMaxLength=320\nDisplayNameMaxLength=16\nTradeItemLimit=99\nVersionLabel=${global:7358}\\n${game:version} (${game:build})\n");
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
                        if ((Plugin.PatchConfig.ContainsKey("OverrideReplicate-" + msg.ObjectId) && Plugin.PatchConfig["OverrideReplicate-" + msg.ObjectId] == "True") || (Plugin.PatchConfig.ContainsKey("EnableReplication") && Plugin.PatchConfig["EnableReplication"] == "True" && (!Plugin.PatchConfig.ContainsKey("OverrideReplicate-" + msg.ObjectId) || Plugin.PatchConfig["OverrideReplicate-" + msg.ObjectId] != "False")))
                        {
                            // Remove manually
                            Plugin.uiActions.Add(() =>
                            {
                                Plugin.logger.LogInfo("Destroying object: " + msg.ObjectId);
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
                        if ((Plugin.PatchConfig.ContainsKey("OverrideReplicate-" + msg.ObjectId) && Plugin.PatchConfig["OverrideReplicate-" + msg.ObjectId] == "True") || (Plugin.PatchConfig.ContainsKey("EnableReplication") && Plugin.PatchConfig["EnableReplication"] == "True" && (!Plugin.PatchConfig.ContainsKey("OverrideReplicate-" + msg.ObjectId) || Plugin.PatchConfig["OverrideReplicate-" + msg.ObjectId] != "False")))
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
                                    logger.LogError("Unhandled FeralTweaks packet: " + id + ": " + reader);
                                    break;
                                }
                        }
                        return true;
                    }
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
