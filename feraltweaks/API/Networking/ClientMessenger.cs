using System;
using FeralTweaks.Mods;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Server;

namespace FeralTweaks.Networking
{
    /// <summary>
    /// Tool for mods to interact with networking code
    /// </summary>
    public class ClientMessenger
    {
        private FeralTweaksMod _mod;
        private List<IModNetworkPacket> _packetRegistry = new List<IModNetworkPacket>();

        /// <summary>
        /// Creates a new ClientMessenger instance
        /// </summary>
        /// <param name="mod">Mod connected to this messenger object</param>
        public ClientMessenger(FeralTweaksMod mod)
        {
            _mod = mod;
        }

        /// <summary>
        /// Registers network packets
        /// </summary>
        /// <param name="packet">Packet to register</param>
        public void RegisterPacket(IModNetworkPacket packet)
        {
            if (_packetRegistry.Any(t => t.ID == packet.ID))
                throw new ArgumentException("Packet already registered: " + packet.ID);
            _packetRegistry.Add(packet);
        }

        /// <summary>
        /// Handles packets
        /// </summary>
        /// <param name="id">Packet ID</param>
        /// <param name="reader">Packet payload reader</param>
        /// <returns>True if handled, false otherwise</returns>
        public bool HandlePacket(string id, INetMessageReader reader)
        {
            // Find packet
            foreach (IModNetworkPacket pkt in _packetRegistry)
            {
                if (pkt.ID == id)
                {
                    // Parse
                    IModNetworkPacket inst = pkt.CreateInstance();
                    inst.Parse(reader);
                    return inst.Handle(this);
                }
            }

            // Not found
            return false;
        }

        /// <summary>
        /// Sends a network packet
        /// </summary>
        /// <param name="packet">Packet to send</param>
        public void SendPacket(IModNetworkPacket packet)
        {
            // Check connection
            if (NetworkManager.instance == null || NetworkManager.instance._serverConnection == null || !NetworkManager.instance._serverConnection.IsConnected)
                throw new IOException("No server connection");

            // Find packet
            if (!_packetRegistry.Any(t => t.ID == packet.ID))
                throw new ArgumentException("Packet not registered: " + packet.ID);

            // Create writer
            XtWriter writer = new XtWriter(Server.XtCmd.MinigameMessage);
            writer.Cmd = "mod:" + _mod.ID;
            INetMessageWriter wr = writer.WriteString(packet.ID);

            // Write payload
            packet.Write(wr);

            try
            {
                // Send packet
                feraltweaks.FeralTweaks.ScheduleDelayedActionForUnity(() => NetworkManager.instance._serverConnection.Send(wr));
            }
            catch
            {
                if (NetworkManager.instance == null || NetworkManager.instance._serverConnection == null || !NetworkManager.instance._serverConnection.IsConnected)
                    throw new IOException("No server connection");
                throw;
            }
        }

        
    }
}
