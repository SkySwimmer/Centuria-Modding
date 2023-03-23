package org.asf.centuria.feraltweaks.api;

import org.asf.centuria.feraltweaks.api.networking.IFeralTweaksPacket;
import org.asf.centuria.feraltweaks.api.networking.IModNetworkHandler;
import org.asf.centuria.feraltweaks.api.networking.ServerMessenger;
import org.asf.centuria.modules.ICenturiaModule;

/**
 * 
 * FeralTweaks wrapper for server module, for defining client/server version
 * requirements and for implementing FeralTweaks packet handling.
 * 
 * @author Sky Swimmer
 *
 */
public abstract class FeralTweaksMod implements ICenturiaModule, IModNetworkHandler {

	private ServerMessenger messenger;

	public FeralTweaksMod() {
		messenger = new ServerMessenger(this);
	}

	/**
	 * Registers network packets
	 * 
	 * @param packet Packet to register
	 */
	protected void registerPacket(IFeralTweaksPacket<?> packet) {
		messenger.registerPacket(packet);
	}

	@Override
	public ServerMessenger getMessenger() {
		return messenger;
	}

}
