package org.asf.centuria.feraltweaks;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.util.HashMap;
import org.asf.centuria.feraltweaks.handlers.ChatMessageHandler;
import org.asf.centuria.feraltweaks.handlers.CommandHandlers;
import org.asf.centuria.feraltweaks.handlers.DisconnectHandler;
import org.asf.centuria.feraltweaks.handlers.handshake.GameHandshakeHandler;
import org.asf.centuria.feraltweaks.handlers.handshake.ChatHandshakeHandler;
import org.asf.centuria.feraltweaks.managers.PlayerNameManager;
import org.asf.centuria.feraltweaks.managers.ScheduledMaintenanceManager;
import org.asf.centuria.feraltweaks.networking.chat.FeralTweaksHandshakePacket;
import org.asf.centuria.feraltweaks.networking.chat.FeralTweaksPostInitPacket;
import org.asf.centuria.feraltweaks.networking.chat.MarkConvoReadPacket;
import org.asf.centuria.feraltweaks.networking.chat.SubscribeTypingStatusPacket;
import org.asf.centuria.feraltweaks.networking.chat.TypingStatusPacket;
import org.asf.centuria.feraltweaks.networking.game.FtModPacket;
import org.asf.centuria.feraltweaks.networking.game.YesNoPopupPacket;
import org.asf.centuria.feraltweaks.networking.http.DataProcessor;
import org.asf.centuria.modules.ICenturiaModule;
import org.asf.centuria.modules.eventbus.EventBus;
import org.asf.centuria.modules.eventbus.EventListener;
import org.asf.centuria.modules.events.servers.APIServerStartupEvent;
import org.asf.centuria.modules.events.servers.ChatServerStartupEvent;
import org.asf.centuria.modules.events.servers.GameServerStartupEvent;

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
	public static int FT_VERSION = 3;

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
		return "beta-1.0.0-b4";
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
		EventBus.getInstance().addAllEventsFromReceiver(new CommandHandlers());
		EventBus.getInstance().addAllEventsFromReceiver(new ChatMessageHandler());
		EventBus.getInstance().addAllEventsFromReceiver(new DisconnectHandler());
		EventBus.getInstance().addAllEventsFromReceiver(new ChatHandshakeHandler());
		EventBus.getInstance().addAllEventsFromReceiver(new GameHandshakeHandler());
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
		event.registerPacket(new FeralTweaksHandshakePacket());
		event.registerPacket(new FeralTweaksPostInitPacket());
		event.registerPacket(new TypingStatusPacket());
		event.registerPacket(new SubscribeTypingStatusPacket());
	}
}
