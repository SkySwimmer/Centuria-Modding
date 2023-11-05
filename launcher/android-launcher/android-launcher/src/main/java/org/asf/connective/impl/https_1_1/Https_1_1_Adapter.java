package org.asf.connective.impl.https_1_1;

import java.io.File;
import java.io.IOException;
import java.net.InetAddress;
import java.nio.file.Files;
import java.util.Map;

import org.asf.connective.ConnectiveHttpServer;
import org.asf.connective.IServerAdapterDefinition;

public class Https_1_1_Adapter implements IServerAdapterDefinition {

	@Override
	public String getName() {
		return "HTTPS/1.1";
	}

	@Override
	public ConnectiveHttpServer createServer(Map<String, String> configuration) throws IllegalArgumentException {
		ConnectiveHttpsServer_1_1 server = new ConnectiveHttpsServer_1_1();
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
		if (configuration.containsKey("Keystore"))
			try {
				// Find password
				char[] password = null;
				if (configuration.containsKey("KeystorePassword"))
					password = configuration.get("KeystorePassword").toCharArray();
				else if (configuration.containsKey("Keystore-Password"))
					password = configuration.get("Keystore-Password").toCharArray();
				else if (configuration.containsKey("Keystore-password"))
					password = configuration.get("Keystore-password").toCharArray();
				else if (configuration.containsKey("keystore-password"))
					password = configuration.get("keystore-password").toCharArray();

				// If still no password, find by file
				String path = configuration.get("Keystore");
				if (password == null && new File(path + ".password").exists())
					password = new String(Files.readAllBytes(new File(path + ".password").toPath()), "UTF-8")
							.toCharArray();
				if (password == null)
					throw new IllegalArgumentException(
							"No keystore password found, assign the Keystore-Password configuration field to assign it");

				// Load
				server.loadTlsContextFrom(new File(path), password);
			} catch (IOException e) {
				throw new IllegalArgumentException(
						"Failed to load keystore for TLS encryption from " + configuration.get("Keystore"), e);
			}
		if (configuration.containsKey("keystore"))
			try {
				// Find password
				char[] password = null;
				if (configuration.containsKey("KeystorePassword"))
					password = configuration.get("KeystorePassword").toCharArray();
				else if (configuration.containsKey("Keystore-Password"))
					password = configuration.get("Keystore-Password").toCharArray();
				else if (configuration.containsKey("Keystore-password"))
					password = configuration.get("Keystore-password").toCharArray();
				else if (configuration.containsKey("keystore-password"))
					password = configuration.get("keystore-password").toCharArray();

				// If still no password, find by file
				String path = configuration.get("keystore");
				if (password == null && new File(path + ".password").exists())
					password = new String(Files.readAllBytes(new File(path + ".password").toPath()), "UTF-8")
							.toCharArray();
				if (password == null)
					throw new IllegalArgumentException(
							"No keystore password found, assign the Keystore-Password configuration field to assign it");

				// Load
				server.loadTlsContextFrom(new File(path), password);
			} catch (IOException e) {
				throw new IllegalArgumentException(
						"Failed to load keystore for TLS encryption from " + configuration.get("keystore"), e);
			}
		return server;
	}

}
