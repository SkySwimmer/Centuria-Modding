package org.asf.centuria.feraltweaks;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.util.ArrayList;
import java.util.HashMap;

import org.asf.centuria.accounts.AccountManager;
import org.asf.centuria.accounts.CenturiaAccount;
import org.asf.centuria.data.XtReader;
import org.asf.centuria.dms.DMManager;
import org.asf.centuria.feraltweaks.chatpackets.MarkConvoReadPacket;
import org.asf.centuria.feraltweaks.http.CdnProcessor;
import org.asf.centuria.modules.ICenturiaModule;
import org.asf.centuria.modules.eventbus.EventListener;
import org.asf.centuria.modules.events.accounts.AccountPreloginEvent;
import org.asf.centuria.modules.events.chat.ChatLoginEvent;
import org.asf.centuria.modules.events.chat.ChatMessageBroadcastEvent;
import org.asf.centuria.modules.events.servers.APIServerStartupEvent;
import org.asf.centuria.modules.events.servers.ChatServerStartupEvent;

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
	public boolean enableByDefault;
	public boolean preventNonFTClients;
	public String ftCdnPath;

	@Override
	public String id() {
		return "feraltweaks";
	}

	@Override
	public String version() {
		return "beta-1.0.0";
	}

	@Override
	public void init() {
		// Check config
		File configFile = new File("feraltweaks.conf");
		if (!configFile.exists()) {
			// Write config
			try {
				Files.writeString(configFile.toPath(), "enable-by-default=false\n" + "prevent-non-ft-clients=true\n"
						+ "cdn-path=feraltweaks/content\n"
						+ "error-unauthorized=\nFeralTweaks is presently not enabled on your account!\\n\\nPlease uninstall the client modding project, contact the server administrator if you believe this is an error.\n"
						+ "error-outdated=Incompatible client!\\nYour client is currently out of date, restart the game to update the client mods.");
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
				}
				properties.put(key, value);
			}
		} catch (IOException e) {
			throw new RuntimeException(e);
		}
		enableByDefault = properties.getOrDefault("enable-by-default", "false").equalsIgnoreCase("true");
		preventNonFTClients = properties.getOrDefault("prevent-non-ft-clients", "false").equalsIgnoreCase("true");
		ftCdnPath = properties.getOrDefault("cdn-path", "feraltweaks/content");
		ftOutdatedErrorMessage = properties.getOrDefault("error-outdated",
				"\nIncompatible client!\nYour client is currently out of date, restart the game to update the client mods.")
				.replaceAll("\\\\n", "\n");
		ftUnsupportedErrorMessage = properties.getOrDefault("error-unauthorized",
				"FeralTweaks is presently not enabled on your account!\\n\\nPlease uninstall the client modding project, contact the server administrator if you believe this is an error.")
				.replaceAll("\\\\n", "\n");

		// Create CDN path
		if (!new File(ftCdnPath + "/feraltweaks/chartpatches").exists())
			new File(ftCdnPath + "/feraltweaks/chartpatches").mkdirs();
		if (!new File(ftCdnPath + "/clientmods/assemblies").exists())
			new File(ftCdnPath + "/clientmods/assemblies").mkdirs();
		if (!new File(ftCdnPath + "/clientmods/assets").exists())
			new File(ftCdnPath + "/clientmods/assets").mkdirs();
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
						if (enableByDefault || acc.getSaveSharedInventory().containsItem("feraltweaks")) {
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
		event.getServer().registerProcessor(new CdnProcessor());
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
					if (rd.hasNext())
						break; // Invalid

					// Check handshake
					if (protVer != FT_VERSION) {
						// Handshake failure
						event.getLoginResponseParameters().addProperty("errorMessage", ftOutdatedErrorMessage);
						event.setStatus(-26);
						return;
					}

					// Check if FT is enabled
					if (!enableByDefault && !event.getAccount().getSaveSharedInventory().containsItem("feraltweaks")) {
						// Handshake failure
						event.setStatus(-26);
						event.getLoginResponseParameters().addProperty("errorMessage", ftUnsupportedErrorMessage);
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
				if ((enableByDefault || event.getAccount().getSaveSharedInventory().containsItem("feraltweaks"))
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
			if (!enableByDefault && !event.getAccount().getSaveSharedInventory().containsItem("feraltweaks")) {
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
			if ((enableByDefault || event.getAccount().getSaveSharedInventory().containsItem("feraltweaks"))
					&& preventNonFTClients) {
				// Unsupported
				event.cancel();
				return;
			}
			event.getClient().addObject(new FeralTweaksClientObject(false, null));
		}
	}
}
