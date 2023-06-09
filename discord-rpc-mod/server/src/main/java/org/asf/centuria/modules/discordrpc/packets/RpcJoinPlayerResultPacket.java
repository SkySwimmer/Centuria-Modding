package org.asf.centuria.modules.discordrpc.packets;

import java.io.IOException;
import java.security.SecureRandom;

import org.asf.centuria.data.XtReader;
import org.asf.centuria.data.XtWriter;
import org.asf.centuria.entities.players.Player;
import org.asf.centuria.feraltweaks.api.networking.IFeralTweaksPacket;
import org.asf.centuria.feraltweaks.api.networking.ServerMessenger;
import org.asf.centuria.modules.discordrpc.SecretContainer;
import org.asf.centuria.networking.gameserver.GameServer;

public class RpcJoinPlayerResultPacket implements IFeralTweaksPacket<RpcJoinPlayerResultPacket> {

	private static SecureRandom rnd = new SecureRandom();

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
				// Build container
				SecretContainer cont = target.getObject(SecretContainer.class);
				if (cont == null) {
					cont = new SecretContainer();
					target.addObject(cont);
				}
				cont.expiry = System.currentTimeMillis() + (60 * 60 * 1000);
				cont.userLoginTimestamp = plr.account.getLastLoginTime();
				cont.userID = plr.account.getAccountID();

				// Generate secret
				String ch = "";
				for (int i = 0; i < 2048; i++)
					ch += (char) rnd.nextInt((int) '0', (int) 'Z');
				cont.secret = ch;

				// Set secret
				secret = cont.secret;
			}

			// Send packet
			messenger.sendPacket(this, target);
		}
		return true;
	}

}
