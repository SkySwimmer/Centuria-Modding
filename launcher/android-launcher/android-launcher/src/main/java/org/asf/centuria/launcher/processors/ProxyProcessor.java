package org.asf.centuria.launcher.processors;

import java.io.IOException;
import java.util.HashMap;

import org.asf.centuria.launcher.FeralTweaksLauncher;
import org.asf.centuria.launcher.io.IoUtil;
import org.asf.connective.RemoteClient;
import org.asf.connective.processors.HttpPushProcessor;

public class ProxyProcessor extends HttpPushProcessor {

	public String newHost;

	public ProxyProcessor(String newHost) {
		if (!newHost.endsWith("/"))
			newHost += "/";
		this.newHost = newHost;
	}

	@Override
	public HttpPushProcessor createNewInstance() {
		return new ProxyProcessor(newHost);
	}

	@Override
	public String path() {
		return "/";
	}

	@Override
	public boolean supportsNonPush() {
		return true;
	}

	@Override
	public boolean supportsChildPaths() {
		return true;
	}

	@Override
	public void process(String path, String method, RemoteClient client, String contentType) throws IOException {
		// Parse path
		if (path.startsWith("/"))
			path = path.substring(1);

		// Build url
		String url = newHost + path;
		if (!getRequest().getRequestQuery().isEmpty())
			url += "?" + getRequest().getRequestQuery();

		// Proxy
		try {
			// Set headers
			HashMap<String, String> headers = new HashMap<String, String>();
			for (String name : getHeaders().getHeaderNames()) {
				if (name.equalsIgnoreCase("Host"))
					continue;
				headers.put(name, getHeader(name));
			}

			byte[] body = null;
			if (getRequest().hasRequestBody()) {
				if (getRequest().getBodyLength() != -1)
					body = IoUtil.readNBytes(getRequest().getBodyStream(), (int) getRequest().getBodyLength());
				else
					body = IoUtil.readAllBytes(getRequest().getBodyStream());
			}
			FeralTweaksLauncher.ResponseData resp = FeralTweaksLauncher.requestRaw(url, method, headers, body);

			// Read response code
			int responseCode = resp.statusCode;
			String responseMessage = resp.statusLine.substring((Integer.toString(resp.statusCode) + " ").length());
			setResponseStatus(responseCode, responseMessage);

			// Read headers
			for (String name : resp.headers.keySet()) {
				if (name == null || name.equalsIgnoreCase("transfer-encoding"))
					continue;
				setResponseHeader(name, resp.headers.get(name), false);
			}

			// Set response
			setResponseContent(resp.bodyStream);
		} catch (IOException e) {
			// Log error
			setResponseStatus(404, "Not found");
		}
	}

}
