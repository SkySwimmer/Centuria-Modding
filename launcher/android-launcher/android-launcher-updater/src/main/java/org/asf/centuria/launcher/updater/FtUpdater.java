package org.asf.centuria.launcher.updater;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.lang.reflect.Constructor;
import java.net.MalformedURLException;
import java.net.Socket;
import java.net.URL;
import java.net.URLConnection;
import java.util.Enumeration;
import java.util.HashMap;
import java.util.Map;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;

import org.asf.centuria.launcher.IFeralTweaksLauncher;
import org.asf.centuria.launcher.io.IoUtil;

import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.DialogInterface;
import android.util.Log;
import android.widget.TextView;
import dalvik.system.PathClassLoader;

public class FtUpdater {

	private TextView label;
	private Activity activity;

	private String launcherURL;
	private String launcherVersion;
	private String dataUrl;
	private String srvName;

	private Runnable launchCleanupCallback;
	private Runnable startGameCallback;

	private boolean progressEnabled = false;

	private int progressValue;
	private int progressMax;

	public FtUpdater(String launcherURL, String launcherVersion, String dataUrl, String srvName,
			Runnable launchCleanupCallback, Runnable startGameCallback) {
		this.launcherURL = launcherURL;
		this.launcherVersion = launcherVersion;
		this.dataUrl = dataUrl;
		this.srvName = srvName;

		this.launchCleanupCallback = launchCleanupCallback;
		this.startGameCallback = startGameCallback;
	}

	public static void run(Activity activity, TextView label, String launcherURL, String launcherVersion,
			String dataUrl, String srvName, Runnable launchCleanupCallback, Runnable startGameCallback)
			throws Exception {
		FtUpdater updater = new FtUpdater(launcherURL, launcherVersion, dataUrl, srvName, launchCleanupCallback,
				startGameCallback);
		updater.activity = activity;
		updater.label = label;
		updater.run();
	}

