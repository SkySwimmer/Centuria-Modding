package org.asf.centuria.feraltweaks;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.util.ArrayList;
import java.util.HashMap;

import org.asf.centuria.Centuria;
import org.asf.centuria.accounts.AccountManager;
import org.asf.centuria.accounts.CenturiaAccount;
import org.asf.centuria.accounts.SaveMode;
import org.asf.centuria.data.XtReader;
import org.asf.centuria.dms.DMManager;
import org.asf.centuria.entities.players.Player;
import org.asf.centuria.feraltweaks.chatpackets.MarkConvoReadPacket;
import org.asf.centuria.feraltweaks.gamepackets.DisconnectPacket;
import org.asf.centuria.feraltweaks.gamepackets.ErrorPopupPacket;
import org.asf.centuria.feraltweaks.gamepackets.FtModPacket;
import org.asf.centuria.feraltweaks.gamepackets.NotificationPacket;
import org.asf.centuria.feraltweaks.gamepackets.OkPopupPacket;
import org.asf.centuria.feraltweaks.gamepackets.YesNoPopupPacket;
import org.asf.centuria.feraltweaks.http.DataProcessor;
import org.asf.centuria.modules.ICenturiaModule;
import org.asf.centuria.modules.eventbus.EventBus;
import org.asf.centuria.modules.eventbus.EventListener;
import org.asf.centuria.modules.events.accounts.AccountDisconnectEvent;
import org.asf.centuria.modules.events.accounts.AccountPreloginEvent;
import org.asf.centuria.modules.events.accounts.MiscModerationEvent;
import org.asf.centuria.modules.events.chat.ChatLoginEvent;
import org.asf.centuria.modules.events.chat.ChatMessageBroadcastEvent;
import org.asf.centuria.modules.events.chatcommands.ChatCommandEvent;
import org.asf.centuria.modules.events.chatcommands.ModuleCommandSyntaxListEvent;
import org.asf.centuria.modules.events.servers.APIServerStartupEvent;
import org.asf.centuria.modules.events.servers.ChatServerStartupEvent;
import org.asf.centuria.modules.events.servers.GameServerStartupEvent;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;

/**
 * 
 * FeralTweaks Server Module
 * 
 * @author Sky Swimmer
 *
 */
public class FeralTweaksModule implements ICenturiaModule {

	/**
	 * FeralTweaks Protocol Version
	 */
	public static int FT_VERSION = 1;
	public String ftUnsupportedErrorMessage;
	public String ftOutdatedErrorMessage;
	public String modDataVersion;
	public boolean enableByDefault;
	public boolean requireManagedSaveData;
	public boolean preventNonFTClients;
	public String ftDataPath;
	public String ftCachePath;
	public String upstreamServerJsonURL;

	public HashMap<String, Boolean> replicatingObjects = new HashMap<String, Boolean>();

	@Override
	public String id() {
		return "feraltweaks";
	}

	@Override
	public String version() {
		return "beta-1.0.0-b3";
	}

