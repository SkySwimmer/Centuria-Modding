package org.asf.centuria.feraltweaks.handlers;

import org.asf.centuria.feraltweaks.FeralTweaksClientObject;
import org.asf.centuria.feraltweaks.networking.game.DisconnectPacket;
import org.asf.centuria.modules.eventbus.EventListener;
import org.asf.centuria.modules.eventbus.IEventReceiver;
import org.asf.centuria.modules.events.accounts.AccountDisconnectEvent;
import org.asf.centuria.modules.events.updates.ServerUpdateEvent;

public class DisconnectHandler implements IEventReceiver {
	private boolean updateShutdown;

	@EventListener
	public void updateServer(ServerUpdateEvent event) {
		if (event.hasVersionInfo())
			updateShutdown = true;
	}

	@EventListener
	public void disconnect(AccountDisconnectEvent event) {
		// Verify FeralTweaks support
		if (event.getAccount().getOnlinePlayerInstance() != null
				&& event.getAccount().getOnlinePlayerInstance().getObject(FeralTweaksClientObject.class) != null
				&& event.getAccount().getOnlinePlayerInstance().getObject(FeralTweaksClientObject.class).isEnabled()) {
			// Create disconnect packet based on disconnect type
			DisconnectPacket pkt = new DisconnectPacket();
			pkt.button = "Quit";
			pkt.title = "Disconnected";
			switch (event.getType()) {

			case DUPLICATE_LOGIN:
				pkt.title = "Disconnected";
				pkt.message = "Your account was logged into from another location.";
				break;

			case BANNED:
				pkt.title = "Banned";
				pkt.message = "Your account was suspended and has been disconnected.";
				if (event.getReason() != null)
					pkt.message += "\n\nReason: " + event.getReason();
				break;

			case KICKED:
				pkt.message = "You were disconnected from the server.";
				if (event.getReason() != null)
					pkt.message += "\n\nReason: " + event.getReason();
				break;

			case MAINTENANCE:
				pkt.title = "Server Closed";
				pkt.message = "The server has been placed under maintenance, hope to be back soon!"
						+ event.getReason() == null ? "" : "\n\nReason: " + event.getReason();
				break;

			case SERVER_SHUTDOWN:
				pkt.title = "Server Closed";
				if (updateShutdown)
					pkt.message = "The server has been shut down for a update, will be back soon!";
				else
					pkt.message = "The server has been temporarily shut down, hope to be back soon!"
							+ event.getReason() == null ? "" : "\n\nReason: " + event.getReason();
				break;

			case UNKNOWN:
				pkt.message = "Disconnected from server due to an unknown error.";
				break;

			}
			event.getAccount().getOnlinePlayerInstance().client.sendPacket(pkt);
			try {
				// Give time to disconnect
				Thread.sleep(500);
			} catch (InterruptedException e) {
			}
		}
	}

}
