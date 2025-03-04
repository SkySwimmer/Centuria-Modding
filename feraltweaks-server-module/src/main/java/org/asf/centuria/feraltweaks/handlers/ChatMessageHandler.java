package org.asf.centuria.feraltweaks.handlers;

import org.asf.centuria.accounts.AccountManager;
import org.asf.centuria.accounts.CenturiaAccount;
import org.asf.centuria.dms.DMManager;
import org.asf.centuria.feraltweaks.FeralTweaksModule;
import org.asf.centuria.feraltweaks.managers.UnreadMessageManager;
import org.asf.centuria.modules.ModuleManager;
import org.asf.centuria.modules.eventbus.EventListener;
import org.asf.centuria.modules.eventbus.IEventReceiver;
import org.asf.centuria.modules.events.chat.ChatConversationDeletionWarningEvent;
import org.asf.centuria.modules.events.chat.ChatMessageBroadcastEvent;

public class ChatMessageHandler implements IEventReceiver {

	@EventListener
	public void onChatConvoDeletionWarning(ChatConversationDeletionWarningEvent event) {
		FeralTweaksModule ftModule = ((FeralTweaksModule) ModuleManager.getInstance().getModule("feraltweaks"));

		// Find dm participants
		if (DMManager.getInstance().dmExists(event.getConversationId())) {
			String[] participants = DMManager.getInstance().getDMParticipants(event.getConversationId());
			for (String accId : participants) {
				// Find account
				CenturiaAccount acc = AccountManager.getInstance().getAccount(accId);
				if (acc == null)
					continue; // Wtf-

				// Check feraltweaks support
				if (ftModule.enableByDefault || acc.getSaveSharedInventory().containsItem("feraltweaks")
						|| acc.getSaveSpecificInventory().containsItem("feraltweaks")) {
					// Add unread if needed
					UnreadMessageManager.receivedMessageInConverstaion(event.getConversationId(), acc);
				}
			}
		}
	}

	@EventListener
	public void chatMessageSent(ChatMessageBroadcastEvent event) {
		FeralTweaksModule ftModule = ((FeralTweaksModule) ModuleManager.getInstance().getModule("feraltweaks"));

		// Check room
		if (event.getClient().isInRoom(event.getConversationId())
				&& event.getClient().isRoomPrivate(event.getConversationId())) {
			// Find dm participants
			if (DMManager.getInstance().dmExists(event.getConversationId())) {
				String[] participants = DMManager.getInstance().getDMParticipants(event.getConversationId());
				for (String accId : participants) {
					// Find account
					CenturiaAccount acc = AccountManager.getInstance().getAccount(accId);
					if (acc == null)
						continue; // Wtf-

					// Check feraltweaks support
					if (ftModule.enableByDefault || acc.getSaveSharedInventory().containsItem("feraltweaks")
							|| acc.getSaveSpecificInventory().containsItem("feraltweaks")) {
						// Add unread if needed
						UnreadMessageManager.receivedMessageInConverstaion(event.getConversationId(), acc);
					}
				}
			}
		}
	}

}
