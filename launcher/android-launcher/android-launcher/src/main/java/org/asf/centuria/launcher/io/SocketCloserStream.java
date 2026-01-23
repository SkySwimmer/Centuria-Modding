package org.asf.centuria.launcher.io;

import java.io.IOException;
import java.io.InputStream;
import java.net.Socket;

public class SocketCloserStream extends InputStream {

	private InputStream delegate;
	private Socket conn;
	private boolean closed;

	public SocketCloserStream(InputStream delegate, Socket conn) {
		this.delegate = delegate;
		this.conn = conn;
	}

	@Override
	public int read() throws IOException {
		// Check
		if (closed)
			throw new IOException("Stream closed");

		// Read
		return delegate.read();
	}

	@Override
	public int read(byte[] data) throws IOException {
		return read(data, 0, data.length);
	}

	@Override
	public int read(byte[] data, int start, int end) throws IOException {
		// Check position
		if (closed)
			throw new IOException("Stream closed");

		// Check
		if (end == 0)
			return 0;

		// Check start and length
		if (start > data.length || end > data.length - start || end < 0 || start < 0)
			throw new IndexOutOfBoundsException();

		// Get amount to read
		int bytesRead = 0;
		int bytesToRead = end - start;

		// Read block
		int amount = bytesToRead;
		byte[] buffer = new byte[amount];
		int read = delegate.read(buffer, 0, amount);
		if (read == -1) {
			// End of stream
			return -1;
		}
		bytesRead += read;

		// Write block to output
		for (int i = 0; i < buffer.length; i++)
			data[start + i] = buffer[i];

		// Return
		return bytesRead;
	}

	@Override
	public void close() throws IOException {
		delegate.close();
		try {
			conn.close();
		} catch (IOException e) {
		}
		closed = true;
	}

}
