package org.asf.connective.objects;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.UnsupportedEncodingException;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.TimeZone;

/**
 * 
 * HTTP Response Object
 * 
 * @author Sky Swimmer
 *
 */
public class HttpResponse extends HttpObject {

	/**
	 * @deprecated Highly recommended to use setContent() instead, this is only
	 *             present for some mechanics that depend on swapping out the
	 *             content stream
	 */
	@Deprecated
	public InputStream body;
	private long contentLength = -1;

	private String httpVersion;

	private int statusCode = 200;
	private String statusMessage = "OK";

	/**
	 * Creates a HttpResponse object instance
	 * 
	 * @param httpVersion HTTP version
	 */
	public HttpResponse(String httpVersion) throws IllegalArgumentException {
		this.httpVersion = httpVersion;
	}

	/**
	 * Adds redirect headers
	 * 
	 * @param destination Redirect address
	 */
	public HttpResponse redirect(String destination) {
		return redirect(destination, 302, "Found");
	}

	/**
	 * Adds redirect headers
	 * 
	 * @param destination Redirect address
	 * @param status      New status code
	 * @param message     New status message
	 */
	public HttpResponse redirect(String destination, int status, String message) {
		setResponseStatus(status, message);
		addHeader("Location", destination);
		return this;
	}

	/**
	 * Assigns a new response status
	 * 
	 * @param status  New status code
	 * @param message New status message
	 */
	public HttpResponse setResponseStatus(int status, String message) {
		this.statusCode = status;
		this.statusMessage = message;
		return this;
	}

	/**
	 * Sets the content to a body string
	 * 
	 * @param type Content type.
	 * @param body Content body.
	 */
	public HttpResponse setContent(String type, String body) {
		// Assign headers
		if (type != null) {
			addHeader("Content-Type", type, false);
		} else if (headers.hasHeader("Content-Type")) {
			headers.removeHeader("Content-Type");
		}

		// Assign length
		if (body != null)
			try {
				contentLength = body.getBytes("UTF-8").length;
			} catch (UnsupportedEncodingException e) {
				throw new RuntimeException(e);
			}
		else
			contentLength = 0;

		// Close old
		if (this.body != null) {
			try {
				this.body.close();
			} catch (IOException e) {
			}
		}

		// Set to null if not present
		if (body == null) {
			this.body = null;
			return this;
		}

		// Assign body
		try {
			this.body = new ByteArrayInputStream(body.getBytes("UTF-8"));
		} catch (UnsupportedEncodingException e) {
			throw new RuntimeException(e);
		}
		return this;
	}

	/**
	 * Sets the body of the response
	 * 
	 * @param type Content type
	 * @param body Input bytes
	 */
	public HttpResponse setContent(String type, byte[] body) {
		// Assign headers
		if (type != null) {
			addHeader("Content-Type", type, false);
		} else if (headers.hasHeader("Content-Type")) {
			headers.removeHeader("Content-Type");
		}

		// Assign length
		if (body != null)
			contentLength = body.length;
		else
			contentLength = 0;

		// Close old body
		if (this.body != null) {
			try {
				this.body.close();
			} catch (IOException e) {
			}
		}

		// Assign new body
		this.body = new ByteArrayInputStream(body);
		return this;
	}

	/**
	 * Sets the body of the response, WARNING: the stream gets closed when the
	 * response is sent.
	 * 
	 * @param body   Input stream
	 */
	public HttpResponse setContent(byte[] body) {
		// Assign headers
		if (!headers.hasHeader("Content-Type"))
			addHeader("Content-Type", "application/octet-stream", false);

		// Assign length
		if (body != null)
			contentLength = body.length;
		else
			contentLength = 0;

		// Close old body
		if (this.body != null) {
			try {
				this.body.close();
			} catch (IOException e) {
			}
		}

		// Assign new body
		this.body = new ByteArrayInputStream(body);
		return this;
	}

