package org.asf.centuria.feraltweaks.handlers.handshake;

import java.util.Map;

import org.asf.centuria.feraltweaks.FeralTweaksClientObject;
import org.asf.centuria.feraltweaks.FeralTweaksModule;
import org.asf.centuria.modules.ModuleManager;
import org.asf.centuria.modules.eventbus.EventListener;
import org.asf.centuria.modules.eventbus.IEventReceiver;
import org.asf.centuria.modules.events.chat.ChatLoginEvent;

public class ChatHandshakeHandler implements IEventReceiver {

	@EventListener
	public void handleChatPrelogin(ChatLoginEvent event) {
		FeralTweaksModule ftModule = ((FeralTweaksModule) ModuleManager.getInstance().getModule("feraltweaks"));

		// Handshake feraltweaks
		if (!event.getLoginRequest().has("ft") || !event.getLoginRequest().get("ft").getAsString().equals("enabled")) {
			// Non-FT client, check if support is enabled
			if ((ftModule.enableByDefault || event.getAccount().getSaveSharedInventory().containsItem("feraltweaks")
					|| event.getAccount().getSaveSpecificInventory().containsItem("feraltweaks"))
					&& ftModule.preventNonFTClients) {
				// Unsupported
				event.cancel();
				return;
			}

			// Add object
			event.getClient().addObject(new FeralTweaksClientObject(false, null, Map.of()));
		} else {
			// Handle FeralTweaks handshake
			if (event.getLoginRequest().get("ft_prot").getAsInt() != FeralTweaksModule.FT_VERSION) {
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
		}
	}

}