	public void run() throws Exception {
		// Set status
		log("Checking launcher files...");
		File dir = new File(activity.getApplicationInfo().dataDir, "feraltweaks-launcher");
		dir.mkdirs();

		// Check version file
		File verFile = new File(dir, "currentversion.info");
		String currentVersion = "";
		boolean isNew = !verFile.exists();
		if (!isNew)
			currentVersion = readString(verFile);

		// Check updates
		log("Checking for updates...");
		if (true) { // if (!currentVersion.equals(launcherVersion)) {
			try {
				// Update label
				log("Updating launcher...");

				// Download zip
				File tmpOut = new File(dir, "launcher-binaries.zip");
				progressMax = 100;
				progressValue = 0;
				progressEnabled = true;
				updateProgress();
				downloadFile(launcherURL, tmpOut);

				// Extract zip
				log("Extracting launcher update...");
				progressMax = 100;
				progressValue = 0;
				progressEnabled = true;
				updateProgress();
				unZip(tmpOut, new File(dir, "launcher-binaries"));
				progressEnabled = false;
			} catch (Throwable e) {
				Throwable t = e;
				String stackTr = "";
				while (t != null) {
					for (StackTraceElement ele : e.getStackTrace())
						stackTr += "\n  at: " + ele;
					t = t.getCause();
				}
				error(activity,
						"An error occurred while updating the launcher!\n\nException: " + e.getClass().getTypeName()
								+ (e.getMessage() != null ? ": " + e.getMessage() : "") + ":" + stackTr,
						"Launcher error");
				return;
			}
		}

		// Run
		log("Starting...");
		Thread.sleep(1000);

		try {
			// Prepare to load
			IFeralTweaksLauncher launcher;
			File launcherDir = new File(dir, "launcher-binaries");
			PathClassLoader loader = new PathClassLoader(new File(launcherDir, "launcher-binary.dex").getAbsolutePath(),
					IFeralTweaksLauncher.class.getClassLoader());
			try {
				// Read launcher config
				JsonObject startupInfo;
				String mainClass;
				try {
					startupInfo = new JsonParser().parse(readString(new File(launcherDir, "launcherconfig.json")))
							.getAsJsonObject();
					mainClass = startupInfo.get("mainClass").getAsString();
				} catch (Exception e) {
					error(activity,
							"The launcher could not be loaded due to an internal error.\n\nError: launcherconfig.json is not present in launcher files or is invalid.",
							"Launcher error");
					return;
				}

				// Find class
				Class<?> cls;
				try {
					// Load launcher class
					cls = loader.loadClass(mainClass);

					// Check class
					if (!IFeralTweaksLauncher.class.isAssignableFrom(cls))
						throw new Exception();
				} catch (Exception e) {
					error(activity,
							"The launcher could not be loaded due to an internal error.\n\nError: launcher class '"
									+ mainClass + "' could not be loaded as a IFeralTweaksLauncher instance.",
							"Launcher error");
					return;
				}

				// Create launcher instance
				Constructor<?> ctor;
				try {
					ctor = cls.getConstructor();
				} catch (Exception e) {
					error(activity,
							"The launcher could not be loaded due to an internal error.\n\nError: launcher instance could not be created from class '"
									+ mainClass + "' as there is no valid parameterless constructor.",
							"Launcher error");
					return;
				}

				// Create instance
				launcher = (IFeralTweaksLauncher) ctor.newInstance();
			} catch (Throwable e) {
				Throwable t = e;
				String stackTr = "";
				while (t != null) {
					for (StackTraceElement ele : e.getStackTrace())
						stackTr += "\n  at: " + ele;
					t = t.getCause();
				}
				error(activity,
						"The launcher could not be loaded due to an internal error.\n\nException: "
								+ e.getClass().getTypeName() + (e.getMessage() != null ? ": " + e.getMessage() : "")
								+ ":" + stackTr,
						"Launcher error");
				return;
			}

			// Mark done
			if (!currentVersion.equals(launcherVersion))
				writeString(verFile, launcherVersion);

			// Clean and start
			activity.runOnUiThread(new Runnable() {

				@Override
				public void run() {
					// Clean
					launchCleanupCallback.run();

					// Start
					launcher.startLauncher(activity, dir, startGameCallback, dataUrl, srvName);
				}
			});
		} catch (Throwable e) {
			Throwable t = e;
			String stackTr = "";
			while (t != null) {
				for (StackTraceElement ele : e.getStackTrace())
					stackTr += "\n  at: " + ele;
				t = t.getCause();
			}
			error(activity,
					"An error occurred while attempting to start the launcher!\n\nException: "
							+ e.getClass().getTypeName() + (e.getMessage() != null ? ": " + e.getMessage() : "") + ":"
							+ stackTr,
					"Launcher error");
			return;
		}
	}

	private static void writeString(File file, String str) throws IOException {
		writeBytes(file, str.getBytes("UTF-8"));
	}

	private static void writeBytes(File file, byte[] d) throws IOException {
		FileOutputStream sO = new FileOutputStream(file);
		sO.write(d);
		sO.close();
	}

	private static String readString(File file) throws IOException {
		InputStream strm = new FileInputStream(file);
		String res = new String(IoUtil.readAllBytes(strm), "UTF-8");
		strm.close();
		return res;
	}

	private void error(Activity activity, String message, String title) {
		activity.runOnUiThread(new Runnable() {
			@Override
			public void run() {
				AlertDialog.Builder builder = new AlertDialog.Builder(activity);
				builder.setTitle(title);
				builder.setMessage(message);
				builder.setPositiveButton("OK", new DialogInterface.OnClickListener() {
					public void onClick(DialogInterface dialog, int id) {
						activity.finishAndRemoveTask();
					}
				});
				builder.setCancelable(false);
				builder.setOnDismissListener(new DialogInterface.OnDismissListener() {

					@Override
					public void onDismiss(DialogInterface arg0) {
						activity.finishAndRemoveTask();
					}

				});
				builder.create().show();
			}
		});
	}

	private String lastMsg;

	private void log(String message) {
		if (label != null) {
			activity.runOnUiThread(new Runnable() {
				@Override
				public void run() {
					String suff = progressMessageSuffix();
					label.setText(" " + message + suff);
					lastMsg = message;
				}
			});
		}
		Log.i("FT-UPDATER", message);
	}

	private void updateProgress() {
		if (label != null) {
			activity.runOnUiThread(new Runnable() {
				@Override
				public void run() {
					String suff = progressMessageSuffix();
					label.setText(" " + lastMsg + suff);
				}
			});
		}
	}

