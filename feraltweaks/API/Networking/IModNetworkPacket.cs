using Server;

namespace FeralTweaks.Networking
{
    /// <summary>
    /// Packet abstract for mods to implement networking
    /// </summary>
    public interface IModNetworkPacket
    {
        /// <summary>
        /// Defines the packet ID
        /// </summary>
        public string ID { get; }

        /// <summary>
        /// Parses the packet
        /// </summary>
        /// <param name="reader">Packet content reader</param>
        public void Parse(INetMessageReader reader);

        /// <summary>
        /// Writes the packet
        /// </summary>
        /// <param name="writer">Packet content writer</param>
        public void Write(INetMessageWriter writer);

        /// <summary>
        /// Creates a new instance of the mod network packet
        /// </summary>
        /// <returns>New IModNetworkPacket instance</returns>
        public IModNetworkPacket CreateInstance();

        /// <summary>
        /// Handles the packet
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool Handle(ClientMessenger messenger);
    }
}
