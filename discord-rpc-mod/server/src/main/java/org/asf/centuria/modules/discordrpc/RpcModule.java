package org.asf.centuria.modules.discordrpc;

import org.asf.centuria.feraltweaks.api.FeralTweaksMod;

public class RpcModule extends FeralTweaksMod {

	@Override
	public String id() {
		return "discordrpc";
	}

	@Override
	public String version() {
		return "1.0.0.0";
	}

	@Override
	public void init() {
		// Main init method

		// Handshake rules
		addModHandshakeRequirementForSelf(version());

		// Packets
		registerPacket(null);
	}

}
