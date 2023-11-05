package org.asf.connective.processors;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.nio.charset.Charset;

import org.asf.connective.ConnectiveHttpServer;
import org.asf.connective.RemoteClient;
import org.asf.connective.objects.HttpRequest;
import org.asf.connective.objects.HttpResponse;

/**
 * 
 * HTTP Upload Processor
 * 
 * @author Sky Swimmer
 *
 */
public abstract class HttpPushProcessor extends HttpRequestProcessor {

	@Override
	public void process(String path, String method, RemoteClient client) throws IOException {
		process(path, method, client, null);
	}

	/**
	 * Retrieves the body content stream
	 * 
	 * @return Stream that leads to the request content body
	 */
	protected InputStream getRequestBody() {
		return getRequest().getBodyStream();
	}

	/**
	 * Retrieves the body content as string
	 * 
	 * @return String representing the request body
	 * @throws IOException If reading fails
	 */
	protected String getRequestBodyAsString() throws IOException {
		return getRequest().getRequestBodyAsString();
	}

	/**
	 * Retrieves the body content as string
	 * 
	 * @param encoding Encoding to use
	 * @return String representing the request body
	 * @throws IOException If reading fails
	 */
	protected String getRequestBodyAsString(String encoding) throws IOException {
		return getRequest().getRequestBodyAsString(encoding);
	}

	/**
	 * Retrieves the body content as string
	 * 
	 * @param encoding Encoding to use
	 * @return String representing the request body
	 * @throws IOException If reading fails
	 */
	protected String getRequestBodyAsString(Charset encoding) throws IOException {
		return getRequest().getRequestBodyAsString(encoding);
	}

	/**
	 * Transfers the request body
	 * 
	 * @param output Target stream
	 * @throws IOException If transferring fails
	 */
	protected void transferRequestBody(OutputStream output) throws IOException {
		getRequest().transferRequestBody(output);
	}

	/**
	 * Retrieves the request body length
	 * 
	 * @return Request body length
	 */
	protected long getRequestBodyLength() {
		return getRequest().getBodyLength();
	}

	/**
	 * Instantiates a new processor with the server, request and response
	 * 
	 * @param server   Server to use
	 * @param request  HTTP request
	 * @param response HTTP response
	 * @return New HttpGetProcessor configured for processing
	 */
	public HttpPushProcessor instantiate(ConnectiveHttpServer server, HttpRequest request, HttpResponse response) {
		return (HttpPushProcessor) super.instantiate(server, request, response);
	}

	/**
	 * Checks if the processor support non-push requests, false by default
	 * 
	 * @return True if the processor supports this, false otherwise
	 */
	public boolean supportsNonPush() {
		return false;
	}

	/**
	 * Called to handle the request
	 * 
	 * @param path        Path string
	 * @param method      Request method
	 * @param client      Remote client
	 * @param contentType Body content type
	 * @throws IOException If processing fails
	 */
	public abstract void process(String path, String method, RemoteClient client, String contentType)
			throws IOException;

	/**
	 * Creates a new instance of this HTTP processor
	 */
	public abstract HttpPushProcessor createNewInstance();

}
