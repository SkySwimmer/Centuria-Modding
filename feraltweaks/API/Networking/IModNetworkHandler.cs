namespace FeralTweaks.Networking
{
    /// <summary>
    /// Interface used by FeralTweaks to bind mods to the networking system of Fer.al
    /// </summary>
    public interface IModNetworkHandler
    {
        /// <summary>
        /// Retrieves the network messenger used to handle and send packets for this mod
        /// </summary>
        /// <returns>ClientMessenger instance</returns>
        public ClientMessenger GetMessenger();
    }
}
