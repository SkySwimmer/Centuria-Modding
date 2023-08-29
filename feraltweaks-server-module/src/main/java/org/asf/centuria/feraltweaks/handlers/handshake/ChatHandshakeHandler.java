package org.asf.centuria.feraltweaks.handlers.handshake;

import org.asf.centuria.feraltweaks.FeralTweaksClientObject;
import org.asf.centuria.feraltweaks.FeralTweaksModule;
import org.asf.centuria.feraltweaks.managers.UnreadMessageManager;
import org.asf.centuria.modules.ModuleManager;
import org.asf.centuria.modules.eventbus.EventListener;
import org.asf.centuria.modules.eventbus.IEventReceiver;
import org.asf.centuria.modules.events.chat.ChatLoginEvent;

import com.google.gson.JsonObject;

public class ChatHandshakeHandler implements IEventReceiver {

	@EventListener
	public void handleChatPrelogin(ChatLoginEvent event) {
		FeralTweaksModule ftModule = ((FeralTweaksModule) ModuleManager.getInstance().getModule("feraltweaks"));

		// Handshake feraltweaks
		if (event.getLoginRequest().has("feraltweaks")
				&& event.getLoginRequest().get("feraltweaks").getAsString().equals("enabled")) {
			// Handle FeralTweaks hanshake
			if (event.getLoginRequest().get("feraltweaks_protocol").getAsInt() != FeralTweaksModule.FT_VERSION) {
				// Handshake failure
				event.cancel();
				return;
			}

			// Check if FT is enabled
			if (!ftModule.enableByDefault && !event.getAccount().getSaveSharedInventory().containsItem("feraltweaks")
					&& !event.getAccount().getSaveSpecificInventory().containsItem("feraltweaks")) {
				// Handshake failure
				event.cancel();
				return;
			}

			// Handshake success
			event.getClient().addObject(new FeralTweaksClientObject(true,
					event.getLoginRequest().get("feraltweaks_version").getAsString()));

			// Send unreads
			JsonObject pkt = new JsonObject();
			pkt.addProperty("eventId", "feraltweaks.unreadconversations");
			pkt.add("conversations",
					UnreadMessageManager.getUnreadConversations(event.getAccount(), event.getClient()));
			event.getClient().sendPacket(pkt);
		} else {
			// Non-FT client, check if support is enabled
			if ((ftModule.enableByDefault || event.getAccount().getSaveSharedInventory().containsItem("feraltweaks")
					|| event.getAccount().getSaveSpecificInventory().containsItem("feraltweaks"))
					&& ftModule.preventNonFTClients) {
				// Unsupported
				event.cancel();
				return;
			}

			// Add object
			event.getClient().addObject(new FeralTweaksClientObject(false, null));
		}
	}

}
