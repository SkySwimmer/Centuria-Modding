package org.asf.rats;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.URLDecoder;
import java.util.HashMap;

/**
 * 
 * HttpRequest, basic parser for HTTP requests
 * 
 * @author Stefan0436 - AerialWorks Software Foundation
 *
 */
public class HttpRequest {
	public HashMap<String, String> headers = new HashMap<String, String>();

	public String path = "";
	public String method = "";
	public String version = "";

	public String subPath = "";

	@Deprecated
	public String body = null;
	protected InputStream bodyStream = null;

	public String query = "";

	/**
	 * Parses a request into a new HttpMessage object.
	 * 
	 * @param request Request stream
	 * @return HttpMessage representing the request.
	 * @throws IOException If reading fails.
	 */
	public static HttpRequest parse(InputStream request) throws IOException {
		String firstLine = readStreamLine(request);
		if (firstLine == null || firstLine.isEmpty())
			return null;

		if (!firstLine.substring(0, 1).matches("[A-Za-z0-9]") || firstLine.split(" ").length < 3) {
			return null;
		}

		HttpRequest msg = new HttpRequest();

		msg.path = firstLine.substring(firstLine.indexOf(" ") + 1, firstLine.lastIndexOf(" "));
		if (msg.path.contains("?")) {
			msg.query = msg.path.substring(msg.path.lastIndexOf("?") + 1);
			msg.path = msg.path.substring(0, msg.path.lastIndexOf("?"));
		} else if (msg.path.contains("&")) {
			msg.query = msg.path.substring(msg.path.indexOf("&") + 1);
			msg.path = msg.path.substring(0, msg.path.indexOf("&"));
		}
		msg.path = URLDecoder.decode(msg.path, "UTF-8");

		while (msg.path.contains("//")) {
			msg.path = msg.path.replace("//", "/");
		}
		msg.method = firstLine.substring(0, firstLine.indexOf(" ")).toUpperCase();
		msg.version = firstLine.substring(firstLine.lastIndexOf(" ") + 1);

		while (true) {
			String line = readStreamLine(request);
			if (line.equals(""))
				break;

			String key = line.substring(0, line.indexOf(": "));
			String value = line.substring(line.indexOf(": ") + 2);
			msg.headers.put(key, value);
		}

		if (msg.method.equals("POST") || msg.method.equals("PUT")) {
			msg.bodyStream = request;
		}

		return msg;
	}

	/**
	 * Reads a single line from an inputstream
	 * 
	 * @param strm Stream to read from.
	 * @return String representing the line read.
	 * @throws IOException If reading fails.
	 */
	protected static String readStreamLine(InputStream strm) throws IOException {
		String buffer = "";
		while (true) {
			char ch = (char) strm.read();
			if (ch == (char) -1)
				return null;
			if (ch == '\n') {
				return buffer;
			} else if (ch != '\r') {
				buffer += ch;
			}
		}
	}

	public void close() throws IOException {
		if (bodyStream != null) {
			bodyStream.close();
		}
	}

	public boolean isBinaryMode() {
		return (headers.containsKey("Content-Disposition") && headers.get("Content-Disposition").equals("attachment"))
				|| (headers.containsKey("Content-Type")
						&& headers.get("Content-Type").equals("application/octet-stream"));
	}

	/**
	 * Returns the request body stream, null if not a post or put request.
	 * 
	 * @return Body InputStream or null.
	 */
	public InputStream getRequestBodyStream() {
		return bodyStream;
	}

	/**
	 * Returns the request body in string format. <b>WARNING:</b> leaves the body
	 * stream in a useless state! (POST or PUT only)
	 * 
	 * @return String representing the body.
	 */
	public String getRequestBody() {
		if (bodyStream != null) {
			if (body == null) {
				try {
					ByteArrayOutputStream strm = new ByteArrayOutputStream();
					ConnectiveHTTPServer.transferRequestBody(headers, bodyStream, strm);
					body = new String(strm.toByteArray());
				} catch (IOException e) {
					throw new RuntimeException(e);
				}
			}
			return body;
		} else {
			return null;
		}
	}

	/**
	 * Transfers the request body to a given output stream. Ignored if not a post or
	 * put request.
	 * 
	 * @param output Output stream
	 * @throws IOException If transferring fails
	 */
	public void transferRequestBody(OutputStream output) throws IOException {
		if (bodyStream != null) {
			ConnectiveHTTPServer.transferRequestBody(headers, bodyStream, output);
		}
	}
}
