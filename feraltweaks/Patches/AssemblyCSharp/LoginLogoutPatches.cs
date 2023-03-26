using FeralTweaks;
using FeralTweaks.Mods;
using FeralTweaks.Versioning;
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
        public static bool ignoreUpdateLevel = false;
        public static bool loggingOut = false;
        private static List<Func<bool>> actionsToRun = new List<Func<bool>>();
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
                Func<bool>[] actions;
                while (true)
                {
                    try
                    {
                        actions = actionsToRun.ToArray();
                        break;
                    }
                    catch { }
                }
                foreach (Func<bool> ac in actions)
                {
                    if (ac == null || ac())
                        actionsToRun.Remove(ac);
                }
            }

            if (FeralTweaks.uiActions.Count != 0)
            {
                Action[] actions;
                while (true)
                {
                    try
                    {
                        actions = FeralTweaks.uiActions.ToArray();
                        break;
                    }
                    catch { }
                }
                foreach (Action ac in actions)
                {
                    FeralTweaks.uiActions.Remove(ac);
                    if (ac != null)
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
                        FeralTweaks.uiActions.Add(() =>
                        {
                            CoreLoadingManager.HideProgressScreen();
                        });
                        return true;
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
            }
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
            if (FeralTweaks.EncryptedGame != -1)
                NetworkManager.Environment.useSecure = FeralTweaks.EncryptedGame == 1;
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
        [HarmonyPatch(typeof(UI_Window_Login), "OnOpen")]
        public static void OnOpen(UI_Window_Login __instance)
        {
            RoomManager.instance.PreviousLevelDef = ChartDataManager.instance.levelChartData.GetLevelDefWithUnityLevelName("Main_Menu");
            UI_Window_OkPopupPatch.SingleTimeOkButtonAction = null;
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
            UI_Window_OkPopupPatch.SingleTimeOkButtonAction = null;
            errorDisplayed = false;
            loggingOut = false;
            doLogout = false;
            ignoreUpdateLevel = false;
            long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            actionsToRun.Add(() =>
            {
                if (errorDisplayed)
                    return true;
                if (UI_ProgressScreen.instance.IsVisibleOrFading)
                {
                    RoomManager.instance.PreviousLevelDef = ChartDataManager.instance.levelChartData.GetLevelDefWithUnityLevelName("Main_Menu");
                    UI_ProgressScreen.instance.UpdateLevel();
                    ignoreUpdateLevel = true;
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
            FeralTweaks.LoginErrorMessage = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LoginHelper), "GetLoginStatusErrorMessage")]
        public static bool GetLoginStatusErrorMessage(ref string __result)
        {
            errorDisplayed = true;
            if (FeralTweaks.LoginErrorMessage != null)
            {
                // Override error message
                __result = FeralTweaks.LoginErrorMessage;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(IssServerConnection), "ProcessLoginData")]
        public static void ProcessLoginData(JsonData json)
        {
            // Clean first
            FeralTweaks.LoginErrorMessage = null;

            // If present, set error message
            if (json["params"].Contains("errorMessage"))
                FeralTweaks.LoginErrorMessage = json["params"]["errorMessage"].ToString();

            // If present, log mods
            if (json["params"].Contains("serverMods"))
            {
                // Log
                string logMsg = "";
                var arr = json["params"]["serverMods"];
                var enumerator = json["params"]["serverMods"].System_Collections_IDictionary_GetEnumerator().Cast<Il2CppSystem.Collections.IEnumerator>();
                while (enumerator.MoveNext())
                {
                    Il2CppSystem.Collections.DictionaryEntry v = enumerator.Current.Cast<Il2CppSystem.Collections.DictionaryEntry>();
                    string id = v.Key.ToString();
                    string version = v.Value.ToString();
                    if (logMsg != "")
                        logMsg += ", ";
                    logMsg += id + " (" + version + ")";
                }
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

            // Prevent loading screen from showing
            errorDisplayed = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(IssClient), "Login")]
        public static void Login(ref string name)
        {
            // Mention feraltweaks support
            name = name + "%feraltweaks%enabled%" + FeralTweaks.ProtocolVersion.ToString() + "%" + FeralTweaksLoader.GetLoadedMod<FeralTweaks>().Version + "%" + FeralTweaks.PatchConfig.GetValueOrDefault("ServerVersion", "undefined");

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

    }
}