package org.asf.connective;

import java.io.IOException;
import java.util.ArrayList;
import java.util.Comparator;

import org.asf.connective.objects.HttpRequest;
import org.asf.connective.objects.HttpResponse;
import org.asf.connective.processors.HttpPushProcessor;
import org.asf.connective.processors.HttpRequestProcessor;

class DefaultContentSource extends ContentSource {

	private String sanitizePath(String path) {
		if (path.contains("\\"))
			path = path.replace("\\", "/");
		while (path.startsWith("/"))
			path = path.substring(1);
		while (path.endsWith("/"))
			path = path.substring(0, path.length() - 1);
		while (path.contains("//"))
			path = path.replace("//", "/");
		if (!path.startsWith("/"))
			path = "/" + path;
		return path;
	}

	@Override
	public boolean process(String path, HttpRequest request, HttpResponse response, RemoteClient client,
			ConnectiveHttpServer server) throws IOException {
		// Load handlers
		ArrayList<HttpRequestProcessor> reqProcessors = new ArrayList<HttpRequestProcessor>(server.reqProcessors);
		ArrayList<HttpPushProcessor> pushProcessors = new ArrayList<HttpPushProcessor>(server.pushProcessors);
		boolean compatible = false;
		for (HttpPushProcessor proc : pushProcessors) {
			if (proc.supportsNonPush()) {
				reqProcessors.add(proc);
			}
		}

		// Find handler
		if (request.hasRequestBody()) {
			HttpPushProcessor impl = null;
			for (HttpPushProcessor proc : pushProcessors) {
				if (!proc.supportsChildPaths()) {
					String url = request.getRequestPath();
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
				pushProcessors.sort(new Comparator<HttpPushProcessor>() {

					@Override
					public int compare(HttpPushProcessor t1, HttpPushProcessor t2) {
						return -Integer.compare(sanitizePath(t1.path()).split("/").length,
								sanitizePath(t2.path()).split("/").length);
					}

				});
				for (HttpPushProcessor proc : pushProcessors) {
					if (proc.supportsChildPaths()) {
						String url = request.getRequestPath();
						if (!url.endsWith("/"))
							url += "/";

						String supportedURL = sanitizePath(proc.path());
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
				HttpPushProcessor processor = impl.instantiate(server, request, response);
				processor.process(path, request.getRequestMethod(), client, request.getHeaderValue("Content-Type"));
			}
		} else {
			HttpRequestProcessor impl = null;
			for (HttpRequestProcessor proc : reqProcessors) {
				if (!proc.supportsChildPaths()) {
					String url = request.getRequestPath();
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
				reqProcessors.sort(new Comparator<HttpRequestProcessor>() {

					@Override
					public int compare(HttpRequestProcessor t1, HttpRequestProcessor t2) {
						return -Integer.compare(sanitizePath(t1.path()).split("/").length,
								sanitizePath(t2.path()).split("/").length);
					}

				});
				for (HttpRequestProcessor proc : reqProcessors) {
					if (proc.supportsChildPaths()) {
						String url = request.getRequestPath();
						if (!url.endsWith("/"))
							url += "/";

						String supportedURL = sanitizePath(proc.path());
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
				HttpRequestProcessor processor = impl.instantiate(server, request, response);
				processor.process(path, request.getRequestMethod(), client);
			}
		}

		// Return
		return compatible;
	}

}