	@Override
	public void init() {
		// Check config
		File configFile = new File("feraltweaks.conf");
		if (!configFile.exists()) {
			// Write config
			try {
				Files.writeString(configFile.toPath(), "enable-by-default=false\n" + "prevent-non-ft-clients=true\n"
						+ "data-path=feraltweaks/content\ncache-path=feraltweaks/cache\nupstream-server-json=https://emuferal.ddns.net:6970/data/server.json\n"
						+ "error-unauthorized=\nFeralTweaks is presently not enabled on your account!\\n\\nPlease uninstall the client modding project, contact the server administrator if you believe this is an error.\n"
						+ "error-outdated=Incompatible client!\\nYour client is currently out of date, restart the game to update the client mods.\n"
						+ "mod-data-version=1\n");
			} catch (IOException e) {
				throw new RuntimeException(e);
			}
		}

		// Read config
		HashMap<String, String> properties = new HashMap<String, String>();
		try {
			for (String line : Files.readAllLines(configFile.toPath())) {
				if (line.startsWith("#") || line.isBlank())
					continue;
				String key = line;
				String value = "";
				if (key.contains("=")) {
					value = key.substring(key.indexOf("=") + 1);
					key = key.substring(0, key.indexOf("="));
					if (key.startsWith("set-replication-for:")) {
						replicatingObjects.put(key.substring("set-replication-for:".length()),
								value.equalsIgnoreCase("enabled"));
					}
				}
				properties.put(key, value);
			}
		} catch (IOException e) {
			throw new RuntimeException(e);
		}
		enableByDefault = properties.getOrDefault("enable-by-default", "false").equalsIgnoreCase("true");
		preventNonFTClients = properties.getOrDefault("prevent-non-ft-clients", "false").equalsIgnoreCase("true");
		ftDataPath = properties.getOrDefault("data-path", "feraltweaks/content");
		ftCachePath = properties.getOrDefault("cache-path", "feraltweaks/cache");
		upstreamServerJsonURL = properties.getOrDefault("upstream-server-json",
				"https://emuferal.ddns.net:6970/data/server.json");
		ftOutdatedErrorMessage = properties.getOrDefault("error-outdated",
				"\nIncompatible client!\nYour client is currently out of date, restart the game to update the client mods.")
				.replaceAll("\\\\n", "\n");
		ftUnsupportedErrorMessage = properties.getOrDefault("error-unauthorized",
				"FeralTweaks is presently not enabled on your account!\\n\\nPlease uninstall the client modding project, contact the server administrator if you believe this is an error.")
				.replaceAll("\\\\n", "\n");
		modDataVersion = properties.getOrDefault("mod-data-version", "1");
		requireManagedSaveData = properties.getOrDefault("require-managed-saves", "false").equalsIgnoreCase("true");

		// Create data folders
		if (!new File(ftDataPath + "/feraltweaks/chartpatches").exists())
			new File(ftDataPath + "/feraltweaks/chartpatches").mkdirs();
		if (!new File(ftDataPath + "/clientmods/assemblies").exists())
			new File(ftDataPath + "/clientmods/assemblies").mkdirs();
		if (!new File(ftDataPath + "/clientmods/assets").exists())
			new File(ftDataPath + "/clientmods/assets").mkdirs();
	}

	@EventListener
	public void disconnect(AccountDisconnectEvent event) {
		if (event.getAccount().getOnlinePlayerInstance() != null
				&& event.getAccount().getOnlinePlayerInstance().getObject(FeralTweaksClientObject.class) != null
				&& event.getAccount().getOnlinePlayerInstance().getObject(FeralTweaksClientObject.class).isEnabled()) {
			DisconnectPacket pkt = new DisconnectPacket();
			pkt.button = "Quit";
			pkt.title = "Disconnected";
			switch (event.getType()) {
			case BANNED:
				pkt.title = "Banned";
				pkt.message = "Your account was suspended and has been disconnected.";
				if (event.getReason() != null)
					pkt.message += "\n\nReason: " + event.getReason();
				break;
			case KICKED:
				pkt.message = "You were disconnected from the server.";
				if (event.getReason() != null)
					pkt.message += "\n\nReason: " + event.getReason();
				break;
			case MAINTENANCE:
				pkt.title = "Server Closed";
				pkt.message = "The server has been placed under maintenance, hope to be back soon!";
				break;
			case SERVER_SHUTDOWN:
				pkt.title = "Server Closed";
				pkt.message = "The server has been temporarily shut down, hope to be back soon!";
				break;
			case UNKNOWN:
				pkt.message = "Disconnected from server due to an unknown error.";
				break;
			}
			event.getAccount().getOnlinePlayerInstance().client.sendPacket(pkt);
			try {
				// Give time to disconnect
				Thread.sleep(300);
			} catch (InterruptedException e) {
			}
		}
	}

	@EventListener
	public void gameServerStartup(GameServerStartupEvent event) {
		event.registerPacket(new YesNoPopupPacket());
		event.registerPacket(new FtModPacket());
	}

	@EventListener
	public void chatMessageSent(ChatMessageBroadcastEvent event) {
		// Check room
		if (event.getClient().isInRoom(event.getConversationId())
				&& event.getClient().isRoomPrivate(event.getConversationId())) {
			// Find dm participants
			if (DMManager.getInstance().dmExists(event.getConversationId())) {
				String[] participants = DMManager.getInstance().getDMParticipants(event.getConversationId());
				for (String accId : participants) {
					// Find account
					CenturiaAccount acc = AccountManager.getInstance().getAccount(accId);
					if (acc == null)
						continue; // Wtf-

					// Check if online
					if (acc.getOnlinePlayerInstance() == null) {
						// Offline, check feraltweaks support
						if (enableByDefault || acc.getSaveSharedInventory().containsItem("feraltweaks")
								|| acc.getSaveSpecificInventory().containsItem("feraltweaks")) {
							// Add to unread history
							JsonArray arr = new JsonArray();

							// Load unreads
							if (acc.getSaveSharedInventory().containsItem("unreadconversations"))
								arr = acc.getSaveSharedInventory().getItem("unreadconversations").getAsJsonArray();

							// Check if present
							boolean found = false;
							for (JsonElement ele : arr) {
								if (ele.getAsString().equals(event.getConversationId())) {
									found = true;
									break;
								}
							}
							if (!found) {
								// Add unread
								arr.add(event.getConversationId());

								// Save
								acc.getSaveSharedInventory().setItem("unreadconversations", arr);
							}
						}
					}
				}
			}
		}
	}

