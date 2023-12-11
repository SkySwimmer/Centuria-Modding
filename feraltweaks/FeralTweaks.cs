using HarmonyLib;
using feraltweaks.Patches.AssemblyCSharp;
using feraltweaks.Patches.Bundles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using FeralTweaks.Mods;
using FeralTweaks;
using FeralTweaks.Actions;
using FeralTweaks.BundleInjection;

namespace feraltweaks
{
    public class FeralTweaks : FeralTweaksMod
    {
        // Protocol
        public static int ProtocolVersion = 3;

        protected override void Define()
        {
        }

        // Patches
        public static Dictionary<string, string> ChartPatches = new Dictionary<string, string>();
        public static Dictionary<string, string> PatchConfig = new Dictionary<string, string>();

        // Autologin
        public static bool IsAutoLogin = false;
        internal static string AutoLoginToken = null;
        internal static string AutoLoginUsername = null;
        internal static string AutoLoginPassword = null;

        // Configuration
        public static string DirectorAddress = null;
        public static string APIAddress = null;
        public static string ChatHost = null;
        public static string VoiceChatHost = null;
        public static int GamePort = -1;
        public static int VoiceChatPort = -1;
        public static int ChatPort = -1;
        public static int BlueboxPort = -1;
        public static bool VanillaEncryptionMode;
        public static int EncryptedGame = -1; // -1 = unset, 0 = false, 1 = true
        public static int EncryptedChat = -1; // -1 = unset, 0 = false, 1 = true
        public static int EncryptedVoiceChat = -1;

