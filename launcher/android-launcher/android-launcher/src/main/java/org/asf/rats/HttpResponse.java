package org.asf.rats;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.HashMap;
import java.util.Locale;
import java.util.TimeZone;

/**
 * 
 * HttpResponse, basic builder for HTTP responses
 * 
 * @author Stefan0436 - AerialWorks Software Foundation
 *
 */
public class HttpResponse {

	public HttpRequest input;

	public HashMap<String, String> headers = new HashMap<String, String>();

	public int status = 200;
	public String message = "OK";
	public InputStream body = null;

	public HttpResponse(int status, String message, HttpRequest input) {
		this.input = input;
		this.status = status;
		this.message = message;
		if (input.headers.containsKey("Connection") && !headers.containsKey("Connection")) {
			headers.put("Connection", "Closed");
		}
	}

	public HttpResponse(HttpRequest input) {
		this.input = input;
	}

	/**
	 * Sets the content to a body string
	 * 
	 * @param type Content type.
	 * @param body Content body.
	 */
	public HttpResponse setContent(String type, String body) {
		if (type != null) {
			headers.put("Content-Type", type);
			if (body != null) {
				headers.put("Content-Length", Integer.toString(body.getBytes().length));
			}
		} else {
			if (headers.containsKey("Content-Type"))
				headers.remove("Content-Type");
			if (headers.containsKey("Content-Length"))
				headers.remove("Content-Length");
		}

		if (this.body != null) {
			try {
				this.body.close();
			} catch (IOException e) {
			}
		}

		if (body == null) {
			this.body = null;
			return this;
		}

		this.body = new ByteArrayInputStream(body.getBytes());
		return this;
	}

	/**
	 * Sets the body of the response. (sets Content-Disposition to attachment)
	 * 
	 * @param type Content type.
	 * @param body Input bytes.
	 */
	public HttpResponse setContent(String type, byte[] body) {
		headers.put("Content-Type", type);
		headers.put("Content-Disposition", "attachment");
		headers.put("Content-Length", Integer.toString(body.length));

		if (this.body != null) {
			try {
				this.body.close();
			} catch (IOException e) {
			}
		}

		this.body = new ByteArrayInputStream(body);
		return this;
	}

	/**
	 * Sets the body of the response, WARNING: the stream gets closed on build.
	 * 
	 * @param type Content type.
	 * @param body Input stream.
	 */
	public HttpResponse setContent(String type, InputStream body) {
		headers.put("Content-Type", type);

		if (this.body != null) {
			try {
				this.body.close();
			} catch (IOException e) {
			}
		}

		this.body = body;
		return this;
	}

	public HttpResponse addDefaultHeaders(ConnectiveHTTPServer server) {
		headers.put("Server", server.getName() + " " + server.getVersion());
		headers.put("Date", getHttpDate(new Date()));
		if ((input.headers.containsKey("Connection") && !headers.containsKey("Connection"))
				|| !headers.containsKey("Content-Length")) {
			headers.put("Connection", "Closed");
		}
		return this;
	}

	public HttpResponse setLastModified(Date date) {
		headers.put("Last-Modified", getHttpDate(new Date()));
		return this;
	}

	public HttpResponse setConnectionState(String state) {
		headers.put("Connection", state);
		return this;
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
		this.status = status;
		this.message = message;
		setHeader("Location", destination);
		return this;
	}

	/**
	 * Assigns a new response status
	 * 
	 * @param status  New status code
	 * @param message New status message
	 */
	public HttpResponse setResponseStatus(int status, String message) {
		this.status = status;
		this.message = message;
		return this;
	}

	/**
	 * Sets a header
	 * 
	 * @param header Header name
	 * @param value  Header value
	 */
	public HttpResponse setHeader(String header, String value) {
		headers.put(header, value);
		return this;
	}

	/**
	 * Sets a header
	 * 
	 * @param header        Header name
	 * @param value         Header value
	 * @param duplicateMode False to replace duplicates, true to name the duplicate
	 *                      header [name]#xxx to allow it. (will be parsed
	 *                      automatically)
	 */
	public HttpResponse setHeader(String header, String value, boolean duplicateMode) {

		if (headers.containsKey(header) && duplicateMode) {
			String headerOld = header;

			int num = 1;
			while (headers.containsKey(header)) {
				header = headerOld + "#" + num++;
			}
		}

		headers.put(header, value);
		return this;
	}

	/**
	 * Retrieves a requested header (returns all values with the same header name)
	 * 
	 * @param header Header name
	 * @return Array of value strings
	 */
	public String[] getHeader(String header) {
		ArrayList<String> lst = new ArrayList<String>();

		for (String k : headers.keySet()) {
			String v = headers.get(k);
			if (k.contains("#")) {
				k = k.substring(0, k.indexOf("#"));
			}
			if (k.equals(header)) {
				lst.add(v);
			}
		}

		return lst.toArray(new String[0]);
	}

	/**
	 * Retrieves a requested header (returns all values with the same header name)
	 * 
	 * @param header          Header name
	 * @param caseInsensitive True to enable case-insensitive mode.
	 * @return Array of value strings
	 */
	public String[] getHeader(String header, boolean caseInsensitive) {
		ArrayList<String> lst = new ArrayList<String>();

		for (String k : headers.keySet()) {
			String v = headers.get(k);
			if (k.contains("#")) {
				k = k.substring(0, k.indexOf("#"));
			}
			if (caseInsensitive) {
				if (k.equalsIgnoreCase(header)) {
					lst.add(v);
				}
			} else {
				if (k.equals(header)) {
					lst.add(v);
				}
			}
		}

		return lst.toArray(new String[0]);
	}

	/**
	 * Builds the HTTP response
	 * 
	 * @param output Output stream to write to.
	 * @throws IOException
	 */
	public void build(OutputStream output) throws IOException {
		StringBuilder resp = new StringBuilder();
		resp.append(input.version).append(" ");
		resp.append(status).append(" ");
		resp.append(message);

		if (input.method.equals("HEAD") || status == 204 || status == 201) {
			if (headers.containsKey("Content-Length"))
				headers.remove("Content-Length");
		}
		if (status == 204 || status == 201) {
			if (headers.containsKey("Content-Type"))
				headers.remove("Content-Type");
		}

		for (String k : headers.keySet()) {
			String v = headers.get(k);
			if (!k.equals("Connection") || !v.equals("Closed")) {
				resp.append("\r\n");
				if (k.contains("#")) {
					k = k.substring(0, k.indexOf("#")); // allows for multiple headers with same name
				}
				resp.append(k).append(": ");
				resp.append(v);
			}
		}

		resp.append("\r\n");
		resp.append("\r\n");
		if (body != null && !input.method.equals("HEAD") && status != 204) {
			output.write(resp.toString().getBytes());
			while (true) {
				try {
					byte[] data = new byte[10000];
					int l = body.read(data);
					if (l == 0)
						break;
					output.write(data, 0, l);
					if (l == 0 || l < 10000)
						break;
				} catch (IOException e) {
					break;
				}
			}
			body.close();
			body = null;
		} else {
			output.write(resp.toString().getBytes());
		}
	}

	// Adapted from SO answer: https://stackoverflow.com/a/8642463
	public synchronized String getHttpDate(Date date) {
		SimpleDateFormat dateFormat = new SimpleDateFormat("EEE, dd MMM yyyy HH:mm:ss z", Locale.US);
		dateFormat.setTimeZone(TimeZone.getTimeZone("GMT"));
		return dateFormat.format(date);
	}

}