	@EventListener
	public void apiStartup(APIServerStartupEvent event) {
		// Register custom processors
		event.getServer().registerProcessor(new DataProcessor());
	}

	@EventListener
	public void chatStartup(ChatServerStartupEvent event) {
		// Register custom chat packets
		event.registerPacket(new MarkConvoReadPacket());
	}

	@EventListener
	public void handleGamePrelogin(AccountPreloginEvent event) {
		// Handshake feraltweaks
		try {
			// Parse nick variable
			boolean feralTweaks = false;
			XtReader rd = new XtReader(event.getAuthPacket().nick);
			while (rd.hasNext()) {
				String entry = rd.read();
				if (entry.equals("feraltweaks")) {
					// Verify the chain
					if (!rd.hasNext())
						break;
					String status = rd.read();
					if (!status.equals("enabled"))
						continue;
					if (!rd.hasNext())
						break; // Invalid
					int protVer = rd.readInt();
					if (!rd.hasNext())
						break; // Invalid
					String ver = rd.read();
					if (!rd.hasNext())
						break; // Invalid
					String dataVer = rd.read();
					if (rd.hasNext())
						break; // Invalid

					// Check handshake
					if (protVer != FT_VERSION
							|| !dataVer.equals(modDataVersion + "/" + Centuria.SERVER_UPDATE_VERSION)) {
						// Handshake failure
						event.getLoginResponseParameters().addProperty("errorMessage", ftOutdatedErrorMessage);
						event.setStatus(-26);
						return;
					}

					// Check if FT is enabled
					if (!enableByDefault && !event.getAccount().getSaveSharedInventory().containsItem("feraltweaks")
							&& !event.getAccount().getSaveSpecificInventory().containsItem("feraltweaks")) {
						// Handshake failure
						event.setStatus(-26);
						event.getLoginResponseParameters().addProperty("errorMessage", ftUnsupportedErrorMessage);
						return;
					}

					// Check managed saves if needed
					if (requireManagedSaveData && event.getAccount().getSaveMode() != SaveMode.MANAGED) {
						// Handshake failure
						event.setStatus(-26);
						event.getLoginResponseParameters().addProperty("errorMessage",
								"Please migrate to managed save data before continuing, you can do this from the account panel.");
						return;
					}

					// Handshake success
					event.getClient().addObject(new FeralTweaksClientObject(true, ver));
					feralTweaks = true;
					break;
				}
			}

			if (!feralTweaks) {
				// No feraltweaks
				if ((enableByDefault || event.getAccount().getSaveSharedInventory().containsItem("feraltweaks")
						|| event.getAccount().getSaveSpecificInventory().containsItem("feraltweaks"))
						&& preventNonFTClients) {
					// Requires feraltweaks and its not installed/active
					// Set to error
					event.setStatus(-24);
					return;
				}
				event.getClient().addObject(new FeralTweaksClientObject(false, null));
			}
		} catch (Exception e) {
			// Uhh what
			event.setStatus(-1);
		}
	}

