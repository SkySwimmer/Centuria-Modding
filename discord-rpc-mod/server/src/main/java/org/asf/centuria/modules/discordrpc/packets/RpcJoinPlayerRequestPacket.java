package org.asf.centuria.modules.discordrpc.packets;

import java.io.IOException;

import org.asf.centuria.data.XtReader;
import org.asf.centuria.data.XtWriter;
import org.asf.centuria.entities.players.Player;
import org.asf.centuria.networking.gameserver.GameServer;
import org.asf.centuria.feraltweaks.api.networking.IFeralTweaksPacket;
import org.asf.centuria.feraltweaks.api.networking.ServerMessenger;

public class RpcJoinPlayerRequestPacket implements IFeralTweaksPacket<RpcJoinPlayerRequestPacket> {

	public String playerID;
	public String partyID;
	public String secret;

	@Override
	public String id() {
		return "rpcjoin";
	}

	@Override
	public RpcJoinPlayerRequestPacket instantiate() {
		return new RpcJoinPlayerRequestPacket();
	}

	@Override
	public void parse(XtReader rd) throws IOException {
		playerID = rd.read();
		partyID = rd.read();
		secret = rd.read();
	}

	@Override
	public void build(XtWriter wr) throws IOException {
		wr.writeString(playerID);
		wr.writeString(partyID);
		wr.writeString(secret);
	}

	@Override
	public boolean handle(Player plr, ServerMessenger messenger) throws IOException {
		// Find player
		Player target = ((GameServer) plr.client.getServer()).getPlayer(playerID);
		if (target != null) {
			// Forward to player
			playerID = plr.account.getAccountID();
			messenger.sendPacket(this, target);
		} else {
			// Send error
			RpcJoinPlayerResultPacket res = new RpcJoinPlayerResultPacket();
			res.playerID = playerID;
			res.success = false;
			messenger.sendPacket(res, plr);
		}
		return true;
	}

}
