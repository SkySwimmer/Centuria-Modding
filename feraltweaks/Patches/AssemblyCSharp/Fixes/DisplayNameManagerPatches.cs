using FeralTweaks.Actions;
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

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class DisplayNameManagerPatches
    {
        private static List<string> displayNamesToFetch = new List<string>();
        private static Dictionary<string, FeralTweaksPromiseController<string>> inProgressDisplayNames = new Dictionary<string, FeralTweaksPromiseController<string>>();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UserManager), "GetDisplayNameBatched")]
        public static bool GetDisplayNameBatched(UserManager __instance, string inUUID, ref Task<string> __result)
        {
            // Already present, return
            if (__instance._users._usersByUUID.ContainsKey(inUUID))
            {
                __result = Task.Run<string>(new Func<string>(() =>
                {
                    return __instance._users._usersByUUID[inUUID].Name;
                }));
                return false;
            }

            // Request
            __result = Task.Run<string>(new Func<string>(() =>
            {
                // Check in progress
                FeralTweaksPromise<string> promise = null;
                lock (inProgressDisplayNames)
                {
                    lock (displayNamesToFetch)
                    {
                        // Check if present
                        if (!inProgressDisplayNames.ContainsKey(inUUID))
                        {
                            // Check if present
                            if (__instance._users._usersByUUID.ContainsKey(inUUID))
                                return __instance._users._usersByUUID[inUUID].Name;

                            // Add if needed
                            if (!displayNamesToFetch.Contains(inUUID))
                            {
                                // Add display name
                                displayNamesToFetch.Add(inUUID);

                                // Create promise
                                inProgressDisplayNames[inUUID] = FeralTweaksPromises.CreatePromise<string>();
                            }
                        }

                        // Get promise
                        promise = inProgressDisplayNames[inUUID].GetPromise();
                        if (promise.HasCompleted)
                            return promise.GetResult();
                    }
                }

                // Await promise
                return promise.AwaitResult();
            }));
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UserManager), "LateUpdate")]
        public static void LateUpdate(UserManager __instance)
        {
            string[] namesToFetch = new string[0];
            lock (inProgressDisplayNames)
            {
                lock (displayNamesToFetch)
                {
                    // Get names to fetch
                    namesToFetch = displayNamesToFetch.ToArray();
                    if (namesToFetch.Count() != 0)
                    {
                        // Clear list
                        displayNamesToFetch.Clear();

                        // Create promises
                        foreach (string id in namesToFetch)
                        {
                            if (!inProgressDisplayNames.ContainsKey(id))
                                inProgressDisplayNames[id] = FeralTweaksPromises.CreatePromise<string>();
                        }
                    }
                }
            }

            if (namesToFetch.Length != 0)
            {
                // Fetch display names
                // First, create the list
                Il2CppSystem.Collections.Generic.List<string> ls = new Il2CppSystem.Collections.Generic.List<string>();
                foreach (string name in namesToFetch)
                    ls.Add(name);

                // Call the server
                FeralTweaksPromise<WWWResponse<IdentityDisplayNamesResponse>> promise = FeralTweaksPromises.CreatePromiseFrom(IdentityService.GetDisplayNames(ls, NetworkManager.JWT));
                promise.OnComplete(res =>
                {
                    // Remove found display names, re-queue non-found
                    if (res.IsSuccess)
                    {
                        // Add successful name fetches
                        Dictionary<string, string> foundDisplayNames = new Dictionary<string, string>();
                        List<string> notFoundDisplayNames = new List<string>();
                        foreach (IdentityDisplayNamesResponse.Identity id in res.value.found)
                        {
                            // Set
                            foundDisplayNames[id.uuid] = id.display_name;

                            // Add to user manager
                            if (!__instance._users._usersByUUID.ContainsKey(id.uuid))
                            {
                                // Add
                                UserInfo uInfo = new UserInfo(id.uuid, id.display_name);
                                __instance._users.Add(uInfo);
                                __instance._users._usersByUUID[id.uuid] = uInfo;
                            }
                        }
                        foreach (IdentityDisplayNamesResponse.Identity id in res.value.not_found)
                        {
                            // Add
                            notFoundDisplayNames.Add(id.uuid);
                        }

                        // Done
                        lock (inProgressDisplayNames)
                        {
                            foreach (string id in namesToFetch)
                            {
                                FeralTweaksPromiseController<string> promise = inProgressDisplayNames[id];
                                if (foundDisplayNames.ContainsKey(id))
                                    promise.CallComplete(foundDisplayNames[id]);
                                else
                                {
                                    Debug.LogError("Display name request failed: " + id + ", server did not recognize the name!");
                                    promise.CallError(new ArgumentException("ID not found: " + id));
                                }
                                inProgressDisplayNames.Remove(id);
                            }
                        }
                    }
                    else
                    {
                        // Failure
                        lock (inProgressDisplayNames)
                        {
                            foreach (string id in namesToFetch)
                            {
                                inProgressDisplayNames[id].CallError(new ArgumentException("The server responded with an error, display name request failed for " + id + "!"));
                                inProgressDisplayNames.Remove(id);
                            }
                        }
                    }
                });
            }
        }
    }
}