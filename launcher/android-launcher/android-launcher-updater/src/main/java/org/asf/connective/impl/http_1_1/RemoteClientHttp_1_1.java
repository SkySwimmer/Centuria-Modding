package org.asf.connective.impl.http_1_1;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.InetSocketAddress;
import java.net.Socket;
import java.net.SocketException;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.Random;
import java.util.TimeZone;
import java.util.function.Function;
import java.util.function.Predicate;
import java.util.stream.Stream;

import javax.net.ssl.SSLException;

import org.asf.connective.RemoteClient;
import org.asf.connective.objects.HttpRequest;
import org.asf.connective.objects.HttpResponse;
import org.asf.connective.io.IoUtil;
import org.asf.connective.io.LengthTrackingStream;
import org.asf.connective.headers.HeaderCollection;
import org.asf.connective.headers.HttpHeader;
import org.asf.connective.tasks.AsyncTaskManager;

public class RemoteClientHttp_1_1 extends RemoteClient {

	private ConnectiveHttpServer_1_1 server;
	private Socket socket;
	private InputStream in;
	private OutputStream out;
	private String addr;
	private int port;

	protected int timeout = 5;
	protected int maxRequests = 0;
	protected int requestNumber = 0;

	protected boolean receiving = false;
	private static Random rnd = new Random();
	private int rndT = 0;
	private long tsT = 0;

	protected RemoteClientHttp_1_1(Socket socket, ConnectiveHttpServer_1_1 server, InputStream in, OutputStream out,
			Function<HttpRequest, HttpResponse> responseCreator) {
		super(server, responseCreator);
		this.server = server;
		this.socket = socket;
		this.in = in;
		this.out = out;

		// Retrieve address and port
		InetSocketAddress addr = (InetSocketAddress) socket.getRemoteSocketAddress();
		this.addr = addr.getAddress().getHostAddress();
		this.port = addr.getPort();
	}

	public Socket getSocket() {
		return socket;
	}

	@Override
	public String getRemoteAddress() {
		return addr;
	}

	@Override
	public int getRemotePort() {
		return port;
	}

	public void beginReceive() throws IOException {
		requestNumber = 0;
		receiving = false;
		AsyncTaskManager.runAsync(new Runnable() {
			@Override
			public void run() {
				receive();
			}
		});
	}

	protected void keepAlive() {
		int rndTc = rndT;
		long tsTc = tsT;

		long start = System.currentTimeMillis();
		while (!receiving && tsTc == tsT && rndTc == rndT) {
			if (receiving || tsTc != tsT || rndTc != rndT || socket == null)
				return;
			if ((System.currentTimeMillis() - start) >= (timeout * 1000)) {
				closeConnection();
				break;
			}
			try {
				Thread.sleep(100);
			} catch (InterruptedException e) {
				break;
			}
		}
	}

	@Override
	public void closeConnection() {
		// Close
		try {
			if (socket != null)
				socket.close();
		} catch (IOException e) {
		}

		// Remove client
		synchronized (server.clients) {
			server.clients.remove(this);
		}

		// Unset socket
		socket = null;
	}

	protected void receive() {
		HttpRequest msg = null;
		while (true) {
			receiving = false;
			try {
				// Handle previous
				try {
					if (msg != null && msg.getBodyStream() != null
							&& msg.getBodyStream() instanceof LengthTrackingStream) {
						LengthTrackingStream strm = (LengthTrackingStream) msg.getBodyStream();
						long read = strm.getBytesRead();
						long len = msg.getBodyLength();
						if (read < len) {
							long remaining = len - read;
							while (remaining != 0) {
								if (remaining < 100000)
									IoUtil.readNBytes(strm, (int) remaining);
								else {
									remaining -= 100000;
									IoUtil.readNBytes(strm, 100000);
								}
							}
						}
					}
				} catch (IllegalStateException e) {
				}

				// Read request
				msg = readRequest();

				// Verify parse result
				if (msg == null) {
					// Malformed
					// Send Bad Request response
					HttpResponse resp = new HttpResponse("HTTP/1.1");
					resp.setResponseStatus(400, "Bad request");
					sendResponse(resp, null);
					closeConnection();
					return;
				}

				// Verify HTTP version
				if (!msg.getHttpVersion().equals("HTTP/1.1")) {
					// Send 505 response
					HttpResponse resp = new HttpResponse(msg.getHttpVersion());
					resp.setResponseStatus(505, "HTTP Version Not Supported");
					sendResponse(resp, null);
					closeConnection();
					return;
				}

				// Process the request
				processRequests(msg);
			} catch (Exception ex) {
				if (!server.connected || ex instanceof SSLException || ex instanceof SocketException) {
					// Remove client
					synchronized (server.clients) {
						server.clients.remove(this);
					}
					return;
				}
				ex.printStackTrace();

				if (msg != null) {
					HttpResponse resp = createResponse(msg);
					resp.setResponseStatus(500, "Internal server error");
					resp.setContent("text/html", server.getErrorPageGenerator().apply(resp, msg));
					try {
						sendResponse(resp, msg);
					} catch (IOException e) {
					}
					closeConnection();
					return;
				}
			}

			// End if needed
			if (requestNumber == 0)
				break;
		}
	}

