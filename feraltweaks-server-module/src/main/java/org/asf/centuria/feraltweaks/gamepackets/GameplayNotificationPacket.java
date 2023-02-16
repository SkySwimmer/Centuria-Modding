package org.asf.centuria.feraltweaks.gamepackets;

import java.io.IOException;

import org.asf.centuria.data.XtReader;
import org.asf.centuria.data.XtWriter;
import org.asf.centuria.networking.smartfox.SmartfoxClient;
import org.asf.centuria.packets.xt.IXtPacket;

public class GameplayNotificationPacket implements IXtPacket<GameplayNotificationPacket> {

	public String message = "";
	public String icon = null;

	@Override
	public String id() {
		return "mod:ft";
	}

	@Override
	public GameplayNotificationPacket instantiate() {
		return new GameplayNotificationPacket();
	}

	@Override
	public void build(XtWriter writer) throws IOException {
		writer.writeInt(DATA_PREFIX);
		writer.writeString("gpnotification");
		writer.writeString(message);
		writer.writeInt(icon != null ? 1 : 0);
		if (icon != null)
			writer.writeString(icon);
		writer.writeString(DATA_SUFFIX);
	}

	@Override
	public void parse(XtReader reader) throws IOException {
	}

	@Override
	public boolean handle(SmartfoxClient client) throws IOException {
		return false;
	}

}
