using FeralTweaks;
using FeralTweaks.Actions;
using FeralTweaks.Mods;
using FeralTweaks.Versioning;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppSystem.IO;
using Iss;
using LitJson;
using NodeCanvas.Tasks.Actions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WW.Waiters;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class LoginLogoutPatches
    {
        internal static string LoginErrorMessage = null;
        private static bool waitingUserInputQuit = false;
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WWTcpClient), "Connect")]
        public static void Connect(WWTcpClient __instance)
        {
            if (__instance.ToString() == "Iss.IssClient")
            {
                // Override encryption if needed
                if (FeralTweaks.EncryptedGame != -1)
                {
                    __instance._isSecured = FeralTweaks.EncryptedGame == 1;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RoomManager), "OnRoomJoinSuccessResponse")]
        public static void OnRoomJoinSuccessResponse()
        {
            // Check initial world join
            if (pendingInitialWorldJoin)
            {
                // Initial world join!
                pendingInitialWorldJoin = false;
                initialWorldJoin = true;
            }
            else
                initialWorldJoin = false;
        }

        public static string serverSoftwareName = "fer.al";
        public static string serverSoftwareVersion = "unknown";
        public static Dictionary<string, string> serverMods = new Dictionary<string, string>();

        public static bool doLogout = false;
        public static bool errorDisplayed = false;
        public static bool ignoreUpdateLevel = false;
        public static bool loggingOut = false;
        public static bool initialWorldJoin = false;
        public static bool pendingInitialWorldJoin = false;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WaitController), "Update")]
        private static void Update(ref WaitController __instance)
        {
            // Call update
            FeralTweaksActionManager.CallUpdate();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CoreSharedUtils), "CoreReset", new Type[] { })]
        public static bool CoreReset()
        {
            if (NetworkManager.instance == null || NetworkManager.instance._serverConnection == null || !NetworkManager.instance._serverConnection.IsConnected)
            {
                // Reset patches
                ChatPatches.ChatPostInit = false;
                ChatPatches.ChatHandshakeDone = false;
                ChatPatches.ChatInitializing = false;
                lock (ChatPatches.typingStatusDisplayNames)
                {
                    ChatPatches.typingStatusDisplayNames.Clear();
                }
                lock (ChatPatches.typingStatuses)
                {
                    ChatPatches.typingStatuses.Clear();
                }
                return true;
            }
            if (loggingOut)
                return false;
            CoreReset(SplashError.NONE, ErrorCode.None);
            return false;
        }

        public static bool WantsToQuit;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Application), "Quit", new Type[] {})]
        public static bool QuitApp()
        {
            // Check window and bundle manager and progress screen
            if (WindowManager.instance == null || CoreBundleManager.coreInstance == null || CoreBundleManager2.coreInstance == null || !CoreBundleManager.coreInstance.Inited || !CoreBundleManager2.coreInstance.Inited || UI_ProgressScreen.instance == null || UI_ProgressScreen.instance.IsVisible || Avatar_Local.instance == null)
            {
                // Clean and exit
                CleanConnection();
                return true;
            }

            // Check quitting
            if (WantsToQuit)
            {
                // Clean and exit
                CleanConnection();
                return true;
            }
            if (waitingUserInputQuit)
            {
                // Smoothly log out
                QuitAppSmooth();
                return false;
            }

            // Smooth quit
            QuitAppSmooth();
            return false;
        }

        public static void CleanConnection()
        {
            if (NetworkManager.instance != null && NetworkManager.instance._serverConnection != null && NetworkManager.instance._serverConnection.IsConnected)
            {
                NetworkManager.instance._serverConnection.Disconnect();
                if (NetworkManager.instance._chatServiceConnection != null && NetworkManager.instance._chatServiceConnection.IsConnected)
                    NetworkManager.instance._chatServiceConnection.Disconnect();
                if (NetworkManager.instance._voiceChatServiceConnection != null && NetworkManager.instance._voiceChatServiceConnection.IsConnected)
                    NetworkManager.instance._voiceChatServiceConnection.Disconnect();
                NetworkManager.instance._serverConnection = null;
                NetworkManager.instance._chatServiceConnection = null;
                NetworkManager.instance._voiceChatServiceConnection = null;
                NetworkManager.instance._jwt = null;
                NetworkManager.instance._uuid = null;
            }
        }

        public static void QuitAppSmooth()
        {
            // Check ingame
            if (NetworkManager.instance == null || NetworkManager.instance._serverConnection == null || !NetworkManager.instance._serverConnection.IsConnected)
            {
                WantsToQuit = true;
                Application.Quit();
                return;
            }

            // Check override
            if (WantsToQuit)
            {
                Application.Quit();
                return;
            }
            WantsToQuit = true;

            // Close settings
            try
            {
                UI_Window_Settings.CloseWindow();
            }
            catch { }

            // Hide reset
            if (UI_Reset.instance != null)
                UI_Reset.instance.Hide();

            // Logout
            Logout("Exiting...");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CoreSharedUtils), "CoreReset", new Type[] { typeof(SplashError), typeof(ErrorCode) })]
        public static bool CoreReset(SplashError inSplashError, ErrorCode inErrorCode)
        {
            if ((NetworkManager.instance == null || NetworkManager.instance._serverConnection == null || !NetworkManager.instance._serverConnection.IsConnected) && !doLogout)
                return HandleReset(inSplashError, inErrorCode);
            if (loggingOut)
                return false;
            else if (inSplashError != SplashError.NONE)
                return HandleReset(inSplashError, inErrorCode);

            loggingOut = true;
            doLogout = false;
            Logout("Logging Out...");
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_Reset), "ResetInCallback")]
        public static void ResetInCallback(UI_Reset __instance)
        {
            if (__instance._resetErrorCode != null && __instance.tmpLabelMessage != null && __instance._resetErrorCode._internalErrorText != null)
            {
                // Add description
                __instance.tmpLabelMessage.text = __instance.tmpLabelMessage.text + "\nDescription: " + __instance._resetErrorCode._internalErrorText;
            }
        }

        private static bool HandleReset(SplashError inSplashError, ErrorCode inErrorCode)
        {
            // Check code
            switch (inErrorCode.Code)
            {

                case 9:
                    // Server connection lost
                    LogoutWithError("Connection lost", "Connection to the server was lost!\nYou have been logged out", inErrorCode);
                    return false;

                case 28:
                    // Server connection lost
                    LogoutWithError("Connection lost", "You were gone for too long and were disconnected!\nYou have been logged out", inErrorCode);
                    return false;

                case 1008:
                    // API error
                    LogoutWithError("Connection lost", "Connection to the server was lost!\nYou have been logged out", inErrorCode);
                    return false;

                default:
                    // Allow reset
                    WantsToQuit = true;

                    // Disconnect
                    if (NetworkManager.instance != null)
                    {
                        if (NetworkManager.instance._serverConnection != null && NetworkManager.instance._serverConnection.IsConnected)
                            NetworkManager.instance._serverConnection.Disconnect();
                        if (NetworkManager.instance._chatServiceConnection != null && NetworkManager.instance._chatServiceConnection.IsConnected)
                            NetworkManager.instance._chatServiceConnection.Disconnect();
                        if (NetworkManager.instance._voiceChatServiceConnection != null && NetworkManager.instance._voiceChatServiceConnection.IsConnected)
                            NetworkManager.instance._voiceChatServiceConnection.Disconnect();
                        NetworkManager.instance._serverConnection = null;
                        NetworkManager.instance._chatServiceConnection = null;
                        NetworkManager.instance._voiceChatServiceConnection = null;
                        NetworkManager.instance._jwt = null;
                        NetworkManager.instance._uuid = null;
                    }

                    // Return
                    return true;

            }
        }

        private static void LogoutWithError(string title, string errorMessage, ErrorCode inErrorCode)
        {
            // Schedule error
            FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
            { 
                // Wait for loading screen
                if (Avatar_Local.instance != null && !UI_ProgressScreen.instance.IsVisibleOrFading)
                    return false;

                // Schedule error
                FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                {
                    if (UI_ProgressScreen.instance.IsVisibleOrFading)
                        return false;
                    FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                    {
                        // Show popup
                        UI_Window_OkPopup.CloseWindow();
                        UI_Window_OkErrorPopup.QueueWindow(title, "<size=80%>\n" + errorMessage + "\n\nError code: " + inErrorCode.Code + "-" + inErrorCode.Subcode + (inErrorCode._internalErrorText != null ? "\nDescription: " + inErrorCode.InternalErrorText : "") + "</size>", "");
                    });
                    return true;
                });

                // Return
                return true;
            });

            // Logout
            Logout(null);
        }

        private static void Logout(string msg)
        {
            if (WindowManager.GetWindow<UI_Window_Login>() != null && WindowManager.GetWindow<UI_Window_Login>().IsOpen && !WindowManager.GetWindow<UI_Window_Login>().IsOpening)
            {
                RoomManager.instance.CurrentLevelDef = ChartDataManager.instance.levelChartData.GetDef("58").GetComponent<LevelDefComponent>();
                if (NetworkManager.instance._serverConnection != null && NetworkManager.instance._serverConnection.IsConnected)
                {
                    NetworkManager.instance._serverConnection.Disconnect();
                    if (NetworkManager.instance._chatServiceConnection != null && NetworkManager.instance._chatServiceConnection.IsConnected)
                        NetworkManager.instance._chatServiceConnection.Disconnect();
                    if (NetworkManager.instance._voiceChatServiceConnection != null && NetworkManager.instance._voiceChatServiceConnection.IsConnected)
                        NetworkManager.instance._voiceChatServiceConnection.Disconnect();
                    NetworkManager.instance._serverConnection = null;
                    NetworkManager.instance._chatServiceConnection = null;
                    NetworkManager.instance._voiceChatServiceConnection = null;
                    NetworkManager.instance._jwt = null;
                    NetworkManager.instance._uuid = null;
                }
                Avatar_Local.instance = null;
                XPManager.instance.PlayerLevel = null;
                NotificationManager.instance.loggedNotifications.Clear();
                GlidingManager.instance.MStart();
                reloadGlidingManager = true;
                QuestManager.instance._linearQuestListData = null;
                UserManager.Me = null;
                UserManager.instance._users.ClearUsersByUUID();
                serverSoftwareName = "fer.al";
                serverSoftwareVersion = "unknown";
                serverMods.Clear();
                UI_Window_Chat chat = GameObject.Find("CanvasRoot").GetComponentInChildren<UI_Window_Chat>(true);
                if (chat != null)
                {
                    chat.SaveWindowSize();
                    GameObject.Destroy(chat.gameObject);
                }
                UI_Window_VoiceChat vchat = GameObject.Find("CanvasRoot").GetComponentInChildren<UI_Window_VoiceChat>(true);
                if (vchat != null)
                    GameObject.Destroy(vchat.gameObject);
                ChatManager.instance._cachedConversations = null;
                ChatManager.instance._unreadConversations.Clear();
                ChatPatches.unreadMessagesPerConversation.Clear();
                ChatPatches.ChatPostInit = false;
                ChatPatches.ChatHandshakeDone = false;
                ChatPatches.ChatInitializing = false;
                lock (ChatPatches.typingStatusDisplayNames)
                {
                    ChatPatches.typingStatusDisplayNames.Clear();
                }
                lock (ChatPatches.typingStatuses)
                {
                    ChatPatches.typingStatuses.Clear();
                }
                if (FeralVivoxManager.instance._vivoxEnabled.GetDecrypted())
                {
                    // Log out of rooms, channels, etc
                    FeralVivoxManager.instance.LeaveVoiceChatGroup();
                }
                return;
            }

            // Check quick-logout
            if (!IsSafeToQuickLogout)
            {
                // Quick logout unavailable
                CoreLoadingManager.coreInstance.StartCoroutine(CoreLoadingManager.coreInstance.LoadLevel("58", new Il2CppSystem.Collections.Generic.List<string>()));
                return;
            }

            // Check if avatar can be transitioned
            Avatar_Local avatar = Avatar_Local.instance;
            if (avatar != null && !UI_ProgressScreen.instance.IsVisibleOrFading)
            {
                // Clear action
                avatar._nextActionType = ActorActionType.None;
                avatar._nextActionBreakLoop = true;

                // Play sound
                FeralAudioInfo audioInfo = new FeralAudioInfo();
                audioInfo.eventRef = "event:/cutscenes/boundary_camera_fade_out";
                FeralAudioBehaviour behaviour = avatar.gameObject.GetComponent<FeralAudioBehaviour>();
                if (behaviour != null)
                    behaviour.Play(audioInfo, null, Il2CppType.Of<Il2CppSystem.Nullable<float>>().GetConstructor(new Il2CppSystem.Type[] { Il2CppType.Of<float>() }).Invoke(new Il2CppSystem.Object[] { Il2CppSystem.Single.Parse("0") }).Cast<Il2CppSystem.Nullable<float>>());

                // Teleport away
                GCR.instance.StartCoroutine(avatar.TransitionDeparture(true, true, "teleport"));
                FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                {
                    // Wait for transition
                    if (!avatar.IsTransitionDeparting)
                        return false;

                    // Run
                    FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                    {
                        // Wait for transition
                        if (avatar.IsTransitionDeparting)
                            return false;

                        // Log out
                        Avatar_Local.instance = null;
                        Logout(msg);

                        // Return
                        return true;
                    });

                    // Return
                    return true;
                });
                return;
            }

            // Logout
            CoreWindowManager.CloseAllWindows();
            if (CoreManagerBase<CoreNotificationManager>.coreInstance != null)
                CoreManagerBase<CoreNotificationManager>.coreInstance.ClearAndScheduleAllLocalNotifications();
            CoreBundleManager2.UnloadAllLevelAssetBundles();
            UI_ProgressScreen.instance.ClearLabels();
            if (msg != null)
                UI_ProgressScreen.instance.SetSpinnerLabelWithIndex(0, msg);
            CoreLoadingManager.ShowProgressScreen(null);
            RoomManager.instance.PreviousLevelDef = ChartDataManager.instance.levelChartData.GetDef("58").GetComponent<LevelDefComponent>();
            RoomManager.instance.CurrentLevelDef = ChartDataManager.instance.levelChartData.GetDef("820").GetComponent<LevelDefComponent>();
            UI_ProgressScreen.instance.UpdateLevel();

            // Schedule
            bool faded = false;
            long start2 = 0;
            long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
            {
                // Check fade
                if (UI_ProgressScreen.instance.IsFading || !UI_ProgressScreen.instance.IsVisible || DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start < 1000)
                    return false;

                // Check camera fade
                if (!faded)
                {
                    // Fade camera in if needed
                    if (CameraFader.current != null)
                    {
                        // Fade out and wait
                        CameraFader.current.FadeIn(1f);
                        start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        faded = true;
                        return false;
                    }
                    faded = true;
                }

                // Wait for a few extra seconds
                if (start2 == 0)
                    start2 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start2 < 3000)
                    return false;

                // Quit if needed
                if (WantsToQuit)
                    Application.Quit();

                // Mark as on loading screen
                RoomManager.instance.CurrentLevelDef = ChartDataManager.instance.levelChartData.GetDef("58").GetComponent<LevelDefComponent>();
                if (NetworkManager.instance._serverConnection != null && NetworkManager.instance._serverConnection.IsConnected)
                {
                    NetworkManager.instance._serverConnection.Disconnect();
                    if (NetworkManager.instance._chatServiceConnection != null && NetworkManager.instance._chatServiceConnection.IsConnected)
                        NetworkManager.instance._chatServiceConnection.Disconnect();
                    if (NetworkManager.instance._voiceChatServiceConnection != null && NetworkManager.instance._voiceChatServiceConnection.IsConnected)
                        NetworkManager.instance._voiceChatServiceConnection.Disconnect();
                    NetworkManager.instance._serverConnection = null;
                    NetworkManager.instance._chatServiceConnection = null;
                    NetworkManager.instance._voiceChatServiceConnection = null;
                    NetworkManager.instance._jwt = null;
                    NetworkManager.instance._uuid = null;
                }
                Avatar_Local.instance = null;
                XPManager.instance.PlayerLevel = null;
                NotificationManager.instance.loggedNotifications.Clear();
                GlidingManager.instance.MStart();
                reloadGlidingManager = true;
                QuestManager.instance._linearQuestListData = null;
                CoreBundleManager2.UnloadAllLevelAssetBundles();
                CoreLevelManager.LoadLevelSingle("Loading");
                UserManager.Me = null;
                UserManager.instance._users.ClearUsersByUUID();
                serverSoftwareName = "fer.al";
                serverSoftwareVersion = "unknown";
                serverMods.Clear();
                FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                {
                    UI_Window_Chat chat = GameObject.Find("CanvasRoot").GetComponentInChildren<UI_Window_Chat>(true);
                    if (chat != null)
                    {
                        chat.SaveWindowSize();
                        GameObject.Destroy(chat.gameObject);
                    }
                    UI_Window_VoiceChat vchat = GameObject.Find("CanvasRoot").GetComponentInChildren<UI_Window_VoiceChat>(true);
                    if (vchat != null)
                        GameObject.Destroy(vchat.gameObject);
                    ChatManager.instance._cachedConversations = null;
                    ChatManager.instance._unreadConversations.Clear();
                    ChatPatches.unreadMessagesPerConversation.Clear();
                    ChatPatches.ChatPostInit = false;
                    ChatPatches.ChatHandshakeDone = false;
                    ChatPatches.ChatInitializing = false;
                    lock (ChatPatches.typingStatusDisplayNames)
                    {
                        ChatPatches.typingStatusDisplayNames.Clear();
                    }
                    lock (ChatPatches.typingStatuses)
                    {
                        ChatPatches.typingStatuses.Clear();
                    }
                    if (FeralVivoxManager.instance._vivoxEnabled.GetDecrypted())
                    {
                        // Log out of rooms, channels, etc
                        FeralVivoxManager.instance.LeaveVoiceChatGroup();
                    }
                    CoreWindowManager.CloseAllWindows();
                    CoreWindowManager.OpenWindow<UI_Window_Login>(null, false);
                    FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                    {
                        if (WindowManager.GetWindow<UI_Window_Login>() != null && WindowManager.GetWindow<UI_Window_Login>().IsOpen && !WindowManager.GetWindow<UI_Window_Login>().IsOpening)
                        {
                            CoreLoadingManager.HideProgressScreen();
                            return true;
                        }
                        return false;
                    });
                    return true;
                });

                // Return
                return true;
            });
        }

        private static bool IsAutologin = false;
        private static bool IsSafeToQuickLogout = false;
        private static LoginData LoginResultData;
        private static LoginStatus LoginResultStatus;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkManager), "OnServerConnectionLost")]
        public static bool OnServerConnectionLost()
        {
            // Clear mod info
            serverSoftwareName = "fer.al";
            serverSoftwareVersion = "unknown";
            serverMods.Clear();

            // Check autologin
            if (IsAutologin)
            {
                // Show error and head to title screen
                IsAutologin = false;
                if (LoginResultData != null && LoginResultStatus != LoginStatus.Success)
                {
                    // Show window when loading closes
                    UI_Window_OkPopup.CloseWindow();
                    FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                    {
                        if (UI_ProgressScreen.instance.IsVisibleOrFading)
                            return false;
                        FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                        {
                            // Show popup
                            UI_Window_OkErrorPopup.QueueWindow("Login Failed!", LoginHelper.GetLoginStatusErrorMessage(LoginResultStatus), "");
                        });
                        return true;
                    });

                    // Load title screen
                    CoreLoadingManager.coreInstance.StartCoroutine(CoreLoadingManager.coreInstance.LoadLevel("58", new Il2CppSystem.Collections.Generic.List<string>()));

                    // Prevent normal disconnect logic
                    return false;
                }
            }

            // Allow normal logic
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkManager), "Init")]
        public static void InitNetworkManager()
        {
            // Automatic login
            if (FeralTweaks.AutoLoginToken != null)
            {
                // TODO: we should improve this its so ugly but the client code is hard to navigate
                //
                // for now i dont mind a code-based user to log in with token on the server, it cannot do much
                // but its a really ugly method for autologin
                //
                // however i dont want to supply a username+password via the launcher handoff as thats just unsafe
                // so this will have to do for now
                NetworkManager.AutoLogin = NetworkManager.AutoLoginState.DoAutoLogin;
                NetworkManager.autoLoginEmailUsername = "sys://fromtoken";
                NetworkManager.autoLoginPassword = FeralTweaks.AutoLoginToken;
                FeralTweaks.AutoLoginToken = null;
                FeralTweaks.IsAutoLogin = false;
                IsAutologin = true;
            }
            if (FeralTweaks.AutoLoginUsername != null && FeralTweaks.AutoLoginPassword != null)
            {
                NetworkManager.AutoLogin = NetworkManager.AutoLoginState.DoAutoLogin;
                NetworkManager.autoLoginEmailUsername = FeralTweaks.AutoLoginUsername;
                NetworkManager.autoLoginPassword = FeralTweaks.AutoLoginPassword;
                FeralTweaks.AutoLoginUsername = null;
                FeralTweaks.AutoLoginPassword = null;
                FeralTweaks.IsAutoLogin = false;
                IsAutologin = true;
            }

            // Reset
            serverSoftwareName = "fer.al";
            serverSoftwareVersion = "unknown";
            serverMods.Clear();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NetworkManager), "Init")]
        public static void PostInitNetworkManager()
        {
            // Server environment
            GlobalSettingsManager.instance.currentServerEnvironment = NetworkManager.Environment;
            if (FeralTweaks.DirectorAddress != null)
                NetworkManager.Environment.directorHost = FeralTweaks.DirectorAddress;
            if (FeralTweaks.APIAddress != null)
            {
                NetworkManager.Environment.serviceApiHost = FeralTweaks.APIAddress;
                NetworkManager.Environment.webAPIHost = FeralTweaks.APIAddress;
            }
            if (FeralTweaks.BlueboxPort != -1)
                NetworkManager.Environment.blueboxPort = FeralTweaks.BlueboxPort;
            if (FeralTweaks.ChatHost != null)
                NetworkManager.Environment.chatHost = FeralTweaks.ChatHost;
            if (FeralTweaks.ChatPort != -1)
                NetworkManager.Environment.chatPort = FeralTweaks.ChatPort;
            if (!FeralTweaks.VanillaEncryptionMode)
                NetworkManager.Environment.useSecure = false;
            if (FeralTweaks.GamePort != -1)
                NetworkManager.Environment.gameServerPort = FeralTweaks.GamePort;
            if (FeralTweaks.VoiceChatHost != null)
                NetworkManager.Environment.voiceChatHost = FeralTweaks.VoiceChatHost;
            if (FeralTweaks.VoiceChatPort != -1)
                NetworkManager.Environment.voiceChatPort = FeralTweaks.VoiceChatPort;
        }

        public static bool reloadGlidingManager = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldXtHandler), "RequestReady")]
        public static void RequestReady()
        {
            if (reloadGlidingManager)
            {
                GlidingManager.instance.MStart();
                reloadGlidingManager = false;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PersistentServiceConnection), "Init")]
        public static void Init(ref PersistentServiceConnection __instance, ref bool isSecured)
        {
            if (__instance.ToString() == "ChatServiceConnection")
            {
                // Override encryption if needed
                if (FeralTweaks.EncryptedChat != -1)
                {
                    isSecured = FeralTweaks.EncryptedChat == 1;
                }
            }
            else if (__instance.ToString() == "VoiceChatServiceConnection")
            {
                // Override encryption if needed
                if (FeralTweaks.EncryptedVoiceChat != -1)
                {
                    isSecured = FeralTweaks.EncryptedVoiceChat == 1;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_Login), "OnOpen")]
        public static void OnOpen(UI_Window_Login __instance)
        {
            IsSafeToQuickLogout = true;
            RoomManager.instance.PreviousLevelDef = ChartDataManager.instance.levelChartData.GetDef("58").GetComponent<LevelDefComponent>();

            // Reset
            if (NetworkManager.instance._serverConnection != null && NetworkManager.instance._serverConnection.IsConnected)
                NetworkManager.instance._serverConnection.Disconnect();
            if (NetworkManager.instance._chatServiceConnection != null && NetworkManager.instance._chatServiceConnection.IsConnected)
                NetworkManager.instance._chatServiceConnection.Disconnect();
            if (NetworkManager.instance._voiceChatServiceConnection != null && NetworkManager.instance._voiceChatServiceConnection.IsConnected)
                NetworkManager.instance._voiceChatServiceConnection.Disconnect();
            NetworkManager.instance._serverConnection = null;
            NetworkManager.instance._chatServiceConnection = null;
            NetworkManager.instance._voiceChatServiceConnection = null;
            NetworkManager.instance._jwt = null;
            NetworkManager.instance._uuid = null;
            Avatar_Local.instance = null;
            XPManager.instance.PlayerLevel = null;
            NotificationManager.instance.loggedNotifications.Clear();
            GlidingManager.instance.MStart();
            reloadGlidingManager = true;
            QuestManager.instance._linearQuestListData = null;
            CoreBundleManager2.UnloadAllLevelAssetBundles();
            CoreLevelManager.LoadLevelSingle("Loading");
            UserManager.Me = null;
            UserManager.instance._users.ClearUsersByUUID();
            serverSoftwareName = "fer.al";
            serverSoftwareVersion = "unknown";
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_ProgressScreen), "UpdateLevel")]
        public static bool UpdateLevel()
        {
            if (ignoreUpdateLevel)
            {
                ignoreUpdateLevel = false;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_Login), "BtnClicked_Login")]
        public static void BtnClicked_Login(UI_Window_Login __instance)
        {
            serverSoftwareName = "fer.al";
            serverSoftwareVersion = "unknown";
            serverMods.Clear();
            errorDisplayed = false;
            loggingOut = false;
            doLogout = false;
            ignoreUpdateLevel = false;
            FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
            {
                if (errorDisplayed)
                    return true;
                if (UI_ProgressScreen.instance.IsVisibleOrFading)
                {
                    RoomManager.instance.PreviousLevelDef = ChartDataManager.instance.levelChartData.GetDef("58").GetComponent<LevelDefComponent>();
                    UI_ProgressScreen.instance.UpdateLevel();
                    ignoreUpdateLevel = true;
                    return true;
                }
                return false;
            });
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_ProgressScreen), "Hide")]
        public static void Hide()
        {
            errorDisplayed = true;

            // Check inital join
            if (initialWorldJoin)
            {
                // This is the initial join
                initialWorldJoin = false;

                // Check if avatar can be transitioned
                Avatar_Local avatar = Avatar_Local.instance;
                if (avatar != null)
                {
                    // Clear action
                    avatar._nextActionType = ActorActionType.None;
                    avatar._nextActionBreakLoop = true;

                    // Play sound
                    FeralAudioInfo audioInfo = new FeralAudioInfo();
                    audioInfo.eventRef = "event:/cutscenes/boundary_camera_fade_in";
                    FeralAudioBehaviour behaviour = avatar.gameObject.GetComponent<FeralAudioBehaviour>();
                    if (behaviour != null)
                        behaviour.Play(audioInfo, null, Il2CppType.Of<Il2CppSystem.Nullable<float>>().GetConstructor(new Il2CppSystem.Type[] { Il2CppType.Of<float>() }).Invoke(new Il2CppSystem.Object[] { Il2CppSystem.Single.Parse("0") }).Cast<Il2CppSystem.Nullable<float>>());

                    // Teleport in
                    GCR.instance.StartCoroutine(avatar.TransitionArrival(false, false, "teleport"));
                    FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                    {
                        // Wait for transition
                        if (!avatar.IsTransitionArriving)
                            return false;

                        // Run
                        FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                        {
                            // Wait for transition
                            if (avatar.IsTransitionArriving)
                                return false;

                            // Return
                            return true;
                        });

                        // Return
                        return true;
                    });
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LoginHelper), "DoLogin")]
        public static void DoLogin()
        {
            // Clean first
            LoginErrorMessage = null;
            serverSoftwareName = "fer.al";
            serverSoftwareVersion = "unknown";
            serverMods.Clear();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LoginHelper), "GetLoginStatusErrorMessage")]
        public static bool GetLoginStatusErrorMessage(ref string __result)
        {
            errorDisplayed = true;
            if (LoginErrorMessage != null)
            {
                // Override error message
                __result = LoginErrorMessage;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(IssClient), "Login")]
        public static bool Login(ref string name, string zone, string pass, IssClient __instance)
        {
            LoginErrorMessage = null;

            // Mention feraltweaks support
            LoginResultData = null;
            name = name + "%feraltweaks%enabled%" + FeralTweaks.ProtocolVersion.ToString() + "%" + FeralTweaksLoader.GetLoadedMod<FeralTweaks>().Version + "%" + FeralTweaks.PatchConfig.GetValueOrDefault("ServerVersion", "undefined");

            if (!FeralTweaks.PatchConfig.ContainsKey("DebugOldServer") || FeralTweaks.PatchConfig["DebugOldServer"].ToLower() != "true")
            {
                // Length
                name = name + "%" + FeralTweaksLoader.GetLoadedMods().Length.ToString();
                foreach (FeralTweaksMod mod in FeralTweaksLoader.GetLoadedMods())
                {
                    // ID
                    name = name + "%" + mod.ID;

                    // Version
                    name = name + "%" + mod.Version;

                    //
                    // Handshake rules
                    //
                    // int - length
                    // --
                    // id
                    // version check string
                    // --
                    if (mod is IModVersionHandler)
                    {
                        Dictionary<string, string> rules = ((IModVersionHandler)mod).GetServerModVersionRules();
                        name = name + "%" + rules.Count;
                        foreach ((string key, string val) in rules)
                        {
                            name = name + "%" + key;
                            name = name + "%" + val;
                        }
                    }
                    else
                        name = name + "%0";
                }
                name = name + "%end";
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(IssServerConnection), "ProcessLoginData")]
        public static void ProcessLoginData(JsonData json)
        {
            // Find error
            LoginResultData = JsonUtility.FromJson<LoginData>(JsonMapper.ToJson(json["params"]));
            LoginResultStatus = (LoginStatus)((int)json["statusId"]);
            if (LoginResultStatus == LoginStatus.Success)
            {
                IsAutologin = false;
                IsSafeToQuickLogout = true;
                pendingInitialWorldJoin = true;
            }
            else
            {
                // Prevent loading screen from showing
                errorDisplayed = true;
            }

            // If present, set error message
            if (json["params"].Contains("errorMessage"))
                LoginErrorMessage = json["params"]["errorMessage"].ToString();

            // If present, assign server details
            serverSoftwareName = "fer.al";
            serverSoftwareVersion = "unknown";
            serverMods.Clear();
            if (json["params"].Contains("serverSoftwareName"))
                serverSoftwareName = json["params"]["serverSoftwareName"].ToString();
            if (json["params"].Contains("serverSoftwareVersion"))
                serverSoftwareVersion = json["params"]["serverSoftwareVersion"].ToString();

            // Log
            FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogInfo("Connected to a '" + serverSoftwareName + "' server, server version: " + serverSoftwareVersion);

            // If present, log mods
            if (json["params"].Contains("serverMods"))
            {
                // Log
                string logMsg = "";
                var arr = json["params"]["serverMods"];
                var enumerator = json["params"]["serverMods"].System_Collections_IDictionary_GetEnumerator().Cast<Il2CppSystem.Collections.IEnumerator>();
                Dictionary<string, string> mods = new Dictionary<string, string>();
                while (enumerator.MoveNext())
                {
                    Il2CppSystem.Collections.DictionaryEntry v = enumerator.Current.Cast<Il2CppSystem.Collections.DictionaryEntry>();
                    string id = v.Key.ToString();
                    string version = v.Value.ToString();
                    if (logMsg != "")
                        logMsg += ", ";
                    logMsg += id + " (" + version + ")";
                    mods[id] = version;
                }
                serverMods = mods;
                FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogInfo("Server has " + arr.Count + " server mod" + (arr.Count == 1 ? "" : "s") + " installed. [" + logMsg + "]");
            }

            // Log error for mods if present
            if (json["params"].Contains("incompatibleServerMods"))
            {
                // Error
                FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogError("Login failed due to " + json["params"]["incompatibleServerModCount"].ToString() + " incompatible SERVER mod" + (json["params"]["incompatibleServerModCount"].ToString() == "1" ? "" : "s") + " [" + json["params"]["incompatibleServerMods"].ToString() + "]");
            }
            if (json["params"].Contains("incompatibleClientMods"))
            {
                // Error
                FeralTweaksLoader.GetLoadedMod<FeralTweaks>().LogError("Login failed due to " + json["params"]["incompatibleClientModCount"].ToString() + " incompatible CLIENT mod" + (json["params"]["incompatibleClientModCount"].ToString() == "1" ? "" : "s") + " [" + json["params"]["incompatibleClientMods"].ToString() + "]");
            }
        }
    }
}