using FeralTweaks;
using FeralTweaks.Networking;
using Server;

namespace FeralDiscordRpcMod
{
    public class RpcJoinPlayerResultPacket : IModNetworkPacket
    {
        public string ID => "rpcjoinresult";

        public bool success;
        public string playerID;
        public string secret;

        public IModNetworkPacket CreateInstance()
        {
            return new RpcJoinPlayerResultPacket();
        }

        public void Parse(INetMessageReader reader)
        {
            success = reader.ReadSuccess();
            playerID = reader.ReadString();
            if (success)
                secret = reader.ReadString();
        }

        public void Write(INetMessageWriter writer)
        {
            writer.WriteString(success ? "true" : "false");
            writer.WriteString(playerID);
        }

        public bool Handle(ClientMessenger messenger)
        {
            RpcMod rpcMod = FeralTweaksLoader.GetLoadedMod<RpcMod>();
            rpcMod.HandleJoinResult(this);
            return true;
        }
    }
}