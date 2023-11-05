package org.asf.connective.impl.http_1_1;

import java.net.InetAddress;
import java.util.Map;

import org.asf.connective.ConnectiveHttpServer;
import org.asf.connective.IServerAdapterDefinition;

public class Http_1_1_Adapter implements IServerAdapterDefinition {

	@Override
	public String getName() {
		return "HTTP/1.1";
	}

	@Override
	public ConnectiveHttpServer createServer(Map<String, String> configuration) throws IllegalArgumentException {
		ConnectiveHttpServer_1_1 server = new ConnectiveHttpServer_1_1();
		if (configuration.containsKey("address"))
			try {
				server.setListenAddress(InetAddress.getByName(configuration.get("address")));
			} catch (Exception e) {
				throw new IllegalArgumentException("Malformed listen address: " + configuration.get("address"));
			}
		if (configuration.containsKey("Address"))
			try {
				server.setListenAddress(InetAddress.getByName(configuration.get("Address")));
			} catch (Exception e) {
				throw new IllegalArgumentException("Malformed listen address: " + configuration.get("Address"));
			}
		if (configuration.containsKey("port"))
			try {
				server.setListenPort(Integer.parseInt(configuration.get("port")));
			} catch (Exception e) {
				throw new IllegalArgumentException("Malformed port: " + configuration.get("port"));
			}
		if (configuration.containsKey("Port"))
			try {
				server.setListenPort(Integer.parseInt(configuration.get("Port")));
			} catch (Exception e) {
				throw new IllegalArgumentException("Malformed port: " + configuration.get("Port"));
			}
		return server;
	}

}
