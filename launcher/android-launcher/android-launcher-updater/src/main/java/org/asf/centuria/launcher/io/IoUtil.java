package org.asf.centuria.launcher.io;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.util.Arrays;

public class IoUtil {

	/**
	 * Reads all bytes from the stream
	 * 
	 * @param strm Stream to read from
	 * @return Byte array
	 */
	public static byte[] readAllBytes(InputStream strm) throws IOException {
		ByteArrayOutputStream bO = new ByteArrayOutputStream();
		transfer(strm, bO);
		return bO.toByteArray();
	}

	/**
	 * Reads a given amount of bytes from the stream
	 * 
	 * @param strm Stream to read from
	 * @param n    Amount of bytes to read
	 * @return Byte array
	 */
	public static byte[] readNBytes(InputStream strm, int n) throws IOException {
		byte[] res = new byte[n];
		int c = 0;
		while (true) {
			try {
				int r = strm.read(res, c, n);
				if (r == -1)
					break;
				c += r;
			} catch (Exception e) {
				int b = strm.read();
				if (b == -1)
					break;
				res[c++] = (byte) b;
			}
			if (c >= n)
				break;
		}
		return Arrays.copyOfRange(res, 0, c);
	}

	/**
	 * Transfers data from stream to stream
	 * 
	 * @param input  Source stream
	 * @param output Target stream
	 */
	public static void transfer(InputStream input, OutputStream output) throws IOException {
		byte[] buf = new byte[20480];
		while (true) {
			// Read
			int c = 0;
			try {
				int r = input.read(buf, c, buf.length);
				if (r == -1)
					break;
				c += r;
			} catch (Exception e) {
				int b = input.read();
				if (b == -1)
					break;
				buf[c++] = (byte) b;
			}
			output.write(buf, 0, c);
		}
	}

}
