package org.asf.connective.headers;

import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.function.Function;
import java.util.function.IntFunction;

/**
 * 
 * HTTP Header Collection
 * 
 * @author Sky Swimmer
 *
 */
public class HeaderCollection {
	private HashMap<String, HttpHeader> headers = new LinkedHashMap<String, HttpHeader>();

	/**
	 * Creates a new empty header collection
	 * 
	 * @return HeaderCollection instance
	 */
	public static HeaderCollection create() {
		return new HeaderCollection();
	}

	/**
	 * Adds HTTP headers (if a header is already present, it is overwritten)
	 * 
	 * @param name  Header name
	 * @param value Header value
	 * @return HttpHeader instance
	 */
	public HttpHeader addHeader(String name, String value) {
		return addHeader(name, value, false);
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
		if (!hasHeader(name)) {
			HttpHeader header = HttpHeader.create(name, value);
			headers.put(name.toLowerCase(), header);
			return header;
		}
		HttpHeader old = getHeader(name);
		if (!append)
			old.clearValues();
		old.addValue(value);
		return old;
	}

	/**
	 * Retrieves the amount of headers that are present in the set
	 * 
	 * @return Header count
	 */
	public int getHeaderCount() {
		return headers.size();
	}

	/**
	 * Checks if a header is present
	 * 
	 * @param header Header name
	 * @return True if present, false otherwise
	 */
	public boolean hasHeader(String header) {
		return headers.containsKey(header.toLowerCase());
	}

	/**
	 * Retrieves HTTP headers
	 * 
	 * @param header Header name
	 * @return HttpHeader instance or null
	 */
	public HttpHeader getHeader(String header) {
		return headers.get(header.toLowerCase());
	}

	/**
	 * Retrieves HTTP header values
	 * 
	 * @param header Header name
	 * @return Header value or null
	 */
	public String getHeaderValue(String header) {
		HttpHeader head = getHeader(header);
		if (head == null)
			return null;
		return head.getValue();
	}

	/**
	 * Retrieves HTTP header values
	 * 
	 * @param header Header name
	 * @return Array of value strings
	 */
	public String[] getHeaderValues(String header) {
		HttpHeader head = getHeader(header);
		if (head == null)
			return new String[0];
		return head.getValues();
	}

	/**
	 * Removes HTTP headers
	 * 
	 * @param header Header name
	 * @return Header that was removed or null
	 */
	public HttpHeader removeHeader(String header) {
		return headers.remove(header.toLowerCase());
	}

	/**
	 * Retrieves all header names
	 * 
	 * @return Array of header name strings
	 */
	public String[] getHeaderNames() {
		return headers.values().stream().map(new Function<HttpHeader, String>() {

			@Override
			public String apply(HttpHeader arg0) {
				return arg0.getName();
			}

		}).toArray(new IntFunction<String[]>() {

			@Override
			public String[] apply(int arg0) {
				return new String[arg0];
			}
		});
	}

	/**
	 * Retrieves an array of all headers
	 * 
	 * @return Array of HttpHeader instances
	 */
	public HttpHeader[] getHeaders() {
		return headers.values().toArray(new HttpHeader[0]);
	}

	/**
	 * Clears the header collection
	 */
	public void clearHeaders() {
		headers.clear();
	}

	@Override
	public String toString() {
		String res = "";
		for (HttpHeader header : getHeaders()) {
			for (String value : header.getValues()) {
				if (!res.isEmpty())
					res += "\n";
				res += header.getName() + ": " + value.replace("\\r", "\\\\r").replace("\\n", "\\\\n")
						.replace("\r", "\\r").replace("\n", "\\n");
			}
		}
		return res;
	}

}
