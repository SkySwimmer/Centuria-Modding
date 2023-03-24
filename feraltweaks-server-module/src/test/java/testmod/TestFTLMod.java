package testmod;

import org.asf.centuria.feraltweaks.api.FeralTweaksMod;

import testmod.packets.TestPacket;

public class TestFTLMod extends FeralTweaksMod {

	@Override
	public String id() {
		return "test";
	}

	@Override
	public String version() {
		return "1.0.0.0";
	}

	@Override
	public void init() {
		this.registerPacket(new TestPacket());
		this.addModHandshakeRequirement("test123");
	}

}
