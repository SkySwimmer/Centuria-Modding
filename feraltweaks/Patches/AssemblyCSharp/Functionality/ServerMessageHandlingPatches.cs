using HarmonyLib;
using Server;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class ServerMessageHandlingPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MessageRouter), "OnServerMessage")]
        public static bool OnServerMessage(INetMessageReader data)
        {
            return !FeralTweaksNetworkHandler.HandlePacket(data.Cmd, data);
        }
    }
}
