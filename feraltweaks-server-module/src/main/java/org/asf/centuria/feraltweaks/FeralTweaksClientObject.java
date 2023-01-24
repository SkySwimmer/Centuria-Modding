package org.asf.centuria.feraltweaks;

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

	public FeralTweaksClientObject(boolean enabled, String version) {
		this.enabled = enabled;
		this.version = version;
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
