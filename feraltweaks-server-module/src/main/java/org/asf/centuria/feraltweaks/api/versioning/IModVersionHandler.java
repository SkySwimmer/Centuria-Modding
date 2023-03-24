package org.asf.centuria.feraltweaks.api.versioning;

import java.util.Map;

/**
 * 
 * Interface for handling mod version handshake rules
 * 
 * @author Sky Swimmer
 *
 */
public interface IModVersionHandler {

	/**
	 * Retrieves the client mod handshake rules
	 * 
	 * @return Map of handshake rules expected for client mods (id and version check
	 *         string pairs)
	 */
	public Map<String, String> getClientModVersionRules();

}
