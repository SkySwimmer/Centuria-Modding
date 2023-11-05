package org.asf.connective;

import java.io.File;
import java.io.IOException;
import java.security.KeyStore;

/**
 *
 * Interface meant to be common to all SLL/TLS-encrypted connective HTTP server
 * implementation
 * 
 * @author Sky Swimmer
 *
 */
public interface TlsSecuredHttpServer {

	/**
	 * Loads a TLS context from a keystore instance
	 * 
	 * @param keystore Keystore instance
	 * @param password Keystore password
	 * @throws IOException If loading fails
	 */
	public void loadTlsContextFrom(KeyStore keystore, char[] password) throws IOException;

	/**
	 * Loads a TLS context from a keystore file
	 * 
	 * @param keystoreFile Keystore file
	 * @param password     Keystore file password
	 * @throws IOException If loading fails
	 */
	public void loadTlsContextFrom(File keystoreFile, char[] password) throws IOException;

}