        public override void Init()
        {
            // Load config
            LoadConfig();

            // Start action thread
            FeralTweaksActionManager.StartActionThread();

            // Patch with harmony
            LogInfo("Applying patches...");
            ApplyPatches();

            // Hook bundle patches
            HookBundlePatches();

            // Attach quitting
            UnityEngine.Application.quitting += new Action(() => { 
                // Clean and exit
                LoginLogoutPatches.CleanConnection();
            });

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
                            string filePath = Path.GetRelativePath(dir.FullName, file.FullName).Replace(Path.DirectorySeparatorChar, '/').ToLower();
                            string bundleId = filePath.Replace("/", "_").Remove(filePath.LastIndexOf(".unity3d"));

                            // Log
                            LogInfo("Found asset for '" + bundleId + "', file path: " + file.FullName);
                            BundlePatches.AssetBundlePaths[bundleId] = file.FullName;
                            BundlePatches.AssetBundleRelativePaths[bundleId] = filePath.Remove(filePath.LastIndexOf(".unity3d"));
                        }
                    }
                }
            }

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
                else if (arg == "--login-token" && i + 1 < Environment.GetCommandLineArgs().Length)
                {
                    IsAutoLogin = true;
                    AutoLoginToken = Environment.GetCommandLineArgs()[i + 1];
                    AutoLoginUsername = null;
                    AutoLoginPassword = null;
                }
                else if (arg == "--login-username" && i + 1 < Environment.GetCommandLineArgs().Length)
                {
                    IsAutoLogin = true;
                    AutoLoginUsername = Environment.GetCommandLineArgs()[i + 1];
                    AutoLoginToken = null;
                }
                else if (arg == "--login-password" && i + 1 < Environment.GetCommandLineArgs().Length)
                {
                    IsAutoLogin = true;
                    AutoLoginPassword = Environment.GetCommandLineArgs()[i + 1];
                    AutoLoginToken = null;
                }
                i++;
            }
            if (handoffPort != 0)
            {
                // Handle handoff
                HandleHandoff(handoffPort);
            }
            else
                LogInfo("No command line parameters received for launcher handoff, starting regularly...");
        }

        private void HookBundlePatches()
        {
            // Bundle patches
            BundleHook.RegisterBundleHook(new AnimationEventsBundleHook());
        }

        private void ApplyPatches()
        {
            // Patches
            ApplyPatch(typeof(ActionWheelPatches));
            ApplyPatch(typeof(ChartPatches));
            ApplyPatch(typeof(InventoryPatches));
            ApplyPatch(typeof(UI_Window_AccountCreationPatch));
            ApplyPatch(typeof(UI_Window_ChangeDisplayNamePatch));
            ApplyPatch(typeof(UI_Window_ResetPasswordPatch));
            ApplyPatch(typeof(TradeLimitPatches));
            ApplyPatch(typeof(WindUpdraftPatch));
            ApplyPatch(typeof(LoginLogoutPatches));
            ApplyPatch(typeof(AssetBundleManagerPatches));
            ApplyPatch(typeof(WorldObjectManagerPatch));
            ApplyPatch(typeof(VersionLabelPatch));
            ApplyPatch(typeof(ServerMessageHandlingPatches));
            ApplyPatch(typeof(ChatPatches));
            ApplyPatch(typeof(DOTweenAnimatorPatch));
            ApplyPatch(typeof(AssetEndpointPatches));
            ApplyPatch(typeof(BundlePatches));
            ApplyPatch(typeof(InitialLoadScreenFadein));
            ApplyPatch(typeof(DecreePatches));
            ApplyPatch(typeof(GlidingManagerPatch));
            ApplyPatch(typeof(PlayerJumpIncreasePatch));
            ApplyPatch(typeof(DecalResolutionPatch));
            ApplyPatch(typeof(EyeBlinkingPatch));
            ApplyPatch(typeof(MultiClothingPerAttachPatch));
            ApplyPatch(typeof(PlayerLoginLogoutAnimsPatch));
            ApplyPatch(typeof(ActorScalingPatch));
            ApplyPatch(typeof(DragonSparkSkeletonPatch));
            ApplyPatch(typeof(NpcHeadRotationPatch));
            ApplyPatch(typeof(PlayerJoinNotifPatch));
            ApplyPatch(typeof(NotificationPatches));
        }

        public static void ApplyPatch(Type type)
        {
            FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogInfo("Applying patch: " + type.FullName);
            Harmony.CreateAndPatchAll(type);
        }

        /// <summary>
        /// Writes the default configuration
        /// </summary>
        public static void WriteDefaultConfig()
        {
            File.WriteAllText(FeralTweaksLoader.GetLoadedMod<FeralTweaks>().ConfigDir + "/settings.props",
                  "DecalResolutionLow=512\n"
                + "DecalResolutionMid=1024\n"
                + "DecalResolutionHigh=2048\n"
                + "\n"
                + "JumpIncreaseFactor=1.2\n"
                + "ActorScaleMultiplier=1\n"
                + "ActorScaleMultiplierLowerMost=0.5\n"
                + "DisableUpdraftAudioSuppressor=true\n"
                + "\n"
                + "AllowNonEmailUsernames=false\n"
                + "FlexibleDisplayNames=false\n"
                + "UserNameRegex=^[\\w%+\\.-]+@(?:[a-zA-Z0-9-]+[\\.{1}])+[a-zA-Z]{2,}$\n"
                + "DisplayNameRegex=^[0-9A-Za-z\\-_. ]+\n"
                + "UserNameMaxLength=320\n"
                + "DisplayNameMaxLength=16\n"
                + "\n"
                + "TradeItemLimit=99\n"
                + "AllowMultipleClothingItemsOfSameType=false\n"
                + "\n"
                + "EnableGroupChatTab=true\n"
                + "VersionLabel=${global:7358}\\n${game:version} (${game:build})\n"
                + "DefaultAvatarActionOrder=[8930, 9108, 9116, 9121, 9122, 9143, 9151, 9190]\n"
                + "\n"
                + "CityFeraMovingRocks=true\n"
                + "CityFeraTeleporterSFX=true\n"
                + "JiggleResourceInteractions=true\n"
                + "\n"
                + "GlidingTurnSpeed=0.1\n"
                + "GlidingGravity=3\n"
                + "GlidingRollAmount=12\n"
                + "GlidingSpeedMultiplier=2\n"
                + "GlidingAllowFlap=true\n"
                + "GlidingFlapCooldown=200\n"
                + "AllowDragonGlidingWithNoWings=true\n"
                + "AllowShinigamiGlidingWithNoWings=true\n"
                + "\n"
                + "GameAssetsProd=https://emuferal.ddns.net/feralassets/\n"
                + "GameAssetsStage=https://emuferal.ddns.net/feralassetsstage/\n"
                + "GameAssetsDev=https://emuferal.ddns.net/feralassetsdev/\n");
        }

        // Configuration parsing
        private void LoadConfig()
        {
            // Load config
            LogInfo("Loading configuration...");
            Directory.CreateDirectory(ConfigDir);
            if (!File.Exists(ConfigDir + "/settings.props"))
            {
                LogInfo("Writing defaults...");
                WriteDefaultConfig();
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

            // Raed internal fields
            if (PatchConfig.ContainsKey("OverrideProtocolVersion"))
                ProtocolVersion = int.Parse(PatchConfig["OverrideProtocolVersion"]);
            if (PatchConfig.ContainsKey("AutoLoginUsername"))
            {
                AutoLoginUsername = PatchConfig["AutoLoginUsername"];
                IsAutoLogin = true;
            }
            if (PatchConfig.ContainsKey("AutoLoginPassword"))
            {
                IsAutoLogin = true;
                AutoLoginPassword = PatchConfig["AutoLoginPassword"];
            }
            if (PatchConfig.ContainsKey("AutoLoginToken"))
            {
                IsAutoLogin = true;
                AutoLoginPassword = PatchConfig["AutoLoginToken"];
            }
            if (PatchConfig.ContainsKey("VanillaEncryptionMode"))
                VanillaEncryptionMode = PatchConfig["VanillaEncryptionMode"].ToLower() == "true";
            PatchConfig.Remove("AutoLoginToken");
            PatchConfig.Remove("AutoLoginUsername");
            PatchConfig.Remove("AutoLoginPassword");

            // Load environment
            if (PatchConfig.ContainsKey("ServerEnvironment"))
                LoadServerEnvironment(PatchConfig["ServerEnvironment"]);
        }

        // Server environment parsing
        private void LoadServerEnvironment(string args)
        {
            // Parse environment
            string[] payload = args.Split(" ");
            if (payload.Length == 0)
                LogError("Error: missing argument(s) for server environment: [directorhost] [apihost] [chathost] [chatport] [gameport] [voicehost] [voiceport] [blueboxport] [encryptedgame: true/false] [encryptedchat: true/false]");
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
                if (payload.Length >= 9)
                {
                    EncryptedChat = payload[9].ToLower() == "true" ? 1 : 0;
                    LogInfo("Encryped chat server: " + (EncryptedChat == 1));
                }
                if (payload.Length >= 10)
                {
                    EncryptedVoiceChat = payload[10].ToLower() == "true" ? 1 : 0;
                    LogInfo("Encryped voice chat server: " + (EncryptedVoiceChat == 1));
                }
            }
            catch
            {
                LogError("Error: invalid server environment arguments, expected: [directorhost] [apihost] [chathost] [chatport] [gameport] [voicehost] [voiceport] [blueboxport] [encryptedgame: true/false] [encryptedchat: true/false]");
            }
        }

        // Launcher handoff
        private void HandleHandoff(int handoffPort)
        {
            // Handle handoff
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
                                        IsAutoLogin = true;
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
                                            ChartPatches[patch] = file;
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
    }
}