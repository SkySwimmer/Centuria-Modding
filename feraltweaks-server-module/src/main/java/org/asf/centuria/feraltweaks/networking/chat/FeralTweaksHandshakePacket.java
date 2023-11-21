package org.asf.centuria.feraltweaks.networking.chat;

import java.util.HashMap;

import org.asf.centuria.Centuria;
import org.asf.centuria.feraltweaks.FeralTweaksClientObject;
import org.asf.centuria.feraltweaks.FeralTweaksModule;
import org.asf.centuria.feraltweaks.api.versioning.IModVersionHandler;
import org.asf.centuria.modules.ICenturiaModule;
import org.asf.centuria.modules.ModuleManager;
import org.asf.centuria.networking.chatserver.ChatClient;
import org.asf.centuria.networking.chatserver.networking.AbstractChatPacket;

import com.google.gson.JsonObject;

public class FeralTweaksHandshakePacket extends AbstractChatPacket {

	public JsonObject pktHS;

	@Override
	public String id() {
		return "feraltweaks.fthandshake";
	}

	@Override
	public AbstractChatPacket instantiate() {
		return new FeralTweaksHandshakePacket();
	}

	@Override
	public void parse(JsonObject data) {
		pktHS = data;
	}

	@Override
	public void build(JsonObject data) {
	}

	@Override
	public boolean handle(ChatClient client) {
		FeralTweaksModule ftModule = ((FeralTweaksModule) ModuleManager.getInstance().getModule("feraltweaks"));
		
		// Get mods list
		HashMap<String, String> mods = new HashMap<String, String>();
		JsonObject modsJson = pktHS.get("feraltweaks_mods").getAsJsonObject();
		for (String id : modsJson.keySet())
			mods.put(id, modsJson.get(id).getAsString());

		// Prepare success
		JsonObject pkt = new JsonObject();
		pkt.addProperty("eventId", "feraltweaks.fthandshake");
		pkt.addProperty("success", true);

		// Add mods to result json
		JsonObject modsJ = new JsonObject();
		modsJ.addProperty("feraltweaks", ftModule.version());
		for (ICenturiaModule module : ModuleManager.getInstance().getAllModules()) {
			if (module instanceof IModVersionHandler) {
				modsJ.addProperty(module.id(), module.version());
			}
		}
		pkt.addProperty("serverSoftwareName", "Centuria");
		pkt.addProperty("serverSoftwareVersion", Centuria.SERVER_UPDATE_VERSION);
		pkt.add("serverMods", modsJ);

		// SEnd
		client.sendPacket(pkt);

		// Handshake success
		client.addObject(new FeralTweaksClientObject(true, pktHS.get("feraltweaks_version").getAsString(), mods));
		return true;
	}

}
