using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class PlayerJoinNotifPatch
    {
        private static bool injected;

        public class FT_JoinNotifPatchVars : MonoBehaviour
        {
            public FT_JoinNotifPatchVars() : base() { }
            public FT_JoinNotifPatchVars(IntPtr ptr) : base(ptr) { }

            public bool JoinNotifAttempted;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NotificationController), "OnNetworkAvatarSpawned")]
        public static bool NetworkedAvatarSpawnedMessage(NetworkedAvatarSpawnedMessage inMessage)
        {
            if (!injected)
                ClassInjector.RegisterTypeInIl2Cpp<FT_JoinNotifPatchVars>();
            injected = true;

            // Get or create vars
            FT_JoinNotifPatchVars vars = inMessage.Avatar.gameObject.GetComponent<FT_JoinNotifPatchVars>();
            if (vars == null)
                vars = inMessage.Avatar.gameObject.AddComponent<FT_JoinNotifPatchVars>();
            if (vars.JoinNotifAttempted)
            {
                // Deny
                return false;
            }

            // Mark as attempted
            vars.JoinNotifAttempted = true;

            // Allow notif to be shown
            return true;
        }
    }
}