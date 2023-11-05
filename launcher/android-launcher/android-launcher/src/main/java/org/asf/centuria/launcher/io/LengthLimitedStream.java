package org.asf.centuria.launcher.io;

import java.io.IOException;
import java.io.InputStream;

public class LengthLimitedStream extends InputStream {

	private InputStream delegate;
	private long currentPos;
	private long length;

	public LengthLimitedStream(InputStream delegate, long length) {
		this.delegate = delegate;
		this.length = length;
	}

	@Override
	public int read() throws IOException {
		// Check
		if (currentPos >= length)
			return -1;

		// Read
		int b = delegate.read();
		currentPos++;
		if (b == -1)
			currentPos = length;
		return b;
	}

	@Override
	public int read(byte[] data) throws IOException {
		// Check position
		if (currentPos >= length)
			return -1;

		// Get amount to read
		int bytesToRead = data.length;
		int bytesRead = 0;
		int position = 0;

		// Read
		while (bytesToRead > 0) {
			// Read block
			int amount = bytesToRead;
			if (amount > (length - currentPos))
				amount = (int) (length - currentPos);
			byte[] res = IoUtil.readNBytes(delegate, amount);
			bytesRead += res.length;
			bytesToRead -= res.length;
			currentPos += res.length;

			// Write block to output
			for (int i = 0; i < res.length; i++)
				data[position++] = res[i];

			// Check position
			if (currentPos >= length)
				break;
		}

		// Return
		return bytesRead;
	}

	@Override
	public int read(byte[] data, int start, int end) throws IOException {
		// Check position
		if (currentPos >= length)
			return -1;

		// Get amount to read
		int bytesToRead = end - start;
		int bytesRead = 0;
		int position = start;

		// Read
		while (bytesToRead > 0) {
			// Read block
			int amount = bytesToRead;
			if (amount > (length - currentPos))
				amount = (int) (length - currentPos);
			byte[] res = IoUtil.readNBytes(delegate, amount);
			bytesRead += res.length;
			bytesToRead -= res.length;
			currentPos += res.length;

			// Write block to output
			for (int i = 0; i < res.length; i++)
				data[position++] = res[i];

			// Check position
			if (currentPos >= length)
				break;
		}

		// Return
		return bytesRead;
	}

	@Override
	public void close() throws IOException {
		delegate.close();
	}

	protected String readStreamLine(InputStream strm) throws IOException {
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

}
