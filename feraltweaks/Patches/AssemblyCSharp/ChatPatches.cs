using FeralTweaks;
using FeralTweaks.Formatters;
using FeralTweaks.Mods;
using HarmonyLib;
using Il2CppInterop.Runtime;
using LitJson;
using Newtonsoft.Json;
using Server;
using Services.Chat;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WW.Waiters;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public static class ChatPatches
    {
        internal static Dictionary<string, Dictionary<string, long>> typingStatuses = new Dictionary<string, Dictionary<string, long>>();
        internal static Dictionary<string, string> typingStatusDisplayNames = new Dictionary<string, string>();

        private static long timeLastTypingStatusCheck;
        private static string lastChatInputText;

        private static string typingStatusString = ".  ";


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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_ChatPanel_Conversations), "SetSelectedConversation")]
        public static void SetSelectedConversation(ChatConversationData inData, bool inFromSetup)
        {
            if (inData == null || inFromSetup)
                return;
            ChatConversationData inConv = inData;
            if (ChatManager.instance._unreadConversations.Contains(inConv.id))
            {
                // Send packet
                Il2CppSystem.Collections.Generic.Dictionary<string, string> pkt = new Il2CppSystem.Collections.Generic.Dictionary<string, string>();
                pkt["cmd"] = "feraltweaks.markread";
                pkt["conversation"] = inConv.id;
                string msg = JsonMapper.ToJson(pkt);
                NetworkManager.ChatServiceConnection._client.WriteToSocket(msg);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_ProgressScreen), "Hide")]
        public static void Hide()
        {
            FeralTweaks.ScheduleDelayedActionForUnity(() =>
            {
                if (UI_ProgressScreen.instance.IsVisibleOrFading)
                    return false;

                FeralTweaks.ScheduleDelayedActionForUnity(() =>
                {
                    if (FeralTweaks.ShowWorldJoinChatUnreadPopup)
                    {
                        FeralTweaks.ShowWorldJoinChatUnreadPopup = false;
                        if (ChatManager.instance._unreadConversations != null && ChatManager.instance._unreadConversations.Count > 0)
                        {
                            NotificationManager.instance.AddNotification(new Notification("You have " + ChatManager.instance._unreadConversations.Count + " unread message(s)"));
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
            if (evt == "chat.postMessage")
            {
                // Check if a author string is present
                if (packet.Contains("author") && packet.Contains("source") && packet.Contains("message") && packet.Contains("conversationId") && packet.Contains("conversationType"))
                {
                    // Check
                    string author = (string)packet["author"];
                    string source = (string)packet["source"];
                    string message = (string)packet["message"];
                    string conversationId = (string)packet["conversationId"];
                    string conversationType = (string)packet["conversationType"];
                    if (source != author)
                    {
                        // Check type
                        if (conversationType == "room")
                        {
                            // Get display name of character
                            var awaiter = UserManager.GetUserInfoAsync(source).GetAwaiter();
                            FeralTweaks.ScheduleDelayedActionForUnity(() =>
                            {
                                // Wait
                                if (!awaiter.IsCompleted)
                                    return false;

                                // Get result
                                var info = awaiter.GetResult();
                                if (info == null)
                                    return true;

                                // Create message container
                                RoomConversationMessage cont = new RoomConversationMessage(new ChatEntry(packet));

                                // Update message string
                                message = "</noparse>" + info.Name + "<noparse>\n" + ChatFormatter.Format(message, true);

                                // Update entry
                                cont.ChatEntry.sourceUUID = author;
                                cont.ChatEntry._message = message;
                                cont.ChatEntry._filteredMessage = message;

                                // Refresh
                                var awaiter2 = cont.ChatEntry.RefreshDisplayData().GetAwaiter();
                                FeralTweaks.ScheduleDelayedActionForUnity(() =>
                                {
                                    // Wait
                                    if (!awaiter2.IsCompleted)
                                        return false;

                                    // Locate actor bubble
                                    AvatarBase[] actors = UnityEngine.Object.FindObjectsOfType<AvatarBase>();
                                    foreach (AvatarBase actor in actors)
                                    {
                                        // Check bubble
                                        if (actor.Bubble != null && actor.Bubble.TargetId == author)
                                        {
                                            // Display
                                            actor.Bubble.OnChatMessage(cont);
                                        }
                                    }

                                    // Reset typing status
                                    lock (typingStatuses)
                                    {
                                        if (typingStatuses.ContainsKey(conversationId))
                                        {
                                            // Check if user is present
                                            if (typingStatuses[conversationId].ContainsKey(author))
                                            {
                                                // Remove status
                                                typingStatuses[conversationId].Remove(author);
                                            }
                                        }
                                    }

                                    // Return
                                    return true;
                                });

                                // Return
                                return true;
                            });
                        }
                        else
                        {
                            // Reset typing status
                            lock (typingStatuses)
                            {
                                if (typingStatuses.ContainsKey(conversationId))
                                {
                                    // Check if user is present
                                    if (typingStatuses[conversationId].ContainsKey(author))
                                    {
                                        // Remove status
                                        typingStatuses[conversationId].Remove(author);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Handle
            return !FeralTweaks.HandleChatPacket(evt, packet);
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
            ChatSessionStartMessage chSt = inMessage.TryCast<ChatSessionStartMessage>();
            if (chSt != null)
                OnChatStart(chSt);
        }

        public static void OnChatStart(ChatSessionStartMessage sMsg)
        {
            // Protocol post-hansdhake
            if (FeralTweaksServer.IsModLoaded("feraltweaks"))
            {
                // Protocol Revision 2 Support
                lock (typingStatusDisplayNames)
                {
                    typingStatusDisplayNames.Clear();
                }
                lock (typingStatuses)
                {
                    typingStatuses.Clear();
                }

                // Send typing status init    
                Dictionary<string, object> pkt = new Dictionary<string, object>();
                pkt["cmd"] = "feraltweaks.typingstatus.subscribe";
                string msg = JsonConvert.SerializeObject(pkt);
                NetworkManager.ChatServiceConnection._client.WriteToSocket(msg);       
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
            
            // If present in cache, update
            if (ChatManager.instance._cachedConversations != null)
            {
                foreach (ChatConversationData convo in ChatManager.instance._cachedConversations)
                {
                    // Check convo
                    if (convo.id == msg.Conversation.id)
                    {
                        // Update
                        convo._mostRecentMessage = msg.Conversation._mostRecentMessage;
                        convo._cacheStartIndex = msg.Conversation._cacheStartIndex;
                        convo._cursors = msg.Conversation._cursors;
                        convo._hasOldestMessages = msg.Conversation._hasOldestMessages;
                        convo.messages = msg.Conversation.messages;
                        convo.participants = msg.Conversation.participants;
                        convo.title = msg.Conversation.title;
                        break;
                    }
                }
            }
        }

        public static void OnChatMessage(ChatConversationMessage msg)
        {
            // Chat formatting
            if (msg.ChatEntry._message != null)
                msg.ChatEntry._message = ChatFormatter.Format(msg.ChatEntry._message, true);
            if (msg.ChatEntry._filteredMessage != null)
                msg.ChatEntry._filteredMessage = ChatFormatter.Format(msg.ChatEntry._filteredMessage, true);
            msg.ChatEntry.RefreshDisplayData();
            
            // Reset typing status
            lock(typingStatuses)
            {
                if (typingStatuses.ContainsKey(msg.ConversationId))
                {
                    // Check if user is present
                    if (typingStatuses[msg.ConversationId].ContainsKey(msg.ChatEntry.sourceUUID))
                    {
                        // Remove status
                        typingStatuses[msg.ConversationId].Remove(msg.ChatEntry.sourceUUID);
                    }
                }
            }
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChatConnectMessage))]
        [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(bool), typeof(string) })]
        public static bool OnConnection(bool success, string message)
        {
            if (success)
            {
                // Mention FeralTweaks support
                Dictionary<string, object> pkt = new Dictionary<string, object>();
                pkt["cmd"] = "sessions.start";
                pkt["uuid"] = UserManager.Me.UUID;
                pkt["auth_token"] = NetworkManager.autoLoginAuthToken;
                pkt["feraltweaks"] = "enabled";
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
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WaitController), "Update")]
        public static void Update()
        {
            // Check time since last typing status list update
            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timeLastTypingStatusCheck >= 200)
            {
                // Status tick
                switch(typingStatusString)
                {
                    case ".  ":
                        typingStatusString = ".. ";
                        break;
                    case ".. ":
                        typingStatusString = "...";
                        break;
                    case "...":
                        typingStatusString = ".  ";
                        break;
                }

                // Chat input box update handling
                // Check if the chat window is open
                GameObject root = GameObject.Find("CanvasRoot");
                if (root != null)
                {
                    UI_Window_Chat chat = root.GetComponentInChildren<UI_Window_Chat>(true);
                    if (chat != null && chat.gameObject.active)
                    {
                        // Get tab
                        int index = chat._tabGroup.CurrentSelected;
                        if (index >= 0 && index < chat._tabGroup._tabs.Count)
                        {
                            // Get chat panel
                            UI_ChatPanel panel = null;
                            string convoId = null;
                            GameObject tabPanel = chat._tabGroup._tabs[index].gameObject;
                            if (index == 0)
                            {
                                // Room chat
                                if (chat._publicChatPanel != null && chat._publicChatPanel._conversationData != null)
                                {
                                    panel = chat._publicChatPanel;
                                    convoId = chat._publicChatPanel._conversationData.id;
                                }
                            }
                            else
                            {
                                // Private/gcs likely
                                UI_ChatPanel_Conversations cont = tabPanel.GetComponent<UI_ChatPanel_Conversations>();
                                if (cont != null && cont._privateChatPanel._conversationData != null)
                                {
                                    panel = cont._privateChatPanel;
                                    convoId = cont._privateChatPanel._conversationData.id;
                                }
                            }

                            // Check result
                            if (panel != null && panel._chatInput != null)
                            {
                                // Get current text
                                string chatText = panel._chatInput.text;
                                if (chatText != lastChatInputText)
                                {
                                    // Send update
                                    if (chatText != "" && !chatText.StartsWith("/") && !chatText.StartsWith(">") && NetworkManager.ChatServiceConnection != null && NetworkManager.ChatServiceConnection.IsConnected && FeralTweaksServer.IsModLoaded("feraltweaks"))
                                    {
                                        // Send typing status update
                                        Dictionary<string, string> pkt = new Dictionary<string, string>();
                                        pkt["cmd"] = "feraltweaks.typing";
                                        pkt["conversationId"] = convoId;
                                        string msg = JsonConvert.SerializeObject(pkt);
                                        NetworkManager.ChatServiceConnection._client.WriteToSocket(msg);
                                    }
                                    lastChatInputText = chatText;
                                }
                            }
                        }
                    }

                    // Recheck typing statuses

                    // Get statuses
                    Dictionary<string, Dictionary<string, long>> statuses;
                    lock (typingStatuses)
                    {
                        statuses = new Dictionary<string, Dictionary<string, long>>(typingStatuses);
                    }

                    // Check
                    foreach (string convoID in statuses.Keys)
                    {
                        // Go through convo statuses
                        foreach (string userID in statuses[convoID].Keys)
                        {
                            // Verify
                            if (statuses[convoID][userID] + 5000 < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                            {
                                // Remove
                                lock (typingStatuses)
                                {
                                    // Check if list is present
                                    if (typingStatuses.ContainsKey(convoID))
                                    {
                                        // Remove
                                        typingStatuses[convoID].Remove(userID);
                                    }
                                }
                            }
                        }
                    }

                    // Update typing status UI
                    UpdateTypingStatusUI();
                }

                // Update timer
                timeLastTypingStatusCheck = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        private static void UpdateTypingStatusUI()
        {
            // Check if the chat window is open
            UI_Window_Chat chat = GameObject.Find("CanvasRoot").GetComponentInChildren<UI_Window_Chat>(true);
            if (chat != null && chat.gameObject.active)
            {
                // Get tab
                int index = chat._tabGroup.CurrentSelected;
                if (index >= 0 && index < chat._tabGroup._tabs.Count)
                {
                    // Get active chat panel
                    // Get chat panel
                    UI_ChatPanel panel = null;
                    string convoId = null;
                    GameObject tabPanel = chat._tabGroup._tabs[index].gameObject;
                    if (index == 0)
                    {
                        // Room chat
                        if (chat._publicChatPanel != null && chat._publicChatPanel._conversationData != null)
                        {
                            panel = chat._publicChatPanel;
                            convoId = chat._publicChatPanel._conversationData.id;
                        }
                    }
                    else
                    {
                        // Private/gcs likely
                        UI_ChatPanel_Conversations cont = tabPanel.GetComponent<UI_ChatPanel_Conversations>();
                        if (cont != null && cont._privateChatPanel._conversationData != null)
                        {
                            panel = cont._privateChatPanel;
                            convoId = cont._privateChatPanel._conversationData.id;
                        }
                    }

                    // Check result
                    if (panel != null)
                    {
                        // Get typing status
                        GameObject typingStatusObj = GetChild(panel.gameObject, "Typing_Status");
                        RectTransform transChatPanel = panel._scrollRect.gameObject.transform.Cast<RectTransform>();
                        WWTextMeshProUGUI typingStatusLbl = typingStatusObj.GetComponent<WWTextMeshProUGUI>();
                        if (typingStatusObj != null)
                        {
                            // Get all statuses for this convo
                            List<string> typingPlayers = new List<string>();
                            Dictionary<string, string> playerDisplays = null;
                            lock (typingStatusDisplayNames)
                            {
                                playerDisplays = new Dictionary<string, string>(typingStatusDisplayNames);
                            }
                            lock (typingStatuses)
                            {
                                if (typingStatuses.ContainsKey(convoId))
                                {
                                    // Get
                                    foreach (string userID in typingStatuses[convoId].Keys)
                                    {
                                        // Check ID
                                        if (userID == UserManager.Me.UUID)
                                            continue;

                                        // Get name
                                        if (playerDisplays.ContainsKey(userID))
                                        {
                                            // Add
                                            string name = playerDisplays[userID];
                                            if (!typingPlayers.Contains(name))
                                            {
                                                typingPlayers.Add(name);
                                            }
                                        }
                                    }
                                }
                            }

                            // Check
                            if (typingPlayers.Count == 0)
                            {
                                // Check text and clear if needed
                                if (typingStatusLbl.text != "")
                                {
                                    // Update text
                                    bool wasAtBottom = panel.IsAtBottom;
                                    typingStatusLbl.text = "";
                                    transChatPanel.offsetMin = new Vector2(0.0001f, 0);

                                    // Scroll if needed
                                    if (wasAtBottom)
                                    {
                                        // Schedule
                                        long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                                        FeralTweaks.ScheduleDelayedActionForUnity(() =>
                                        {
                                            // Scroll
                                            panel.SnapToBottom(true);
                                        });
                                    }
                                }
                            }
                            else
                            {
                                // Generate message
                                string msg = "Various sparks are thinking" + typingStatusString;
                                if (typingPlayers.Count == 1)
                                {
                                    // One player
                                    msg = typingPlayers[0] + " is thinking" + typingStatusString;
                                }
                                else if (typingPlayers.Count <= 4)
                                {
                                    // Two to four players
                                    msg = "";
                                    for (int i = 0; i < typingPlayers.Count - 1; i++)
                                    {
                                        if (msg != "")
                                            msg += ", ";
                                        msg += typingPlayers[i];
                                    }
                                    msg += " and " + typingPlayers[typingPlayers.Count - 1] + " are thinking" + typingStatusString;
                                }

                                // Assign UI text if needed
                                if (typingStatusLbl.text != msg)
                                {
                                    bool wasAtBottom = panel.IsAtBottom;
                                    typingStatusLbl.text = msg;
                                    typingStatusLbl.ForceMeshUpdate();

                                    // Calculate height
                                    float height = typingStatusLbl.textBounds.size.y;
                                    height += 7;

                                    // Verify how much off it is
                                    if (transChatPanel.offsetMin.y < height - 0.01f || transChatPanel.offsetMin.y > height + 0.01f)
                                    {
                                        // Adjust size
                                        transChatPanel.offsetMin = new Vector2(0.0001f, height);

                                        // Scroll if needed
                                        if (wasAtBottom)
                                        {
                                            // Schedule
                                            long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                                            FeralTweaks.ScheduleDelayedActionForUnity(() =>
                                            {
                                                // Scroll
                                                panel.SnapToBottom(true);
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_ActorBubble), "SetChatText")]
        public static bool SetChatText(UI_ActorBubble __instance, ChatEntry inChatEntry)
        {
            // Find all emoji
            Il2CppSystem.Collections.Generic.List<ChatEmojiDefComponent> emojis = new Il2CppSystem.Collections.Generic.List<ChatEmojiDefComponent>();
            foreach (BaseDef def in ListChartData.instance.GetDef("11704").GetComponent<ListDefComponent>().Defs)
            {
                ChatEmojiDefComponent emoji = def.GetComponent<ChatEmojiDefComponent>();
                if (emoji != null)
                    emojis.Add(emoji);
            }            

            // Check if emoji message
            if (inChatEntry.IsSingleEmojiMessage(emojis))
            {
                // Disable regular chat bubble and show emoji bubble
                __instance._chatStringBuilder.Clear();
                __instance._currentChats.Clear();
                __instance._chatBubbleText.text = "";
                __instance._emojiBubbleText.text = inChatEntry.DisplayData.DisplayMessage;
                __instance._emojiBubbleTransform.gameObject.SetActive(true);
                __instance._normalChatBubbleGroup.gameObject.SetActive(false);   
            }
            else
            {
                // Generate message
                int currentMessageCount = 0;
                __instance._chatStringBuilder.Clear();
                foreach (string message in __instance._currentChats)
                {
                    string msgT = message;
                    while (msgT.Length > __instance._charsPerLine)
                    {
                        msgT = msgT.Substring(__instance._charsPerLine);
                        currentMessageCount++;
                    }   
                    currentMessageCount++;
                }

                // Remove messages past max
                for (int i = currentMessageCount + 1; i > __instance._maxLines && __instance._currentChats.Count > 0 && __instance._currentChats.Count > 3; i--)
                {
                    // Remove
                    __instance._currentChats.RemoveAt(0);
                }

                // Add message
                __instance._currentChats.Add(inChatEntry.DisplayData.DisplayMessage);

                // Create message
                __instance._chatStringBuilder.AppendLine("</noparse><size=10> </size><noparse>"); // Spacing between messages
                foreach (string message in __instance._currentChats)
                {
                    __instance._chatStringBuilder.AppendLine(message);
                    __instance._chatStringBuilder.AppendLine("</noparse><size=6> </size><noparse>"); // Spacing between messages
                }

                // Show message
                __instance._chatBubbleText.text = __instance._chatStringBuilder.ToString();
                __instance._emojiBubbleText.text = "";
                __instance._emojiBubbleTransform.gameObject.SetActive(false);
                __instance._normalChatBubbleGroup.gameObject.SetActive(true);   
            }

            // DONT run original
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_ActorBubble), "MUpdate")]
        public static bool ActorBubbleStateUpdate(UI_ActorBubble __instance)
        {
            if (__instance.TargetId != null && __instance._chatBubbleText != null)
            {
                // Check typing status
                string id = __instance.TargetId;

                // Get room
                ChatConversationData room = ChatManager.instance._roomConversation;
                if (room != null)
                {
                    // Get typing statuses
                    Dictionary<string, Dictionary<string, long>> statuses;
                    lock (typingStatuses)
                    {
                        statuses = new Dictionary<string, Dictionary<string, long>>(typingStatuses);
                    }
                    if (statuses.ContainsKey(room.id))
                    {
                        // Check if user is present
                        if (statuses[room.id].ContainsKey(id))
                        {
                            // User is typing
                            if (__instance.CurrentChatBubbleState != UI_ActorBubble.ChatBubbleState.ChatEllipsis)
                            {
                                // Set state
                                if (__instance.CurrentChatBubbleState != UI_ActorBubble.ChatBubbleState.Chat)
                                    __instance.CurrentChatBubbleState = UI_ActorBubble.ChatBubbleState.Chat;

                                // Generate bubble text
                                string msg = __instance._chatStringBuilder.ToString();
                                __instance._chatBubbleText.text = "<noparse>" + msg + "</noparse>" + typingStatusString;
                                __instance._emojiBubbleTransform.gameObject.SetActive(false);
                                __instance._normalChatBubbleGroup.gameObject.SetActive(true);
                            }

                            // Prevent regular logic from decreasing the timer
                            return false;
                        }
                    }
                }

                // Check if the bubble still has a typing status suffix
                if (__instance._chatBubbleText.text.EndsWith(".") || __instance._chatBubbleText.text.EndsWith(". ") || __instance._chatBubbleText.text.EndsWith(".  "))
                {
                    // Generate bubble text
                    string msg = __instance._chatStringBuilder.ToString();
                    if (msg.Replace("\r", "").Replace("\n", "") == "")
                    {
                        __instance._chatBubbleText.text = "";
                        __instance.CurrentChatBubbleState = UI_ActorBubble.ChatBubbleState.Off;
                    }
                    else if (__instance.CurrentChatBubbleState != UI_ActorBubble.ChatBubbleState.ChatEllipsis)
                        __instance._chatBubbleText.text = "<noparse>" + msg + "</noparse>";
                    else
                        __instance._chatBubbleText.text = "...";
                }
            }
            return true;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_ChatPanel), "OnEnable")]
        public static void OnEnableChatPanelGeneric(ref UI_ChatPanel __instance)
        {
            // On Chat panel enable

            // Create typing status entry if needed
            GameObject typingStatus = GetChild(__instance.gameObject, "Typing_Status");
            if (typingStatus == null)
            {
                // Create object
                typingStatus = new GameObject("Typing_Status", Il2CppType.Of<RectTransform>());
                typingStatus.transform.parent = __instance.gameObject.transform;

                // Update transform
                RectTransform trans = typingStatus.transform.Cast<RectTransform>();
                trans.pivot = new Vector2(1, 1);
                trans.anchorMax = new Vector2(1, 0);
                trans.anchorMin = new Vector2(0, 0);
                trans.offsetMax = new Vector2(0, 0);
                trans.offsetMin = new Vector2(5, 2);
                trans.localScale = new Vector3(1, 1, 1);
                trans.anchoredPosition3D = new Vector3(trans.anchoredPosition3D.x, trans.anchoredPosition3D.y, 0);

                // Create label
                WWTextMeshProUGUI label = typingStatus.AddComponent<WWTextMeshProUGUI>();
                label.fontSize = 18;
                label.fontSizeMax = 20;
                label.fontSizeMin = 18;
                label.alignment = TextAlignmentOptions.BottomLeft;

                // Update transform of panel
                RectTransform transChatPanel = __instance._scrollRect.gameObject.transform.Cast<RectTransform>();
                transChatPanel.offsetMin = new Vector2(0.0001f, 0);
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
