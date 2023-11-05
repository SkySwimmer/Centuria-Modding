package org.asf.connective;

import java.io.IOException;
import java.io.InputStream;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.Map;
import java.util.function.BiFunction;
import java.util.function.Predicate;

import org.asf.centuria.launcher.io.IoUtil;
import org.asf.connective.headers.HeaderCollection;
import org.asf.connective.objects.HttpRequest;
import org.asf.connective.objects.HttpResponse;
import org.asf.connective.processors.HttpPushProcessor;
import org.asf.connective.processors.HttpRequestProcessor;

import org.asf.connective.impl.http_1_1.Http_1_1_Adapter;
import org.asf.connective.impl.https_1_1.Https_1_1_Adapter;

/**
 * 
 * Connective HTTP server abstract
 * 
 * @author Sky Swimmer
 *
 */
public abstract class ConnectiveHttpServer {
	/**
	 * Version of the ConnectiveHTTP library
	 */
	public static final String CONNECTIVE_VERSION = "1.0.0.A13";

	private ContentSource contentSource = new DefaultContentSource();
	private HeaderCollection defaultHeaders = new HeaderCollection();

	protected ArrayList<HttpRequestProcessor> reqProcessors = new ArrayList<HttpRequestProcessor>();
	protected ArrayList<HttpPushProcessor> pushProcessors = new ArrayList<HttpPushProcessor>();

	public HttpRequestProcessor[] getRequestProcessors() {
		return reqProcessors.toArray(new HttpRequestProcessor[0]);
	}

	public HttpPushProcessor[] getPushProcessors() {
		return pushProcessors.toArray(new HttpPushProcessor[0]);
	}

	private static ArrayList<IServerAdapterDefinition> adapters;
	static {
		adapters = new ArrayList<IServerAdapterDefinition>();
		adapters.add(new Http_1_1_Adapter());
		adapters.add(new Https_1_1_Adapter());
	}

	private BiFunction<HttpResponse, HttpRequest, String> errorGenerator = new BiFunction<HttpResponse, HttpRequest, String>() {
		protected String htmlCache = null;

		@Override
		public String apply(HttpResponse response, HttpRequest request) {
			try {
				InputStream strm = getClass().getResource("/error.template.html").openStream();
				htmlCache = new String(IoUtil.readAllBytes(strm));
			} catch (Exception ex) {
				if (htmlCache == null)
					return "FATAL ERROR GENERATING PAGE: " + ex.getClass().getTypeName() + ": " + ex.getMessage();
			}

			String str = htmlCache;

			str = str.replace("%path%", request.getRequestPath());
			str = str.replace("%server-name%", getServerName());
			str = str.replace("%server-version%", getServerVersion());
			str = str.replace("%error-status%", Integer.toString(response.getResponseCode()));
			str = str.replace("%error-message%", response.getResponseMessage());

			return str;
		}

	};

	/**
	 * Retrieves the current ContentSource instance
	 * 
	 * @return ContentSource instance
	 */
	public ContentSource getContentSource() {
		return contentSource;
	}

	/**
	 * Assigns the ContentSource instance used by the server
	 * 
	 * @param newSource New ContentSource instance to handle server requests
	 */
	public void setContentSource(ContentSource newSource) {
		newSource.parent = contentSource;
		contentSource = newSource;
	}

	/**
	 * Registers adapters
	 * 
	 * @param adapter Adapter to register
	 */
	public static void registerAdapter(IServerAdapterDefinition adapter) {
		synchronized (adapters) {
			adapters.add(adapter);
		}
	}

	/**
	 * Finds adapters by name
	 * 
	 * @param adapterName Adapter name
	 * @return IServerAdapterDefinition instance or null
	 */
	public static IServerAdapterDefinition findAdapter(String adapterName) {
		IServerAdapterDefinition[] adapterLst;
		synchronized (adapters) {
			adapterLst = adapters.toArray(new IServerAdapterDefinition[0]);
		}
		for (IServerAdapterDefinition adapter : adapterLst) {
			if (adapter != null) {
				if (adapter.getName().equalsIgnoreCase(adapterName))
					return adapter;
			}
		}
		return null;
	}

	/**
	 * Creates a server instance by adapter name
	 * 
	 * @param adapterName Adapter name
	 * @return ConnectiveHttpServer instance or null if not found
	 * @throws IllegalArgumentException If the configuration is invalid
	 */
	public static ConnectiveHttpServer create(String adapterName) throws IllegalArgumentException {
		return create(adapterName, new HashMap<String, String>());
	}

	/**
	 * Creates a server instance by adapter name and configuration
	 * 
	 * @param adapterName   Adapter name
	 * @param configuration Server configuration
	 * @return ConnectiveHttpServer instance or null if not found
	 * @throws IllegalArgumentException If the configuration is invalid
	 */
	public static ConnectiveHttpServer create(String adapterName, Map<String, String> configuration)
			throws IllegalArgumentException {
		IServerAdapterDefinition adapter = findAdapter(adapterName);
		if (adapter == null)
			return null;
		return adapter.createServer(configuration);
	}

