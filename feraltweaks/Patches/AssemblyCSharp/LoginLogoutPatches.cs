using HarmonyLib;
using Iss;
using LitJson;
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
        public static bool doLogout = false;
        public static bool errorDisplayed = false;
        public static bool loggingOut = false;
        private static List<Action> actionsToRun = new List<Action>();
        private static LoadingScreenAction loadWaiter;
        private class LoadingScreenAction
        {
            public Action action;
            public bool inited = false;
            public float stamp = 0;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WaitController), "Update")]
        private static void Update(ref WaitController __instance)
        {
            LoadingScreenAction waiter = loadWaiter;
            if (waiter != null && ((UI_ProgressScreen.instance.IsVisible && !UI_ProgressScreen.instance.IsFading) || waiter.inited) && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= waiter.stamp)
            {
                if (!waiter.inited)
                {
                    waiter.stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3000;
                    waiter.inited = true;
                    return;
                }
                loadWaiter = null;
                waiter.action.Invoke();
            }

            if (actionsToRun.Count != 0)
            {
                Action[] actions;
                while (true)
                {
                    try
                    {
                        actions = actionsToRun.ToArray();
                        break;
                    }
                    catch { }
                }
                foreach (Action ac in actions)
                {
                    actionsToRun.Remove(ac);
                    ac.Invoke();
                }
            }

            if (Plugin.uiActions.Count != 0)
            {
                Action[] actions;
                while (true)
                {
                    try
                    {
                        actions = Plugin.uiActions.ToArray();
                        break;
                    }
                    catch { }
                }
                foreach (Action ac in actions)
                {
                    Plugin.uiActions.Remove(ac);
                    ac.Invoke();
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CoreSharedUtils), "CoreReset", new Type[] { })]
        public static bool CoreReset()
        {
            if (NetworkManager.instance == null || NetworkManager.instance._serverConnection == null || !NetworkManager.instance._serverConnection.IsConnected)
                return true;
            if (loggingOut)
                return false;
            CoreReset(SplashError.NONE, ErrorCode.None);
            return false;
        }
         
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CoreSharedUtils), "CoreReset", new Type[] { typeof(SplashError), typeof(ErrorCode) })]
        public static bool CoreReset(SplashError inSplashError, ErrorCode inErrorCode)
        {
            if ((NetworkManager.instance == null || NetworkManager.instance._serverConnection == null || !NetworkManager.instance._serverConnection.IsConnected) && !doLogout)
                return true;
            if (loggingOut)
                return false;
            else if (inSplashError != SplashError.NONE)
                return true;

            loggingOut = true;
            doLogout = false;
            if (CoreManagerBase<CoreNotificationManager>.coreInstance != null)
            {
                CoreManagerBase<CoreNotificationManager>.coreInstance.ClearAndScheduleAllLocalNotifications();
            }
            CoreBundleManager2.UnloadAllLevelAssetBundles();
            UI_ProgressScreen.instance.ClearLabels();
            UI_ProgressScreen.instance.SetSpinnerLabelWithIndex(0, "Logging Out...");
            RoomManager.instance.PreviousLevelDef = ChartDataManager.instance.levelChartData.GetLevelDefWithUnityLevelName("Main_Menu");
            RoomManager.instance.CurrentLevelDef = ChartDataManager.instance.levelChartData.GetLevelDefWithUnityLevelName("CityFera");
            UI_ProgressScreen.instance.UpdateLevel();
            CoreLoadingManager.ShowProgressScreen(null);
            if (Application.wantsToQuit != null)
                return false;
            loadWaiter = new LoadingScreenAction()
            {
                action = () =>
                {
                    RoomManager.instance.CurrentLevelDef = ChartDataManager.instance.levelChartData.GetDef("58").GetComponent<LevelDefComponent>();
                    if (NetworkManager.instance._serverConnection != null && NetworkManager.instance._serverConnection.IsConnected)
                    {
                        NetworkManager.instance._serverConnection.Disconnect();
                        if (NetworkManager.instance._chatServiceConnection.IsConnected)
                            NetworkManager.instance._chatServiceConnection.Disconnect();
                        NetworkManager.instance._serverConnection = null;
                        NetworkManager.instance._chatServiceConnection = null;
                        NetworkManager.instance._jwt = null;
                    }
                    Avatar_Local.instance = null;
                    GlidingManager.instance.MStart();
                    reloadGlidingManager = true;
                    QuestManager.instance._linearQuestListData = null;
                    CoreBundleManager2.UnloadAllLevelAssetBundles();
                    CoreLevelManager.LoadLevelSingle("Loading");
                    UserManager.Me = null;
                    actionsToRun.Add(() =>
                    {
                        UI_Window_Chat chat = GameObject.Find("CanvasRoot").GetComponentInChildren<UI_Window_Chat>(true);
                        if (chat != null)
                            GameObject.Destroy(chat.gameObject);
                        ChatManager.instance._cachedConversations = null;
                        ChatManager.instance._unreadConversations.Clear();
                        CoreWindowManager.CloseAllWindows();
                        CoreWindowManager.OpenWindow<UI_Window_Login>(null, false);
                        Plugin.uiActions.Add(() =>
                        {
                            CoreLoadingManager.HideProgressScreen();
                        });
                    });
                }
            };
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkManager), "Init")]
        public static void InitNetworkManager()
        {
            // Automatic login
            if (Plugin.AutoLoginToken != null)
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
                NetworkManager.autoLoginPassword = Plugin.AutoLoginToken;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NetworkManager), "Init")]
        public static void PostInitNetworkManager()
        {
            // Server environment
            GlobalSettingsManager.instance.currentServerEnvironment = NetworkManager.Environment;
            if (Plugin.DirectorAddress != null)
                NetworkManager.Environment.directorHost = Plugin.DirectorAddress;
            if (Plugin.APIAddress != null)
            {
                NetworkManager.Environment.serviceApiHost = Plugin.APIAddress;
                NetworkManager.Environment.webAPIHost = Plugin.APIAddress;
            }
            if (Plugin.BlueboxHost != null)
                NetworkManager.Environment.blueboxHost = Plugin.BlueboxHost;
            if (Plugin.BlueboxPort != -1)
                NetworkManager.Environment.blueboxPort = Plugin.BlueboxPort;
            if (Plugin.ChatHost != null)
                NetworkManager.Environment.chatHost = Plugin.ChatHost;
            if (Plugin.ChatPort != -1)
                NetworkManager.Environment.chatPort = Plugin.ChatPort;
            if (Plugin.EncryptedGame != -1)
                NetworkManager.Environment.useSecure = Plugin.EncryptedGame == 1;
            if (Plugin.GamePort != -1)
                NetworkManager.Environment.gameServerPort = Plugin.GamePort;
            if (Plugin.VoiceChatHost != null)
                NetworkManager.Environment.voiceChatHost = Plugin.VoiceChatHost;
            if (Plugin.VoiceChatPort != -1)
                NetworkManager.Environment.voiceChatPort = Plugin.VoiceChatPort;
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
        [HarmonyPatch(typeof(UI_Window_Login), "OnOpen")]
        public static void OnOpen(UI_Window_Login __instance)
        {
            RoomManager.instance.PreviousLevelDef = ChartDataManager.instance.levelChartData.GetLevelDefWithUnityLevelName("Main_Menu");
            UI_Window_OkPopupPatch.SingleTimeOkButtonAction = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_Login), "BtnClicked_Login")]
        public static void BtnClicked_Login(UI_Window_Login __instance)
        {
            UI_Window_OkPopupPatch.SingleTimeOkButtonAction = null;
            errorDisplayed = false;
            loggingOut = false;
            doLogout = false;
            long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Plugin.actions.Add(() =>
            {
                if (errorDisplayed)
                {
                    return true;
                }
                if (UI_ProgressScreen.instance.IsVisibleOrFading)
                {
                    Plugin.uiActions.Add(() =>
                    {
                        RoomManager.instance.PreviousLevelDef = ChartDataManager.instance.levelChartData.GetLevelDefWithUnityLevelName("Main_Menu");
                        UI_ProgressScreen.instance.UpdateLevel();
                    });
                    return true;
                }
                return false;
            });
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_ProgressScreen), "Hide")]
        public static void Hide()
        {
            errorDisplayed = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LoginHelper), "DoLogin")]
        public static void DoLogin()
        {
            // Clean first
            Plugin.LoginErrorMessage = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LoginHelper), "GetLoginStatusErrorMessage")]
        public static bool GetLoginStatusErrorMessage(ref string __result)
        {
            errorDisplayed = true;
            if (Plugin.LoginErrorMessage != null)
            {
                // Override error message
                __result = Plugin.LoginErrorMessage;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(IssServerConnection), "ProcessLoginData")]
        public static void ProcessLoginData(JsonData json)
        {
            // Clean first
            Plugin.LoginErrorMessage = null;

            // If present, set error message
            if (json["params"].Contains("errorMessage"))
                Plugin.LoginErrorMessage = json["params"]["errorMessage"].ToString();
            errorDisplayed = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(IssClient), "Login")]
        public static void Login(ref string name)
        {
            // Mention feraltweaks support
            name = name + "%feraltweaks%enabled%" + Plugin.ProtocolVersion.ToString() + "%" + Plugin.Version + "%" + Plugin.PatchConfig.GetValueOrDefault("ServerVersion", "undefined");
        }

    }
}
