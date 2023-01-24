using HarmonyLib;
using LitJson;
using Server;
using Services.Chat;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public static class ChatPatches 
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChatEntry), "GetTimeStampUIString")]
        public static void GetTimeStampUIString(ref ChatEntry __instance, ref string __result)
        {
            // Return timestamp
            // First, lets explain what happens here
            // Due to a mistake by WW, the ChatEntry.timeStamp field is not the actual UTC timestamp
            // It is actually the UTC timestamp minus the UTC offset of the current timezone
            // We cannot patch this issue sadly due to an issue with BepInEx, we cannot edit constructors
            //
            // Instead, since localTimeStamp is the actual UTC timestamp so we use that and add the current UTC
            // offset to the current time to get the actual local time of the message
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
            GameObject marker = GetChild(__instance._lastChatTimeText.gameObject.transform.parent.gameObject, "UI_UnreadIndicator");
            if (ChatManager.instance.UnreadConversations.Contains(inChatEntry.conversationId))
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
            Plugin.actions.Add(() =>
            {
                if (UI_ProgressScreen.instance.IsVisibleOrFading)
                    return false;

                Plugin.uiActions.Add(() =>
                {
                    if (Plugin.ShowWorldJoinChatUnreadPopup)
                    {
                        Plugin.ShowWorldJoinChatUnreadPopup = false;
                        if (ChatManager.instance._unreadConversations.Count > 0)
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
            return !Plugin.HandleChatPacket(evt, packet);
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
                pkt["feraltweaks_protocol"] = Plugin.ProtocolVersion.ToString();
                pkt["feraltweaks_version"] = Plugin.Version;
                string msg = JsonMapper.ToJson(pkt);
                NetworkManager.ChatServiceConnection._client.WriteToSocket(msg);
                return false;
            }
            return true;
        }

        private static GameObject GetChild(GameObject parent, string name)
        {
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
    }
}
