package org.asf.rats.processors;

import java.io.InputStream;
import java.net.Socket;

import org.asf.rats.ConnectiveHTTPServer;
import org.asf.rats.HttpRequest;

/**
 * 
 * HTTP Upload Request Processor, processes post, delete and put requests.
 * 
 * @author Stefan0436 - AerialWorks Software Foundation
 *
 */
public abstract class HttpUploadProcessor extends HttpGetProcessor {

	@Override
	public void process(Socket client) {
		process(null, client, "GET");
	}

	/**
	 * Retrieves the body text of the request.
	 * 
	 * @return Body text.
	 */
	protected String getRequestBody() {
		return getRequest().getRequestBody();
	}

	/**
	 * Retrieves the body input stream. (unusable if getBody was called)
	 * 
	 * @return Body input stream.
	 */
	protected InputStream getRequestBodyStream() {
		return getRequest().getRequestBodyStream();
	}

	/**
	 * Instanciates a new processor with the server and request.
	 * 
	 * @param server  Server to use
	 * @param request HTTP request
	 * @return New HttpUploadProcessor configured for processing.
	 */
	public HttpUploadProcessor instanciate(ConnectiveHTTPServer server, HttpRequest request) {
		return (HttpUploadProcessor) super.instanciate(server, request);
	}

	/**
	 * Checks if the processor support get requests, false by default.
	 * 
	 * @return True if the processor supports this, false otherwise.
	 */
	public boolean supportsGet() {
		return false;
	}

	/**
	 * Processes a request, the upload-specific parameters will be null if get is
	 * used.
	 * 
	 * @param contentType Content type
	 * @param client      Client used to contact the server.
	 * @param method      Method used. (GET, PUT, DELETE or POST)
	 */
	public abstract void process(String contentType, Socket client, String method);

	/**
	 * Creates an instance for processing HTTP requests.
	 * 
	 * @return New instance of this processor.
	 */
	public abstract HttpUploadProcessor createNewInstance();

}