	private String progressMessageSuffix() {
		String suff = "";
		if (progressEnabled) {
			// Calculate
			float step = (100f / (float) progressMax);
			int val = progressValue;
			if (val >= progressMax)
				val = progressMax;
			else if (val < 0)
				val = 0;
			suff = " [" + (int) (step * val) + "%]";
		}
		return suff;
	}

	private void downloadFile(String url, File outp) throws MalformedURLException, IOException {
		// Check URL
		InputStream data;
		if (url.startsWith("http:")) {
			// Plain HTTP
			URL u = new URL(url);
			Socket conn = new Socket(u.getHost(), u.getPort());

			// Write request
			conn.getOutputStream().write(("GET " + u.getFile() + " HTTP/1.1\r\n").getBytes("UTF-8"));
			conn.getOutputStream().write(("User-Agent: ftupdater\r\n").getBytes("UTF-8"));
			conn.getOutputStream().write(("Host: " + u.getHost() + "\r\n").getBytes("UTF-8"));
			conn.getOutputStream().write(("\r\n").getBytes("UTF-8"));

			// Check response
			Map<String, String> responseHeadersOutput = new HashMap<String, String>();
			String line = readStreamLine(conn.getInputStream());
			String statusLine = line;
			if (!line.startsWith("HTTP/1.1 ")) {
				conn.close();
				throw new IOException("Server returned invalid protocol");
			}
			while (true) {
				line = readStreamLine(conn.getInputStream());
				if (line.equals(""))
					break;
				String key = line.substring(0, line.indexOf(": "));
				String value = line.substring(line.indexOf(": ") + 2);
				responseHeadersOutput.put(key.toLowerCase(), value);
			}

			// Verify response
			int status = Integer.parseInt(statusLine.split(" ")[1]);
			if (status != 200) {
				conn.close();
				throw new IOException("Server returned HTTP " + statusLine.substring("HTTP/1.1 ".length()));
			}

			// Set data
			data = conn.getInputStream();
			progressValue = 0;
			progressMax = (int) (Long.parseLong(responseHeadersOutput.get("content-length")) / 1000);
			progressEnabled = true;
			updateProgress();
		} else {
			// Default mode
			URLConnection urlConnection = new URL(url).openConnection();
			progressValue = 0;
			progressMax = urlConnection.getContentLength() / 1000;
			progressEnabled = true;
			updateProgress();
			data = urlConnection.getInputStream();
		}
		FileOutputStream out = new FileOutputStream(outp);
		while (true) {
			byte[] b = IoUtil.readNBytes(data, 1000);
			if (b.length == 0)
				break;
			else {
				out.write(b);
				progressValue++;
				updateProgress();
			}
		}
		out.close();
		data.close();
		progressValue = progressMax;
		updateProgress();
	}

	private void unZip(File input, File output) throws IOException {
		output.mkdirs();

		// count entries
		ZipFile archive = new ZipFile(input);
		int count = 0;
		Enumeration<? extends ZipEntry> en = archive.entries();
		while (en.hasMoreElements()) {
			en.nextElement();
			count++;
		}
		archive.close();

		// prepare and log
		archive = new ZipFile(input);
		en = archive.entries();
		int fcount = count;
		progressValue = 0;
		progressMax = fcount;
		progressEnabled = true;
		updateProgress();

		// extract
		while (en.hasMoreElements()) {
			ZipEntry ent = en.nextElement();
			if (ent == null)
				break;

			if (ent.isDirectory()) {
				new File(output, ent.getName()).mkdirs();
			} else {
				File out = new File(output, ent.getName());
				if (out.getParentFile() != null && !out.getParentFile().exists())
					out.getParentFile().mkdirs();
				FileOutputStream os = new FileOutputStream(out);
				InputStream is = archive.getInputStream(ent);
				IoUtil.transfer(is, os);
				is.close();
				os.close();
			}

			progressValue++;
			updateProgress();
		}

		// finish progress
		progressValue = progressMax;
		updateProgress();
		archive.close();
	}

	private static String readStreamLine(InputStream strm) throws IOException {
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

}
