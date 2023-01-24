package org.asf.centuria.feraltweaks.gamepackets;

import java.io.IOException;

import org.asf.centuria.data.XtReader;
import org.asf.centuria.data.XtWriter;
import org.asf.centuria.networking.smartfox.SmartfoxClient;
import org.asf.centuria.packets.xt.IXtPacket;

public class ErrorPopupPacket implements IXtPacket<ErrorPopupPacket> {

	public String title;
	public String message;
	
	@Override
	public String id() {
		return "mod:ft";
	}

	@Override
	public ErrorPopupPacket instantiate() {
		return new ErrorPopupPacket();
	}

	@Override
	public void build(XtWriter writer) throws IOException {
		writer.writeInt(DATA_PREFIX);
		writer.writeString("errorpopup");
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
