using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using BestHTTP;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class HttpRequestPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HTTPRequest), "Send")]
        public static void Send(ref HTTPRequest __instance)
        {
            // Find DNS overrides
            string host = __instance.Uri.Host;
            if (Plugin.HasDnsOverride(host))
            {
                // Override
                __instance.Uri = new Il2CppSystem.Uri(__instance.Uri.Scheme + "://" + Plugin.GetDnsOverride(host) + __instance.Uri.PathAndQuery);
                __instance.SetHeader("Host", host);
            }
        }
    }
}
