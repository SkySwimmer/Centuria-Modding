package org.asf.centuria.modules.discordrpc.packets;

import java.io.IOException;

import org.asf.centuria.data.XtReader;
import org.asf.centuria.data.XtWriter;
import org.asf.centuria.entities.players.Player;
import org.asf.centuria.feraltweaks.api.networking.IFeralTweaksPacket;
import org.asf.centuria.feraltweaks.api.networking.ServerMessenger;
import org.asf.centuria.networking.gameserver.GameServer;

public class RpcJoinPlayerResultPacket implements IFeralTweaksPacket<RpcJoinPlayerResultPacket> {

	public boolean success;
	public String playerID;
	public String targetPlayerID;
	public String secret;

	@Override
	public String id() {
		return "rpcjoinresult";
	}

	@Override
	public RpcJoinPlayerResultPacket instantiate() {
		return new RpcJoinPlayerResultPacket();
	}

	@Override
	public void parse(XtReader rd) throws IOException {
		// Incoming only expects a boolean and player id
		success = rd.readBoolean();
		targetPlayerID = rd.read();
	}

	@Override
	public void build(XtWriter wr) throws IOException {
		wr.writeBoolean(success);
		wr.writeString(playerID);
		if (success)
			wr.writeString(secret);
	}

	@Override
	public boolean handle(Player plr, ServerMessenger messenger) throws IOException {
		// Forward to other player
		Player target = ((GameServer) plr.client.getServer()).getPlayer(targetPlayerID);
		if (target != null) {
			// Set player id
			playerID = plr.account.getAccountID();

			// Generate tp secret
			if (success) {
				// TODO: generate teleport secret
				secret = "12345";
			}

			// Send packet
			messenger.sendPacket(this, target);
		}
		return true;
	}

}
