package org.asf.connective.impl.https_1_1;

import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.net.InetAddress;
import java.net.ServerSocket;
import java.nio.file.Files;
import java.security.KeyManagementException;
import java.security.KeyStore;
import java.security.KeyStoreException;
import java.security.NoSuchAlgorithmException;
import java.security.UnrecoverableKeyException;
import java.security.cert.CertificateException;

import javax.net.ssl.KeyManagerFactory;
import javax.net.ssl.SSLContext;

import org.asf.connective.TlsSecuredHttpServer;
import org.asf.connective.impl.http_1_1.ConnectiveHttpServer_1_1;

public class ConnectiveHttpsServer_1_1 extends ConnectiveHttpServer_1_1 implements TlsSecuredHttpServer {
	private SSLContext context = null;

	public ConnectiveHttpsServer_1_1() {
		super();
		port = 8043;
	}

	@Override
	public void start() throws IOException {
		super.start();
	}

	@Override
	public void loadTlsContextFrom(File keystoreFile, char[] password) throws IOException {
		try {
			KeyStore mainStore = KeyStore.getInstance("JKS");
			mainStore.load(new FileInputStream(keystoreFile), password);
			loadTlsContextFrom(mainStore, password);
		} catch (IOException | NoSuchAlgorithmException | CertificateException | KeyStoreException e) {
			throw new IOException("Failed to load keystore file from " + keystoreFile.getPath(), e);
		}
	}

	@Override
	public void loadTlsContextFrom(KeyStore keystore, char[] password) throws IOException {
		try {
			KeyManagerFactory managerFactory = KeyManagerFactory.getInstance("SunX509");
			managerFactory.init(keystore, password);

			SSLContext cont = SSLContext.getInstance("TLS");
			cont.init(managerFactory.getKeyManagers(), null, null);
			context = cont;
		} catch (KeyManagementException | NoSuchAlgorithmException | UnrecoverableKeyException | KeyStoreException e) {
			throw new IOException("Failed to initialize the SSLContext with the given keystore and password", e);
		}
	}

	@Override
	public ServerSocket getServerSocket(int port, InetAddress addr) throws IOException {
		if (context == null) {
			// Attempt to load from file
			File keystore = new File("keystore.jks");
			if (keystore.exists()) {
				// Try to load
				File keystorePassword = new File("keystore.jks.password");
				if (keystorePassword.exists())
					loadTlsContextFrom(keystore,
							new String(Files.readAllBytes(keystorePassword.toPath()), "UTF-8").toCharArray());
				else
					throw new IOException(
							"Default keystore file (keystore.jks) needs another file next to it named keystore.jks.password containing the keystore password for this to function.");
			} else {
				throw new IOException(
						"No keystore.jks file and no configuration provided to create a TLS-encrypted HTTP server instance");
			}
		}
		return context.getServerSocketFactory().createServerSocket(port, 0, addr);
	}

}
