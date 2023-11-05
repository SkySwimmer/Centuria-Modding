package org.asf.rats;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.Socket;
import java.net.SocketException;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.function.Predicate;
import java.util.stream.Stream;

import javax.net.ssl.SSLException;

import org.asf.rats.processors.HttpGetProcessor;
import org.asf.rats.processors.HttpUploadProcessor;

/**
 * 
 * HTTP Connected Clients
 * 
 * @author Stefan0436 - AerialWorks Software Foundation
 *
 */
public class ConnectedClient {
	protected static String[] uploadMethods = new String[] { "POST", "PUT", "DELETE", "PATCH" };

	protected static int timeout = 5;
	protected static int maxRequests = 1000;

	protected Socket client;
	protected OutputStream output;
	protected InputStream input;

	protected ConnectiveHTTPServer server;

	protected Thread executionThread = null;
	protected Thread keepAliveProcessor = null;

	protected int requestNumber = 0;
	protected boolean keepAliveTHEnd = true;

	public ConnectedClient(Socket client, InputStream input, OutputStream output, ConnectiveHTTPServer server) {
		this.server = server;
		this.client = client;
		this.input = input;
		this.output = output;
		executionThread = new Thread(new Runnable() {
			@Override
			public void run() {
				receive();
			}
		}, "Client processor " + client);
		executionThread.setDaemon(true);
	}

	/**
	 * Closes the client connection
	 * 
	 * @param request The request used
	 * @param status  Status code
	 * @param message Status message
	 * @return HttpResponse instance.
	 * @throws IOException If transmitting the response fails
	 */
	public synchronized HttpResponse closeConnection(HttpRequest request, int status, String message)
			throws IOException {
		HttpResponse resp = new HttpResponse(status, message, request);
		resp.addDefaultHeaders(server).setConnectionState("Closed").build(output);
		closeConnection();
		return resp;
	}

	/**
	 * Closes the client connection
	 * 
	 * @param response Http response
	 * @return HttpResponse instance.
	 * @throws IOException If transmitting the response fails
	 */
	public synchronized HttpResponse closeConnection(HttpResponse response, Socket client) throws IOException {
		response.addDefaultHeaders(server).setConnectionState("Closed").build(output);
		closeConnection();
		return response;
	}

	/**
	 * Closes the client connection, sends no response
	 */
	public synchronized void closeConnection() {
		try {
			client.close();
		} catch (IOException e) {
		}

		InputStream strm = input;
		OutputStream strmOut = output;

		try {
			strm.close();
		} catch (IOException e) {
		}
		try {
			strmOut.close();
		} catch (IOException e) {
		}

		server.clients.remove(this);
	}

