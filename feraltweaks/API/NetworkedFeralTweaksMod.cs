using FeralTweaks.Networking;

namespace FeralTweaks.Mods
{
    /// <summary>
    /// FeralTweaks mod with networking support
    /// </summary>
    public abstract class NetworkedFeralTweaksMod : FeralTweaksMod, IModNetworkHandler
    {
        private ClientMessenger messenger;

        /// <inheritdoc/>
        public NetworkedFeralTweaksMod()
        {
            messenger = new ClientMessenger(this);
        }

        /// <summary>
        /// Registers network packets
        /// </summary>
        /// <param name="packet">Packet to register</param>
        protected void RegisterPacket(IModNetworkPacket packet)
        {
            GetMessenger().RegisterPacket(packet);
        }

        /// <inheritdoc/>
        public ClientMessenger GetMessenger()
        {
            return messenger;
        }
    }
}