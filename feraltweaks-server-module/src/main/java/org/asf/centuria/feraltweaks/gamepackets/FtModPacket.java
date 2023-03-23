package org.asf.centuria.feraltweaks.gamepackets;

import java.io.IOException;

import org.asf.centuria.data.XtReader;
import org.asf.centuria.data.XtWriter;
import org.asf.centuria.networking.smartfox.SmartfoxClient;
import org.asf.centuria.packets.xt.IXtPacket;

public class FtModPacket implements IXtPacket<FtModPacket> {

	public String modID;
	public String pktID;
	public String payload;

	@Override
	public boolean canParse(String content) {
		if (!content.startsWith("%xt%"))
			return false;

		XtReader rd = new XtReader(content);
		String packetID = rd.read();
		if (!packetID.startsWith("mod:"))
			return false;
		return true;
	}

	@Override
	public boolean parse(String content) throws IOException {
		if (!content.startsWith("%xt%"))
			return false;

		XtReader rd = new XtReader(content);
		String packetID = rd.read();
		if (!packetID.startsWith("mod:"))
			return false;
		modID = packetID.substring(4);
		parse(rd);
		return true;
	}

	@Override
	public String id() {
		return "mod:" + modID;
	}

	@Override
	public FtModPacket instantiate() {
		return new FtModPacket();
	}

	@Override
	public void build(XtWriter writer) throws IOException {
		writer.writeInt(DATA_PREFIX);
		writer.writeString(pktID);
		writer.writeString(payload);
		writer.writeString(DATA_SUFFIX);
	}

	@Override
	public void parse(XtReader reader) throws IOException {
		pktID = reader.read();
		payload = reader.readRemaining();
	}

	@Override
	public boolean handle(SmartfoxClient client) throws IOException {
		return false;
	}
}
