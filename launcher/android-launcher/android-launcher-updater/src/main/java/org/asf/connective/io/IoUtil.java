package org.asf.connective.io;

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
		byte[] buf = new byte[20480];
		int c = 0;
		while (true) {
			// Read
			try {
				int r = strm.read(buf, c, buf.length - c);
				if (r == -1)
					break;
				c += r;
			} catch (Exception e) {
				int b = strm.read();
				if (b == -1)
					break;
				buf[c++] = (byte) b;
			}
			if (c >= buf.length) {
				// Grow buffer
				if (c == Integer.MAX_VALUE)
					break;

				// Get new size
				int nL;
				if ((long) buf.length + 20480l >= Integer.MAX_VALUE) {
					nL = Integer.MAX_VALUE;
				} else
					nL = buf.length + 20480;

				// Grow
				byte[] newBuf = new byte[nL];
				for (int i = 0; i < buf.length; i++)
					newBuf[i] = buf[i];
				buf = newBuf;
			}
		}
		return Arrays.copyOfRange(buf, 0, c);
	}

	/**
	 * Reads a given amount of bytes from the stream
	 * 
	 * @param input Stream to read from
	 * @param num   Amount of bytes to read
	 * @return Byte array
	 */
	public static byte[] readNBytes(InputStream input, int num) throws IOException {
		byte[] res = new byte[num];
		int c = 0;
		while (true) {
			try {
				int r = input.read(res, c, num - c);
				if (r == -1)
					break;
				c += r;
			} catch (Exception e) {
				int b = input.read();
				if (b == -1)
					break;
				res[c++] = (byte) b;
			}
			if (c >= num)
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
				int r = input.read(buf, c, buf.length - c);
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
