package org.asf.centuria.feraltweaks.api;

import java.util.HashMap;
import java.util.Map;

import org.asf.centuria.feraltweaks.api.networking.IFeralTweaksPacket;
import org.asf.centuria.feraltweaks.api.networking.IModNetworkHandler;
import org.asf.centuria.feraltweaks.api.networking.ServerMessenger;
import org.asf.centuria.feraltweaks.api.versioning.IModVersionHandler;
import org.asf.centuria.modules.ICenturiaModule;

/**
 * 
 * FeralTweaks wrapper for server module, for defining client/server version
 * requirements and for implementing FeralTweaks packet handling.
 * 
 * @author Sky Swimmer
 *
 */
public abstract class FeralTweaksMod implements ICenturiaModule, IModVersionHandler, IModNetworkHandler {

	private ServerMessenger messenger;
	private HashMap<String, String> handshakeRules = new HashMap<String, String>();

	public FeralTweaksMod() {
		messenger = new ServerMessenger(this);
	}

	@Override
	public Map<String, String> getClientModVersionRules() {
		return handshakeRules;
	}

	/**
	 * Adds a handshake version requirement for the current mod
	 * 
	 * @param versionCheck Version check string (start with '>=', '>', '&lt;',
	 *                     '&lt;=' or '!=' to define minimal/maximal versions,
	 *                     '&amp;' allows for multiple version rules, '||' functions
	 *                     as the OR operator, spaces are stripped during parsing)
	 */
	protected void addModHandshakeRequirementForSelf(String versionCheck) {
		handshakeRules.put(id(), versionCheck);
	}

	/**
	 * Adds a handshake requirement for the current mod to be present on the client
	 */
	protected void addModHandshakeRequirementForSelf() {
		handshakeRules.put(id(), "");
	}

	/**
	 * Adds a handshake requirement for the specified mod to be present on the
	 * client
	 * 
	 * @param id Mod ID that needs to be present
	 */
	protected void addModHandshakeRequirement(String id) {
		handshakeRules.put(id, "");
	}

	/**
	 * Adds a handshake version requirement for the specified mod
	 * 
	 * @param id           Mod ID that needs to be present
	 * @param versionCheck Version check string (start with '>=', '>', '&lt;',
	 *                     '&lt;=' or '!=' to define minimal/maximal versions,
	 *                     '&amp;' allows for multiple version rules, '||' functions
	 *                     as the OR operator, spaces are stripped during parsing)
	 */
	protected void addModHandshakeRequirement(String id, String versionCheck) {
		handshakeRules.put(id, versionCheck);
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
