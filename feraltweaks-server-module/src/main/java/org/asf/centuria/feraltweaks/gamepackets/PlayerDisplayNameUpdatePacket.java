package org.asf.centuria.feraltweaks.gamepackets;

import java.io.IOException;

import org.asf.centuria.data.XtReader;
import org.asf.centuria.data.XtWriter;
import org.asf.centuria.networking.smartfox.SmartfoxClient;
import org.asf.centuria.packets.xt.IXtPacket;

public class PlayerDisplayNameUpdatePacket implements IXtPacket<PlayerDisplayNameUpdatePacket> {

	public String id;
	public String name;

	@Override
	public String id() {
		return "mod:ft";
	}

	@Override
	public PlayerDisplayNameUpdatePacket instantiate() {
		return new PlayerDisplayNameUpdatePacket();
	}

	@Override
	public void build(XtWriter writer) throws IOException {
		writer.writeInt(DATA_PREFIX);
		writer.writeString("displaynameupdate");
		writer.writeString(id);
		writer.writeString(name);
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
