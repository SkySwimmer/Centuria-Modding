package org.asf.centuria.modules.discordrpc;

import org.asf.centuria.feraltweaks.api.FeralTweaksMod;
import org.asf.centuria.modules.events.servers.GameServerStartupEvent;
import org.asf.centuria.modules.eventbus.EventListener;
import org.asf.centuria.modules.discordrpc.packets.*;

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
		registerPacket(new RpcJoinPlayerRequestPacket());
		registerPacket(new RpcJoinPlayerResultPacket());
	}

	@EventListener
	public void gameStart(GameServerStartupEvent ev) {
		ev.registerPacket(new RelationshipJumpToPlayerOverridePacket());
	}

}
