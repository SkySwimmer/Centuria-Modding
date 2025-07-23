using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Il2CppSystem.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using UnityEngine;
using FeralTweaks.Actions;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class DisplayNameManagerPatches
    {
        private static List<string> inProgressDisplayNames = new List<string>();
        private static List<string> fetchDisplayNames = new List<string>();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UserManager), "GetDisplayNameBatched")]
        public static bool GetDisplayNameBatched(UserManager __instance, string inUUID, ref Task<string> __result)
        {
            if (__instance._users._usersByUUID.ContainsKey(inUUID))
                __result = Task.Run<string>(new Func<string>(() =>
                {
                    return __instance._users._usersByUUID[inUUID].Name;
                }));
            __result = Task.Run<string>(new Func<string>(() =>
            {
                // Check in progress
                lock (fetchDisplayNames)
                {
                    if (!inProgressDisplayNames.Contains(inUUID))
                    {
                        if (!fetchDisplayNames.Contains(inUUID))
                        {
                            fetchDisplayNames.Add(inUUID);
                        }
                    }
                }

                // Wait for response
                long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                while (!__instance._users._usersByUUID.ContainsKey(inUUID))
                {
                    // Check
                    if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start >= 30000)
                        return null;

                    // Check in progress
                    if (!inProgressDisplayNames.Contains(inUUID) && !fetchDisplayNames.Contains(inUUID))
                        return null; // Failure

                    // Wait
                    Thread.Sleep(10);
                }

                return __instance._users._usersByUUID[inUUID].Name;
            }));
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UserManager), "LateUpdate")]
        public static void LateUpdate(UserManager __instance)
        {
            lock (inProgressDisplayNames)
            {
                lock (fetchDisplayNames)
                {
                    // Get names to fetch
                    string[] namesToFetch = fetchDisplayNames.ToArray();
                    if (namesToFetch.Count() != 0)
                    {
                        fetchDisplayNames.Clear();
                        inProgressDisplayNames.AddRange(namesToFetch);

                        // Fetch
                        Il2CppSystem.Collections.Generic.List<string> ls = new Il2CppSystem.Collections.Generic.List<string>();
                        foreach (string name in namesToFetch)
                            ls.Add(name);
                        Task<WWWResponse<IdentityDisplayNamesResponse>> task = IdentityService.GetDisplayNames(ls, NetworkManager.JWT);
                        TaskAwaiter<WWWResponse<IdentityDisplayNamesResponse>> awaiter = task.GetAwaiter();
                        FeralTweaksActions.Unity.Oneshot(() =>
                        {
                            // Wait for completion
                            if (!awaiter.IsCompleted)
                                return false;

                            // Get result
                            WWWResponse<IdentityDisplayNamesResponse> res = awaiter.GetResult();

                            // Remove found display names, re-queue non-found
                            if (res.IsSuccess)
                            {
                                // Add successful name fetches
                                foreach (IdentityDisplayNamesResponse.Identity id in res.value.found)
                                {
                                    if (!__instance._users._usersByUUID.ContainsKey(id.uuid))
                                    {
                                        // Add
                                        UserInfo uInfo = new UserInfo(id.uuid, id.display_name);
                                        __instance._users.Add(uInfo);
                                        __instance._users._usersByUUID[id.uuid] = uInfo;
                                    }
                                }

                                // Done
                                lock (inProgressDisplayNames)
                                {
                                    foreach (string name in namesToFetch)
                                        inProgressDisplayNames.Remove(name);
                                }
                            }
                            else
                            {
                                // Failure
                                lock (inProgressDisplayNames)
                                {
                                    foreach (string name in namesToFetch)
                                        inProgressDisplayNames.Remove(name);
                                }
                            }

                            // Done
                            return true;
                        });
                    }
                }
            }
        }
    }
}