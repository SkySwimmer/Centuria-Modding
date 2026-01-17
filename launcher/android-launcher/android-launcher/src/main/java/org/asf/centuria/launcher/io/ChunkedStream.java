package org.asf.centuria.launcher.io;

import java.io.IOException;
import java.io.InputStream;
import java.util.Arrays;

public class ChunkedStream extends InputStream {

	private PrependedBufferStream delegate;
	private int currentPos = 0;
	private int currentLength = -1;
	private boolean atEnd = false;

	public ChunkedStream(InputStream delegate) {
		this.delegate = new PrependedBufferStream(delegate);
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

	protected String readStreamLine(PrependedBufferStream strm) throws IOException {
		// Read a number of bytes
		byte[] content = new byte[20480];
		int read = strm.read(content, 0, content.length);
		if (read <= -1) {
			// Failed
			return null;
		} else {
			// Trim array
			content = Arrays.copyOfRange(content, 0, read);

			// Find newline
			String newData = new String(content, "UTF-8");
			if (newData.contains("\n")) {
				// Found newline
				String line = newData.substring(0, newData.indexOf("\n"));
				int offset = line.length() + 1;
				int returnLength = content.length - offset;
				if (returnLength > 0) {
					// Return
					strm.returnToBuffer(Arrays.copyOfRange(content, offset, content.length));
				}
				return line.replace("\r", "");
			} else {
				// Read more
				while (true) {
					byte[] addition = new byte[20480];
					read = strm.read(addition, 0, addition.length);
					if (read <= -1) {
						// Failed
						strm.returnToBuffer(content);
						return null;
					}

					// Trim
					addition = Arrays.copyOfRange(addition, 0, read);

					// Append
					byte[] newContent = new byte[content.length + addition.length];
					for (int i = 0; i < content.length; i++)
						newContent[i] = content[i];
					for (int i = content.length; i < newContent.length; i++)
						newContent[i] = addition[i - content.length];
					content = newContent;

					// Find newline
					newData = new String(content, "UTF-8");
					if (newData.contains("\n")) {
						// Found newline
						String line = newData.substring(0, newData.indexOf("\n"));
						int offset = line.length() + 1;
						int returnLength = content.length - offset;
						if (returnLength > 0) {
							// Return
							strm.returnToBuffer(Arrays.copyOfRange(content, offset, content.length));
						}
						return line.replace("\r", "");
					}
				}
			}
		}
	}

}
