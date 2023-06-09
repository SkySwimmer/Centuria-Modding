using FeralTweaks;
using FeralTweaks.Networking;
using Server;

namespace FeralDiscordRpcMod
{
    public class RpcJoinPlayerRequestPacket : IModNetworkPacket
    {
        public string ID => "rpcjoin";

        public string playerID;
        public string partyID;
        public string secret;

        public IModNetworkPacket CreateInstance()
        {
            return new RpcJoinPlayerRequestPacket();
        }

        public void Parse(INetMessageReader reader)
        {
            playerID = reader.ReadString();
            partyID = reader.ReadString();
            secret = reader.ReadString();
        }

        public void Write(INetMessageWriter writer)
        {
            writer.WriteString(playerID);
            writer.WriteString(partyID);
            writer.WriteString(secret);
        }

        public bool Handle(ClientMessenger messenger)
        {
            RpcMod rpcMod = FeralTweaksLoader.GetLoadedMod<RpcMod>();
            rpcMod.HandleJoinRequest(this);
            return true;
        }
    }
}