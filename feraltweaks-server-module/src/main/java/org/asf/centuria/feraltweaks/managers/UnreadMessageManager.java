package org.asf.centuria.feraltweaks.managers;

import java.util.ArrayList;

import org.asf.centuria.accounts.CenturiaAccount;
import org.asf.centuria.dms.DMManager;
import org.asf.centuria.networking.chatserver.ChatClient;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;

public class UnreadMessageManager {

	/**
	 * Called to handle unreads, when a message is received and a player is offline,
	 * this method will mark the conversation as unread
	 * 
	 * @param conversationId Conversation where a new message was received
	 * @param receiver       Account instance that received the message
	 */
	public static void receivedMessageInConverstaion(String conversationId, CenturiaAccount receiver) {
		// Check if online
		if (receiver.getOnlinePlayerInstance() == null) {
			// Add to unread history
			JsonArray arr = new JsonArray();

			// Load unreads
			if (receiver.getSaveSharedInventory().containsItem("unreadconversations"))
				arr = receiver.getSaveSharedInventory().getItem("unreadconversations").getAsJsonArray();

			// Check if present
			boolean found = false;
			for (JsonElement ele : arr) {
				if (ele.getAsString().equals(conversationId)) {
					found = true;
					break;
				}
			}
			if (!found) {
				// Add unread
				arr.add(conversationId);

				// Save
				receiver.getSaveSharedInventory().setItem("unreadconversations", arr);
			}
		}
	}

	/**
	 * Retrieves unread conversations of a player
	 * 
	 * @param account Account to retrieve the conversations of
	 * @param client  Chat client instance used to remove rooms the client is no
	 *                longer in
	 * @return JsonArray instance containing all unread conversation IDs
	 */
	public static JsonElement getUnreadConversations(CenturiaAccount account, ChatClient client) {
		JsonArray arr = new JsonArray();

		// Load unreads
		if (account.getSaveSharedInventory().containsItem("unreadconversations")) {
			// Load
			arr = account.getSaveSharedInventory().getItem("unreadconversations").getAsJsonArray();
			if (client != null) {
				// Remove nonexistent items and rooms the player is no longer in
				// Dms are joined by now, so would gcs as this event is bound later than any
				// module normally binds, meaning the other modules are fired before this one
				ArrayList<JsonElement> toRemove = new ArrayList<JsonElement>();
				for (JsonElement ele : arr) {
					if (!DMManager.getInstance().dmExists(ele.getAsString()) || !client.isInRoom(ele.getAsString()))
						toRemove.add(ele);
				}
				for (JsonElement id : toRemove)
					arr.remove(id);

				// Save if needed
				if (toRemove.size() != 0)
					account.getSaveSharedInventory().setItem("unreadconversations", arr);
			}
		} else {
			// Save
			account.getSaveSharedInventory().setItem("unreadconversations", arr);
		}

		// Return
		return arr;
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
			JsonArray convos = client.getPlayer().getSaveSharedInventory().getItem("unreadconversations")
					.getAsJsonArray();
			for (JsonElement ele : convos) {
				if (ele.getAsString().equals(conversation)) {
					// Found it
					convos.remove(ele);
					client.getPlayer().getSaveSharedInventory().setItem("unreadconversations", convos);
					break;
				}
			}
		}
	}

}
