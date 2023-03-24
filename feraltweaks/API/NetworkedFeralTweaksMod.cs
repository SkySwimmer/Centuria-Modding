using FeralTweaks.Networking;
using FeralTweaks.Versioning;
using System.Collections.Generic;

namespace FeralTweaks.Mods
{
    /// <summary>
    /// FeralTweaks mod with networking support
    /// </summary>
    public abstract class NetworkedFeralTweaksMod : FeralTweaksMod, IModNetworkHandler, IModVersionHandler
    {
        private ClientMessenger messenger;
        private Dictionary<string, string> handshakeRules = new Dictionary<string, string>();

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

        /// <summary>
        /// Adds a handshake version requirement for the current mod
        /// </summary>
        /// <param name="versionCheck">Version check string (start with '>=', '>', '&lt;', '&lt;=' or '!=' to define minimal/maximal versions, '&amp;' allows for multiple version rules, '||' functions as the OR operator, spaces are stripped during parsing)</param>
        protected void AddModHandshakeRequirementForSelf(string versionCheck)
        {
            handshakeRules[ID] = versionCheck;
        }

        /// <summary>
        /// Adds a handshake requirement for the specified mod to be present on the server
        /// </summary>
        protected void AddModHandshakeRequirementForSelf()
        {
            handshakeRules[ID] = "";
        }

        /// <summary>
        /// Adds a handshake requirement for the specified mod to be present on the server
        /// </summary>
        /// <param name="id">Mod ID that needs to be present</param>
        protected void AddModHandshakeRequirement(string id)
        {
            handshakeRules[id] = "";
        }

        /// <summary>
        /// Adds a handshake version requirement for the specified mod
        /// </summary>
        /// <param name="id">Mod ID that needs to be present</param>
        /// <param name="versionCheck">Version check string (start with '>=', '>', '&lt;', '&lt;=' or '!=' to define minimal/maximal versions, '&amp;' allows for multiple version rules, '||' functions as the OR operator, spaces are stripped during parsing)</param>
        protected void AddModHandshakeRequirement(string id, string versionCheck)
        {
            handshakeRules[id] = versionCheck;
        }

        /// <inheritdoc/>
        public ClientMessenger GetMessenger()
        {
            return messenger;
        }

        /// <inheritdoc/>
        public Dictionary<string, string> GetServerModVersionRules()
        {
            return handshakeRules;
        }
    }
}