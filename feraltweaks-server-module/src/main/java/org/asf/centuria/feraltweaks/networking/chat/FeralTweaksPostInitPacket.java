package org.asf.centuria.feraltweaks.networking.chat;

import org.asf.centuria.feraltweaks.FeralTweaksClientObject;
import org.asf.centuria.feraltweaks.managers.UnreadMessageManager;
import org.asf.centuria.networking.chatserver.ChatClient;
import org.asf.centuria.networking.chatserver.networking.AbstractChatPacket;

import com.google.gson.JsonObject;

public class FeralTweaksPostInitPacket extends AbstractChatPacket {

	private class PostInitObject {
	}

	@Override
	public String id() {
		return "feraltweaks.postinit";
	}

	@Override
	public AbstractChatPacket instantiate() {
		return new FeralTweaksPostInitPacket();
	}

	@Override
	public void parse(JsonObject data) {
	}

	@Override
	public void build(JsonObject data) {
	}

	@Override
	public boolean handle(ChatClient client) {
		// Get object
		FeralTweaksClientObject obj = client.getObject(FeralTweaksClientObject.class);
		if (obj != null && client.getObject(PostInitObject.class) == null) {
			// Post-init

			// Send unreads
			JsonObject pkt = new JsonObject();
			pkt.addProperty("eventId", "feraltweaks.unreadconversations");
			pkt.add("conversations", UnreadMessageManager.getUnreadConversations(client.getPlayer(), client));
			client.sendPacket(pkt);

			// Send success
			pkt = new JsonObject();
			pkt.addProperty("eventId", "feraltweaks.postinit");
			pkt.addProperty("success", true);
			client.sendPacket(pkt);

			// Mark done
			client.addObject(PostInitObject.class);
		}
		return true;
	}

}
