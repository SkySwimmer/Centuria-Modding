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
using System.Reflection;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class DisplayNameManagerPatches
    {
        private class DisplayNameState
        {
            private object _lock = new object();

            private Exception ex;
            public bool complete;
            public string result;

            public void Call(string result)
            {
                lock (_lock)
                {
                    this.result = result;
                    complete = true;
                }

                // Release
                lock (_lock)
                {
                    Monitor.PulseAll(_lock);
                }
            }

            public void Error(Exception ex)
            {
                lock (_lock)
                {
                    this.ex = ex;
                    result = null;
                    complete = true;
                }

                // Release
                lock (_lock)
                {
                    Monitor.PulseAll(_lock);
                }
            }

            public string Await()
            {
                lock (_lock)
                {
                    // Check exception
                    if (ex != null)
                        throw new TargetInvocationException("Target function has thrown an exception", ex); // Throw exception

                    // Check completed
                    if (complete)
                        return result;

                    // Wait
                    while (!complete)
                        Monitor.Wait(_lock);
                }

                // Check exception
                if (ex != null)
                    throw new TargetInvocationException("Target function has thrown an exception", ex); // Throw exception

                // Check completed
                if (complete)
                    return result;
                return null;
            }
        }

        private static List<string> displayNamesToFetch = new List<string>();
        private static Dictionary<string, DisplayNameState> inProgressDisplayNames = new Dictionary<string, DisplayNameState>();

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
                DisplayNameState state = null;
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
                                inProgressDisplayNames[inUUID] = new DisplayNameState();
                            }
                        }

                        // Get promise
                        state = inProgressDisplayNames[inUUID];
                        if (state.complete)
                            return state.result;
                    }
                }

                // Await promise
                return state.Await();
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
                                inProgressDisplayNames[id] = new DisplayNameState();
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
                Task<WWWResponse<IdentityDisplayNamesResponse>> tsk = IdentityService.GetDisplayNames(ls, NetworkManager.JWT);
                FeralTweaks.ScheduleDelayedAction(() =>
                {
                    // Wait
                    if (!tsk.IsCompleted)
                        return false;

                    // Get result
                    WWWResponse<IdentityDisplayNamesResponse> res = tsk.GetAwaiter().GetResult();

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
                                DisplayNameState promise = inProgressDisplayNames[id];
                                if (foundDisplayNames.ContainsKey(id))
                                    promise.Call(foundDisplayNames[id]);
                                else
                                {
                                    Debug.LogError("Display name request failed: " + id + ", server did not recognize the name!");
                                    promise.Error(new ArgumentException("ID not found: " + id));
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
                                inProgressDisplayNames[id].Error(new ArgumentException("The server responded with an error, display name request failed for " + id + "!"));
                                inProgressDisplayNames.Remove(id);
                            }
                        }
                    }

                    // Return
                    return true;
                });
            }
        }
    }
}