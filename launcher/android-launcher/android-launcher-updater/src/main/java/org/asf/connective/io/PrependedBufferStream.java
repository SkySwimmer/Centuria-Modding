package org.asf.connective.io;

import java.io.IOException;
import java.io.InputStream;
import java.util.ArrayList;
import java.util.Arrays;

/**
 * 
 * Pre-pended buffer delegating stream, a stream that can push read content back
 * to the buffer
 * 
 * @author Sky Swimmer
 * 
 */
public class PrependedBufferStream extends InputStream {

	private InputStream delegate;

	private ArrayList<byte[]> buffers = new ArrayList<byte[]>();

	public PrependedBufferStream(InputStream delegate) {
		this.delegate = delegate;
	}

	public void returnToBuffer(byte[] data) {
		buffers.add(0, data);
	}

	@Override
	public int read(byte[] buffer) throws IOException {
		// Check buffers
		if (!buffers.isEmpty()) {
			// Read
			int max = buffer.length;

			// Get buffered data
			byte[] data = buffers.remove(0);
			if (data.length > max) {
				// Shorten
				byte[] res = Arrays.copyOfRange(data, 0, max);
				byte[] remainer = Arrays.copyOfRange(data, max, data.length);

				// Push remainer back to buffer
				returnToBuffer(remainer);

				// Assign
				data = res;
			}

			// Return
			for (int i = 0; i < data.length; i++)
				buffer[i] = data[i];
			return data.length;
		}

		// Read delegate
		return delegate.read(buffer);
	}

	@Override
	public int read(byte[] buffer, int start, int len) throws IOException {
		// Check buffers
		if (!buffers.isEmpty()) {
			// Check
			if (len == 0)
				return 0;

			// Check start and length
			if (start > buffer.length || len > buffer.length - start || len < 0 || start < 0)
				throw new IndexOutOfBoundsException();

			// Read
			int max = len;

			// Get buffered data
			byte[] data = buffers.remove(0);
			if (data.length > max) {
				// Shorten
				byte[] res = Arrays.copyOfRange(data, 0, max);
				byte[] remainer = Arrays.copyOfRange(data, max, data.length);

				// Push remainer back to buffer
				returnToBuffer(remainer);

				// Assign
				data = res;
			}

			// Return
			for (int i = 0; i < data.length; i++)
				buffer[start + i] = data[i];
			return data.length;
		}

		// Read delegate
		return delegate.read(buffer, start, len);
	}

	@Override
	public int read() throws IOException {

		// Check buffers
		if (!buffers.isEmpty()) {
			// Get buffered data
			byte[] data = buffers.remove(0);
			if (data.length > 1) {
				// Shorten
				byte[] res = Arrays.copyOfRange(data, 0, 1);
				byte[] remainer = Arrays.copyOfRange(data, 1, data.length);

				// Push remainer back to buffer
				returnToBuffer(remainer);

				// Assign
				data = res;
			}

			// Return
			return data[0];
		}

		// Read delegate
		return delegate.read();
	}

	@Override
	public void close() throws IOException {
		delegate.close();
		buffers.clear();
	}

}
