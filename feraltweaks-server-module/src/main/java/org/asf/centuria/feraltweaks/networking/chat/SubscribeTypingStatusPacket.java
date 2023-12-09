package org.asf.centuria.feraltweaks.networking.chat;

import org.asf.centuria.networking.chatserver.ChatClient;
import org.asf.centuria.networking.chatserver.networking.AbstractChatPacket;
import org.asf.centuria.feraltweaks.networking.entities.TypingStatusSupported;

import com.google.gson.JsonObject;

/**
 * 
 * Subscribes the 'typing' status system
 * 
 * @author Sky Swimmer
 *
 */
public class SubscribeTypingStatusPacket extends AbstractChatPacket {

	@Override
	public String id() {
		return "feraltweaks.typingstatus.subscribe";
	}

	@Override
	public AbstractChatPacket instantiate() {
		return new SubscribeTypingStatusPacket();
	}

	@Override
	public void build(JsonObject obj) {
	}

	@Override
	public void parse(JsonObject obj) {
	}

	@Override
	public boolean handle(ChatClient client) {
		// Make the client supported
		if (client.getObject(TypingStatusSupported.class) == null)
			client.addObject(new TypingStatusSupported());
		return true;
	}

}
