using FeralTweaks;
using HarmonyLib;
using Il2CppInterop.Runtime;
using LitJson;
using Server;
using Services.Chat;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public static class ChatPatches
    {
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
        [HarmonyPatch(typeof(UI_ChatPanel_Conversations), "OnConversationItemClicked")]
        public static void OnConversationItemClicked(ChatConversationData inConversation)
        {
            if (ChatManager.instance._unreadConversations.Contains(inConversation.id))
            {
                // Send packet
                Il2CppSystem.Collections.Generic.Dictionary<string, string> pkt = new Il2CppSystem.Collections.Generic.Dictionary<string, string>();
                pkt["cmd"] = "feraltweaks.markread";
                pkt["conversation"] = inConversation.id;
                string msg = JsonMapper.ToJson(pkt);
                NetworkManager.ChatServiceConnection._client.WriteToSocket(msg);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_ProgressScreen), "Hide")]
        public static void Hide()
        {
            FeralTweaks.actions.Add(() =>
            {
                if (UI_ProgressScreen.instance.IsVisibleOrFading)
                    return false;

                FeralTweaks.uiActions.Add(() =>
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
            return !FeralTweaks.HandleChatPacket(evt, packet);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChatConnectMessage))]
        [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(bool), typeof(string) })]
        public static bool OnConnection(bool success, string message)
        {
            if (success)
            {
                // Mention FeralTweaks support
                Il2CppSystem.Collections.Generic.Dictionary<string, string> pkt = new Il2CppSystem.Collections.Generic.Dictionary<string, string>();
                pkt["cmd"] = "sessions.start";
                pkt["uuid"] = UserManager.Me.UUID;
                pkt["auth_token"] = NetworkManager.autoLoginAuthToken;
                pkt["feraltweaks"] = "enabled";
                pkt["feraltweaks_protocol"] = FeralTweaks.ProtocolVersion.ToString();
                pkt["feraltweaks_version"] = FeralTweaksLoader.GetLoadedMod<FeralTweaks>().Version;
                string msg = JsonMapper.ToJson(pkt);
                NetworkManager.ChatServiceConnection._client.WriteToSocket(msg);
                return false;
            }
            return true;
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
