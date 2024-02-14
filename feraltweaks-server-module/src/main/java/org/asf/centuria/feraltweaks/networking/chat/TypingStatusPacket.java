package org.asf.centuria.feraltweaks.networking.chat;

import org.asf.centuria.networking.chatserver.ChatClient;
import org.asf.centuria.networking.chatserver.networking.AbstractChatPacket;
import org.asf.centuria.feraltweaks.networking.entities.TypingStatusSupported;

import com.google.gson.JsonObject;

/**
 * 
 * Typing status broadcast packet
 * 
 * @author Sky Swimmer
 *
 */
public class TypingStatusPacket extends AbstractChatPacket {

	public String playerID;
	public String playerDisplay;
	public String conversationId;

	@Override
	public String id() {
		return "feraltweaks.typing";
	}

	@Override
	public AbstractChatPacket instantiate() {
		return new TypingStatusPacket();
	}

	@Override
	public void build(JsonObject obj) {
		obj.addProperty("conversationId", conversationId);
		obj.addProperty("uuid", playerID);
		obj.addProperty("displayName", playerDisplay);
	}

	@Override
	public void parse(JsonObject obj) {
		conversationId = obj.get("conversationId").getAsString();
	}

	@Override
	public boolean handle(ChatClient client) {
		// Check supported
		if (client.getObject(TypingStatusSupported.class) != null) {
			// Update packet
			playerID = client.getPlayer().getAccountID();
			playerDisplay = client.getPlayer().getDisplayName();

			// Broadcast
			for (ChatClient cl : client.getServer().getClients()) {
				// Check supported
				if (cl.getObject(TypingStatusSupported.class) != null && cl.isInRoom(conversationId)) {
					// Send
					cl.sendPacket(this);
				}
			}
		}
		return true;
	}
}
