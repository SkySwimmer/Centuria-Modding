package org.asf.centuria.feraltweaks.managers;

import java.util.ArrayList;
import java.util.HashMap;

import org.asf.centuria.Centuria;
import org.asf.centuria.accounts.CenturiaAccount;
import org.asf.centuria.entities.players.Player;
import org.asf.centuria.feraltweaks.FeralTweaksClientObject;
import org.asf.centuria.feraltweaks.networking.game.PlayerDisplayNameUpdatePacket;
import org.asf.centuria.networking.gameserver.GameServer;

public class PlayerNameManager {

	private static HashMap<String, String> playerNames = new HashMap<String, String>();

	/**
	 * Initializes the player name manager
	 */
	public static void initPlayerNameManager() {
		// Start username refresher
		Thread th = new Thread(() -> {
			while (true) {
				// Go through users
				ArrayList<String> users = new ArrayList<String>();
				if (Centuria.gameServer != null)
					for (Player plr : Centuria.gameServer.getPlayers()) {
						users.add(plr.account.getAccountID());
						synchronized (playerNames) {
							String nm = GameServer.getPlayerNameWithPrefix(plr.account);
							if (!playerNames.containsKey(plr.account.getAccountID())
									|| !playerNames.get(plr.account.getAccountID()).equals(nm)) {
								updateUser(plr);
								playerNames.put(plr.account.getAccountID(), nm);
							}
						}
					}
				synchronized (playerNames) {
					String[] names = playerNames.keySet().toArray(t -> new String[t]);
					for (String id : names) {
						if (!users.contains(id))
							playerNames.remove(id);
					}
				}
				try {
					Thread.sleep(30000);
				} catch (InterruptedException e) {
				}
			}
		}, "User update handler (FeralTweaks)");
		th.setDaemon(true);
		th.start();
	}

	/**
	 * Sends a username update
	 * 
	 * @param account Player to send a update of
	 */
	public static void updatePlayer(CenturiaAccount account) {
		// Send username update packet to all players
		PlayerDisplayNameUpdatePacket pkt = new PlayerDisplayNameUpdatePacket();
		pkt.id = account.getAccountID();
		pkt.name = GameServer.getPlayerNameWithPrefix(account);
		for (Player plr : Centuria.gameServer.getPlayers()) {
			if (plr != null) {
				if (plr.getObject(FeralTweaksClientObject.class) != null
						&& plr.getObject(FeralTweaksClientObject.class).isEnabled()) {
					// Send
					plr.client.sendPacket(pkt);
				}
			}
		}

		// Save
		synchronized (playerNames) {
			playerNames.put(pkt.id, pkt.name);
		}
	}

	private static void updateUser(Player plr) {
		// Send username update packet to all players
		PlayerDisplayNameUpdatePacket pkt = new PlayerDisplayNameUpdatePacket();
		pkt.id = plr.account.getAccountID();
		pkt.name = GameServer.getPlayerNameWithPrefix(plr.account);
		for (Player plr2 : Centuria.gameServer.getPlayers()) {
			if (plr2 != null) {
				if (plr2.getObject(FeralTweaksClientObject.class) != null
						&& plr2.getObject(FeralTweaksClientObject.class).isEnabled()) {
					// Send
					plr2.client.sendPacket(pkt);
				}
			}
		}
	}

}
