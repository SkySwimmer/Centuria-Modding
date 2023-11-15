package org.asf.centuria.launcher.processors;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.util.HashMap;

import org.asf.centuria.launcher.FeralTweaksLauncher;
import org.asf.connective.RemoteClient;
import org.asf.connective.processors.HttpPushProcessor;

import android.util.Log;

public class AssetProxyProcessor extends HttpPushProcessor {

	public String newHost;

	public AssetProxyProcessor(String newHost) {
		if (!newHost.endsWith("/"))
			newHost += "/";
		this.newHost = newHost;
	}

	@Override
	public HttpPushProcessor createNewInstance() {
		return new AssetProxyProcessor(newHost);
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
		try {
			// Parse path
			if (path.startsWith("/"))
				path = path.substring(1);

			// Build url
			String url = newHost + path;
			if (getRequest().getRequestQuery().isEmpty())
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
					ByteArrayOutputStream bO = new ByteArrayOutputStream();
					getRequest().transferRequestBody(bO);
					body = bO.toByteArray();
				}
				FeralTweaksLauncher.ResponseData resp = FeralTweaksLauncher.getInstance().requestRaw(url, method,
						headers, body);

				// Read response code
				int responseCode = resp.statusCode;
				String responseMessage = resp.statusLine
						.substring(("HTTP/1.1 " + Integer.toString(resp.statusCode) + " ").length());
				setResponseStatus(responseCode, responseMessage);

				// Read headers
				for (String name : resp.headers.keySet()) {
					if (name == null || name.equalsIgnoreCase("transfer-encoding"))
						continue;
					setResponseHeader(name, resp.headers.get(name));
				}

				// Set response
				if (responseCode != 204) {
					if (!resp.headers.containsKey("content-length") && resp.headers.containsKey("transfer-encoding"))
						getResponse().setContent(contentType, resp.bodyStream);
					else if (resp.headers.containsKey("content-length"))
						getResponse().setContent(contentType, resp.bodyStream,
								Long.parseLong(resp.headers.get("content-length")));
					else
						resp.bodyStream.close();
				} else
					resp.bodyStream.close();
			} catch (IOException e) {
				setResponseStatus(404, "Not found");
			}
		} finally {
			Log.i("FT-LAUNCHER", getRequest().getRequestMethod() + " " + path + " : " + getResponse().getResponseCode()
					+ " " + getResponse().getResponseMessage() + " [" + client.getRemoteAddress() + "]");
		}
	}

}
