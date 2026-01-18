using Server;
using UnityEngine;
using LitJson;
using FeralTweaks.Mods;
using FeralTweaks;
using FeralTweaks.Networking;
using feraltweaks.Patches.AssemblyCSharp;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using FeralTweaks.Actions;

namespace feraltweaks
{
    public static class FeralTweaksNetworkHandler
    {
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
                            FeralTweaksActions.Unity.Oneshot(() =>
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
                            FeralTweaksActions.Unity.Oneshot(() =>
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
                                        NetworkManager.DisconnectReason = DisconnectReason.Unknown;
                                        NetworkManager.instance._serverConnection.Disconnect();
                                        if (NetworkManager.instance._chatServiceConnection.IsConnected)
                                            NetworkManager.instance._chatServiceConnection.Disconnect();
                                        NetworkManager.instance._serverConnection = null;
                                        NetworkManager.instance._chatServiceConnection = null;
                                        NetworkManager.instance._jwt = null;
                                    }

                                    // Show window and log out
                                    FeralTweaksActions.EventQueue.Oneshot(() =>
                                    {
                                        // Show window
                                        FeralTweaksActions.Unity.Oneshot(() =>
                                        {
                                            if (UI_ProgressScreen.instance.IsVisible)
                                                UI_ProgressScreen.instance.Hide();
                                            CoreWindowManager.OpenWindow<UI_Window_OkErrorPopup>(new Action<UI_Window_OkErrorPopup>(window =>
                                            {
                                                window.Setup(ChartDataManager.instance.localizationChartData.Get(title, title), ChartDataManager.instance.localizationChartData.Get(message, message), ChartDataManager.instance.localizationChartData.Get(button, button));
                                                window.OnCloseEvent.AddListener(new Action<UI_Window>((win) =>
                                                {
                                                    LoginLogoutPatches.loggingOut = false;
                                                    LoginLogoutPatches.doLogout = true;
                                                    CoreSharedUtils.CoreReset(SplashError.NONE, ErrorCode.None);
                                                }));
                                            }), true);
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
                                    FeralTweaksActions.Unity.Oneshot(() =>
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
                                    FeralTweaksActions.Unity.Oneshot(() =>
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
                                    FeralTweaksActions.Unity.Oneshot(() =>
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
                                    FeralTweaksActions.Unity.Oneshot(() =>
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
                                    FeralTweaksActions.Unity.Oneshot(() =>
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
                                    FeralTweaksActions.Unity.Oneshot(() =>
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
                                    FeralTweaksActions.Unity.Oneshot(() =>
                                    {
                                        try
                                        {
                                            UI_Window_YesNoPopup.CloseWindow();
                                        }
                                        catch { }
                                        UI_Window_YesNoPopup.OpenWindow(ChartDataManager.instance.localizationChartData.Get(title, title), ChartDataManager.instance.localizationChartData.Get(message, message),
                                            ChartDataManager.instance.localizationChartData.Get(yesBtn, yesBtn), ChartDataManager.instance.localizationChartData.Get(noBtn, noBtn), (Il2CppSystem.Action<bool>)new System.Action<bool>(res =>
                                            {
                                                // Send response
                                                XtWriter wr = new XtWriter(XtCmd.FacilitatorSetBusy);
                                                wr.Cmd = "mod:ft";
                                                wr.WriteString("yesnopopup");
                                                wr.WriteString(popupID);
                                                NetworkManager.instance._serverConnection.Send(wr.WriteBool(res));
                                            }));
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
                    case "fthandshake":
                        // Handshake done
                        ChatPatches.ChatHandshakeDone = true;
                        break;
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
                    case "postinit":
                        // Postinit
                        ChatPatches.ChatPostInit = true;
                        ChatPatches.ChatReadyForConnFixer = true;
                        lock (ChatPatches.typingStatusDisplayNames)
                        {
                            ChatPatches.typingStatusDisplayNames.Clear();
                        }
                        lock (ChatPatches.typingStatuses)
                        {
                            ChatPatches.typingStatuses.Clear();
                        }

                        // Send typing status init    
                        Dictionary<string, object> pkt = new Dictionary<string, object>();
                        pkt["cmd"] = "feraltweaks.typingstatus.subscribe";
                        string msg = JsonConvert.SerializeObject(pkt);
                        NetworkManager.ChatServiceConnection._client.WriteToSocket(msg);
                        break;
                    case "unreadconversations":
                        {
                            // Add unreads
                            Il2CppSystem.Collections.Generic.List<string> convos = JsonMapper.ToObject<Il2CppSystem.Collections.Generic.List<string>>(JsonMapper.ToJson(packet["conversations"]));
                            if (ChatManager.instance._unreadConversations == null)
                                ChatManager.instance._unreadConversations = new Il2CppSystem.Collections.Generic.List<string>();
                            foreach (string convo in convos)
                                if (!ChatManager.instance._unreadConversations.Contains(convo))
                                    ChatManager.instance._unreadConversations.Add(convo);

                            // Add unread message counts for each conversation
                            if (packet.Contains("messageCounts"))
                            {
                                // Read counts
                                Il2CppSystem.Collections.Generic.Dictionary<string, int> messageCounts = JsonMapper.ToObject<Il2CppSystem.Collections.Generic.Dictionary<string, int>>(JsonMapper.ToJson(packet["messageCounts"]));

                                // Add all
                                foreach (string convo in convos)
                                {
                                    // Check
                                    if (messageCounts.ContainsKey(convo))
                                    {
                                        // Add to list
                                        ChatPatches.unreadMessagesPerConversation[convo] = messageCounts[convo];
                                    }
                                }
                            }

                            // Schedule popup
                            ChatPatches.ShowWorldJoinChatUnreadPopup = true;

                            // Schedule reload of
                            FeralTweaksActions.Unity.Oneshot(() =>
                            {
                                if (NetworkManager.ChatServiceConnection == null || NetworkManager.ChatServiceConnection._client == null || !NetworkManager.ChatServiceConnection._client.connected)
                                    return true;
                                if (!ChatPatches.ChatPostInit)
                                    return false;

                                // Schedule
                                FeralTweaksActions.Unity.Oneshot(() =>
                                {
                                    // Check unreads
                                    if (ChatPatches.ShowWorldJoinChatUnreadPopup && !UI_ProgressScreen.instance.IsVisibleOrFading)
                                    {
                                        ChatPatches.ShowWorldJoinChatUnreadPopup = false;
                                        if (ChatManager.instance._unreadConversations != null && ChatManager.instance._unreadConversations.Count > 0 && !ChatPatches.DisplayedUnreads)
                                        {
                                            int unreads = 0;
                                            foreach (string convo in ChatManager.instance._unreadConversations)
                                            {
                                                // Check if room
                                                if (ChatManager.instance._roomConversation != null && ChatManager.instance._roomConversation.id == convo)
                                                    continue;
                                                unreads += ChatPatches.unreadMessagesPerConversation.GetValueOrDefault(convo, 1);
                                            }
                                            if (unreads != 0)
                                            {
                                                NotificationManager.instance.AddNotification(new Notification("You have " + unreads + " unread message(s)"));
                                                ChatPatches.DisplayedUnreads = true;
                                            }
                                        }
                                    }
                                });

                                // Return
                                return true;
                            });
                            break;
                        }
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