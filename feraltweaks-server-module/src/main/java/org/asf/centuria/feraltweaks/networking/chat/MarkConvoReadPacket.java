package org.asf.centuria.feraltweaks.networking.chat;

import org.asf.centuria.feraltweaks.managers.UnreadMessageManager;
import org.asf.centuria.networking.chatserver.ChatClient;
import org.asf.centuria.networking.chatserver.networking.AbstractChatPacket;

import com.google.gson.JsonObject;

/**
 * 
 * Marks a conversation as read
 * 
 * @author Sky Swimmer
 *
 */
public class MarkConvoReadPacket extends AbstractChatPacket {

	public String conversation;

	@Override
	public String id() {
		return "feraltweaks.markread";
	}

	@Override
	public AbstractChatPacket instantiate() {
		return new MarkConvoReadPacket();
	}

	@Override
	public void build(JsonObject obj) {
		obj.addProperty("conversation", conversation);
	}

	@Override
	public void parse(JsonObject obj) {
		conversation = obj.get("conversation").getAsString();
	}

	@Override
	public boolean handle(ChatClient client) {
		// Mark as read
		UnreadMessageManager.markConversationAsRead(conversation, client);
		return true;
	}

}
