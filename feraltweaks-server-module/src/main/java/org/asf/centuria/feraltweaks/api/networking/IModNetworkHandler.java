package org.asf.centuria.feraltweaks.api.networking;

/**
 * 
 * Interface used by FeralTweaks to bind mods to the server networking system
 * 
 * @author Sky Swimmer
 *
 */
public interface IModNetworkHandler {

	/**
	 * Retrieves the network messenger used to handle and send packets for this mod
	 * 
	 * @return ServerMessenger instance
	 */
	public ServerMessenger getMessenger();

}