	/**
	 * Creates a networked server instance by adapter name
	 * 
	 * @param adapterName Adapter name
	 * @return NetworkedConnectiveHttpServer instance or null if not found
	 * @throws IllegalArgumentException If the configuration is invalid
	 */
	public static NetworkedConnectiveHttpServer createNetworked(String adapterName) throws IllegalArgumentException {
		return createNetworked(adapterName, new HashMap<String, String>());
	}

	/**
	 * Creates a networked server instance by adapter name and configuration
	 * 
	 * @param adapterName   Adapter name
	 * @param configuration Server configuration
	 * @return NetworkedConnectiveHttpServer instance or null if not found
	 * @throws IllegalArgumentException If the configuration is invalid
	 */
	public static NetworkedConnectiveHttpServer createNetworked(String adapterName, Map<String, String> configuration)
			throws IllegalArgumentException {
		IServerAdapterDefinition adapter = findAdapter(adapterName);
		if (adapter == null)
			return null;
		ConnectiveHttpServer srv = adapter.createServer(configuration);
		if (srv instanceof NetworkedConnectiveHttpServer)
			return (NetworkedConnectiveHttpServer) srv;
		return null;
	}

	/**
	 * Retrieves the server name
	 * 
	 * @return Server name string
	 */
	public abstract String getServerName();

	/**
	 * Retrieves the server version
	 * 
	 * @return Server version string
	 */
	public abstract String getServerVersion();

	/**
	 * Re-assigns the HTTP server name to a custom value
	 * 
	 * @param name HTTP server name
	 */
	public abstract void setServerName(String name);

	/**
	 * Starts the HTTP server
	 * 
	 * @throws IOException If starting fails
	 */
	public abstract void start() throws IOException;

	/**
	 * Stops the HTTP server
	 * 
	 * @throws IOException If stopping the server fails
	 */
	public abstract void stop() throws IOException;

	/**
	 * Stops the HTTP server without waiting for all clients to disconnect
	 * 
	 * @throws IOException If stopping the server fails
	 */
	public abstract void stopForced() throws IOException;

	/**
	 * Checks if the server is running
	 * 
	 * @return True if running, false otherwise
	 */
	public abstract boolean isRunning();

	/**
	 * Retrieves the default server headers
	 * 
	 * @return Default header collection
	 */
	public HeaderCollection getDefaultHeaders() {
		return defaultHeaders;
	}

	/**
	 * Waits for the server to shut down
	 */
	public void waitForExit() {
		while (isRunning()) {
			try {
				Thread.sleep(100);
			} catch (InterruptedException e) {
				break;
			}
		}
	}

	/**
	 * Registers a new push processor
	 * 
	 * @param processor The processor implementation to register
	 */
	public void registerProcessor(HttpPushProcessor processor) {
		if (!pushProcessors.stream().anyMatch(new Predicate<HttpPushProcessor>() {
			@Override
			public boolean test(HttpPushProcessor t) {
				return t.getClass().getTypeName().equals(processor.getClass().getTypeName())
						&& t.supportsChildPaths() == processor.supportsChildPaths()
						&& t.supportsNonPush() == processor.supportsNonPush() && t.path() == processor.path();
			}
		})) {
			pushProcessors.add(processor);
		}
	}

	/**
	 * Registers a new request processor
	 * 
	 * @param processor The processor implementation to register.
	 */
	public void registerProcessor(HttpRequestProcessor processor) {
		if (processor instanceof HttpPushProcessor) {
			registerProcessor((HttpPushProcessor) processor);
			return;
		}
		if (!reqProcessors.stream().anyMatch(new Predicate<HttpRequestProcessor>() {
			@Override
			public boolean test(HttpRequestProcessor t) {
				return t.getClass().getTypeName().equals(processor.getClass().getTypeName())
						&& t.supportsChildPaths() == processor.supportsChildPaths() && t.path() == processor.path();
			}
		})) {
			reqProcessors.add(processor);
		}
	}

	/**
	 * Retrieves the error page generator
	 * 
	 * @return Error page generator
	 */
	public BiFunction<HttpResponse, HttpRequest, String> getErrorPageGenerator() {
		return errorGenerator;
	}

	/**
	 * Assigns the error page generator
	 * 
	 * @param errorGenerator New error page generator
	 */
	public void setErrorPageGenerator(BiFunction<HttpResponse, HttpRequest, String> errorGenerator) {
		this.errorGenerator = errorGenerator;
	}

	/**
	 * Retrieves the protocol name
	 * 
	 * @return Server protocol name
	 */
	public abstract String getProtocolName();
}