	/**
	 * Sets the body of the response, WARNING: the stream gets closed when the
	 * response is sent.
	 * 
	 * @param type   Content type
	 * @param body   Input stream
	 * @param length Content length
	 */
	public HttpResponse setContent(String type, InputStream body, long length) {
		// Assign headers
		if (type != null) {
			addHeader("Content-Type", type, false);
		} else if (headers.hasHeader("Content-Type")) {
			headers.removeHeader("Content-Type");
		}

		// Assign length
		contentLength = length;

		// Close old body
		if (this.body != null) {
			try {
				this.body.close();
			} catch (IOException e) {
			}
		}

		// Assign new body
		this.body = body;
		return this;
	}

	/**
	 * Sets the body of the response, WARNING: the stream gets closed when the
	 * response is sent.
	 * 
	 * @param body   Input stream.
	 * @param length Content length.
	 */
	public HttpResponse setContent(InputStream body, long length) {
		// Assign headers
		if (!headers.hasHeader("Content-Type"))
			addHeader("Content-Type", "application/octet-stream", false);

		// Assign length
		contentLength = length;

		// Close old body
		if (this.body != null) {
			try {
				this.body.close();
			} catch (IOException e) {
			}
		}

		// Assign new body
		this.body = body;
		return this;
	}

	/**
	 * Sets the body of the response, WARNING: the stream gets closed when the
	 * response is sent.
	 * 
	 * @param type Content type.
	 * @param body Input stream.
	 */
	public HttpResponse setContent(String type, InputStream body) {
		// Assign headers
		if (type != null) {
			addHeader("Content-Type", type, false);
		} else if (headers.hasHeader("Content-Type")) {
			headers.removeHeader("Content-Type");
		}

		// Assign length
		contentLength = -1;

		// Close old body
		if (this.body != null) {
			try {
				this.body.close();
			} catch (IOException e) {
			}
		}

		// Assign new body
		this.body = body;
		return this;
	}

	/**
	 * Sets the body of the response, WARNING: the stream gets closed when the
	 * response is sent.
	 * 
	 * @param body Input stream
	 */
	public HttpResponse setContent(InputStream body) {
		// Assign headers
		if (!headers.hasHeader("Content-Type"))
			addHeader("Content-Type", "application/octet-stream", false);

		// Assign length
		contentLength = -1;

		// Close old body
		if (this.body != null) {
			try {
				this.body.close();
			} catch (IOException e) {
			}
		}

		// Assign new body
		this.body = body;
		return this;
	}

	/**
	 * Assigns the Last-Modified header
	 * 
	 * @param date Header date to assign
	 */
	public HttpResponse setLastModified(Date date) {
		SimpleDateFormat dateFormat = new SimpleDateFormat("EEE, dd MMM yyyy HH:mm:ss z", Locale.US);
		dateFormat.setTimeZone(TimeZone.getTimeZone("GMT"));
		headers.addHeader("Last-Modified", dateFormat.format(date));
		return this;
	}

	/**
	 * Checks if the response code is a success status
	 * 
	 * @return True if the response is a success status, false otherwise
	 */
	public boolean isSuccessResponseCode() {
		return statusCode >= 100 && statusCode < 400;
	}

	/**
	 * Retrieves the HTTP status code
	 * 
	 * @return HTTP status code
	 */
	public int getResponseCode() {
		return statusCode;
	}

	/**
	 * Retrieves the HTTP status message
	 * 
	 * @return HTTP status message
	 */
	public String getResponseMessage() {
		return statusMessage;
	}

	/**
	 * Retrieves the HTTP version
	 * 
	 * @return HTTP version string
	 */
	public String getHttpVersion() {
		return httpVersion;
	}

	/**
	 * Checks if the HTTP response has a response body
	 * 
	 * @return True if a response body is present, false otherwise
	 */
	public boolean hasResponseBody() {
		return body != null;
	}

	@Override
	public InputStream getBodyStream() {
		return body;
	}

	/**
	 * Retrieves the response body length, returns -1 if unset
	 * 
	 * @return Response body length or -1
	 */
	public long getBodyLength() {
		return contentLength;
	}

	@Override
	public String toString() {
		return statusCode + " " + statusMessage;
	}

}
