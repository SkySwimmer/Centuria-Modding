package org.asf.centuria.modules.discordrpc.packets;

import java.io.IOException;

import org.asf.centuria.accounts.AccountManager;
import org.asf.centuria.accounts.CenturiaAccount;
import org.asf.centuria.data.XtReader;
import org.asf.centuria.data.XtWriter;
import org.asf.centuria.entities.generic.Quaternion;
import org.asf.centuria.entities.generic.Vector3;
import org.asf.centuria.entities.players.Player;
import org.asf.centuria.entities.uservars.UserVarValue;
import org.asf.centuria.modules.discordrpc.SecretContainer;
import org.asf.centuria.networking.gameserver.GameServer;
import org.asf.centuria.networking.smartfox.SmartfoxClient;
import org.asf.centuria.packets.xt.IXtPacket;
import org.asf.centuria.packets.xt.gameserver.object.ObjectUpdatePacket;
import org.asf.centuria.packets.xt.gameserver.room.RoomJoinPacket;
import org.asf.centuria.social.SocialManager;

public class RelationshipJumpToPlayerOverridePacket implements IXtPacket<RelationshipJumpToPlayerOverridePacket> {

	private static final String PACKET_ID = "rfjtr";

	public String accountID;
	public String tpSecret;

	@Override
	public RelationshipJumpToPlayerOverridePacket instantiate() {
		return new RelationshipJumpToPlayerOverridePacket();
	}

	@Override
	public String id() {
		return PACKET_ID;
	}

	@Override
	public void parse(XtReader reader) throws IOException {
		accountID = reader.read();
		if (reader.hasNext()) {
			// Check secret
			if (reader.readInt() == 1) {
				// TP secret
				tpSecret = reader.read();
			}
		}
	}

	@Override
	public void build(XtWriter writer) throws IOException {
	}

	@Override
	public boolean handle(SmartfoxClient client) throws IOException {
		if (tpSecret != null) {
			// Verify secret
			Player player = ((Player) client.container);
			Player otherPlayer = ((GameServer) client.getServer()).getPlayer(accountID);
			SecretContainer cont = client.getObject(SecretContainer.class);
			if (cont != null && cont.secret != null && System.currentTimeMillis() < cont.expiry
					&& cont.userID.equalsIgnoreCase(accountID) && cont.secret.equalsIgnoreCase(tpSecret)) {
				// Unset secret
				cont.secret = null;

				// Force-teleport
				if (!otherPlayer.room.equals(player.room)) {
					// Check sanc
					if (otherPlayer.levelType == 2 && otherPlayer.room.startsWith("sanctuary_")) {
						String sanctuaryOwner = otherPlayer.room.substring("sanctuary_".length());
						// Find owner
						CenturiaAccount sancOwner = AccountManager.getInstance().getAccount(sanctuaryOwner);
						if (!sancOwner.getSaveSpecificInventory().containsItem("201")) {
							Player plr2 = sancOwner.getOnlinePlayerInstance();
							if (plr2 != null)
								plr2.activeSanctuaryLook = sancOwner.getActiveSanctuaryLook();
						}

						// Check owner
						boolean isOwner = player.account.getAccountID().equals(sanctuaryOwner);

						if (!isOwner && (!player.overrideTpLocks || !player.hasModPerms)) {
							// Load privacy settings
							int privSetting = 0;
							UserVarValue val = sancOwner.getSaveSpecificInventory().getUserVarAccesor()
									.getPlayerVarValue("17544", 0);
							if (val != null)
								privSetting = val.value;

							// Verify access
							if (privSetting == 1 && !SocialManager.getInstance().getPlayerIsFollowing(
									otherPlayer.account.getAccountID(), player.account.getAccountID())) {
								XtWriter writer = new XtWriter();
								writer.writeString("rfjtr");
								writer.writeInt(-1); // data prefix
								writer.writeInt(0); // failure
								writer.writeString(""); // data suffix
								client.sendPacket(writer.encode());
								return true;
							} else if (privSetting == 2) {
								XtWriter writer = new XtWriter();
								writer.writeString("rfjtr");
								writer.writeInt(-1); // data prefix
								writer.writeInt(0); // failure
								writer.writeString(""); // data suffix
								client.sendPacket(writer.encode());
								return true;
							}
						}
					}

					XtWriter writer = new XtWriter();
					writer.writeString("rfjtr");
					writer.writeInt(-1); // data prefix
					writer.writeInt(1); // other world
					writer.writeString("");
					writer.writeString(""); // data suffix
					client.sendPacket(writer.encode());

					// Build room join
					RoomJoinPacket join = new RoomJoinPacket();
					join.levelType = otherPlayer.levelType;
					join.levelID = otherPlayer.levelID;
					join.roomIdentifier = otherPlayer.room;
					join.teleport = otherPlayer.account.getAccountID();
					player.teleportDestination = otherPlayer.account.getAccountID();
					player.targetPos = new Vector3(otherPlayer.lastPos.x, otherPlayer.lastPos.y, otherPlayer.lastPos.z);
					player.targetRot = new Quaternion(otherPlayer.lastRot.x, otherPlayer.lastRot.y,
							otherPlayer.lastRot.z, otherPlayer.lastRot.w);

					// Sync
					GameServer srv = (GameServer) client.getServer();
					for (Player plr2 : srv.getPlayers()) {
						if (plr2.room != null && player.room != null && player.room != null
								&& plr2.room.equals(player.room) && plr2 != player) {
							player.destroyAt(plr2);
						}
					}

					// Assign room
					player.roomReady = false;
					player.pendingLevelID = otherPlayer.levelID;
					player.pendingRoom = otherPlayer.room;
					player.levelType = otherPlayer.levelType;

					// Send packet
					client.sendPacket(join);
				} else {
					XtWriter writer = new XtWriter();
					writer.writeString("rfjtr");
					writer.writeInt(-1); // data prefix
					writer.writeInt(1); // other world
					writer.writeString("");
					writer.writeString(""); // data suffix
					client.sendPacket(writer.encode());

					// Same room, sync player
					ObjectUpdatePacket pkt = new ObjectUpdatePacket();
					pkt.action = 0;
					pkt.mode = 0; // InitPosition triggers teleport amims for FT clients, for vanilla it just moves
					pkt.targetUUID = player.account.getAccountID();
					pkt.position = otherPlayer.lastPos;
					pkt.rotation = otherPlayer.lastRot;
					pkt.heading = otherPlayer.lastHeading;
					pkt.time = System.currentTimeMillis() / 1000;

					// Broadcast sync
					GameServer srv = (GameServer) client.getServer();
					for (Player p : srv.getPlayers()) {
						if (p != player && p.room != null && p.room.equals(player.room)
								&& (!player.ghostMode || p.hasModPerms) && !p.disableSync) {
							p.client.sendPacket(pkt);
						}
					}
				}

				// Done handling
				return true;
			}
		}
		return false;
	}

}
