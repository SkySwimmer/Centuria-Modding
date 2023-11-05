package org.asf.centuria.launcher.updater.http;

import java.io.File;
import java.io.IOException;
import org.asf.centuria.launcher.updater.LauncherUpdaterMain;
import org.asf.connective.RemoteClient;
import org.asf.connective.processors.HttpPushProcessor;

import android.util.Log;
import android.widget.TextView;

public class RootRequestProcessor extends HttpPushProcessor {

	@Override
	public HttpPushProcessor createNewInstance() {
		return new RootRequestProcessor();
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
	public void process(String path, String method, RemoteClient client, String contentType) throws IOException {
		try {
			// Make sure its not attempting to access a resource outside of the scope
			if (path.startsWith("..") || path.endsWith("..") || path.contains("/..") || path.contains("../")) {
				setResponseStatus(403, "Forbidden");
				return;
			}

			// Check method
			if (!method.equalsIgnoreCase("GET") && !method.equalsIgnoreCase("HEAD")) {
				// Not found
				setResponseStatus(404, "Not found");
				return;
			}

			// Generate page
			CommonIndexPage.index(new File("root"), getRequest(), getResponse(),

					new File[] {

							new File("cache"),

							new File("data"),

							new File("externalcache"),

							new File("externalfiles")

					}, new File[0]);
			return;
		} finally {
			// Log
			if (txt == null)
				return;
			Log.i("FT-LAUNCHER", getRequest().getRequestMethod() + " " + path + " : " + getResponse().getResponseCode()
					+ " " + getResponse().getResponseMessage() + " [" + client.getRemoteAddress() + "]");
			log(getRequest().getRequestMethod() + " " + path + " : " + getResponse().getResponseCode() + " "
					+ getResponse().getResponseMessage() + " [" + client.getRemoteAddress() + "]");
		}
	}

	private static boolean logDone;
	public static TextView txt;

	private static synchronized void log(String message) {
		logDone = false;
		LauncherUpdaterMain.getEntryActivity().runOnUiThread(new Runnable() {
			@Override
			public void run() {
				txt.setText(message + "\n" + txt.getText());
				logDone = true;
			}
		});
		while (!logDone)
			try {
				Thread.sleep(10);
			} catch (InterruptedException e) {
			}
	}

}
