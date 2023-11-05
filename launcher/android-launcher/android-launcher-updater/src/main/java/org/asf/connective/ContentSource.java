package org.asf.connective;

import java.io.IOException;

import org.asf.connective.objects.HttpRequest;
import org.asf.connective.objects.HttpResponse;

/**
 * 
 * HTTP Content Source - Called to process HTTP requests
 * 
 * @author Sky Swimmer
 *
 */
public abstract class ContentSource {
	ContentSource parent;

	/**
	 * Retrieves the parent content source
	 * 
	 * @return ContentSource instance or null
	 */
	protected ContentSource getParent() {
		return parent;
	}

	/**
	 * Processes HTTP requests
	 * 
	 * @param path     Request path
	 * @param request  Request object
	 * @param response Response output object
	 * @param client   Client making the request
	 * @param server   Server instance
	 * @return True if successful, false otherwise
	 * @throws IOException If processing fails
	 */
	public abstract boolean process(String path, HttpRequest request, HttpResponse response, RemoteClient client,
			ConnectiveHttpServer server) throws IOException;

}
