package org.asf.centuria.launcher.io;

import java.io.IOException;
import java.io.InputStream;
import java.nio.ByteBuffer;

/**
 * 
 * Data Reader
 * 
 * @author Sky Swimmer
 *
 */
public class DataReader {

	private InputStream input;

	public DataReader(InputStream input) {
		this.input = input;
	}

	public InputStream getStream() {
		return input;
	}

	/**
	 * Reads all bytes
	 * 
	 * @return Array of bytes
	 * @throws IOException If reading fails
	 */
	public byte[] readAllBytes() throws IOException {
		return IoUtil.readAllBytes(input);
	}

	/**
	 * Reads a number bytes
	 * 
	 * @param num Number of bytes to read
	 * @return Array of bytes
	 * @throws IOException If reading fails
	 */
	public byte[] readNBytes(int num) throws IOException {
		return IoUtil.readNBytes(input, num);
	}

	/**
	 * Reads a single byte
	 * 
	 * @return Byte value
	 * @throws IOException If reading fails
	 */
	public byte readRawByte() throws IOException {
		int i = input.read();
		if (i == -1)
			throw new IOException("Stream closed");
		return (byte) i;
	}

	/**
	 * Reads a single integer
	 * 
	 * @return Integer value
	 * @throws IOException If reading fails
	 */
	public int readInt() throws IOException {
		return ByteBuffer.wrap(readNBytes(4)).getInt();
	}

	/**
	 * Reads a single short integer
	 * 
	 * @return Short value
	 * @throws IOException If reading fails
	 */
	public short readShort() throws IOException {
		return ByteBuffer.wrap(readNBytes(2)).getShort();
	}

	/**
	 * Reads a single long integer
	 * 
	 * @return Long value
	 * @throws IOException If reading fails
	 */
	public long readLong() throws IOException {
		return ByteBuffer.wrap(readNBytes(8)).getLong();
	}

	/**
	 * Reads a single floating-point
	 * 
	 * @return Float value
	 * @throws IOException If reading fails
	 */
	public float readFloat() throws IOException {
		return ByteBuffer.wrap(readNBytes(4)).getFloat();
	}

	/**
	 * Reads a single double-precision floating-point
	 * 
	 * @return Double value
	 * @throws IOException If reading fails
	 */
	public double readDouble() throws IOException {
		return ByteBuffer.wrap(readNBytes(4)).getDouble();
	}

	/**
	 * Reads a single character
	 * 
	 * @return Char value
	 * @throws IOException If reading fails
	 */
	public char readChar() throws IOException {
		return ByteBuffer.wrap(readNBytes(2)).getChar();
	}

	/**
	 * Reads a single boolean
	 * 
	 * @return Boolean value
	 * @throws IOException If reading fails
	 */
	public boolean readBoolean() throws IOException {
		int data = readRawByte();
		if (data != 0)
			return true;
		else
			return false;
	}

	/**
	 * Reads a single length-prefixed byte array
	 * 
	 * @return Array of bytes
	 * @throws IOException If reading fails
	 */
	public byte[] readBytes() throws IOException {
		return readNBytes(readInt());
	}

	/**
	 * Reads a string
	 * 
	 * @return String value
	 * @throws IOException If reading fails
	 */
	public String readString() throws IOException {
		return new String(readBytes(), "UTF-8");
	}

}
