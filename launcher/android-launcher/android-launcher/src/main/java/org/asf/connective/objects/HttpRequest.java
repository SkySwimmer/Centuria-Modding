package org.asf.connective.objects;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.URLDecoder;
import java.nio.charset.Charset;
import java.util.LinkedHashMap;
import java.util.Map;

import org.asf.connective.headers.HeaderCollection;
import org.asf.connective.io.IoUtil;

/**
 * 
 * HTTP Request Object
 * 
 * @author Sky Swimmer
 *
 */
public class HttpRequest extends HttpObject {

	private InputStream body;
	private String httpVersion;
	private long bodyContentLength = -1;
	private String requestMethod;
	private String requestResource;
	private String requestPath;
	private String requestQuery;

	private LinkedHashMap<String, String> queryParameters = new LinkedHashMap<String, String>();

	/**
	 * Creates a HttpRequest object instance
	 * 
	 * @param body              Body stream
	 * @param bodyContentLength Length of the request body;
	 * @param headers           Request headers
	 * @param httpVersion       HTTP version
	 * @param requestMethod     Request method
	 * @param requestResource   Request path with query
	 * @throws IllegalArgumentException If the request is not valid
	 */
	public HttpRequest(InputStream body, long bodyContentLength, HeaderCollection headers, String httpVersion,
			String requestMethod, String requestResource) throws IllegalArgumentException {
		this.body = body;
		this.bodyContentLength = bodyContentLength;
		this.headers = headers;
		this.httpVersion = httpVersion;
		this.requestResource = requestResource;
		this.requestMethod = requestMethod.toUpperCase();

		// Parse request
		requestPath = "";
		requestQuery = "";
		try {
			// Parse path and split query off of it
			requestPath = requestResource;
			if (requestPath.contains("?")) {
				// Remove query and save it to its own field
				requestQuery = requestPath.substring(requestPath.indexOf("?") + 1);
				requestPath = requestPath.substring(0, requestPath.indexOf("?"));
			}

			// Parse request path
			requestPath = URLDecoder.decode(requestPath, "UTF-8");

			// Sanitize path
			if (requestPath.contains("\\"))
				requestPath = requestPath.replace("\\", "/");
			while (requestPath.startsWith("/"))
				requestPath = requestPath.substring(1);
			while (requestPath.endsWith("/"))
				requestPath = requestPath.substring(0, requestPath.length() - 1);
			while (requestPath.contains("//"))
				requestPath = requestPath.replace("//", "/");
			if (!requestPath.startsWith("/"))
				requestPath = "/" + requestPath;
		} catch (Exception e) {
			// Malformed
			throw new IllegalArgumentException("Malformed request");
		}

		// Make sure its not attempting to access a resource outside of the scope
		if (requestPath.startsWith("..") || requestPath.endsWith("..") || requestPath.contains("/..")
				|| requestPath.contains("../"))
			throw new IllegalArgumentException("Invalid resource requested, forbidden path");

		// Parse query
		String key = "";
		String value = "";
		boolean isKey = true;
		for (int i = 0; i < requestQuery.length(); i++) {
			char ch = requestQuery.charAt(i);
			if (ch == '&') {
				if (isKey && !key.isEmpty()) {
					queryParameters.put(key, "");
					key = "";
				} else if (!isKey && !key.isEmpty()) {
					try {
						queryParameters.put(key, URLDecoder.decode(value, "UTF-8"));
					} catch (Exception e) {
						queryParameters.put(key, value);
					}
					isKey = true;
					key = "";
					value = "";
				}
			} else if (ch == '=') {
				isKey = !isKey;
			} else {
				if (isKey) {
					key += ch;
				} else {
					value += ch;
				}
			}
		}
		if (!key.isEmpty() || !value.isEmpty()) {
			try {
				queryParameters.put(key, URLDecoder.decode(value, "UTF-8"));
			} catch (Exception e) {
				queryParameters.put(key, value);
			}
		}
	}

