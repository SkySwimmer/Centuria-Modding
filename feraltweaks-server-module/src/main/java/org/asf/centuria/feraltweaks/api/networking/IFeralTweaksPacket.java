package org.asf.centuria.feraltweaks.api.networking;

import java.io.IOException;

import org.asf.centuria.data.XtReader;
import org.asf.centuria.data.XtWriter;
import org.asf.centuria.entities.players.Player;

/**
 *
 * 
 * Class for creating mod packets.
 * 
 * @author Sky Swimmer
 *
 *
 */
public interface IFeralTweaksPacket<T extends IFeralTweaksPacket<T>> {

	/**
	 * Creates a new instance of this packet type
	 * 
	 * @return New packet instance
	 */
	public T instantiate();

	/**
	 * Defines the packet ID
	 * 
	 * @return Packet ID string
	 */
	public abstract String id();

	/**
	 * Reads the packet content
	 * 
	 * @param reader Packet reader
	 */
	public void parse(XtReader reader) throws IOException;

	/**
	 * Writes the packet content to the output writer
	 * 
	 * @param writer Packet writer
	 */
	public void build(XtWriter writer) throws IOException;

	/**
	 * Called to handle the packet
	 * 
	 * @param player Player instance
	 * @return True if handled, false otherwise
	 * @throws IOException If handling fails
	 */
	public boolean handle(Player player) throws IOException;

}
