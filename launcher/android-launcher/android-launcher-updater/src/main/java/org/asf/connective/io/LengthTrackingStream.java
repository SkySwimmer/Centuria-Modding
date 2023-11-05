package org.asf.connective.io;

import java.io.IOException;
import java.io.InputStream;

/**
 * 
 * An InputStream that keeps track of how many bytes are read
 * 
 * @author Sky Swimmer
 *
 */
public class LengthTrackingStream extends InputStream {

	private InputStream delegate;
	private long read;

	public LengthTrackingStream(InputStream delegate) {
		this.delegate = delegate;
	}

	@Override
	public int read() throws IOException {
		int i = delegate.read();
		if (i != -1)
			read++;
		return i;
	}

	/**
	 * Retrieves the amount of bytes that were read from the stream
	 * 
	 * @return Amount of bytes that were read
	 */
	public long getBytesRead() {
		return read;
	}

}