	/**
	 * Checks if the HTTP request has a request body
	 * 
	 * @return True if a request body is present, false otherwise
	 */
	public boolean hasRequestBody() {
		return body != null;
	}

	/**
	 * Retrieves the HTTP request path
	 * 
	 * @return Request path string
	 */
	public String getRequestPath() {
		return requestPath;
	}

	/**
	 * Retrieves the HTTP request query
	 * 
	 * @return Request query string
	 */
	public String getRequestQuery() {
		return requestQuery;
	}

	/**
	 * Transfers the request body
	 * 
	 * @param output Target stream
	 * @throws IOException If transferring fails
	 */
	public void transferRequestBody(OutputStream output) throws IOException {
		if (body == null)
			return;
		if (bodyContentLength > -1) {
			long length = bodyContentLength;
			int tr = 0;
			for (long i = 0; i < length; i += tr) {
				tr = 1000;
				if ((length - (long) i) < tr) {
					tr = body.available();
					if (tr == 0) {
						output.write(body.read());
						i += 1;
					}
					tr = body.available();
				}
				output.write(IoUtil.readNBytes(body, tr));
			}
		} else {
			// TODO: chunked content
			IoUtil.transfer(body, output);
		}
	}

	/**
	 * Retrieves the map of request query parameters
	 * 
	 * @return Request query parameters
	 */
	public Map<String, String> getRequestQueryParameters() {
		return queryParameters;
	}

	/**
	 * Retrieves the unparsed request path string
	 * 
	 * @return HTTP raw request string
	 */
	public String getRawRequestResource() {
		return requestResource;
	}

	/**
	 * Retrieves the request method
	 * 
	 * @return HTTP request method string
	 */
	public String getRequestMethod() {
		return requestMethod;
	}

	/**
	 * Retrieves the request body length, returns -1 if unset
	 * 
	 * @return Request body length or -1
	 */
	public long getBodyLength() {
		return bodyContentLength;
	}

	/**
	 * Retrieves the HTTP version
	 * 
	 * @return HTTP version string
	 */
	public String getHttpVersion() {
		return httpVersion;
	}

	@Override
	public InputStream getBodyStream() {
		if (bodyStr != null)
			throw new IllegalStateException("This method cannot be used if getRequestBodyAsString() is called");
		return body;
	}

	private String bodyStr;

	/**
	 * Retrieves the body content as string
	 * 
	 * @return String representing the request body
	 * @throws IOException If reading fails
	 */
	public String getRequestBodyAsString() throws IOException {
		if (bodyStr != null)
			return bodyStr;
		ByteArrayOutputStream strm = new ByteArrayOutputStream();
		transferRequestBody(strm);
		bodyStr = new String(strm.toByteArray(), "UTF-8");
		return bodyStr;
	}

	/**
	 * Retrieves the body content as string
	 * 
	 * @param encoding Encoding to use
	 * @return String representing the request body
	 * @throws IOException If reading fails
	 */
	public String getRequestBodyAsString(String encoding) throws IOException {
		if (bodyStr != null)
			return bodyStr;
		ByteArrayOutputStream strm = new ByteArrayOutputStream();
		transferRequestBody(strm);
		bodyStr = new String(strm.toByteArray(), encoding);
		return bodyStr;
	}

	/**
	 * Retrieves the body content as string
	 * 
	 * @param encoding Encoding to use
	 * @return String representing the request body
	 * @throws IOException If reading fails
	 */
	public String getRequestBodyAsString(Charset encoding) throws IOException {
		if (bodyStr != null)
			return bodyStr;
		ByteArrayOutputStream strm = new ByteArrayOutputStream();
		transferRequestBody(strm);
		bodyStr = new String(strm.toByteArray(), encoding);
		return bodyStr;
	}

	@Override
	public String toString() {
		return requestMethod + " " + requestResource;
	}

}