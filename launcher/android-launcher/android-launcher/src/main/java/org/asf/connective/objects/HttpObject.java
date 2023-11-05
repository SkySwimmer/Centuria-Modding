package org.asf.connective.objects;

import java.io.InputStream;

import org.asf.connective.headers.HeaderCollection;
import org.asf.connective.headers.HttpHeader;

/**
 * 
 * Abstract HTTP object, headers and object body.
 * 
 * @author Sky Swimmer
 *
 */
public abstract class HttpObject {

	protected HeaderCollection headers = HeaderCollection.create();

	/**
	 * Retrieves the body content stream
	 * 
	 * @return Stream that leads to this HTTP object's content body
	 */
	public abstract InputStream getBodyStream();

	/**
	 * Retrieves the headers of the HTTP object
	 * 
	 * @return HeaderCollection instance
	 */
	public HeaderCollection getHeaders() {
		return headers;
	}

	/**
	 * Adds HTTP headers (if a header is already present, it is overwritten)
	 * 
	 * @param name  Header name
	 * @param value Header value
	 * @return HttpHeader instance
	 */
	public HttpHeader addHeader(String name, String value) {
		return headers.addHeader(name, value);
	}

	/**
	 * Adds HTTP headers
	 * 
	 * @param name   Header name
	 * @param value  Header value
	 * @param append True to add to the existing header if present, false to
	 *               overwrite values (clears the header if already present)
	 * @return HttpHeader instance
	 */
	public HttpHeader addHeader(String name, String value, boolean append) {
		return headers.addHeader(name, value, append);
	}

	/**
	 * Retrieves the amount of headers that are present in the set
	 * 
	 * @return Header count
	 */
	public int getHeaderCount() {
		return headers.getHeaderCount();
	}

	/**
	 * Checks if a header is present
	 * 
	 * @param header Header name
	 * @return True if present, false otherwise
	 */
	public boolean hasHeader(String header) {
		return headers.hasHeader(header);
	}

	/**
	 * Retrieves HTTP headers
	 * 
	 * @param header Header name
	 * @return HttpHeader instance or null
	 */
	public HttpHeader getHeader(String header) {
		return headers.getHeader(header);
	}

	/**
	 * Retrieves HTTP header values
	 * 
	 * @param header Header name
	 * @return Header value or null
	 */
	public String getHeaderValue(String header) {
		return headers.getHeaderValue(header);
	}

	/**
	 * Retrieves HTTP header values
	 * 
	 * @param header Header name
	 * @return Array of value strings
	 */
	public String[] getHeaderValues(String header) {
		return headers.getHeaderValues(header);
	}

	/**
	 * Removes HTTP headers
	 * 
	 * @param header Header name
	 * @return Header that was removed or null
	 */
	public HttpHeader removeHeader(String header) {
		return headers.removeHeader(header);
	}

	/**
	 * Retrieves all header names
	 * 
	 * @return Array of header name strings
	 */
	public String[] getHeaderNames() {
		return headers.getHeaderNames();
	}

	/**
	 * Clears the header collection
	 */
	public void clearHeaders() {
		headers.clearHeaders();
	}

}
