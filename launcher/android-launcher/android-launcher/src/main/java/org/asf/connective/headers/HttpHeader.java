package org.asf.connective.headers;

import java.util.ArrayList;
import java.util.Collection;

/**
 * 
 * Header object
 * 
 * @author Sky Swimmer
 *
 */
public class HttpHeader {

	private String name;
	private ArrayList<String> values = new ArrayList<String>();

	/**
	 * Creates a new header container
	 * 
	 * @param name  Header name
	 * @param value Initial value
	 */
	public HttpHeader(String name, String value) {
		this.name = name;
		this.values.add(value);
	}

	/**
	 * Creates a new header container
	 * 
	 * @param name Header name
	 */
	public HttpHeader(String name) {
		this.name = name;
	}

	/**
	 * Creates a new HTTP header container
	 * 
	 * @param name  Header name
	 * @param value Header value
	 * @return HttpHeader instance
	 */
	public static HttpHeader create(String name, String value) {
		return new HttpHeader(name, value);
	}

	/**
	 * Creates a new empty HTTP header container
	 * 
	 * @param name Header namee
	 * @return HttpHeader instance
	 */
	public static HttpHeader create(String name) {
		return new HttpHeader(name);
	}

	/**
	 * Retrieves the header name
	 * 
	 * @return Header name string
	 */
	public String getName() {
		return name;
	}

	/**
	 * Retrieves the FIRST value of the header
	 * 
	 * @return Value string or null
	 */
	public String getValue() {
		if (isEmpty())
			return null;
		return values.get(0);
	}

	/**
	 * Retrieves all values
	 * 
	 * @return Array of header value strings
	 */
	public String[] getValues() {
		return values.toArray(new String[0]);
	}

	/**
	 * Clears all values from the header
	 */
	public void clearValues() {
		values.clear();
	}

	/**
	 * Removes a value from the header
	 * 
	 * @param value Value string to remove
	 * @return True if successful, false otherwise
	 */
	public boolean removeValue(String value) {
		return values.remove(value);
	}

	/**
	 * Checks if the given value is present in this header
	 * 
	 * @param value Value string
	 * @return True if present, false otherwise
	 */
	public boolean containsValue(String value) {
		return values.contains(value);
	}

	/**
	 * Retrieves the amount of values in the header
	 * 
	 * @return Amount of values stored in this header
	 */
	public int getValueCount() {
		return values.size();
	}

	/**
	 * Retrieves a value at the specified index
	 * 
	 * @param index Value index
	 * @return Value string
	 */
	public String getValue(int index) {
		if (index < 0 || index > values.size())
			throw new IndexOutOfBoundsException("Index: " + index);
		return values.get(index);
	}

	/**
	 * Adds a value to the header
	 * 
	 * @param value Value to add
	 * @return Value index
	 */
	public int addValue(String value) {
		values.add(value);
		return values.lastIndexOf(value);
	}

	/**
	 * Adds values to the header
	 * 
	 * @param values Values to add
	 */
	public void addValues(Collection<? extends String> values) {
		this.values.addAll(values);
	}

	/**
	 * Checks if the header list is empty
	 * 
	 * @return True if empty, false otherwise
	 */
	public boolean isEmpty() {
		return values.isEmpty();
	}

	/**
	 * Checks if the header list is not empty
	 * 
	 * @return True if not empty, false otherwise
	 */
	public boolean hasValues() {
		return !isEmpty();
	}

	@Override
	public String toString() {
		String res = name;
		if (isEmpty())
			res += ": [EMPTY HEADER]";
		else {
			for (String ent : values) {
				if (res.equals(name))
					res += ": " + ent.replace("\\r", "\\\\r").replace("\\n", "\\\\n").replace("\r", "\\r").replace("\n",
							"\\n");
				else
					res += ", " + ent.replace("\\r", "\\\\r").replace("\\n", "\\\\n").replace("\r", "\\r").replace("\n",
							"\\n");
			}
		}
		return res;
	}

}
