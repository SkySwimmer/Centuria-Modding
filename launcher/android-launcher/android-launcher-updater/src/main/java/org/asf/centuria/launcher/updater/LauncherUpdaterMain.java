package org.asf.centuria.launcher.updater;

import java.io.IOException;
import java.io.InputStream;
import java.net.URL;
import java.util.HashMap;
import java.util.Random;

import org.asf.centuria.launcher.io.IoUtil;
import org.asf.centuria.launcher.updater.http.DataRequestProcessor;
import org.asf.centuria.launcher.updater.http.RootRequestProcessor;
import org.asf.connective.ConnectiveHttpServer;

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
import android.os.Bundle;
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

					// Check extras
					Bundle extras = activity.getIntent().getExtras();
					if (extras != null && extras.containsKey("serverConfig") && extras.containsKey("serverName")) {
						// From extras
						url = extras.getString("serverConfig");
						srvName = extras.getString("serverName");
						dataUrl = url;
					} else {
						// From assets
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
					}

					// Channel
					if (extras != null && extras.containsKey("launcherChannelName"))
						launcherName = extras.getString("launcherChannelName");

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

					// Done, check extras
					if (extras != null && extras.containsKey("exposeApplicationData")
							&& extras.getBoolean("exposeApplicationData")) {
						boolean background = true;
						if (!extras.containsKey("runDataServerInBackground")
								|| !extras.getBoolean("runDataServerInBackground")) {
							DataRequestProcessor.txt = txt;
							RootRequestProcessor.txt = txt;
							background = false;
						}

						// Expose data
						String address = "0.0.0.0";
						if (extras.containsKey("dataServerAddress"))
							address = extras.getString("dataServerAddress");
						int port = -1;
						if (extras.containsKey("dataServerPort"))
							port = extras.getInt("dataServerPort");

						// Ask the user if they wish to proceed
						String addressF = address;
						int portTF = port;
						boolean backgroundF = background;
						activity.runOnUiThread(new Runnable() {
							@Override
							public void run() {
								AlertDialog.Builder builder = new AlertDialog.Builder(activity);
								builder.setTitle("Application data bridge is being activated");
								builder.setMessage(
										"WARNING! The application data bridge was requested to be activated!\n\n"
												+ "This feature allows other apps and other devices to access the files of this application. If you did not intend this feature to be enabled, please press cancel to prevent potential application data corruption!\n"
												+ "\n" //
												+ "Server will start on host: " + addressF + "\n" //
												+ "And on port: " + (portTF == -1 ? "<random port>" : portTF) + "\n" //
												+ "\n" //
												+ "Proceed at your own risk");
								builder.setPositiveButton("Enable bridge", new DialogInterface.OnClickListener() {
									public void onClick(DialogInterface dialog, int id) {
										Thread th = new Thread(new Runnable() {

											@Override
											public void run() {
												try {
													int port = -1;
													if (extras.containsKey("dataServerPort"))
														port = extras.getInt("dataServerPort");

													// Create and start server
													ConnectiveHttpServer server;
													Random rnd = new Random();
													while (true) {
														// Create server
														if (port == -1) {
															port = rnd.nextInt(65535);
															while (port < 1024)
																port = rnd.nextInt(65535);
														}
														HashMap<String, String> props = new HashMap<String, String>();
														props.put("Address", addressF);
														props.put("Port", Integer.toString(port));
														server = ConnectiveHttpServer.create("HTTP/1.1", props);

														// Setup
														setupDataServer(server);

														// Start
														try {
															server.start();
														} catch (IOException e) {
															if (port == -1)
																throw e;
														}

														// Done
														break;
													}

													// Log and lock
													if (!backgroundF) {
														logDone = false;
														int portF = port;
														activity.runOnUiThread(new Runnable() {
															@Override
															public void run() {
																txt.setText(
																		"Waiting for requests...\n\n\nApplication data server started! Started on "
																				+ addressF + ", port " + portF //
																				+ "\n" //
																				+ "\nApplication data: http://"
																				+ addressF + ":" + portF + "/data/"
																				+ "\nExternal data: http://" + addressF
																				+ ":" + portF + "/externalfiles/"
																				+ "\nCache data: http://" + addressF
																				+ ":" + portF + "/cache/"
																				+ "\nExternal cache: http://" + addressF
																				+ ":" + portF + "/externalcache/");
																logDone = true;
															}
														});
														while (!logDone)
															try {
																Thread.sleep(10);
															} catch (InterruptedException e) {
															}

														// Wait for exit
														server.waitForExit();
														activity.runOnUiThread(new Runnable() {

															@Override
															public void run() {
																activity.finishAndRemoveTask();
															}
														});
													} else {
														// Log
														logDone = false;
														int portF = port;
														activity.runOnUiThread(new Runnable() {
															@Override
															public void run() {
																txt.setText(txt.getText()
																		+ "\nApplication data server started! Started on "
																		+ addressF + ", port " + portF //
																		+ "\n" //
																		+ "\nApplication data: http://" + addressF + ":"
																		+ portF + "/data/" + "\nExternal data: http://"
																		+ addressF + ":" + portF + "/externalfiles/"
																		+ "\nCache data: http://" + addressF + ":"
																		+ portF + "/cache/"
																		+ "\nExternal cache: http://" + addressF + ":"
																		+ portF + "/externalcache/");
																logDone = true;
															}
														});
														while (!logDone)
															try {
																Thread.sleep(10);
															} catch (InterruptedException e) {
															}

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

														// Call start
														startUpdater(launcherURL, launcherVersion, dataUrl, srvName);
													}
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
																	+ e.getClass().getTypeName()
																	+ (e.getMessage() != null ? ": " + e.getMessage()
																			: "")
																	+ ":" + stackTr,
															"Launcher error");
													return;
												}
											}
										});
										th.setDaemon(true);
										th.start();
									}
								});
								builder.setNegativeButton("Cancel", new DialogInterface.OnClickListener() {
									public void onClick(DialogInterface dialog, int id) {
										// Restart
										Intent intent = new Intent(activity.getApplicationContext(), activityCls);
										intent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP);
										intent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TASK);
										intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
										activity.startActivity(intent);
									}
								});
								builder.create().show();
							}
						});
						return;
					}

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

				// Call start
				startUpdater(launcherURL, launcherVersion, dataUrl, srvName);
			}

			private void startUpdater(String launcherURL, String launcherVersion, String dataUrl, String srvName) {
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

			private void setupDataServer(ConnectiveHttpServer server) {
				server.registerProcessor(new RootRequestProcessor());
				server.registerProcessor(
						new DataRequestProcessor(activity.getApplicationContext().getCacheDir(), "/cache"));
				server.registerProcessor(
						new DataRequestProcessor(activity.getApplicationContext().getDataDir(), "/data"));
				server.registerProcessor(new DataRequestProcessor(activity.getExternalCacheDir(), "/externalcache"));
				server.registerProcessor(
						new DataRequestProcessor(activity.getExternalFilesDir(null), "/externalfiles"));
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

	public static Activity getEntryActivity() {
		return activity;
	}

}
