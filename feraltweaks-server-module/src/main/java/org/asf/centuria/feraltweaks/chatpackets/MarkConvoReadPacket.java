package org.asf.centuria.feraltweaks.chatpackets;

import org.asf.centuria.networking.chatserver.ChatClient;
import org.asf.centuria.networking.chatserver.networking.AbstractChatPacket;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;;

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
		// Find unread convo map
		if (client.getPlayer().getSaveSharedInventory().containsItem("unreadconversations")) {
			JsonArray convos = client.getPlayer().getSaveSharedInventory().getItem("unreadconversations").getAsJsonArray();
			for (JsonElement ele : convos) {
				if (ele.getAsString().equals(conversation)) {
					// Found it
					convos.remove(ele);
					client.getPlayer().getSaveSharedInventory().setItem("unreadconversations", convos);
					break;
				}	
			}
		}
		return true;
	}

}
