using System;
using System.Globalization;
using System.Reflection;
using CodeStage.AntiCheat.ObscuredTypes;
using FeralTweaks.Mods;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using Newtonsoft.Json;
using NodeCanvas.Tasks.Actions;
using StrayTech;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class NotificationPatches
    {
        private static bool patched;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CoreChartDataManager), "SetChartObjectInstances")]
        public static void SetChartObjectInstances()
        {
            if (patched)
                return;
            patched = true;
            Harmony.CreateAndPatchAll(typeof(PatchesLate));
        }
        
        public static class PatchesLate
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(NotificationController), "OnPrimaryQuestAdded")]
            public static bool OnPrimaryQuestAdded(PrimaryQuestAddedMessage inMessage)
            {
                // Create notification
                Notification notif = new Notification();
                notif.isLogged = true;
                notif._isUnread = false;
                notif.imageDefId = null;
                notif.messageText = LocalizationChartData.Format("14215");
                notif.timeStamp = CoreDateUtils.Now;
                notif.notificationGroup = NotificationGroup.System;

                // Show notif
                NotificationManager.instance.AddNotification(notif);
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(NotificationController), "OnPrimaryQuestUpdated")]
            public static bool OnPrimaryQuestUpdated(PrimaryQuestUpdatedMessage inMessage)
            {
                if (inMessage.SecondaryQuest.IsCompleted)
                {
                    // Create notification
                    Notification notif = new Notification();
                    notif.isLogged = true;
                    notif._isUnread = false;
                    notif.imageDefId = null;
                    notif.messageText = LocalizationChartData.Format("14216");
                    notif.timeStamp = CoreDateUtils.Now;
                    notif.notificationGroup = NotificationGroup.System;

                    // Show notif
                    NotificationManager.instance.AddNotification(notif);
                }
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(NotificationManager), "AddNotification")]
            public static void AddNotification(Notification inNotification)
            {
                // Get stack
                Il2CppSystem.Diagnostics.StackTrace stack = new Il2CppSystem.Diagnostics.StackTrace();
                foreach (Il2CppSystem.Diagnostics.StackFrame frame in stack.frames)
                {
                    // Check type
                    if (frame.GetMethod().ReflectedType.FullName == "NotificationController" || frame.GetMethod().ReflectedType.FullName.StartsWith("NotificationController+"))
                    {
                        // Check type
                        if (frame.GetMethod().ReflectedType.FullName.StartsWith("NotificationController+<OnChat>"))
                        {
                            // On chat call

                            // Edit notif to be marked as read right away
                            inNotification._isUnread = false;

                            // Break
                            break;
                        }

                        // Check method name
                        string methodName = frame.GetMethod().Name;
                        switch (methodName)
                        {
                            case "OnGiftPush":
                            case "OnSeasonPassChallengeUpdated":
                            case "OnSeasonPassChallengeCompleted":
                            case "OnSeasonPassTierCompleted":
                            case "OnXPUpdated":
                                // Edit notif to be marked as read right away
                                inNotification._isUnread = false;
                                break;
                        }

                        // Break
                        break;
                    }
                    else if (frame.GetMethod().ReflectedType.IsAssignableFrom(Il2CppType.Of<DailyQuestNotification>()))
                    {
                        // Daily quest notif

                        // Check method name
                        string methodName = frame.GetMethod().Name;
                        if (methodName == "OnExecute")
                        {
                            // Edit notif to be marked as read right away
                            inNotification._isUnread = false;
                        }

                        // Break
                        break;
                    }
                }
            }
        }
    }
}