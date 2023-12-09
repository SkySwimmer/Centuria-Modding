package org.asf.centuria.feraltweaks.managers;

import java.util.ArrayList;

import org.asf.centuria.accounts.CenturiaAccount;
import org.asf.centuria.dms.DMManager;
import org.asf.centuria.networking.chatserver.ChatClient;
import org.asf.centuria.networking.gameserver.GameServer;
import org.asf.centuria.social.SocialManager;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;

public class UnreadMessageManager {

	private static JsonObject loadUnreads(CenturiaAccount account)
	{
		JsonObject unreads = new JsonObject();

		// Load unreads
		if (account.getSaveSharedInventory().containsItem("unreadconversations")) {
			// Read unread info
			JsonElement unreadsRaw = account.getSaveSharedInventory().getItem("unreadconversations");
			if (unreadsRaw.isJsonObject()) {
				// Read with 1.8 format
				unreads = unreadsRaw.getAsJsonObject();
			} else {
				// Convert 1.7 to 1.8
				JsonArray arr = unreadsRaw.getAsJsonArray();
				for (JsonElement ele : arr) {
					unreads.addProperty(ele.getAsString(), 1);
				}
			}
		}

		// Return
		return unreads;
	}

	/**
	 * Called to handle unreads, when a message is received and a player is offline,
	 * this method will mark the conversation as unread
	 * 
	 * @param conversationId Conversation where a new message was received
	 * @param sender         Account instance that sent the message
	 * @param receiver       Account instance that received the message
	 */
	public static void receivedMessageInConverstaion(String conversationId, CenturiaAccount sender,
			CenturiaAccount receiver) {
		// Check if online
		if (receiver.getOnlinePlayerInstance() == null) {
			// Check blocked and if the sender is not a moderator
			String permLevel = "member";
			if (sender.getSaveSharedInventory().containsItem("permissions")) {
				permLevel = sender.getSaveSharedInventory().getItem("permissions").getAsJsonObject()
						.get("permissionLevel").getAsString();
			}
			if (!GameServer.hasPerm(permLevel, "moderator")) {
				SocialManager socialManager = SocialManager.getInstance();
				if (socialManager.socialListExists(receiver.getAccountID())
						&& socialManager.getPlayerIsBlocked(receiver.getAccountID(), sender.getAccountID()))
					return; // Skip
			}

			// Add to unread history

			// Load unreads
			JsonObject unreads = loadUnreads(receiver);

			// Update
			unreads.addProperty(conversationId, unreads.has(conversationId) ? unreads.get(conversationId).getAsInt() + 1 : 1);

			// Save
			receiver.getSaveSharedInventory().setItem("unreadconversations", unreads);
		}
	}

	/**
	 * Retrieves unread messages of a player
	 * 
	 * @param account Account to retrieve the unreads of
	 * @param client  Chat client instance used to remove rooms the client is no longer in
	 * @return JsonObject instance containing all unreads, structured by strings as conversation IDs with integers for the unread counts
	 */
	public static JsonObject getUnreadMessageCounts(CenturiaAccount account, ChatClient client) {
		JsonObject unreads = new JsonObject();

		// Load unreads
		if (account.getSaveSharedInventory().containsItem("unreadconversations")) {
			// Load unreads
			unreads = loadUnreads(account);

			// Check client
			if (client != null) {
				// Remove nonexistent items and rooms the player is no longer in
				// Dms are joined by now, so would gcs as this event is bound later than any
				// module normally binds, meaning the other modules are fired before this one
				ArrayList<String> toRemove = new ArrayList<String>();
				for (String convoID : unreads.keySet()) {
					if (!DMManager.getInstance().dmExists(convoID) || !client.isInRoom(convoID))
						toRemove.add(convoID);
				}
				for (String id : toRemove)
					unreads.remove(id);

				// Save if needed
				if (toRemove.size() != 0)
					account.getSaveSharedInventory().setItem("unreadconversations", unreads);
			}
		} else {
			// Save
			account.getSaveSharedInventory().setItem("unreadconversations", unreads);
		}

		// Return
		return unreads;
	}

	/**
	 * Marks conversations as read
	 * 
	 * @param conversation Conversation to mark as read
	 * @param client       Client instance
	 */
	public static void markConversationAsRead(String conversation, ChatClient client) {
		// Find unread convo map
		if (client.getPlayer().getSaveSharedInventory().containsItem("unreadconversations")) {
			JsonObject unreads = loadUnreads(client.getPlayer());
			for (String convoID : unreads.keySet()) {
				if (convoID.equals(conversation)) {
					// Found it
					unreads.remove(convoID);
					client.getPlayer().getSaveSharedInventory().setItem("unreadconversations", unreads);
					break;
				}
			}
		}
	}

}
