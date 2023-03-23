package org.asf.centuria.feraltweaks.api.networking;

import java.io.IOException;
import java.util.ArrayList;

import org.asf.centuria.data.XtReader;
import org.asf.centuria.data.XtWriter;
import org.asf.centuria.entities.players.Player;
import org.asf.centuria.feraltweaks.gamepackets.FtModPacket;
import org.asf.centuria.modules.ICenturiaModule;

/**
 * 
 * Server-side version of the ClientMessenger, designed to let feraltweaks mods
 * communicate with their client counterpart with ease.
 * 
 * @author Sky Swimmer
 *
 */
public class ServerMessenger {

	private ICenturiaModule mod;
	private ArrayList<IFeralTweaksPacket<?>> packetRegistry = new ArrayList<IFeralTweaksPacket<?>>();

	/**
	 * Creates a new ServerMessenger instance
	 * 
	 * @param module Server module to connect this messenger to
	 */
	public ServerMessenger(ICenturiaModule module) {
		mod = module;
	}

	/**
	 * Registers network packets
	 * 
	 * @param packet Packet to register
	 */
	public void registerPacket(IFeralTweaksPacket<?> packet) {
		if (packetRegistry.stream().anyMatch(t -> t.id().equals(packet.id())))
			throw new IllegalArgumentException("Packet already registered: " + packet.id());
		packetRegistry.add(packet);
	}

	/**
	 * Handles packets
	 * 
	 * @param id     Packet ID
	 * @param reader Packet payload reader
	 * @param player Player that sent the packet
	 * @return True if handled, false otherwise
	 * @throws IOException If handling fails
	 */
	public boolean handlePacket(String id, XtReader reader, Player player) throws IOException {
		// Find packet
		for (IFeralTweaksPacket<?> pkt : packetRegistry) {
			if (pkt.id().equals(id)) {
				// Parse
				pkt = pkt.instantiate();
				pkt.parse(reader);
				return pkt.handle(player);
			}
		}

		// Not found
		return false;
	}

	/**
	 * Sends a network packet
	 * 
	 * @param packet Packet to send
	 * @param player Player to send the packet to
	 * @throws IOException If sending fails
	 */
	public void sendPacket(IFeralTweaksPacket<?> packet, Player player) throws IOException {
		// Find packet
		if (!packetRegistry.stream().anyMatch(t -> t.id().equals(packet.id())))
			throw new IllegalArgumentException("Packet not registered: " + packet.id());

		// Build packet
		FtModPacket pkt = new FtModPacket();
		pkt.modID = mod.id();
		pkt.pktID = packet.id();
		XtWriter wr = new XtWriter();
		packet.build(wr);
		pkt.payload = wr.encode().substring(4);
		pkt.payload = pkt.payload.substring(0, pkt.payload.lastIndexOf("%"));

		// Send
		if (player.client == null)
			throw new IOException("Not connected");
		player.client.sendPacket(pkt);
	}

}