	/**
	 * Processes HTTP Requests
	 * 
	 * @param msg Request
	 * @throws IOException If processing fails
	 */
	protected void processRequest(HttpRequest msg) throws IOException {
		receiving = true;
		while (!keepAliveTHEnd) {
			try {
				Thread.sleep(10);
			} catch (InterruptedException e) {
				break;
			}
		}

		boolean compatible = false;
		ArrayList<HttpGetProcessor> getProcessorLst = new ArrayList<HttpGetProcessor>(server.getProcessors);
		ArrayList<HttpUploadProcessor> uploadProcessorLst = new ArrayList<HttpUploadProcessor>(server.uploadProcessors);

		for (HttpUploadProcessor proc : uploadProcessorLst) {
			if (proc.supportsGet()) {
				getProcessorLst.add(proc);
			}
		}

		if (Stream.of(uploadMethods).anyMatch(new Predicate<String>() {

			@Override
			public boolean test(String t) {
				return t.equals(msg.method);
			}

		})) {
			HttpUploadProcessor impl = null;
			for (HttpUploadProcessor proc : uploadProcessorLst) {
				if (!proc.supportsChildPaths()) {
					String url = msg.path;
					if (!url.endsWith("/"))
						url += "/";

					String supportedURL = proc.path();
					if (!supportedURL.endsWith("/"))
						supportedURL += "/";

					if (url.equals(supportedURL)) {
						compatible = true;
						impl = proc;
						break;
					}
				}
			}
			if (!compatible) {
				uploadProcessorLst.sort(new Comparator<HttpUploadProcessor>() {

					@Override
					public int compare(HttpUploadProcessor t1, HttpUploadProcessor t2) {
						return -Integer.compare(t1.path().split("/").length, t2.path().split("/").length);
					}

				});
				for (HttpUploadProcessor proc : uploadProcessorLst) {
					if (proc.supportsChildPaths()) {
						String url = msg.path;
						if (!url.endsWith("/"))
							url += "/";

						String supportedURL = proc.path();
						if (!supportedURL.endsWith("/"))
							supportedURL += "/";

						if (url.startsWith(supportedURL)) {
							compatible = true;
							impl = proc;
							break;
						}
					}
				}
			}
			if (compatible) {
				HttpUploadProcessor processor = impl.instanciate(server, msg);
				processor.process((msg.headers.get("Content-Type") == null ? msg.headers.get("Content-type")
						: msg.headers.get("Content-Type")), client, msg.method);
				HttpResponse resp = processor.getResponse();

				if ((!msg.headers.containsKey("Connection") || !msg.headers.get("Connection").equals("Keep-Alive"))
						|| (resp.headers.containsKey("Connection")
								&& !resp.headers.get("Connection").equals("Keep-Alive"))
						|| (maxRequests != 0 && requestNumber == maxRequests))
					closeConnection(resp, client);
				else {
					receiving = false;
					if (maxRequests != 0)
						requestNumber++;
					keepAliveProcessor = new Thread(new Runnable() {
						@Override
						public void run() {
							keepAlive();
						}
					}, "Client keepalive " + client);
					keepAliveProcessor.start();
					resp.addDefaultHeaders(server);
					resp.setHeader("Keep-Alive", "timeout=" + timeout + ", max=" + maxRequests);
					resp.setConnectionState("Keep-Alive");
					resp.build(output);
				}
			}
		} else if (msg.method.equals("GET") || msg.method.equals("HEAD")) {
			HttpGetProcessor impl = null;
			for (HttpGetProcessor proc : getProcessorLst) {
				if (!proc.supportsChildPaths()) {
					String url = msg.path;
					if (!url.endsWith("/"))
						url += "/";

					String supportedURL = proc.path();
					if (!supportedURL.endsWith("/"))
						supportedURL += "/";

					if (url.equals(supportedURL)) {
						compatible = true;
						impl = proc;
						break;
					}
				}
			}
			if (!compatible) {
				getProcessorLst.sort(new Comparator<HttpGetProcessor>() {

					@Override
					public int compare(HttpGetProcessor t1, HttpGetProcessor t2) {
						return -Integer.compare(t1.path().split("/").length, t2.path().split("/").length);
					}

				});
				for (HttpGetProcessor proc : getProcessorLst) {
					if (proc.supportsChildPaths()) {
						String url = msg.path;
						if (!url.endsWith("/"))
							url += "/";

						String supportedURL = proc.path();
						if (!supportedURL.endsWith("/"))
							supportedURL += "/";

						if (url.startsWith(supportedURL)) {
							compatible = true;
							impl = proc;
							break;
						}
					}
				}
			}
			if (compatible) {
				HttpGetProcessor processor = impl.instanciate(server, msg);
				processor.process(client);
				HttpResponse resp = processor.getResponse();

				if ((!msg.headers.containsKey("Connection") || !msg.headers.get("Connection").equals("Keep-Alive"))
						|| (resp.headers.containsKey("Connection")
								&& !resp.headers.get("Connection").equals("Keep-Alive"))
						|| (maxRequests != 0 && requestNumber == maxRequests))
					closeConnection(resp, client);
				else {
					receiving = false;
					if (maxRequests != 0)
						requestNumber++;
					keepAliveProcessor = new Thread(new Runnable() {
						@Override
						public void run() {
							keepAlive();
						}
					}, "Client keepalive " + client);
					keepAliveProcessor.start();
					resp.addDefaultHeaders(server);
					resp.setHeader("Keep-Alive", "timeout=" + timeout + ", max=" + maxRequests);
					resp.setConnectionState("Keep-Alive");
					resp.build(output);
				}
			}
		}

		if (!compatible) {
			if (!msg.method.equals("GET") && !msg.method.equals("HEAD")) {
				closeConnection(msg, 405, "Unsupported request");
			} else {
				closeConnection(msg, 404, "Command not found");
			}
		}
	}

	public void beginReceive() {
		requestNumber = 0;
		receiving = false;
		executionThread.start();
	}

	protected boolean receiving = false;

	protected void keepAlive() {
		if (timeout == 0)
			return;

		keepAliveTHEnd = false;

		int current = 0;
		while (!receiving) {
			if (receiving) {
				keepAliveTHEnd = true;
				return;
			}
			if (current >= timeout) {
				closeConnection();
				break;
			}
			current++;
			try {
				Thread.sleep(1000);
			} catch (InterruptedException e) {
				break;
			}
		}

		keepAliveTHEnd = true;
	}

	protected void receive() {
		while (true) {
			receiving = false;
			HttpRequest msg = null;
			try {
				msg = HttpRequest.parse(input);
				receiving = true;
				if (msg == null) {
					HttpRequest dummy = new HttpRequest();
					dummy.version = server.getPreferredProtocol();

					closeConnection(dummy, 503, "Unsupported request");
					dummy.close();
				} else {

					// change to different version system once http reaches 1.x.x (if it ever does)
					if (Double.valueOf(msg.version.substring("HTTP/".length())) < server.httpVersion) {
						HttpRequest dummy = new HttpRequest();
						dummy.version = msg.version;

						closeConnection(dummy, 505, "HTTP Version Not Supported");
						dummy.close();
					}

					processRequest(msg);
					msg.close();
				}
			} catch (IOException ex) {
				if (!server.connected || ex instanceof SSLException || ex instanceof SocketException)
					return;

				try {
					if (msg == null) {
						msg = new HttpRequest();
						msg.version = server.getPreferredProtocol();
					}
					closeConnection(msg, 503, "Internal server error");
				} catch (IOException ex2) {
				}
			}

			if (requestNumber == 0)
				break;
		}
	}
}
