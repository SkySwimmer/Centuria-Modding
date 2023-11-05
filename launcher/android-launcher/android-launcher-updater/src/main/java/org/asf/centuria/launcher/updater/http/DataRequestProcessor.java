package org.asf.centuria.launcher.updater.http;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.FileFilter;
import java.io.IOException;
import java.io.InputStream;
import java.util.function.Predicate;
import java.util.stream.Stream;

import org.asf.centuria.launcher.updater.LauncherUpdaterMain;
import org.asf.connective.RemoteClient;
import org.asf.connective.processors.HttpPushProcessor;

import android.util.Log;
import android.webkit.MimeTypeMap;
import android.widget.TextView;

public class DataRequestProcessor extends HttpPushProcessor {

	private String path;
	private File sourceDir;

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

	public DataRequestProcessor(File sourceDir, String path) {
		this.sourceDir = sourceDir;
		this.path = sanitizePath(path);
	}

	@Override
	public HttpPushProcessor createNewInstance() {
		return new DataRequestProcessor(sourceDir, path);
	}

	@Override
	public String path() {
		return path;
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
			// Compute subpath
			path = sanitizePath(path.substring(this.path.length()));

			// Make sure its not attempting to access a resource outside of the scope
			if (path.startsWith("..") || path.endsWith("..") || path.contains("/..") || path.contains("../")) {
				setResponseStatus(403, "Forbidden");
				return;
			}

			// Find file
			File requestedFile = new File(sourceDir, path);
			if (!requestedFile.exists()) {
				// Not found
				setResponseStatus(404, "Not found");
				return;
			} else if (requestedFile.isDirectory()) {
				// Directory
				if (method.equalsIgnoreCase("DELETE")) {
					// Delete
					deleteDir(requestedFile);
					setResponseStatus(200, "OK");
					return;
				} else {
					// Check
					if (!method.equalsIgnoreCase("GET") && !method.equalsIgnoreCase("HEAD")) {
						// Not found
						setResponseStatus(404, "Not found");
						return;
					}
				}

				// Find index page

				// Find one by extension
				File indexPage = null;
				if (indexPage == null) {
					// Find in directory
					File[] files = requestedFile.listFiles(new FileFilter() {

						@Override
						public boolean accept(File t) {
							return t.isDirectory() && t.getName().startsWith("index.")
									&& !t.getName().substring("index.".length()).contains(".");
						}

					});
					if (files.length != 0) {
						if (Stream.of(files).anyMatch(new Predicate<File>() {
							@Override
							public boolean test(File t) {
								return t.getName().equals("index.html");
							}
						})) {
							indexPage = new File(requestedFile, "index.html");
						} else if (Stream.of(files).anyMatch(new Predicate<File>() {
							@Override
							public boolean test(File t) {
								return t.getName().equals("index.htm");
							}
						})) {
							indexPage = new File(requestedFile, "index.html");
						} else
							indexPage = files[0];
					}
				}

				// Check
				if (indexPage != null) {
					// Assign new page
					requestedFile = indexPage;
					path = path + "/" + indexPage.getName();
				} else {
					// Index
					CommonIndexPage.index(requestedFile, getRequest(), getResponse());
					return;
				}
			} else {
				if (method.equalsIgnoreCase("DELETE")) {
					// Delete
					requestedFile.delete();
					setResponseStatus(200, "OK");
					return;
				} else if (method.equalsIgnoreCase("PUT") || method.equalsIgnoreCase("POST")) {
					// Upload
					if (requestedFile.getParentFile() != null)
						requestedFile.getParentFile().mkdirs();
					boolean existed = requestedFile.exists();

					// Write
					FileOutputStream fO = new FileOutputStream(requestedFile);
					getRequest().transferRequestBody(fO);
					fO.close();

					// Return
					if (existed)
						setResponseStatus(200, "OK");
					else
						setResponseStatus(201, "Created");
					return;
				}
			}

			// Find type
			String type = "";
			if (requestedFile.getName().endsWith(".xml"))
				type = "application/xml";
			else if (requestedFile.getName().endsWith(".json"))
				type = "application/json";
			else if (requestedFile.getName().endsWith(".js"))
				type = "text/javascript";
			else if (requestedFile.getName().endsWith(".css"))
				type = "text/css";
			else if (requestedFile.getName().endsWith(".html"))
				type = "text/html";
			else if (requestedFile.getName().endsWith(".ini"))
				type = "text/ini";
			else {
				String extension = requestedFile.getName();
				if (extension.contains("."))
					extension = extension.substring(extension.lastIndexOf(".") + 1);
				type = MimeTypeMap.getSingleton().getMimeTypeFromExtension(extension);
				if (type == null)
					type = "application/octet-stream";
			}

			// Load file
			InputStream fileStream = new FileInputStream(requestedFile);

			// Set output
			if (getResponse().hasHeader("Content-Type"))
				type = getResponse().getHeaderValue("Content-Type");
			setResponseContent(type, fileStream, requestedFile.length());
		} finally {
			// Log
			if (txt == null)
				return;
			Log.i("FT-LAUNCHER",
					getRequest().getRequestMethod() + " " + path() + path + " : " + getResponse().getResponseCode()
							+ " " + getResponse().getResponseMessage() + " [" + client.getRemoteAddress() + "]");
			log(getRequest().getRequestMethod() + " " + path() + path + " : " + getResponse().getResponseCode() + " "
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

	private static void deleteDir(File dir) {
		if (!dir.exists())
			return;
		for (File subDir : dir.listFiles(new FileFilter() {
			@Override
			public boolean accept(File t) {
				return t.isDirectory();
			}
		})) {
			deleteDir(subDir);
		}
		for (File file : dir.listFiles(new FileFilter() {
			@Override
			public boolean accept(File t) {
				return !t.isDirectory();
			}
		})) {
			file.delete();
		}
		dir.delete();
	}
}