	@EventListener
	public void handleChatPrelogin(ChatLoginEvent event) {
		// Handshake feraltweaks
		if (event.getLoginRequest().has("feraltweaks")
				&& event.getLoginRequest().get("feraltweaks").getAsString().equals("enabled")) {
			// Handle FeralTweaks hanshake
			if (event.getLoginRequest().get("feraltweaks_protocol").getAsInt() != FT_VERSION) {
				// Handshake failure
				event.cancel();
				return;
			}

			// Check if FT is enabled
			if (!enableByDefault && !event.getAccount().getSaveSharedInventory().containsItem("feraltweaks")
					&& !event.getAccount().getSaveSpecificInventory().containsItem("feraltweaks")) {
				// Handshake failure
				event.cancel();
				return;
			}

			// Handshake success
			event.getClient().addObject(new FeralTweaksClientObject(true,
					event.getLoginRequest().get("feraltweaks_version").getAsString()));

			// Prepare to send unreads
			JsonObject pkt = new JsonObject();
			pkt.addProperty("eventId", "feraltweaks.unreadconversations");
			JsonArray arr = new JsonArray();

			// Load unreads
			if (event.getAccount().getSaveSharedInventory().containsItem("unreadconversations")) {
				arr = event.getAccount().getSaveSharedInventory().getItem("unreadconversations").getAsJsonArray();

				// Remove nonexistent items
				ArrayList<JsonElement> toRemove = new ArrayList<JsonElement>();
				for (JsonElement ele : arr) {
					if (!DMManager.getInstance().dmExists(ele.getAsString()))
						toRemove.add(ele);
				}
				for (JsonElement id : toRemove)
					arr.remove(id);

				// Save if needed
				if (toRemove.size() != 0)
					event.getAccount().getSaveSharedInventory().setItem("unreadconversations", arr);
			} else
				event.getAccount().getSaveSharedInventory().setItem("unreadconversations", arr);

			// Send packet
			pkt.add("conversations", arr);
			event.getClient().sendPacket(pkt);
		} else {
			if ((enableByDefault || event.getAccount().getSaveSharedInventory().containsItem("feraltweaks")
					|| event.getAccount().getSaveSpecificInventory().containsItem("feraltweaks"))
					&& preventNonFTClients) {
				// Unsupported
				event.cancel();
				return;
			}
			event.getClient().addObject(new FeralTweaksClientObject(false, null));
		}
	}

	@EventListener
	public void registerCommands(ModuleCommandSyntaxListEvent event) {
		if (event.hasPermission("moderator")) {
			event.addCommandSyntaxMessage("announce \"<message>\" [\"<title>\"]");
			event.addCommandSyntaxMessage("warn \"<player>\" \"<message>\" [\"<title>\"]");
			event.addCommandSyntaxMessage(
					"request \"<player>\" \"<message>\" [\"<title>\"] [\"<yes-button>\"] [\"<no-button>\"]");
			event.addCommandSyntaxMessage("notify \"<player>\" \"<message>\"");
		}
	}

