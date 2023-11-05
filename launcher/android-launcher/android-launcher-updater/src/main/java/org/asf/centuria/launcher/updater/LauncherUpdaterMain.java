package org.asf.centuria.launcher.updater;

import java.io.IOException;
import java.io.InputStream;
import java.net.URL;
import org.asf.centuria.launcher.io.IoUtil;

import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.Context;
import android.content.DialogInterface;
import android.content.Intent;
import android.content.res.AssetManager;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Color;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageView;
import androidx.constraintlayout.widget.ConstraintLayout;
import androidx.constraintlayout.widget.ConstraintLayout.LayoutParams;
import android.widget.TextView;

/**
 * 
 * Main startup class
 * 
 * @author Sky Swimmer
 * 
 */
public class LauncherUpdaterMain {

	private static boolean inited = false;
	private static Activity activity;
	private static Context context;
	private static Class<? extends Activity> activityCls;

	private static boolean logDone = false;

	/**
	 * Starts the game
	 */
	public static void startGame() {
		inited = true;
		Intent intent = new Intent(activity.getApplicationContext(), activityCls);
		intent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP);
		intent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TASK);
		intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
		activity.startActivity(intent);
	}

	/**
	 * Main startup method
	 * 
	 * @param activity Activity instance
	 */
	public static boolean mainInit(Activity activity) {
		LauncherUpdaterMain.activity = activity;
		context = activity.getApplicationContext();
		activityCls = activity.getClass();
		if (inited) {
			inited = false;
			return false;
		}

		// Hide UI
		activity.getWindow().getDecorView()
				.setSystemUiVisibility(View.SYSTEM_UI_FLAG_HIDE_NAVIGATION | View.SYSTEM_UI_FLAG_FULLSCREEN);

		// Start updater
		runUpdater(activity);

		// Return
		return true;
	}

	private static void runUpdater(Activity activity) {
		// Setup UI

		// Create main panel
		ConstraintLayout main = new ConstraintLayout(activity);
		activity.addContentView(main, new LayoutParams(LayoutParams.MATCH_PARENT, LayoutParams.MATCH_PARENT));

		// Create image view
		ImageView view = new ImageView(activity);
		main.addView(view, new LayoutParams(LayoutParams.MATCH_PARENT, LayoutParams.MATCH_PARENT));

		// Create status message
		TextView txt = new TextView(activity);
		txt.setTextColor(Color.WHITE);
		txt.setText("Loading FeralTweaks launcher... Please wait...");
		txt.setTextAlignment(TextView.TEXT_ALIGNMENT_CENTER);
		main.addView(txt, new LayoutParams(LayoutParams.MATCH_PARENT, LayoutParams.MATCH_PARENT));

		// Run launcher
		txt.setText(txt.getText() + "\nJumping to launcher thread...");
		Thread th = new Thread(new Runnable() {
			@Override
			public void run() {
				// Log
				logDone = false;
				activity.runOnUiThread(new Runnable() {
					@Override
					public void run() {
						txt.setText(txt.getText() + "\nPreparing launcher...");
						logDone = true;
					}
				});
				while (!logDone)
					try {
						Thread.sleep(10);
					} catch (InterruptedException e) {
					}

				// Prepare
				AssetManager am = activity.getAssets();
				String launcherVersion;
				String launcherURL;
				String dataUrl;
				String srvName;
				try {
					// Log
					logDone = false;
					activity.runOnUiThread(new Runnable() {
						@Override
						public void run() {
							txt.setText(txt.getText() + "\nParsing server information...");
							logDone = true;
						}
					});
					while (!logDone)
						try {
							Thread.sleep(10);
						} catch (InterruptedException e) {
						}

					// Read server info
					String url;
					String launcherName = "androidLauncher";
					try {
						InputStream strm = am.open("server.json");
						JsonObject conf = new JsonParser().parse(new String(IoUtil.readAllBytes(strm), "UTF-8"))
								.getAsJsonObject();
						srvName = conf.get("serverName").getAsString();
						url = conf.get("serverConfig").getAsString();
						if (conf.has("launcherChannelName"))
							launcherName = conf.get("launcherChannelName").getAsString();
						dataUrl = url;
						strm.close();
					} catch (Exception e) {
						Throwable t = e;
						String stackTr = "";
						while (t != null) {
							for (StackTraceElement ele : e.getStackTrace())
								stackTr += "\n  at: " + ele;
							t = t.getCause();
						}
						error(activity,
								"Invalid launcher configuration, please add a valid file named 'server.json' to 'assets' in a resource-type patch file.\n\nExpected structure of server.json:\n"
										+ "{\n" //
										+ "    \"serverName\": \"<name of server>\"," //
										+ "\n    \"serverConfig\": \"<url to server.json on the remote server>\"\n" //
										+ "}" //
										+ "\n\nException: " + e.getClass().getTypeName()
										+ (e.getMessage() != null ? ": " + e.getMessage() : "") + ":" + stackTr,
								"Launcher error");
						return;
					}

					// Log
					logDone = false;
					activity.runOnUiThread(new Runnable() {
						@Override
						public void run() {
							txt.setText(txt.getText() + "\nDownloading server manifest...");
							logDone = true;
						}
					});
					while (!logDone)
						try {
							Thread.sleep(10);
						} catch (InterruptedException e) {
						}

					// Download data
					InputStream strm = new URL(url).openStream();
					String data = new String(IoUtil.readAllBytes(strm), "UTF-8");
					strm.close();
					JsonObject info = new JsonParser().parse(data).getAsJsonObject();
					if (!info.has(launcherName))
						throw new IOException("Missing JSON element in server response: " + launcherName);
					JsonObject launcher = info.get(launcherName).getAsJsonObject();
					JsonObject launcherBase = info.get("launcher").getAsJsonObject();
					String splash = launcherBase.get("splash").getAsString();
					url = launcher.get("url").getAsString();
					String version = launcher.get("version").getAsString();

					// Handle relative paths for banner
					if (!splash.startsWith("http://") && !splash.startsWith("https://")) {
						JsonObject serverInfo = info.get("server").getAsJsonObject();
						JsonObject hosts = serverInfo.get("hosts").getAsJsonObject();
						String api = hosts.get("api").getAsString();
						if (!api.endsWith("/"))
							api += "/";
						while (splash.startsWith("/"))
							splash = splash.substring(1);
						splash = api + splash;
					}

					// Handle relative paths for banner
					if (!splash.startsWith("http://") && !splash.startsWith("https://")) {
						JsonObject serverInfo = info.get("server").getAsJsonObject();
						JsonObject hosts = serverInfo.get("hosts").getAsJsonObject();
						String api = hosts.get("api").getAsString();
						if (!api.endsWith("/"))
							api += "/";
						while (splash.startsWith("/"))
							splash = splash.substring(1);
						splash = api + splash;
					}

					// Log
					logDone = false;
					activity.runOnUiThread(new Runnable() {
						@Override
						public void run() {
							txt.setText(txt.getText() + "\nDownloading splash...");
							logDone = true;
						}
					});
					while (!logDone)
						try {
							Thread.sleep(10);
						} catch (InterruptedException e) {
						}

					// Download splash
					strm = new URL(splash).openStream();
					Bitmap img = BitmapFactory.decodeStream(strm);
					strm.close();

					// Assign fields
					launcherVersion = version;
					launcherURL = url;

					// Log
					logDone = false;
					activity.runOnUiThread(new Runnable() {
						@Override
						public void run() {
							txt.setText(txt.getText() + "\nJumping to launcher...");
							logDone = true;
						}
					});
					while (!logDone)
						try {
							Thread.sleep(10);
						} catch (InterruptedException e) {
						}

					// Run UI logic, update image
					activity.runOnUiThread(new Runnable() {
						@Override
						public void run() {
							// Update image and remove text
							((ViewGroup) txt.getParent()).removeView(txt);
							view.setImageBitmap(img);
						}
					});
				} catch (Exception e) {
					Throwable t = e;
					String stackTr = "";
					while (t != null) {
						for (StackTraceElement ele : e.getStackTrace())
							stackTr += "\n  at: " + ele;
						t = t.getCause();
					}
					error(activity,
							"Could not connect with the launcher servers, please check your internet connection. If you are connected, please wait a few minutes and try again.\n\nIf the issue remains and you are connected to the internet, please submit a support ticket."
									+ "\n\nException: " + e.getClass().getTypeName()
									+ (e.getMessage() != null ? ": " + e.getMessage() : "") + ":" + stackTr,
							"Launcher error");
					return;
				}

				// Run launcher updater
				try {
					// Create new status label in corner
					TextView txtStatus = new TextView(activity);
					txtStatus.setTextColor(Color.WHITE);
					activity.runOnUiThread(new Runnable() {

						@Override
						public void run() {
							LayoutParams params = new LayoutParams(LayoutParams.WRAP_CONTENT,
									LayoutParams.MATCH_PARENT);
							main.addView(txtStatus, params);
							txtStatus.setGravity(Gravity.BOTTOM | Gravity.LEFT);
						}

					});

					// Run
					FtUpdater.run(activity, txtStatus, launcherURL, launcherVersion, dataUrl, srvName, new Runnable() {

						@Override
						public void run() {
							// Remove main panel
							activity.runOnUiThread(new Runnable() {

								@Override
								public void run() {
									((ViewGroup) main.getParent()).removeView(main);
								}

							});
						}

					}, new Runnable() {

						@Override
						public void run() {
							activity.runOnUiThread(new Runnable() {
								@Override
								public void run() {
									// Start game
									startGame();
								}
							});
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
							"An error occurred while running the launcher update program!\n\nException: "
									+ e.getClass().getTypeName() + (e.getMessage() != null ? ": " + e.getMessage() : "")
									+ ":" + stackTr,
							"Launcher error");
					return;
				}
			}
		}, "Launcher thread");
		th.setDaemon(true);
		th.start();
	}

	private static void error(Activity activity, String message, String title) {
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

	public static Context getApplicationContext() {
		return context;
	}

}
