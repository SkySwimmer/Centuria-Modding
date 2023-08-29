package org.asf.centuria.feraltweaks.networking.game;

import java.io.IOException;

import org.asf.centuria.data.XtReader;
import org.asf.centuria.data.XtWriter;
import org.asf.centuria.networking.smartfox.SmartfoxClient;
import org.asf.centuria.packets.xt.IXtPacket;

public class OkPopupPacket implements IXtPacket<OkPopupPacket> {

	public String title;
	public String message;
	
	@Override
	public String id() {
		return "mod:ft";
	}

	@Override
	public OkPopupPacket instantiate() {
		return new OkPopupPacket();
	}

	@Override
	public void build(XtWriter writer) throws IOException {
		writer.writeInt(DATA_PREFIX);
		writer.writeString("okpopup");
		writer.writeString(title);
		writer.writeString(message);
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
