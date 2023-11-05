package org.asf.connective;

import java.net.InetAddress;

/**
 * 
 * Simple abstract common to all network-based connective implementations
 * 
 * @author Sky Swimmer
 *
 */
public abstract class NetworkedConnectiveHttpServer extends ConnectiveHttpServer {

	/**
	 * Assigns the address to listen on
	 * 
	 * @param address Address to listen on
	 */
	public abstract void setListenAddress(InetAddress address);

	/**
	 * Assigns the port to listen on
	 * 
	 * @param port Port to listen on
	 */
	public abstract void setListenPort(int port);

	/**
	 * Retrieves the address the server is listening on
	 * 
	 * @return Listen IP address
	 */
	public abstract InetAddress getListenAddress();

	/**
	 * Retrieves the port the server is listening on
	 * 
	 * @return Listen port
	 */
	public abstract int getListenPort();

}
