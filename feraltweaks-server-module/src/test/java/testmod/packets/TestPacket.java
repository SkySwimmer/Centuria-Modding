package testmod.packets;

import java.io.IOException;

import org.asf.centuria.data.XtReader;
import org.asf.centuria.data.XtWriter;
import org.asf.centuria.entities.players.Player;
import org.asf.centuria.feraltweaks.api.networking.IFeralTweaksPacket;
import org.asf.centuria.feraltweaks.api.networking.ServerMessenger;

public class TestPacket implements IFeralTweaksPacket<TestPacket> {

	public String payload;

	@Override
	public String id() {
		return "test";
	}

	@Override
	public TestPacket instantiate() {
		return new TestPacket();
	}

	@Override
	public void parse(XtReader reader) throws IOException {
		payload = reader.read();
	}

	@Override
	public void build(XtWriter writer) throws IOException {
		writer.writeString(payload);
	}

	@Override
	public boolean handle(Player player, ServerMessenger messenger) throws IOException {
		// Handle

		// Send response
		TestPacket resp = new TestPacket();
		resp.payload = "Hi";
		messenger.sendPacket(resp, player);

		return true;
	}

}
