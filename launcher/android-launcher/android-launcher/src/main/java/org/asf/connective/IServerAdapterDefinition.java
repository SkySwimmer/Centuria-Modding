package org.asf.connective;

import java.util.Map;

/**
 * 
 * Server Adapter Definition Interface - Helper interface for locating server
 * implementations.
 * 
 * @author Sky Swimmer
 *
 */
public interface IServerAdapterDefinition {

	/**
	 * Retrieves the adapter name
	 * 
	 * @return Adapter name string
	 */
	public String getName();

	/**
	 * Creates a server instance
	 * 
	 * @param configuration Configuration for the server (use this to assign ports
	 *                      etc from a configuration file)
	 * @return ConnectiveHttpServer instance
	 * @throws IllegalArgumentException If the configuration is invalid
	 */
	public ConnectiveHttpServer createServer(Map<String, String> configuration) throws IllegalArgumentException;

}