	private void processRequests(HttpRequest msg) throws IOException {
		// Mark as receiving
		receiving = true;

		// Handle request
		processRequest(msg);
	}

	private HttpRequest readRequest() throws IOException {
		// Read first request line
		String firstLine = readStreamLine(in);
		if (firstLine == null || firstLine.isEmpty())
			return null;

		// Verify line validity
		if (!firstLine.substring(0, 1).matches("[A-Za-z0-9]") || firstLine.split(" ").length != 3) {
			return null;
		}

		try {
			// Decode first line
			String path = firstLine.substring(firstLine.indexOf(" ") + 1, firstLine.lastIndexOf(" "));
			String method = firstLine.substring(0, firstLine.indexOf(" ")).toUpperCase();
			String version = firstLine.substring(firstLine.lastIndexOf(" ") + 1);

			// Parse headers
			HeaderCollection headers = new HeaderCollection();
			while (true) {
				// Read header
				String line = readStreamLine(in);
				if (line.equals(""))
					break; // Done with headers

				// Parse header
				String key = line.substring(0, line.indexOf(": "));
				String value = line.substring(line.indexOf(": ") + 2);

				// Prevent injection by checking if its already present
				if (!headers.hasHeader(key))
					headers.addHeader(key, value); // Set header
			}

			// Load body if needed
			InputStream body = null;
			long contentLength = -1;
			if (headers.hasHeader("Content-Length")) {
				contentLength = Long.parseLong(headers.getHeaderValue("Content-Length"));
				if (contentLength > 0)
					body = new LengthTrackingStream(in);
			}

			// Create request object
			return new HttpRequest(body, contentLength, headers, version, method, path);
		} catch (Exception e) {
			return null;
		}
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

	@Override
	protected void sendResponse(HttpResponse response, HttpRequest sourceRequest) throws IOException {
		// Add headers
		SimpleDateFormat dateFormat = new SimpleDateFormat("EEE, dd MMM yyyy HH:mm:ss z", Locale.US);
		dateFormat.setTimeZone(TimeZone.getTimeZone("GMT"));
		if (response.getBodyStream() != null && !response.hasHeader("Content-Length") && response.getBodyLength() >= 0)
			response.addHeader("Content-Length", Long.toString(response.getBodyLength()));
		response.addHeader("Server", server.getServerName());
		response.addHeader("Date", dateFormat.format(new Date()));
		for (String name : server.getDefaultHeaders().getHeaderNames())
			if (!response.hasHeader(name))
				response.addHeader(name, server.getDefaultHeaders().getHeaderValue(name));
		if (response.getBodyLength() < 0 && (response.getBodyStream() != null
				&& (sourceRequest != null && !sourceRequest.getRequestMethod().equalsIgnoreCase("HEAD"))
				&& response.getResponseCode() != 204))
			response.addHeader("Transfer-Encoding", "chunked");

		if (response.getHeaders().hasHeader("Connection")
				&& response.getHeaders().getHeaderValue("Connection").equalsIgnoreCase("Keep-Alive")
				&& (maxRequests == 0 || requestNumber < maxRequests)) {
			if (response.getHeaders().hasHeader("Keep-Alive")) {
				// Set values from existing header
				String keepAliveInfo = response.getHeaderValue("Keep-Alive");
				timeout = 5;
				maxRequests = 0;
				for (String entry : keepAliveInfo.split(", ")) {
					// Parse
					if (entry.contains("=")) {
						String key = entry.substring(0, entry.indexOf("="));
						String value = entry.substring(entry.indexOf("=") + 1);
						switch (key) {

						case "timeout": {
							if (value.matches("^[0-9]+$"))
								timeout = Integer.parseInt(value);
							break;
						}
						case "max": {
							if (value.matches("^[0-9]+$"))
								maxRequests = Integer.parseInt(value);
							break;
						}

						}
					}
				}
			} else if (timeout != 0 || maxRequests != 0)
				response.addHeader("Keep-Alive", "timeout=" + timeout + ", max=" + maxRequests);
		}
		if (!response.hasHeader("Connection") && response.getResponseCode() != 101)
			response.addHeader("Connection", "Closed");
		else if (response.getResponseCode() == 101)
			response.addHeader("Connection", "Upgrade");

		// Build top line
		StringBuilder resp = new StringBuilder();
		resp.append(response.getHttpVersion()).append(" ");
		resp.append(response.getResponseCode()).append(" ");
		resp.append(response.getResponseMessage());

		// Remove headers if needed
		if ((sourceRequest != null && sourceRequest.getRequestMethod().equals("HEAD"))
				|| response.getResponseCode() == 204 || response.getResponseCode() == 201) {
			if (response.getHeaders().hasHeader("Content-Length"))
				response.removeHeader("Content-Length");
		}
		if (response.getResponseCode() == 204 || response.getResponseCode() == 201) {
			if (response.getHeaders().hasHeader("Content-Type"))
				response.removeHeader("Content-Type");
		}

		// Add all headers
		for (HttpHeader header : response.getHeaders().getHeaders()) {
			if (header.getName().equalsIgnoreCase("connection") || !header.getValue().equalsIgnoreCase("closed")) {
				for (String val : header.getValues()) {
					// Write newline for the header
					resp.append("\r\n");

					// Write key and value
					resp.append(header.getName()).append(": ");
					resp.append(val);
				}
			}
		}

		// Add padding
		resp.append("\r\n");
		resp.append("\r\n");

		// Write body if needed
		if (response.getBodyStream() != null
				&& (sourceRequest != null && !sourceRequest.getRequestMethod().equalsIgnoreCase("HEAD"))
				&& response.getResponseCode() != 204) {
			// Write headers
			out.write(resp.toString().getBytes("UTF-8"));

			// Transfer body
			if (response.getBodyLength() >= 0) {
				long length = response.getBodyLength();
				int tr = 0;
				for (long i = 0; i < length; i += tr) {
					tr = 1024 * 1024;
					if (tr > length)
						tr = (int) length;
					byte[] b = IoUtil.readNBytes(response.getBodyStream(), tr);
					tr = b.length;
					out.write(b);
				}
			} else {
				// Write in chunks
				while (true) {
					try {
						// Read 1mb
						byte[] buffer = new byte[1024 * 1024];
						int chunkSize = response.getBodyStream().read(buffer, 0, buffer.length);
						if (chunkSize == -1) {
							// End of stream
							out.write("0\r\n".toString().getBytes("UTF-8"));
							out.write("\r\n".toString().getBytes("UTF-8"));
							break;
						}

						// Write chunk size
						out.write((Integer.toString(chunkSize, 16) + "\r\n").toString().getBytes("UTF-8"));

						// Write chunk payload
						out.write(buffer, 0, chunkSize);

						// Write chunk end
						out.write("\r\n".toString().getBytes("UTF-8"));
					} catch (IOException e) {
						break;
					}
				}
			}
			response.getBodyStream().close();
		} else {
			// Write headers only
			out.write(resp.toString().getBytes("UTF-8"));
		}

		// Handle upgrade
		if (response.getResponseCode() == 101) {
			// Return so that the connection can be picked up by the upgrade implementation
			response.addHeader("Upgraded", "True"); // Header to mark that the HTTP upgrade has been done

			// Remove client
			synchronized (server.clients) {
				server.clients.remove(this);
			}
			return;
		}

		// Handle keepalive
		if ((!response.getHeaders().hasHeader("Connection")
				|| !response.getHeaders().getHeaderValue("Connection").equalsIgnoreCase("Keep-Alive"))
				|| (maxRequests != 0 && requestNumber >= maxRequests))
			closeConnection();
		else {
			if (maxRequests != 0)
				requestNumber++;
			else
				requestNumber = 1;
			rndT = rnd.nextInt();
			tsT = System.currentTimeMillis();
			receiving = false;
			AsyncTaskManager.runAsync(new Runnable() {
				@Override
				public void run() {
					receive();
				}
			});
			response.addHeader("Connection", "Keep-Alive");
		}
	}

	@Override
	public OutputStream getOutputStream() {
		return out;
	}

	@Override
	public InputStream getInputStream() {
		return in;
	}

	@Override
	protected void postProcessResponse(HttpResponse response, HttpRequest msg) {
		// Handle client keep-alive
		boolean clientKeepAlive = false;
		if (msg.getHeaders().hasHeader("Connection")
				&& Stream.of(msg.getHeaderValue("Connection").split(", ")).anyMatch(new Predicate<String>() {

					@Override
					public boolean test(String t) {
						return t.equalsIgnoreCase("Keep-Alive");
					}

				})) {
			if (msg.getHeaders().hasHeader("Keep-Alive")) {
				// Set values from existing header
				String keepAliveInfo = msg.getHeaders().getHeaderValue("Keep-Alive");
				timeout = 5;
				maxRequests = 0;
				for (String entry : keepAliveInfo.split(", ")) {
					// Parse
					if (entry.contains("=")) {
						// Read option
						String key = entry.substring(0, entry.indexOf("="));
						String value = entry.substring(entry.indexOf("=") + 1);

						// Handle option
						switch (key) {

						case "timeout": {
							if (value.matches("^[0-9]+$"))
								timeout = Integer.parseInt(value);
							break;
						}
						case "max": {
							if (value.matches("^[0-9]+$"))
								maxRequests = Integer.parseInt(value);
							break;
						}

						}
					}
				}
			} else if (timeout != 0 || maxRequests != 0)
				response.addHeader("Keep-Alive", "timeout=" + timeout + ", max=" + maxRequests);

			// Keep alive
			clientKeepAlive = true;
		}

		// Handle keep-alive
		if (clientKeepAlive)
			response.addHeader("Connection", "Keep-Alive");
	}

	@Override
	public String getRemoteHost() {
		return socket.getInetAddress().getCanonicalHostName();
	}

	@Override
	public boolean isConnected() {
		return socket != null;
	}
}
