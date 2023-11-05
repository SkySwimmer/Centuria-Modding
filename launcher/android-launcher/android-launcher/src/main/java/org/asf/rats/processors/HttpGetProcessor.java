package org.asf.rats.processors;

import java.io.IOException;
import java.io.InputStream;
import java.net.Socket;
import java.util.HashMap;

import org.asf.rats.ConnectiveHTTPServer;
import org.asf.rats.HttpRequest;
import org.asf.rats.HttpResponse;

public abstract class HttpGetProcessor {

	private ConnectiveHTTPServer server;
	private HttpResponse response;
	private HttpRequest request;

	/**
	 * Instanciates a new processor with the server and request.
	 * 
	 * @param server  Server to use
	 * @param request HTTP request
	 * @return New HttpGetProcessor configured for processing.
	 */
	public HttpGetProcessor instanciate(ConnectiveHTTPServer server, HttpRequest request) {
		HttpGetProcessor inst = createNewInstance();
		inst.server = server;
		inst.request = request;
		if (request.headers.containsKey("Connection"))  {
			inst.getResponse().headers.put("Connection", request.headers.get("Connection"));
		}
		return inst;
	}

	/**
	 * Retrieves the request HTTP headers.
	 * 
	 * @return Request HTTP headers.
	 */
	protected HashMap<String, String> getHeaders() {
		return getRequest().headers;
	}

	/**
	 * Retrieves a specific request HTTP header.
	 * 
	 * @return HTTP header value.
	 */
	protected String getHeader(String name) {
		return getRequest().headers.get(name);
	}

	/**
	 * Checks if a specific request HTTP header is present.
	 * 
	 * @return True if the header is present, false otherwise.
	 */
	protected boolean hasHeader(String name) {
		return getRequest().headers.containsKey(name);
	}

	/**
	 * Assigns the value of the given HTTP header.
	 * 
	 * @param header Header name
	 * @param value  Header value
	 */
	protected void setResponseHeader(String header, String value) {
		getResponse().setHeader(header, value);
	}

	/**
	 * Retrieves the server processing the request.
	 * 
	 * @return ConnectiveHTTPServer instance.
	 */
	protected ConnectiveHTTPServer getServer() {
		return server;
	}

	/**
	 * Sets the response object
	 * 
	 * @param response HttpResponse instance.
	 */
	protected void setResponse(HttpResponse response) {
		this.response = response;
	}

	/**
	 * Sets the response body
	 * 
	 * @param type Content type
	 * @param body Response body string
	 */
	protected void setBody(String type, String body) {
		getResponse().setContent(type, body);
	}

	/**
	 * Sets the response body (plaintext)
	 * 
	 * @param body Response body string
	 */
	protected void setBody(String body) {
		setBody("text/plain", body);
	}

	/**
	 * Sets the response body (binary)
	 * 
	 * @param body Response body string
	 */
	protected void setBody(byte[] body) {
		getResponse().setContent("application/octet-stream", body);
	}

	/**
	 * Sets the response body (InputStream)
	 * 
	 * @param body Response body string
	 * @throws IOException If reading the available bytes fails.
	 */
	protected void setBody(InputStream body) throws IOException {
		getResponse().setContent("application/octet-stream", body);
	}

	/**
	 * Sets the response code.
	 * 
	 * @param code Response status code.
	 */
	protected void setResponseCode(int code) {
		getResponse().status = code;
	}

	/**
	 * Sets the response message.
	 * 
	 * @param message Response message.
	 */
	protected void setResponseMessage(String message) {
		getResponse().message = message;
	}

	/**
	 * Retrieves the HTTP request object.
	 * 
	 * @return HttpRequest instance.
	 */
	protected HttpRequest getRequest() {
		return request;
	}

	/**
	 * Retrieves the HTTP response object.
	 * 
	 * @return HttpResponse instance.
	 */
	public HttpResponse getResponse() {
		if (response == null)
			response = new HttpResponse(200, "OK", getRequest()).addDefaultHeaders(getServer());

		return response;
	}

	/**
	 * Retrieves the HTTP request 'path'
	 * 
	 * @return Request path
	 */
	public String getRequestPath() {
		return getRequest().path;
	}

	/**
	 * Retrieves the path this processor supports
	 * 
	 * @return File path string.
	 */
	public abstract String path();

	/**
	 * Checks if this processor supports child paths, false by default.
	 * 
	 * @return True if the processor supports this, false otherwise.
	 */
	public boolean supportsChildPaths() {
		return false;
	}

	public abstract void process(Socket client);

	public abstract HttpGetProcessor createNewInstance();
}
