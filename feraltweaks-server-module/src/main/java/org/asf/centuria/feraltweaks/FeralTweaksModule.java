package org.asf.centuria.feraltweaks;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.Map;
import org.asf.centuria.Centuria;
import org.asf.centuria.accounts.SaveMode;
import org.asf.centuria.data.XtReader;
import org.asf.centuria.feraltweaks.api.versioning.IModVersionHandler;
import org.asf.centuria.feraltweaks.handlers.ChatMessageHandler;
import org.asf.centuria.feraltweaks.handlers.CommandHandlers;
import org.asf.centuria.feraltweaks.handlers.DisconnectHandler;
import org.asf.centuria.feraltweaks.handlers.handshake.GameHandshakeHandler;
import org.asf.centuria.feraltweaks.handlers.handshake.ChatHandshakeHandler;
import org.asf.centuria.feraltweaks.managers.PlayerNameManager;
import org.asf.centuria.feraltweaks.managers.ScheduledMaintenanceManager;
import org.asf.centuria.feraltweaks.networking.chat.MarkConvoReadPacket;
import org.asf.centuria.feraltweaks.networking.game.FtModPacket;
import org.asf.centuria.feraltweaks.networking.game.YesNoPopupPacket;
import org.asf.centuria.feraltweaks.networking.http.DataProcessor;
import org.asf.centuria.feraltweaks.utils.VersionUtils;
import org.asf.centuria.modules.ICenturiaModule;
import org.asf.centuria.modules.ModuleManager;
import org.asf.centuria.modules.eventbus.EventBus;
import org.asf.centuria.modules.eventbus.EventListener;
import org.asf.centuria.modules.eventbus.IEventReceiver;
import org.asf.centuria.modules.events.accounts.AccountPreloginEvent;
import org.asf.centuria.modules.events.servers.APIServerStartupEvent;
import org.asf.centuria.modules.events.servers.ChatServerStartupEvent;
import org.asf.centuria.modules.events.servers.GameServerStartupEvent;
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
	public static int FT_VERSION = 2;

	public boolean enableByDefault;
	public boolean requireManagedSaveData;
	public boolean preventNonFTClients;

	public String ftDataPath;
	public String ftCachePath;
	public String upstreamServerJsonURL;

	public String ftUnsupportedErrorMessage;
	public String ftOutdatedErrorMessage;
	public String modDataVersion;

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
				Files.writeString(configFile.toPath(), ""

						+ "enable-by-default=false\n" //
						+ "prevent-non-ft-clients=true\n" //
						+ "data-path=feraltweaks/content\n" //
						+ "cache-path=feraltweaks/cache\n" //
						+ "upstream-server-json=https://emuferal.ddns.net:6970/data/server.json\n" //
						+ "error-unauthorized=\\nFeralTweaks is presently not enabled on your account!\\n\\nPlease uninstall the client modding project, contact the server administrator if you believe this is an error.\n" //
						+ "error-outdated=Incompatible client!\\nYour client is currently out of date, restart the game to update the client mods.\n" //
						+ "mod-data-version=1\n"

				);
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

		// Load config
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

		// Init managers
		ScheduledMaintenanceManager.initMaintenanceManager();
		PlayerNameManager.initPlayerNameManager();

		// Bind events
		EventBus.getInstance().addEventReceiver(new CommandHandlers());
		EventBus.getInstance().addEventReceiver(new ChatMessageHandler());
		EventBus.getInstance().addEventReceiver(new DisconnectHandler());
		EventBus.getInstance().addEventReceiver(new ChatHandshakeHandler());
		EventBus.getInstance().addEventReceiver(new GameHandshakeHandler());
		EventBus.getInstance().addEventReceiver(new LateEventContainer());
	}

	private class LateEventContainer implements IEventReceiver {

		@EventListener
		public void handleGamePrelogin(AccountPreloginEvent event) {
			// Handshake feraltweaks

			// Add mods to result json
			JsonObject modsJ = new JsonObject();
			modsJ.addProperty("feraltweaks", version());
			for (ICenturiaModule module : ModuleManager.getInstance().getAllModules()) {
				if (module instanceof IModVersionHandler) {
					modsJ.addProperty(module.id(), module.version());
				}
			}
			event.getLoginResponseParameters().addProperty("serverSoftwareName", "Centuria");
			event.getLoginResponseParameters().addProperty("serverSoftwareVersion", Centuria.SERVER_UPDATE_VERSION);
			event.getLoginResponseParameters().add("serverMods", modsJ);

			// Handshake
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
						if (!rd.hasNext())
							break; // Invalid
						int modCount = rd.readInt();
						HashMap<String, String> mods = new HashMap<String, String>();
						HashMap<String, HashMap<String, String>> handshakeRules = new HashMap<String, HashMap<String, String>>();
						for (int i = 0; i < modCount; i++) {
							// Read id
							if (!rd.hasNext())
								break; // Invalid
							String id = rd.read();

							// Read version
							if (!rd.hasNext())
								break; // Invalid
							String version = rd.read();

							// Read handshake rules
							if (!rd.hasNext())
								break; // Invalid
							int l = rd.readInt();
							HashMap<String, String> rules = new HashMap<String, String>();
							for (int i2 = 0; i2 < l; i2++) {
								// Read id
								if (!rd.hasNext())
									break; // Invalid
								String rID = rd.read();

								// Read version check string
								if (!rd.hasNext())
									break; /// Invalid
								String rVer = rd.read();
								rules.put(rID, rVer);
							}

							// Add
							mods.put(id, version);
							handshakeRules.put(id, rules);
						}
						if (!rd.hasNext() || !rd.read().equals("end"))
							break; // Invalid
						if (rd.hasNext())
							break; // Invalid

						// Check handshake
						if (protVer != FT_VERSION || (!dataVer.equals("undefined")
								&& !dataVer.equals(modDataVersion + "/" + Centuria.SERVER_UPDATE_VERSION))) {
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

						// Log
						String modsStr = "";
						for (String id : mods.keySet()) {
							if (!modsStr.isEmpty())
								modsStr += ", ";
							modsStr += id + " (" + mods.get(id) + ")";
						}
						Centuria.logger.info("Player " + event.getAccount().getDisplayName() + " is logging in with "
								+ mods.size() + " client mod" + (mods.size() == 1 ? "" : "s") + " [" + modsStr + "]");

						// Verify handshake rules of server
						HashMap<String, String> localMods = new HashMap<String, String>();
						ArrayList<String> missingMods = new ArrayList<String>();
						ArrayList<String> incompatibleMods = new ArrayList<String>();
						localMods.put("feraltweaks", version());
						for (ICenturiaModule module : ModuleManager.getInstance().getAllModules()) {
							if (module instanceof IModVersionHandler) {
								localMods.put(module.id(), module.version());
								IModVersionHandler handler = (IModVersionHandler) module;
								Map<String, String> rules = handler.getClientModVersionRules();

								// Verify rules
								for (String id : rules.keySet()) {
									if (!mods.containsKey(id)) {
										// Missing
										if (!missingMods.contains(id))
											missingMods.add(id);
									} else {
										// Check version
										if (!VersionUtils.verifyVersionRequirement(mods.get(id), rules.get(id))) {
											// Incompatible
											if (!incompatibleMods.contains(id))
												incompatibleMods.add(id);
										}
									}
								}
							}
						}

						// Verify
						if (missingMods.size() != 0 || incompatibleMods.size() != 0) {
							// Handshake error

							event.setStatus(-26);
							String msg = "Your current game installation is not compatible with the server.\n\n";

							// Build message
							String logMsg = "";
							msg += "Missing/outdated client mods:\n";
							boolean first = true;
							for (String mod : missingMods) {
								if (!first)
									msg += ", ";
								msg += mod;
								if (!logMsg.isEmpty())
									logMsg += ", ";
								logMsg += mod;
								first = false;
							}
							for (String mod : incompatibleMods) {
								if (!first)
									msg += ", ";
								if (!logMsg.isEmpty())
									logMsg += ", ";
								logMsg += mod;
								msg += mod;
								first = false;
							}

							// Log
							Centuria.logger.error("Player " + event.getAccount().getDisplayName()
									+ " failed to log in due to " + (missingMods.size() + incompatibleMods.size())
									+ " incompatible/missing CLIENT mod"
									+ ((missingMods.size() + incompatibleMods.size()) == 1 ? "" : "s") + " [" + logMsg
									+ "]");
							event.getLoginResponseParameters().addProperty("incompatibleClientMods", logMsg);
							event.getLoginResponseParameters().addProperty("incompatibleClientModCount",
									(missingMods.size() + incompatibleMods.size()));

							// Set result
							event.getLoginResponseParameters().addProperty("errorMessage", msg);
							return;
						}

						// Verify client mod rules
						incompatibleMods.clear();
						for (HashMap<String, String> rules : handshakeRules.values()) {
							// Verify rules
							for (String id : rules.keySet()) {
								if (!localMods.containsKey(id)) {
									// Missing
									if (!incompatibleMods.contains(id))
										incompatibleMods.add(id);
								} else {
									// Check version
									if (!VersionUtils.verifyVersionRequirement(localMods.get(id), rules.get(id))) {
										// Incompatible
										if (!incompatibleMods.contains(id))
											incompatibleMods.add(id);
									}
								}
							}
						}

						// Verify
						if (incompatibleMods.size() != 0) {
							// Handshake error

							// Set status
							event.setStatus(-26);

							// Build message
							String logMsg = "";
							String msg = "You are running mods that require mods that require a up-to-date server mod:\n";
							boolean first = true;
							for (String mod : incompatibleMods) {
								if (!first)
									msg += ", ";
								if (!logMsg.isEmpty())
									logMsg += ", ";
								logMsg += mod;
								msg += mod;
								first = false;
							}

							// Log
							Centuria.logger
									.error("Player " + event.getAccount().getDisplayName() + " failed to log in due to "
											+ incompatibleMods.size() + " incompatible/missing SERVER mod"
											+ (incompatibleMods.size() == 1 ? "" : "s") + " [" + logMsg + "]");
							event.getLoginResponseParameters().addProperty("incompatibleServerMods", logMsg);
							event.getLoginResponseParameters().addProperty("incompatibleServerModCount",
									incompatibleMods.size());

							// Set result
							event.getLoginResponseParameters().addProperty("errorMessage", msg);
							return;
						}

						// Handshake success
						event.getClient().addObject(new FeralTweaksClientObject(true, ver));
						feralTweaks = true;

						// Set
						PlayerNameManager.updatePlayer(event.getAccount());
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

	}

	@EventListener
	public void gameServerStartup(GameServerStartupEvent event) {
		event.registerPacket(new YesNoPopupPacket());
		event.registerPacket(new FtModPacket());
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
}
