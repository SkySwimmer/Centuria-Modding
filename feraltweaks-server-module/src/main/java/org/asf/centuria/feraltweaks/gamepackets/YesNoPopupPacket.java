package org.asf.centuria.feraltweaks.gamepackets;

import java.io.IOException;

import org.asf.centuria.Centuria;
import org.asf.centuria.data.XtReader;
import org.asf.centuria.data.XtWriter;
import org.asf.centuria.entities.players.Player;
import org.asf.centuria.feraltweaks.FeralTweaksClientObject;
import org.asf.centuria.networking.smartfox.SmartfoxClient;
import org.asf.centuria.packets.xt.IXtPacket;

public class YesNoPopupPacket implements IXtPacket<YesNoPopupPacket> {

	private boolean match;
	public String id;
	public String title;
	public String message;
	public String yesButton;
	public String noButton;
	private boolean state;

	@Override
	public String id() {
		return "mod:ft";
	}

	@Override
	public YesNoPopupPacket instantiate() {
		return new YesNoPopupPacket();
	}

	@Override
	public void build(XtWriter writer) throws IOException {
		writer.writeInt(DATA_PREFIX);
		writer.writeString("yesnopopup");
		writer.writeString(id);
		writer.writeString(title);
		writer.writeString(message);
		writer.writeString(yesButton);
		writer.writeString(noButton);
		writer.writeString(DATA_SUFFIX);
	}

	@Override
	public void parse(XtReader reader) throws IOException {
		match = reader.read().equals("yesnopopup");
		if (!match)
			return;
		id = reader.read();
		state = reader.readInt() == 1;
	}

	@Override
	public boolean handle(SmartfoxClient client) throws IOException {
		if (match) {
			// It is a Yes/no popup

			// Handle popup
			if (id.startsWith("playersent/")) {
				String player = id.substring("playersent/".length());
				Player plr = Centuria.gameServer.getPlayer(player);
				if (plr != null) {
					// Check stat
					String msg = ((Player) client.container).account.getDisplayName() + " replied with ";
					if (state) {
						// Yes
						msg += " 'Yes' to your popup.";
					} else {
						// No
						msg += " 'No' to your popup.";
					}

					// Send message
					if (plr.getObject(FeralTweaksClientObject.class) != null
							&& plr.getObject(FeralTweaksClientObject.class).isEnabled()) {
						// Send notif
						NotificationPacket pkt = new NotificationPacket();
						pkt.message = msg;
						plr.client.sendPacket(pkt);
					}

					// Use dms
					Centuria.systemMessage(plr, msg, true);
				}
			}
		}
		return false;
	}

}
