using CodeStage.AntiCheat.ObscuredTypes;
using FeralTweaks;
using FeralTweaks.Actions;
using FeralTweaks.Formatters;
using FeralTweaks.Mods;
using HarmonyLib;
using LitJson;
using Newtonsoft.Json;
using Server;
using Services.Chat;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using WW.Waiters;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public static class ChatPatches
    {
        internal static bool ShowWorldJoinChatUnreadPopup;
        internal static bool ChatPostInit;
        internal static bool ChatHandshakeDone;

        private static bool patched;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CoreChartDataManager), "SetChartObjectInstances")]
        public static void SetChartObjectInstances()
        {
            if (patched)
                return;
            patched = true;

            // Add patches
            Harmony.CreateAndPatchAll(typeof(VivoxPatchesLate));
        }

        public static class VivoxPatchesLate
        {

            [HarmonyPrefix]
            [HarmonyPatch(typeof(FeralVivoxManager), "OnLoginComplete")]
            public static void OnLoginComplete(FeralVivoxManager __instance)
            {
                // Check patch config
                if (FeralTweaks.PatchConfig.ContainsKey("VivoxProdServer")
                    && FeralTweaks.PatchConfig.ContainsKey("VivoxProdDomain")
                    && FeralTweaks.PatchConfig.ContainsKey("VivoxProdTokenIssuer")
                    && FeralTweaks.PatchConfig.ContainsKey("VivoxProdTokenKey"))
                {
                    // Replace prod config
                    __instance._vivoxProduction.server.SetEncrypted(FeralTweaks.PatchConfig["VivoxProdServer"]);
                    __instance._vivoxProduction.domain.SetEncrypted(FeralTweaks.PatchConfig["VivoxProdDomain"]);
                    __instance._vivoxProduction.tokenIssuer.SetEncrypted(FeralTweaks.PatchConfig["VivoxProdTokenIssuer"]);
                    __instance._vivoxProduction.tokenKey.SetEncrypted(FeralTweaks.PatchConfig["VivoxProdTokenKey"]);
                }
                if (FeralTweaks.PatchConfig.ContainsKey("VivoxDevServer")
                    && FeralTweaks.PatchConfig.ContainsKey("VivoxDevDomain")
                    && FeralTweaks.PatchConfig.ContainsKey("VivoxDevTokenIssuer")
                    && FeralTweaks.PatchConfig.ContainsKey("VivoxDevTokenKey"))
                {
                    // Replace dev config
                    __instance._vivoxDevelopment.server.SetEncrypted(FeralTweaks.PatchConfig["VivoxDevServer"]);
                    __instance._vivoxDevelopment.domain.SetEncrypted(FeralTweaks.PatchConfig["VivoxDevDomain"]);
                    __instance._vivoxDevelopment.tokenIssuer.SetEncrypted(FeralTweaks.PatchConfig["VivoxDevTokenIssuer"]);
                    __instance._vivoxDevelopment.tokenKey.SetEncrypted(FeralTweaks.PatchConfig["VivoxDevTokenKey"]);
                }
            }

        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(UI_LazyItemList<ChatConversationData>), nameof(UI_LazyItemList<ChatConversationData>.Setup))]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void SetupDummy(UI_LazyItemList_ChatConversation instance) { }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_LazyItemList_ChatConversation), "OnConversationAdded")]
        public static bool OnConversationAdded(ref UI_LazyItemList_ChatConversation __instance, CachedConversationAddedMessage inMessage)
        {
            if (FeralTweaks.PatchConfig.GetValueOrDefault("EnableGroupChatTab", "false").ToLower() == "true")
            {
                // Check type
                if (!inMessage.Conversation.IsRoomChat)
                {
                    if (inMessage.Conversation.participants.Count > 0 && inMessage.Conversation.participants[0].StartsWith("plaintext:[GC] "))
                    {
                        // GC
                        if (__instance.gameObject.transform.parent.gameObject.name != "Panel_GC")
                            return false;
                    }
                    else
                    {
                        // DM
                        if (__instance.gameObject.transform.parent.gameObject.name == "Panel_GC")
                            return false;
                    }
                }
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_LazyItemList_ChatConversation), "OnConversationRemoved")]
        public static bool OnConversationRemoved(ref UI_LazyItemList_ChatConversation __instance, CachedConversationRemovedMessage inMessage)
        {
            if (FeralTweaks.PatchConfig.GetValueOrDefault("EnableGroupChatTab", "false").ToLower() == "true")
            {
                // Check type
                if (!inMessage.Conversation.IsRoomChat)
                {
                    if (inMessage.Conversation.participants.Count > 0 && inMessage.Conversation.participants[0].StartsWith("plaintext:[GC] "))
                    {
                        // GC
                        if (__instance.gameObject.transform.parent.gameObject.name != "Panel_GC")
                            return false;
                    }
                    else
                    {
                        // DM
                        if (__instance.gameObject.transform.parent.gameObject.name == "Panel_GC")
                            return false;
                    }
                }
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_LazyItemList_ChatConversation), "Setup")]
        public static bool SetupConvoList(ref UI_LazyItemList_ChatConversation __instance)
        {
            if (FeralTweaks.PatchConfig.GetValueOrDefault("EnableGroupChatTab", "false").ToLower() == "true")
            {
                // Filter it
                Il2CppSystem.Collections.Generic.List<ChatConversationData> convos = new Il2CppSystem.Collections.Generic.List<ChatConversationData>();
                foreach (ChatConversationData convo in ChatManager.instance._cachedConversations)
                {
                    if (!convo.IsRoomChat)
                    {
                        if (convo.participants.Count > 0 && convo.participants[0].StartsWith("plaintext:[GC] "))
                        {
                            // GC
                            if (__instance.gameObject.transform.parent.gameObject.name == "Panel_GC")
                                convos.Add(convo);
                        }
                        else
                        {
                            // DM
                            if (__instance.gameObject.transform.parent.gameObject.name != "Panel_GC")
                                convos.Add(convo);
                        }
                    }
                    else
                        convos.Add(convo);
                }

                __instance._dataItems = convos;
                SetupDummy(__instance);
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_Window_Chat), "OnOpen")]
        public static void OnOpen(ref UI_Window_Chat __instance)
        {
            if (FeralTweaks.PatchConfig.GetValueOrDefault("EnableGroupChatTab", "false").ToLower() == "true")
            {
                // Enable GC tab button
                __instance._tabGroup._tabs[2].button.gameObject.SetActive(true);
                __instance._tabGroup._tabs[2].button.interactable = true;
                __instance._tabGroup._tabs[2].button._textElements[0].gameObject.SetActive(true);
                if (GetChild(__instance._conversationsPanel.gameObject.transform.parent.gameObject, "Panel_GC") != null)
                    return;

                // Create GC tab
                GameObject tab = GameObject.Instantiate(__instance._conversationsPanel.gameObject);
                tab.name = "Panel_GC";
                tab.transform.parent = __instance._conversationsPanel.gameObject.transform.parent;
                __instance._tabGroup._tabs[2].gameObject = tab;
                RectTransform trans = tab.GetComponent<RectTransform>();
                trans.anchorMin = __instance._conversationsPanel.gameObject.GetComponent<RectTransform>().anchorMin;
                trans.anchorMax = __instance._conversationsPanel.gameObject.GetComponent<RectTransform>().anchorMax;
                trans.anchoredPosition = __instance._conversationsPanel.gameObject.GetComponent<RectTransform>().anchoredPosition;
                trans.anchoredPosition3D = __instance._conversationsPanel.gameObject.GetComponent<RectTransform>().anchoredPosition3D;
                trans.offsetMax = __instance._conversationsPanel.gameObject.GetComponent<RectTransform>().offsetMax;
                trans.offsetMin = __instance._conversationsPanel.gameObject.GetComponent<RectTransform>().offsetMin;
                trans.pivot = __instance._conversationsPanel.gameObject.GetComponent<RectTransform>().pivot;
                trans.sizeDelta = __instance._conversationsPanel.gameObject.GetComponent<RectTransform>().sizeDelta;
                trans.localScale = __instance._conversationsPanel.gameObject.GetComponent<RectTransform>().localScale;

                // Bind panel
                UI_ChatPanel_Conversations cont = tab.GetComponent<UI_ChatPanel_Conversations>();
                cont._closeConversationButtonGroup = GetChild(tab, "ChatPanel_Private/Group_Participant/Button_Back").GetComponent<CanvasGroup>();
                cont._conversationItemList = GetChild(tab, "ConversationList").GetComponent<UI_LazyItemList_ChatConversation>();
                cont._conversationListGroup = GetChild(tab, "ConversationList").GetComponent<CanvasGroup>();
                cont._messageListGroup = GetChild(tab, "ChatPanel_Private").GetComponent<CanvasGroup>();
                cont._privateChatPanel = GetChild(tab, "ChatPanel_Private").GetComponent<UI_ChatPanel_Private>();

                // Swap out the back button handler
                FeralButton btn = GetChild(tab, "ChatPanel_Private/Group_Participant/Button_Back").GetComponent<FeralButton>();
                btn.onClick.m_PersistentCalls.m_Calls.Add(new UnityEngine.Events.PersistentCall()
                {
                    m_Target = cont,
                    m_MethodName = "BtnClicked_CloseConversation"
                });
                btn.onClick.m_PersistentCalls.m_Calls.RemoveAt(1);

                // Set inactive
                tab.SetActive(false);

                // Set public chat as active
                __instance._inputField.gameObject.transform.parent.parent.gameObject.SetActive(true);
                __instance._publicChatPanel.gameObject.SetActive(true);

                // Add notification popup
                GameObject lblContainer = __instance._tabGroup._tabs[2].button._targetTransform.gameObject;
                GameObject lblOriginalContainer = __instance._tabGroup._tabs[1].button._targetTransform.gameObject;
                GameObject lbl = GameObject.Instantiate(GetChild(lblOriginalContainer, "NotificationCount"));
                lbl.transform.parent = lblContainer.transform;
                lbl.name = "NotificationCount_GC";
                lbl.transform.localScale = new Vector3(1, 1, 1);

                // Load it
                lbl.GetComponent<UI_UnreadConversationCount>().Start();

                // Find emoji panel and move it
                GameObject emojiPanel = GetChild(__instance._conversationsPanel.transform.parent.gameObject, "Panel_Emoji");
                emojiPanel.transform.SetSiblingIndex(tab.gameObject.transform.GetSiblingIndex() + 1);

                // Add handler to send button for gcs
                FeralButton sendBtn = GetChild(__instance._inputField.transform.parent.gameObject.transform.parent.gameObject, "Button_Send").GetComponent<FeralButton>();
                sendBtn.onClick.m_PersistentCalls.m_Calls.Add(new UnityEngine.Events.PersistentCall()
                {
                    m_Target = cont._privateChatPanel,
                    m_MethodName = "BtnClicked_SubmitChat"
                });
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_UnreadConversationCount), "RefreshText")]
        public static void RefreshText(ref UI_UnreadConversationCount __instance, ref int inUnreadCount)
        {
            GameObject obj = __instance.gameObject.transform.parent.parent.gameObject;
            if (obj.name == "Button_Chat")
                return; // Main UI
            if (FeralTweaks.PatchConfig.GetValueOrDefault("EnableGroupChatTab", "false").ToLower() == "true")
            {
                // Filter it
                inUnreadCount = 0;
                if (ChatManager.instance._cachedConversations != null)
                {
                    foreach (ChatConversationData convo in ChatManager.instance._cachedConversations)
                    {
                        if (!convo.IsRoomChat && ChatManager.instance._unreadConversations.Contains(convo.id))
                        {
                            if (convo.participants.Count > 0 && convo.participants[0].StartsWith("plaintext:[GC] "))
                            {
                                // GC
                                if (__instance.gameObject.name.EndsWith("_GC"))
                                    inUnreadCount++;
                            }
                            else
                            {
                                // DM
                                if (!__instance.gameObject.name.EndsWith("_GC"))
                                    inUnreadCount++;
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChatEntry), "GetTimeStampUIString")]
        public static void GetTimeStampUIString(ref ChatEntry __instance, ref string __result)
        {
            // Return timestamp
            Il2CppSystem.DateTime local = __instance.localTimeStamp.Add(Il2CppSystem.TimeZone.CurrentTimeZone.GetUtcOffset(Il2CppSystem.DateTime.Now.Date));
            Il2CppSystem.TimeSpan span = Il2CppSystem.DateTime.Now - local;

            // Check day count
            if (span.Days >= 7)
            {
                // Too long ago
                __result = "7+ days";
            }
            else if (span.Days >= 1)
            {
                // Few days ago
                __result = span.Days + "d ago";
            }
            else if (span.Hours >= 1)
            {
                // Few hours ago
                __result = span.Hours + "h ago";
            }
            else if (span.Minutes >= 1)
            {
                // Only a few minutes
                __result = span.Minutes + "m ago";
            }
            else
            {
                // Just now
                __result = "Just now";
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_LazyListItem_ChatConversation), "RefreshLastChatEntry")]
        public static void RefreshLastChatEntry(ref UI_LazyListItem_ChatConversation __instance, ChatEntry inChatEntry)
        {
            if (inChatEntry == null)
                return;
            GameObject marker = GetChild(__instance._lastChatTimeText.gameObject.transform.parent.gameObject, "UI_UnreadIndicator");
            if (ChatManager.instance._unreadConversations != null && ChatManager.instance.UnreadConversations.Contains(inChatEntry.conversationId))
            {
                // Enable unread marker
                marker.SetActive(true);
            }
            else
            {
                // Disable unread marker
                marker.SetActive(false);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_LazyListItem_ChatConversation), "RefreshReadState")]
        public static void RefreshReadState(ref UI_LazyListItem_ChatConversation __instance, bool inIsRead)
        {
            GameObject marker = GetChild(__instance._lastChatTimeText.gameObject.transform.parent.gameObject, "UI_UnreadIndicator");
            if (!inIsRead)
            {
                // Enable unread marker
                marker.SetActive(true);
            }
            else
            {
                // Disable unread marker
                marker.SetActive(false);
            }
        }

        private static bool ChatInitializing = false;
        public static bool ChatReadyForConnFixer = false;
        private static bool ChatFixerBusy = false;
        private static bool ChatLoadingScreenWantedToHide = false;

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
        [HarmonyPatch(typeof(PersistentServiceConnection), "OnConnectionLost")]
        public static void OnDisconnect(ref PersistentServiceConnection __instance)
        {
            if (__instance.TryCast<ChatServiceConnection>() != null)
            {
                // On client disconnect
                if (ChatInitializing)
                {
                    // Stop initializing
                    ChatInitializing = false;

                    // Close loading sceen if needed
                    if (ChatLoadingScreenWantedToHide)
                        UI_ProgressScreen.instance.Hide();
                    ChatLoadingScreenWantedToHide = false;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_ProgressScreen), "Hide")]
        public static bool HideProg()
        {
            // Check chat initializing
            if (ChatInitializing)
            {
                // Mark as wanting to hide
                ChatLoadingScreenWantedToHide = true;

                // Prevent hide
                return false;
            }

            // Allow hide
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_ProgressScreen), "Hide")]
        public static void Hide()
        {
            FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
            {
                if (UI_ProgressScreen.instance.IsVisibleOrFading)
                    return false;

                FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                {
                    if (ShowWorldJoinChatUnreadPopup)
                    {
                        ShowWorldJoinChatUnreadPopup = false;
                        if (ChatManager.instance._unreadConversations != null && ChatManager.instance._unreadConversations.Count > 0 && !DisplayedUnreads)
                        {
                            NotificationManager.instance.AddNotification(new Notification("You have " + ChatManager.instance._unreadConversations.Count + " unread message(s)"));
                            DisplayedUnreads = true;
                        }
                    }
                });
                return true;
            });
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MessageRouter), "OnWebServiceMessage")]
        public static bool OnWebServiceMessage(string jsonData)
        {
            // Handle chat packets
            JsonData packet = JsonMapper.ToObject(jsonData);
            string evt = (string)packet["eventId"];

            // Check event
            if (evt == "users.conversations")
            {
                // Mark received
                ExpectedChatConvoCount = packet["conversations"].Count;
                ChatConvosReceived = true;
            }
            else if (evt == "conversations.addParticipant" && LastRoomJoinMessage != null)
            {
                // Mark received
                if (((string)packet["conversationId"]) == LastRoomJoinMessage.RoomConversationId)
                    ChatConvoJoinReceived = true;
            }

            // Handle
            return !FeralTweaksNetworkHandler.HandleChatPacket(evt, packet);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_ChatPanel), "PrepareChatForSubmission")]
        public static bool PrepareChatForSubmission(string inChatText, ref string __result)
        {
            if (inChatText.ToLower().Contains("<noparse>") || inChatText.ToLower().Contains("</noparse>"))
            {
                inChatText = UI_ChatPanel.ReplaceCaseInsensitive(inChatText, "<noparse>", "");
                inChatText = UI_ChatPanel.ReplaceCaseInsensitive(inChatText, "</noparse>", "");
            }
            __result = inChatText;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CoreMessageManager), "SendMessageToRegisteredListeners")]
        public static void SendMessageToRegisteredListeners(CoreMessageManager __instance, string tag, IMessage inMessage)
        {
            // Handle message by type
            ChatConversationMessage chMsg = inMessage.TryCast<ChatConversationMessage>();
            if (chMsg != null)
                OnChatMessage(chMsg);
            ChatConversationHistoryResponse chHMsg = inMessage.TryCast<ChatConversationHistoryResponse>();
            if (chHMsg != null)
                OnChatHistory(chHMsg);
            ChatConversationListResponse chCLsg = inMessage.TryCast<ChatConversationListResponse>();
            if (chCLsg != null)
                OnChatConvos(chCLsg);
            ChatConversationGetResponse chCMsg = inMessage.TryCast<ChatConversationGetResponse>();
            if (chCMsg != null)
                OnChatConvo(chCMsg);
            ConversationAddResponse chAMsg = inMessage.TryCast<ConversationAddResponse>();
            if (chAMsg != null)
                OnChatAdded(chAMsg);
            ChatSessionStartMessage chSt = inMessage.TryCast<ChatSessionStartMessage>();
            if (chSt != null)
                OnChatStart(chSt);
            RoomJoinSuccessMessage rjMsg = inMessage.TryCast<RoomJoinSuccessMessage>();
            if (rjMsg != null)
                LastRoomJoinMessage = rjMsg;
        }

        public static void OnChatStart(ChatSessionStartMessage sMsg)
        {
            // Mark initializing 
            ChatInitializing = true;
            ChatReadyForConnFixer = false;

            // Send FT handshake if game server supports FeralTweaks
            if (FeralTweaksServer.IsModLoaded("feraltweaks") && FeralTweaks.ProtocolVersion >= 3)
            {
                // Create FT handshake
                Dictionary<string, object> pkt = new Dictionary<string, object>();
                pkt["cmd"] = "feraltweaks.fthandshake";
                pkt["feraltweaks_protocol"] = FeralTweaks.ProtocolVersion.ToString();
                pkt["feraltweaks_version"] = FeralTweaksLoader.GetLoadedMod<FeralTweaks>().Version;

                // Add mods
                Dictionary<string, string> ftMods = new Dictionary<string, string>();
                foreach (FeralTweaksMod mod in FeralTweaksLoader.GetLoadedMods())
                {
                    ftMods[mod.ID] = mod.Version;
                }
                pkt["feraltweaks_mods"] = ftMods;

                // Create json
                string msg = JsonConvert.SerializeObject(pkt);
                NetworkManager.ChatServiceConnection._client.WriteToSocket(msg);

                // Schedule post-init
                FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                {
                    if (NetworkManager.ChatServiceConnection == null || NetworkManager.ChatServiceConnection._client == null || !NetworkManager.ChatServiceConnection._client.connected)
                        return true;
                    if (!ChatHandshakeDone)
                        return false;

                    // Post-init
                    if (NetworkManager.ChatServiceConnection != null && NetworkManager.ChatServiceConnection._client != null && NetworkManager.ChatServiceConnection._client.connected)
                    {
                        // Send post-init
                        DisplayedUnreads = false;
                        ChatPostInit = false;
                        ShowWorldJoinChatUnreadPopup = false;
                        pkt = new Dictionary<string, object>();
                        pkt["cmd"] = "feraltweaks.postinit";
                        msg = JsonConvert.SerializeObject(pkt);
                        NetworkManager.ChatServiceConnection._client.WriteToSocket(msg);
                    }

                    // Return
                    return true;
                });
            }
            else
                ChatReadyForConnFixer = true;
        }

        public static void OnChatMessage(ChatConversationMessage msg)
        {
            if (msg.ChatEntry._message != null)
                msg.ChatEntry._message = ChatFormatter.Format(msg.ChatEntry._message, true);
            if (msg.ChatEntry._filteredMessage != null)
                msg.ChatEntry._filteredMessage = ChatFormatter.Format(msg.ChatEntry._filteredMessage, true);
            msg.ChatEntry.RefreshDisplayData();
        }

        public static void OnChatHistory(ChatConversationHistoryResponse msg)
        {
            foreach (ChatEntry entry in msg.Messages)
            {
                if (entry._message != null)
                    entry._message = ChatFormatter.Format(entry._message, true);
                if (entry._filteredMessage != null)
                    entry._filteredMessage = ChatFormatter.Format(entry._filteredMessage, true);
                entry.RefreshDisplayData();
            }
        }

        public static void OnChatConvo(ChatConversationGetResponse msg)
        {
            foreach (ChatEntry entry in msg.Conversation.messages)
            {
                if (entry._message != null)
                    entry._message = ChatFormatter.Format(entry._message, true);
                if (entry._filteredMessage != null)
                    entry._filteredMessage = ChatFormatter.Format(entry._filteredMessage, true);
                entry.RefreshDisplayData();
            }

            // If initializing, set room chat
            if (ChatConnFixerRunning && LastRoomJoinMessage != null && msg.Conversation.id == LastRoomJoinMessage.RoomConversationId)
            {
                // Update
                ChatManager.instance._roomConversation = msg.Conversation;
                msg.Conversation.RequestAdditionalChats(true);
                CoreMessageManager.SendMessage<RoomConversationChangedMessage>(new RoomConversationChangedMessage(msg.Conversation));
            }
        }

        public static void OnChatAdded(ConversationAddResponse msg)
        {
            // If initializing, fetch conversation
            if (ChatConnFixerRunning && LastRoomJoinMessage != null && msg.ConversationId == LastRoomJoinMessage.RoomConversationId)
            {
                // Update
                ChatSrvHandler.RequestConversation(msg.ConversationId);
            }
        }

        public static void OnChatConvos(ChatConversationListResponse msg)
        {
            // Update convos
            foreach (ChatConversationData convo in msg.Conversations)
            {
                foreach (ChatEntry entry in convo.messages)
                {
                    if (entry._message != null)
                        entry._message = ChatFormatter.Format(entry._message, true);
                    if (entry._filteredMessage != null)
                        entry._filteredMessage = ChatFormatter.Format(entry._filteredMessage, true);
                    entry.RefreshDisplayData();
                }
            }

            // If initializing, add to cache
            ChatManager.instance._cachedConversations = new Il2CppSystem.Collections.Generic.List<ChatConversationData>(msg.Conversations.Cast<Il2CppSystem.Collections.Generic.IEnumerable<ChatConversationData>>());
            foreach (ChatConversationData convo in msg.Conversations)
            {
                CoreMessageManager.SendMessage<CachedConversationAddedMessage>(new CachedConversationAddedMessage(convo));
            }
        }

        public static bool DisplayedUnreads = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChatConnectMessage))]
        [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(bool), typeof(string) })]
        public static bool OnConnection(bool success, string message)
        {
            if (success)
            {
                // Send handshake
                Dictionary<string, object> pkt = new Dictionary<string, object>();
                pkt["cmd"] = "sessions.start";
                pkt["uuid"] = UserManager.Me.UUID;
                pkt["auth_token"] = NetworkManager.autoLoginAuthToken;
                pkt["ft"] = "enabled";
                pkt["ft_prot"] = FeralTweaks.ProtocolVersion.ToString();

                // Create json
                string msg = JsonConvert.SerializeObject(pkt);
                NetworkManager.ChatServiceConnection._client.WriteToSocket(msg);
                return false;
            }
            return true;
        }

        private static bool ChatConnFixerRunning = false;
        private static bool ChatConvosReceived = false;
        private static bool ChatConvoJoinReceived = false;
        private static bool ConvoSortRequired = false;
        private static int ExpectedChatConvoCount = 0;
        private static long ChatConnFixerLastAttempt;
        private static int ChatConnFixerAttempt;
        private static RoomJoinSuccessMessage LastRoomJoinMessage;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WaitController), "Update")]
        public static void Update()
        {
            // Check if initializing
            if (ChatInitializing)
            {
                // Chat's being inited
                // The fixer runs every frame update until its ready
                // It does this by checking the chat manager and if its not in a room or if there was no convo cache received, it re-transmits the packets for room join up to 5 times until giving up

                // Check if ready
                if (ChatReadyForConnFixer)
                {
                    // Run fixer
                    ChatConnFixerLastAttempt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    ChatConnFixerRunning = true;
                    ChatReadyForConnFixer = false;
                    ConvoSortRequired = false;
                    ChatConnFixerAttempt = 0;
                    ExpectedChatConvoCount = 0;
                    ChatConvosReceived = false;
                    ChatConvoJoinReceived = false;
                }

                // Check running
                if (ChatConnFixerRunning && LastRoomJoinMessage != null)
                {
                    // Check if chat convos are received
                    bool allReady = true;
                    if (!ChatConvosReceived || ChatManager.instance._cachedConversations == null || ChatManager.instance._cachedConversations.Count < ExpectedChatConvoCount)
                    {
                        // Need conversation list update
                        // Check last attempt
                        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - ChatConnFixerLastAttempt >= 12000)
                        {
                            // Send packet
                            Dictionary<string, object> pkt = new Dictionary<string, object>();
                            pkt["cmd"] = "users.conversations";
                            pkt["pageSize"] = 20;
                            pkt["cursor"] = "";
                            string msg = JsonConvert.SerializeObject(pkt);
                            NetworkManager.ChatServiceConnection._client.WriteToSocket(msg);
                        }
                        if (!ChatConvosReceived)
                            ConvoSortRequired = true;
                        allReady = false;
                    }
                    else if (ConvoSortRequired)
                    {
                        // Sort
                        ChatManager.instance.SortCachedConversations();
                        ConvoSortRequired = false;
                    }

                    // Check room
                    if (ChatManager.instance._roomConversation == null && LastRoomJoinMessage != null)
                    {
                        // Need room conversation update
                        // Check last attempt
                        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - ChatConnFixerLastAttempt >= 12000)
                        {
                            // Send packet
                            Dictionary<string, string> pkt = new Dictionary<string, string>();
                            pkt["cmd"] = "conversations.get";
                            pkt["conversationId"] = LastRoomJoinMessage.RoomConversationId;
                            string msg = JsonConvert.SerializeObject(pkt);
                            NetworkManager.ChatServiceConnection._client.WriteToSocket(msg);
                        }
                        allReady = false;
                    }

                    // Check ready
                    if (!allReady)
                    {
                        // Check last attempt
                        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - ChatConnFixerLastAttempt >= 12000)
                        {
                            // Check chat status and if needed, retry
                            if (ChatConnFixerAttempt < 5)
                            {
                                // Increase attempt
                                ChatConnFixerAttempt++;
                                ChatConnFixerLastAttempt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            }
                            else
                            {
                                // Reset
                                ChatConnFixerRunning = false;
                                ChatInitializing = false;
                                LastRoomJoinMessage = null;

                                // Close loading sceen if needed
                                if (ChatLoadingScreenWantedToHide)
                                    UI_ProgressScreen.instance.Hide();
                                ChatLoadingScreenWantedToHide = false;
                            }
                        }
                    }
                    else
                    {
                        // Chat is ready!
                        ChatConnFixerRunning = false;
                        ChatInitializing = false;
                        LastRoomJoinMessage = null;

                        // Close loading sceen if needed
                        if (ChatLoadingScreenWantedToHide)
                            UI_ProgressScreen.instance.Hide();
                        ChatLoadingScreenWantedToHide = false;
                    }
                }
            }
            else
            {
                ChatReadyForConnFixer = false;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_ChatPanel_Conversations), "SetSelectedConversation")]
        public static void SetSelectedConversation(ChatConversationData inData, bool inFromSetup)
        {
            if (inData == null || inFromSetup)
                return;
            ChatConversationData inConv = inData;
            if (ChatManager.instance._unreadConversations.Contains(inConv.id) && NetworkManager.ChatServiceConnection != null && NetworkManager.ChatServiceConnection.IsConnected)
            {
                if (FeralTweaksServer.IsModLoaded("feraltweaks"))
                {
                    // Send packet
                    Il2CppSystem.Collections.Generic.Dictionary<string, string> pkt = new Il2CppSystem.Collections.Generic.Dictionary<string, string>();
                    pkt["cmd"] = "feraltweaks.markread";
                    pkt["conversation"] = inConv.id;
                    string msg = JsonMapper.ToJson(pkt);
                    NetworkManager.ChatServiceConnection._client.WriteToSocket(msg);
                }
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

        private static GameObject[] GetChildren(this GameObject parent)
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
    }
}