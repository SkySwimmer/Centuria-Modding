package org.asf.centuria.launcher.io;

import java.io.IOException;
import java.io.InputStream;

public class ChunkedStream extends InputStream {

	private InputStream delegate;
	private int currentPos = 0;
	private int currentLength = -1;
	private boolean atEnd = false;

	public ChunkedStream(InputStream delegate) {
		this.delegate = delegate;
	}

	private void readHeader() throws IOException {
		if (atEnd)
			return;
		if (currentLength != -1 && currentPos < currentLength)
			return;

		// Read ending
		if (currentLength != -1)
			readStreamLine(delegate);

		// Read header
		String line = readStreamLine(delegate);

		// Parse
		currentLength = Integer.parseInt(line, 16);
		currentPos = 0;
		if (currentLength <= 0) {
			readStreamLine(delegate);
			atEnd = true;
			delegate.close();
		}
	}

	@Override
	public int read() throws IOException {
		// Read header
		readHeader();
		if (atEnd)
			return -1;

		// Read
		int b = delegate.read();
		currentPos++;
		if (b == -1)
			atEnd = true;
		return b;
	}

	@Override
	public int read(byte[] data) throws IOException {
		// Read header
		readHeader();
		if (atEnd)
			return -1;

		// Get amount to read
		int bytesToRead = data.length;
		int bytesRead = 0;
		int position = 0;

		// Read
		while (bytesToRead > 0) {
			// Read block
			int amount = bytesToRead;
			if (amount > (currentLength - currentPos))
				amount = (currentLength - currentPos);
			byte[] res = IoUtil.readNBytes(delegate, amount);
			bytesRead += res.length;
			bytesToRead -= res.length;
			currentPos += res.length;

			// Write block to output
			for (int i = 0; i < res.length; i++)
				data[position++] = res[i];

			// Read header
			readHeader();
			if (atEnd)
				break;
		}

		// Return
		return bytesRead;
	}

	@Override
	public int read(byte[] data, int start, int end) throws IOException {
		// Read header
		readHeader();
		if (atEnd)
			return -1;

		// Get amount to read
		int bytesToRead = end - start;
		int bytesRead = 0;
		int position = start;

		// Read
		while (bytesToRead > 0) {
			// Read block
			int amount = bytesToRead;
			if (amount > (currentLength - currentPos))
				amount = (currentLength - currentPos);
			byte[] res = IoUtil.readNBytes(delegate, amount);
			bytesRead += res.length;
			bytesToRead -= res.length;
			currentPos += res.length;

			// Write block to output
			for (int i = 0; i < res.length; i++)
				data[position++] = res[i];

			// Read header
			readHeader();
			if (atEnd)
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
