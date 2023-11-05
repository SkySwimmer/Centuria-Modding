package org.asf.centuria.launcher.processors;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.net.Socket;
import java.util.HashMap;

import org.asf.centuria.launcher.FeralTweaksLauncher;
import org.asf.centuria.launcher.io.IoUtil;
import org.asf.rats.ConnectiveHTTPServer;
import org.asf.rats.processors.HttpUploadProcessor;

public class ProxyProcessor extends HttpUploadProcessor {

	public String newHost;

	public ProxyProcessor(String newHost) {
		if (!newHost.endsWith("/"))
			newHost += "/";
		this.newHost = newHost;
	}

	@Override
	public HttpUploadProcessor createNewInstance() {
		return new ProxyProcessor(newHost);
	}

	@Override
	public String path() {
		return "/";
	}

	@Override
	public boolean supportsGet() {
		return true;
	}

	@Override
	public boolean supportsChildPaths() {
		return true;
	}

	@Override
	public void process(String contentType, Socket client, String method) {
		// Parse path
		String path = getRequestPath();
		if (path.startsWith("/"))
			path = path.substring(1);

		// Build url
		String url = newHost + path;
		if (getRequest().query != null && !getRequest().query.isEmpty())
			url += "?" + getRequest().query;

		// Proxy
		try {
			// Set headers
			HashMap<String, String> headers = new HashMap<String, String>();
			for (String name : getHeaders().keySet()) {
				if (name.equalsIgnoreCase("Host"))
					continue;
				headers.put(name, getHeader(name));
			}

			byte[] body = null;
			if (getRequest().getRequestBodyStream() != null) {
				ByteArrayOutputStream bO = new ByteArrayOutputStream();
				getRequest().transferRequestBody(bO);
				body = bO.toByteArray();
			}
			FeralTweaksLauncher.ResponseData resp = FeralTweaksLauncher.requestRaw(url, method, headers, body);

			// Read response code
			int responseCode = resp.statusCode;
			String responseMessage = resp.statusLine.substring((Integer.toString(resp.statusCode) + " ").length());
			setResponseCode(responseCode);
			setResponseMessage(responseMessage);

			// Read headers
			for (String name : resp.headers.keySet()) {
				if (name == null || name.equalsIgnoreCase("transfer-encoding"))
					continue;
				setResponseHeader(name, resp.headers.get(name));
			}

			// Set response
			getResponse().setContent(contentType, new ByteArrayInputStream(IoUtil.readAllBytes(resp.bodyStream)));
		} catch (IOException e) {
			setResponseCode(404);
			setResponseMessage("Not found");
		}
	}

}
