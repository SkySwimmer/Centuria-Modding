package org.asf.centuria.feraltweaks;

import java.util.Map;

/**
 * 
 * Object for holding information about the remote FeralTweaks installation
 * 
 * @author Sky Swimmer
 *
 */
public class FeralTweaksClientObject {

	private boolean enabled;
	private String version;
	private Map<String, String> mods;

	public FeralTweaksClientObject(boolean enabled, String version, Map<String, String> mods) {
		this.enabled = enabled;
		this.version = version;
		this.mods = mods;
	}

	/**
	 * Checks if a client mod is loaded
	 * 
	 * @param id Client mod ID
	 * @return True if loaded, false otherwise
	 */
	public boolean isClientModLoaded(String id) {
		return mods.containsKey(id);
	}

	/**
	 * Retrieves the version of a client mod
	 * 
	 * @param id Client mod ID
	 * @return Version string or null if not found
	 */
	public String getClientModVersion(String id) {
		return mods.get(id);
	}

	/**
	 * Retrieves all known client mods
	 * 
	 * @return Array of client mod IDs
	 */
	public String[] getClientMods() {
		return mods.keySet().toArray(t -> new String[t]);
	}

	/**
	 * Retrieves the feraltweaks version
	 * 
	 * @return Version string or null if not enabled
	 */
	public String getFeralTweaksVersion() {
		return version;
	}

	/**
	 * Checks if feraltweaks support is enabled
	 * 
	 * @return True if enabled, false otherwise
	 */
	public boolean isEnabled() {
		return enabled;
	}

}