	@EventListener
	public void runCommand(ChatCommandEvent event) {
		if (event.hasPermission("moderator")) {
			switch (event.getCommandID().toLowerCase()) {
			case "announce": {
				// Handle announcement commands

				event.setHandled();
				if (event.getCommandArguments().length == 0) {
					event.respond("Error: missing argument: message");
					return;
				}

				// Send announcement
				for (Player plr : Centuria.gameServer.getPlayers()) {
					if (plr != null) {
						if (plr.getObject(FeralTweaksClientObject.class) == null
								|| !plr.getObject(FeralTweaksClientObject.class).isEnabled()) {
							// Send as DM
							Centuria.systemMessage(plr, "Server announcement!\n" + event.getCommandArguments()[0],
									true);
						} else {
							// Show popup
							OkPopupPacket pkt = new OkPopupPacket();
							pkt.title = "Server Announcement";
							pkt.message = event.getCommandArguments()[0];
							if (event.getCommandArguments().length >= 2)
								pkt.title = event.getCommandArguments()[1];
							plr.client.sendPacket(pkt);
						}
					}
				}
				event.respond(
						"Sent announcement, note that for some this might be sent as a DM as not every client supports FeralTweaks.");

				// Log
				HashMap<String, String> details = new HashMap<String, String>();
				details.put("Message", event.getCommandArguments()[0]);
				EventBus.getInstance().dispatchEvent(new MiscModerationEvent("announce", "Made a server announcement",
						details, event.getClient().getPlayer().getAccountID(), null));

				break;
			}

			case "request": {
				// Handle request commands

				event.setHandled();
				if (event.getCommandArguments().length == 0) {
					event.respond("Error: missing argument: player");
					return;
				}
				if (event.getCommandArguments().length == 1) {
					event.respond("Error: missing argument: message");
					return;
				}

				// Find player
				String uuid = AccountManager.getInstance().getUserByDisplayName(event.getCommandArguments()[0]);
				if (uuid == null) {
					// Player not found
					event.respond("Specified account could not be located");
					return;
				}
				CenturiaAccount acc = AccountManager.getInstance().getAccount(uuid);
				if (acc == null) {
					// Player not found
					event.respond("Specified account could not be located");
					return;
				}
				Player plr = acc.getOnlinePlayerInstance();
				if (plr == null) {
					// Player offline
					event.respond(
							"Player is offline, cannot send popups to them unless they are ingame, please use DMs instead.");
					return;
				}

				// Check support
				if (plr.getObject(FeralTweaksClientObject.class) == null
						|| !plr.getObject(FeralTweaksClientObject.class).isEnabled()) {
					// Send as DM
					event.respond(
							"Error: the given player has no client mods that support this command, cannot send a popup.");
					return;
				} else {
				}

				// Send popup
				YesNoPopupPacket pkt = new YesNoPopupPacket();
				pkt.title = event.getClient().getPlayer().getDisplayName() + " asks";
				pkt.message = event.getCommandArguments()[1];
				pkt.yesButton = "Yes";
				pkt.noButton = "No";
				pkt.id = "playersent/" + event.getAccount().getAccountID();
				if (event.getCommandArguments().length >= 3)
					pkt.title = event.getCommandArguments()[2];
				if (event.getCommandArguments().length >= 4)
					pkt.yesButton = event.getCommandArguments()[3];
				if (event.getCommandArguments().length >= 5)
					pkt.noButton = event.getCommandArguments()[4];
				plr.client.sendPacket(pkt);
				event.respond("Request sent.");

				break;
			}

			case "warn": {
				// Handle warning commands

				event.setHandled();
				if (event.getCommandArguments().length == 0) {
					event.respond("Error: missing argument: player");
					return;
				}
				if (event.getCommandArguments().length == 1) {
					event.respond("Error: missing argument: message");
					return;
				}

				// Find player
				String uuid = AccountManager.getInstance().getUserByDisplayName(event.getCommandArguments()[0]);
				if (uuid == null) {
					// Player not found
					event.respond("Specified account could not be located");
					return;
				}
				CenturiaAccount acc = AccountManager.getInstance().getAccount(uuid);
				if (acc == null) {
					// Player not found
					event.respond("Specified account could not be located");
					return;
				}
				Player plr = acc.getOnlinePlayerInstance();
				if (plr == null) {
					// Player offline
					event.respond(
							"Player is offline, cannot warn them unless they are ingame, please use DMs instead.");
					return;
				}

				// Warn
				if (plr.getObject(FeralTweaksClientObject.class) == null
						|| !plr.getObject(FeralTweaksClientObject.class).isEnabled()) {
					// Send as DM
					Centuria.systemMessage(plr, "You have been warned!\n" + event.getCommandArguments()[1], true);
					event.respond(
							"Sent DM as system as the user does not have feraltweaks active, cannot display popups without it.");
				} else {
					// Show popup
					ErrorPopupPacket pkt = new ErrorPopupPacket();
					pkt.title = "You have been warned!";
					pkt.message = event.getCommandArguments()[1];
					if (event.getCommandArguments().length >= 3)
						pkt.title = event.getCommandArguments()[2];
					plr.client.sendPacket(pkt);
					event.respond("Warning sent.");
				}

				// Log
				HashMap<String, String> details = new HashMap<String, String>();
				details.put("Message", event.getCommandArguments()[1]);
				EventBus.getInstance().dispatchEvent(new MiscModerationEvent("request", "Issued a warning", details,
						event.getClient().getPlayer().getAccountID(), plr.account));

				break;
			}

			case "notify": {
				// Handle warning commands

				event.setHandled();
				if (event.getCommandArguments().length == 0) {
					event.respond("Error: missing argument: player");
					return;
				}
				if (event.getCommandArguments().length == 1) {
					event.respond("Error: missing argument: message");
					return;
				}

				// Find player
				String uuid = AccountManager.getInstance().getUserByDisplayName(event.getCommandArguments()[0]);
				if (uuid == null) {
					// Player not found
					event.respond("Specified account could not be located");
					return;
				}
				CenturiaAccount acc = AccountManager.getInstance().getAccount(uuid);
				if (acc == null) {
					// Player not found
					event.respond("Specified account could not be located");
					return;
				}
				Player plr = acc.getOnlinePlayerInstance();
				if (plr == null) {
					// Player offline
					event.respond(
							"Player is offline, cannot notify them unless they are ingame, please use DMs instead.");
					return;
				}

				// Check support
				if (plr.getObject(FeralTweaksClientObject.class) == null
						|| !plr.getObject(FeralTweaksClientObject.class).isEnabled()) {
					// Send as DM
					event.respond(
							"Error: the given player has no client mods that support this command, cannot send a popup.");
					return;
				} else {
				}

				// Show popup
				NotificationPacket pkt = new NotificationPacket();
				pkt.message = event.getCommandArguments()[1];
				plr.client.sendPacket(pkt);
				event.respond("Notification sent.");

				break;
			}
			}
		}
	}
}
