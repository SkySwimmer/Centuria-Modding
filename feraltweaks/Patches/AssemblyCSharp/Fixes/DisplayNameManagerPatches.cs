using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class DisplayNameManagerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UserManager), "GetDisplayNameBatched")]
        public static bool GetDisplayNameBatched(UserManager __instance, string inUUID, ref Task<string> __result)
        {
            if (__instance._users._usersByUUID.ContainsKey(inUUID))
                __result = Task.Run<string>(new Func<string>(() =>
                {
                    return __instance._users._usersByUUID[inUUID].Name;
                }));
            __result = IdentityService.GetDisplayName(inUUID, NetworkManager.JWT);
            return false;
        }
    }
}