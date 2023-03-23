using HarmonyLib;
using Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class MessageRouterPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MessageRouter), "OnServerMessage")]
        public static bool OnServerMessage(INetMessageReader data)
        {
            return !FeralTweaks.HandlePacket(data.Cmd, data);
        }
    }
}
