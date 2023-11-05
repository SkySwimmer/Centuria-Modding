package org.asf.connective.impl.http_1_1;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.InetAddress;
import java.net.ServerSocket;
import java.net.Socket;
import java.net.UnknownHostException;
import java.util.ArrayList;
import java.util.function.Function;

import org.asf.connective.ConnectiveHttpServer;
import org.asf.connective.NetworkedConnectiveHttpServer;
import org.asf.connective.objects.HttpRequest;
import org.asf.connective.objects.HttpResponse;

public class ConnectiveHttpServer_1_1 extends NetworkedConnectiveHttpServer {

	protected String serverName = "ASF Connective";
	protected String serverVersion = ConnectiveHttpServer.CONNECTIVE_VERSION;
	protected InetAddress address;
	protected int port = 8080;

	protected boolean connected = false;
	protected ServerSocket socket = null;

	protected ArrayList<RemoteClientHttp_1_1> clients = new ArrayList<RemoteClientHttp_1_1>();

	public ConnectiveHttpServer_1_1() {
		try {
			address = InetAddress.getByName("0.0.0.0");
		} catch (UnknownHostException e) {
		}
	}

	@Override
	public void setListenAddress(InetAddress address) {
		this.address = address;
	}

	@Override
	public void setListenPort(int port) {
		this.port = port;
	}

	@Override
	public InetAddress getListenAddress() {
		return address;
	}

	@Override
	public int getListenPort() {
		return port;
	}

	@Override
	public String getServerName() {
		return serverName;
	}

	@Override
	public String getServerVersion() {
		return serverVersion;
	}

	@Override
	public void setServerName(String name) {
		serverName = name;
	}

	protected Thread serverThread;

	/**
	 * Retrieves the client output stream (override only)
	 */
	protected OutputStream getClientOutput(Socket client) throws IOException {
		return client.getOutputStream();
	}

	/**
	 * Retrieves the client input stream (override only)
	 */
	protected InputStream getClientInput(Socket client) throws IOException {
		return client.getInputStream();
	}

	/**
	 * Called on client connect, potential override
	 */
	protected void acceptConnection(Socket client) {
	}

	/**
	 * Called to construct a new server socket (override only)
	 */
	protected ServerSocket getServerSocket(int port, InetAddress ip) throws IOException {
		return new ServerSocket(port, 0, ip);
	}

	@Override
	public void start() throws IOException {
		// Check state
		if (socket != null)
			throw new IllegalStateException("Server already running!");

		// Start server
		connected = true;
		socket = getServerSocket(port, address);
		serverThread = new Thread(new Runnable() {
			@Override
			public void run() {
				// Server loop
				while (connected) {
					try {
						Socket client = socket.accept();

						acceptConnection(client);
						InputStream in = getClientInput(client);
						OutputStream out = getClientOutput(client);

						RemoteClientHttp_1_1 cl = new RemoteClientHttp_1_1(client, ConnectiveHttpServer_1_1.this, in,
								out, new Function<HttpRequest, HttpResponse>() {

									@Override
									public HttpResponse apply(HttpRequest t) {
										return new HttpResponse("HTTP/1.1");
									}

								});
						try {
							synchronized (clients) {
								clients.add(cl);
							}
							cl.beginReceive();
						} catch (IOException ex) {
							if (!connected)
								break;
						}
					} catch (IOException ex) {
						if (!connected)
							break;
					}
				}
			}
		}, "Connective server thread");
		serverThread.setDaemon(true);
		serverThread.start();
	}

	@Override
	public void stop() throws IOException {
		// Check state
		if (!connected)
			return;

		// Close server
		connected = false;
		try {
			socket.close();
		} catch (IOException e) {
		}

		// Wait for clients to disconnect
		while (true) {
			int c;
			synchronized (clients) {
				c = clients.size();
			}
			if (c == 0)
				break;
			try {
				Thread.sleep(100);
			} catch (InterruptedException e) {
			}
		}

		// Unset server
		socket = null;
	}

	@Override
	public void stopForced() throws IOException {
		// Check state
		if (!connected)
			return;

		// Disconnect
		connected = false;
		try {
			socket.close();
		} catch (IOException e) {
		}

		// Disconnect clients
		RemoteClientHttp_1_1[] clientLst;
		synchronized (clients) {
			clientLst = clients.toArray(new RemoteClientHttp_1_1[0]);
		}
		for (RemoteClientHttp_1_1 client : clientLst) {
			try {
				client.closeConnection();
			} catch (Exception e) {
			}
		}
		clients.clear();

		// Unset server
		socket = null;
	}

	@Override
	public boolean isRunning() {
		return connected;
	}

	@Override
	public String getProtocolName() {
		return "HTTP/1.1";
	}

}
