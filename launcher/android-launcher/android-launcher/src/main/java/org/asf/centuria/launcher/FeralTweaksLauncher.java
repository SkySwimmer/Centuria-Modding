package org.asf.centuria.launcher;

import java.io.Closeable;
import java.io.File;
import java.io.FileFilter;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.UnsupportedEncodingException;
import java.net.HttpURLConnection;
import java.net.InetAddress;
import java.net.MalformedURLException;
import java.net.ServerSocket;
import java.net.Socket;
import java.net.URL;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Base64;
import java.util.Enumeration;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Map.Entry;
import java.util.Random;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;

import javax.net.ssl.SSLSocketFactory;

import org.asf.centuria.launcher.io.ChunkedStream;
import org.asf.centuria.launcher.io.IoUtil;
import org.asf.centuria.launcher.io.LengthLimitedStream;
import org.asf.centuria.launcher.io.PrependedBufferStream;
import org.asf.centuria.launcher.io.SocketCloserStream;
import org.asf.centuria.launcher.processors.AssetProxyProcessor;
import org.asf.centuria.launcher.processors.ProxyProcessor;
import org.asf.connective.ConnectiveHttpServer;
import org.asf.windowsill.WMNI;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.DialogInterface;
import android.content.res.AssetManager;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Color;
import android.os.Bundle;
import android.text.InputType;
import android.util.Log;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.widget.EditText;
import android.widget.ImageView;
import android.widget.ImageView.ScaleType;
import android.widget.LinearLayout;
import android.widget.RelativeLayout;
import android.widget.RelativeLayout.LayoutParams;
import android.widget.TextView;

public class FeralTweaksLauncher implements IFeralTweaksLauncher {

	private static FeralTweaksLauncher instance;

	private boolean tappedStatus;

	private TextView label;
	private Activity activity;

	private File launcherDir;
	private File nativeLibraryDir;
	private File modPersistentDataDir;
	private File persistentDataDir;
	private File loaderDir;

	private String os;
	private String feralPlat;
	private JsonObject serverInfo;
	private JsonObject hosts;
	private JsonObject ports;
	private JsonObject modloader;

	private boolean progressEnabled = false;

	private int progressValue;
	private int progressMax;

	private boolean logDone = false;
	private boolean disableLogin;
	private boolean disableModloader;
	private boolean useProxyMethod;

	private String proxyAssetUrl = "https://game-assets.emuferal.openferal.net/";

	private String accountID;

	private static boolean windowsillLoaded;

	@Override
	public void startLauncher(Activity activity, File launcherDir, Runnable startGameCallback, String dataUrl,
			String srvName) {
		// Assign
		instance = this;
		this.activity = activity;
		this.launcherDir = launcherDir;
		this.nativeLibraryDir = new File(activity.getApplicationInfo().dataDir, "natives");
		this.modPersistentDataDir = new File(activity.getExternalFilesDir(null), "FT Save Data");
		this.persistentDataDir = activity.getExternalFilesDir(null);
		persistentDataDir.mkdirs();
		nativeLibraryDir.mkdirs();
		launcherDir.mkdirs();

		// Setup UI

		// Create main panel
		RelativeLayout main = new RelativeLayout(activity);
		activity.addContentView(main, new LayoutParams(LayoutParams.MATCH_PARENT, LayoutParams.MATCH_PARENT));

		// Create image view
		ImageView view = new ImageView(activity);
		view.setScaleType(ScaleType.FIT_CENTER);
		main.addView(view, new LayoutParams(LayoutParams.MATCH_PARENT, LayoutParams.MATCH_PARENT));

		// Create status message
		TextView txt = new TextView(activity);
		txt.setTextColor(Color.WHITE);
		txt.setText("FeralTweaks Launcher Loading...");
		txt.setTextSize(24);
		txt.setGravity(Gravity.CENTER);
		txt.setTextAlignment(TextView.TEXT_ALIGNMENT_CENTER);
		main.addView(txt, new LayoutParams(LayoutParams.MATCH_PARENT, LayoutParams.MATCH_PARENT));

		// Run launcher
		Thread th = new Thread(new Runnable() {
			@Override
			public void run() {
				// Load natives
				loadNativeLibraries(activity);

				// Contact server
				try {
					// Read server info
					String url = dataUrl;

					// Load launcher channel
					String launcherName = "androidLauncher";
					AssetManager am = activity.getAssets();
					InputStream strm = am.open("server.json");
					JsonObject conf = new JsonParser().parse(new String(IoUtil.readAllBytes(strm), "UTF-8"))
							.getAsJsonObject();
					if (conf.has("launcherChannelName"))
						launcherName = conf.get("launcherChannelName").getAsString();
					strm.close();

					// Channel
					Bundle extras = activity.getIntent().getExtras();
					if (extras != null && extras.containsKey("launcherChannelName"))
						launcherName = extras.getString("launcherChannelName");

					// Download data
					strm = new URL(url).openStream();
					String data = new String(IoUtil.readAllBytes(strm), "UTF-8");
					strm.close();
					JsonObject info = new JsonParser().parse(data).getAsJsonObject();
					JsonObject launcher = info.get("launcher").getAsJsonObject();
					JsonObject androidLauncher = info.get(launcherName).getAsJsonObject();
					String banner = launcher.get("banner").getAsString();
					url = launcher.get("url").getAsString();
					serverInfo = info.get("server").getAsJsonObject();
					hosts = serverInfo.get("hosts").getAsJsonObject();
					ports = serverInfo.get("ports").getAsJsonObject();
					JsonObject loader = launcher.get("modloader").getAsJsonObject();

					// Check settings
					if (androidLauncher.has("disableModloader"))
						disableModloader = androidLauncher.get("disableModloader").getAsBoolean();
					else if (androidLauncher.has("disableFeraltweaks"))
						disableModloader = androidLauncher.get("disableFeraltweaks").getAsBoolean();
					if (androidLauncher.has("disableLogin"))
						disableLogin = androidLauncher.get("disableLogin").getAsBoolean();
					if (androidLauncher.has("useProxyMethod"))
						useProxyMethod = androidLauncher.get("useProxyMethod").getAsBoolean();
					if (androidLauncher.has("proxyAssetUrl"))
						proxyAssetUrl = androidLauncher.get("proxyAssetUrl").getAsString();
					if (!proxyAssetUrl.endsWith("/"))
						proxyAssetUrl += "/";

					// Assign banner
					String api = hosts.get("api").getAsString();
					if (!api.endsWith("/"))
						api += "/";
					String apiData = api + "data/";
					banner = processRelative(apiData, banner);
					proxyAssetUrl = processRelative(apiData, proxyAssetUrl);

					// Assign platform
					os = "android-";
					String channel = "";
					if (android.os.Process.is64Bit()) {
						os += "arm64";
						channel = "android-arm64";
						if (androidLauncher.has("modloaderChannelArm64")) {
							channel = androidLauncher.get("modloaderChannelArm64").getAsString();
						}
					} else {
						os += "armeabi";
						channel = "android-armeabi";
						if (androidLauncher.has("modloaderChannelArmeabi")) {
							channel = androidLauncher.get("modloaderChannelArmeabi").getAsString();
						}
					}
					feralPlat = os;

					// Check
					if (!loader.has(os)) {
						error("Unsupported platform!\n\nThe launcher cannot load on your device due to there being no modloader for your platform in the server configuration. Please wait until your device is supported.\n\nOS Name: "
								+ os, "Launcher Error");
						return;
					}

					// Assign
					modloader = loader.get(channel).getAsJsonObject();
					loaderDir = new File(activity.getExternalFilesDir(null), modloader.get("name").getAsString());
					modPersistentDataDir.mkdirs();
					loaderDir.mkdirs();

					// Download banner
					strm = new URL(banner).openStream();
					Bitmap img = BitmapFactory.decodeStream(strm);
					strm.close();

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
					error("Could not connect with the launcher servers, please check your internet connection. If you are connected, please wait a few minutes and try again.\n\nIf the issue remains and you are connected to the internet, please submit a support ticket.",
							"Launcher Error");
					return;
				}

				// Run launcher
				try {
					// Get urls
					String api = hosts.get("api").getAsString();
					if (!api.endsWith("/"))
						api += "/";
					String apiData = api + "data/";
					String apiDataF = apiData;

					// Create title
					TextView txtTitle = new TextView(activity);
					txtTitle.setTextColor(Color.WHITE);
					txtTitle.setBackgroundColor(Color.DKGRAY);
					txtTitle.setTextSize(18);
					txtTitle.setText(srvName + " Launcher");
					txtTitle.setGravity(Gravity.CENTER);

					// Create new status label in corner
					TextView txtStatus = new TextView(activity);
					txtStatus.setTextColor(Color.WHITE);
					txtStatus.setBackgroundColor(Color.DKGRAY);
					txtStatus.setTextSize(18);
					activity.runOnUiThread(new Runnable() {

						@Override
						public void run() {
							RelativeLayout.LayoutParams params = new RelativeLayout.LayoutParams(
									LayoutParams.MATCH_PARENT, LayoutParams.WRAP_CONTENT);
							params.addRule(RelativeLayout.ALIGN_PARENT_TOP);
							params.addRule(RelativeLayout.ALIGN_PARENT_LEFT);
							main.addView(txtTitle, params);
							params = new RelativeLayout.LayoutParams(LayoutParams.MATCH_PARENT,
									LayoutParams.WRAP_CONTENT);
							params.addRule(RelativeLayout.ALIGN_PARENT_BOTTOM);
							params.addRule(RelativeLayout.ALIGN_PARENT_LEFT);
							main.addView(txtStatus, params);
							txtStatus.setOnClickListener(new View.OnClickListener() {

								@Override
								public void onClick(View arg0) {
									tappedStatus = true;
								}

							});
						}

					});
					label = txtStatus;

					// Run
					log("Preparing...");

					// Check credentials
					if (!disableLogin) {
						log("Verifying login... (tap this label to switch accounts)");
						String lastAccountName = "";
						String lastToken = "";
						String authToken = "";
						String accountID = "";
						tappedStatus = false;
						boolean isManuallySelected = false;
						if (new File(launcherDir, "login.json").exists()) {
							try {
								JsonObject login = new JsonParser()
										.parse(readFileAsString(new File(launcherDir, "login.json"))).getAsJsonObject();
								lastToken = login.get("token").getAsString();
								lastAccountName = login.get("loginName").getAsString();

								// Check if shift is down
								for (int i = 0; i < 50; i++) {
									if (tappedStatus) {
										isManuallySelected = true;
										break;
									}
									Thread.sleep(100);
								}
							} catch (Exception e) {
								// Corrupted login data file likely
							}
						}
						tappedStatus = false;
						boolean requireRelogin = true;
						if (!lastToken.isEmpty()) {
							// Contact API
							try {
								HashMap<String, String> headers = new HashMap<String, String>();
								headers.put("Authorization", "Bearer " + lastToken);
								InputStream res = request(api + "centuria/refreshtoken", "POST", headers, null);

								// Read response
								String data = new String(IoUtil.readAllBytes(res), "UTF-8");
								res.close();
								JsonObject resp = new JsonParser().parse(data).getAsJsonObject();
								authToken = resp.get("auth_token").getAsString();
								lastToken = resp.get("refresh_token").getAsString();
								accountID = resp.get("uuid").getAsString();

								// Save
								requireRelogin = false;
								JsonObject login = new JsonObject();
								login.addProperty("token", lastToken);
								login.addProperty("loginName", lastAccountName);
								writeString(new File(launcherDir, "login.json"), login.toString());
							} catch (Exception e) {
								isManuallySelected = false;
							}
						}

						// Check
						if (requireRelogin || isManuallySelected) {
							// Show login
							final String accNmF = lastAccountName;
							log("Opened login window");

							// Open window
							String accountIDF = accountID;
							String authTokenF = authToken;
							boolean isManuallySelectedF = isManuallySelected;
							activity.runOnUiThread(new Runnable() {
								@Override
								public void run() {
									showLogin();
								}

								private void showLogin() {
									AlertDialog.Builder builder = new AlertDialog.Builder(activity);
									builder.setTitle("Player Login");
									builder.setCancelable(false);

									// Set text boxes
									LinearLayout lila1 = new LinearLayout(activity);
									lila1.setOrientation(LinearLayout.VERTICAL);
									EditText usr = new EditText(activity);
									if (accNmF != null)
										usr.setText(accNmF);
									EditText pass = new EditText(activity);
									pass.setInputType(
											InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PASSWORD);
									lila1.addView(new TextView(activity));
									lila1.addView(new TextView(activity) {
										{
											setText("Log into your Centuria accoint");
											setGravity(Gravity.CENTER);
											setTextColor(Color.BLACK);
											setTextSize(18);
										}
									});
									lila1.addView(new TextView(activity));
									lila1.addView(new TextView(activity) {
										{
											setText("Username");
										}
									});
									lila1.addView(usr);
									lila1.addView(new TextView(activity) {
										{
											setText("Password");
										}
									});
									lila1.addView(pass);
									lila1.addView(new TextView(activity));
									builder.setView(lila1);

									// Set buttons
									builder.setPositiveButton("OK", new DialogInterface.OnClickListener() {
										public void onClick(DialogInterface dialog, int id) {
											// Try login
											Thread th = new Thread(new Runnable() {
												@Override
												public void run() {
													// Contact API
													try {
														String api = hosts.get("api").getAsString();
														if (!api.endsWith("/"))
															api += "/";
														HashMap<String, String> headers = new HashMap<String, String>();
														headers.put("Content-Type", "application/json");
														JsonObject payload = new JsonObject();
														payload.addProperty("username", usr.getText().toString());
														payload.addProperty("password", pass.getText().toString());
														ResponseData res = requestRaw(api + "a/authenticate", "POST",
																headers, payload.toString().getBytes("UTF-8"));
														if (res.statusCode == 200) {
															// Read response
															String data = new String(
																	IoUtil.readAllBytes(res.bodyStream), "UTF-8");
															res.bodyStream.close();
															JsonObject resp = new JsonParser().parse(data)
																	.getAsJsonObject();
															String auth = resp.get("auth_token").getAsString();
															String refresh = resp.get("refresh_token").getAsString();

															// Result
															String accountID = resp.get("uuid").getAsString();
															String accountName = usr.getText().toString();
															String authToken = auth;
															String refreshToken = refresh;

															try {
																// Save
																JsonObject login = new JsonObject();
																login.addProperty("token", refreshToken);
																login.addProperty("loginName", accountName);
																writeString(new File(launcherDir, "login.json"),
																		login.toString());

																// Run launcher post-auth
																postAuth(apiDataF, accountID, authToken);
															} catch (Throwable e) {
																Throwable t = e;
																String stackTr = "";
																while (t != null) {
																	for (StackTraceElement ele : e.getStackTrace())
																		stackTr += "\n  at: " + ele;
																	t = t.getCause();
																}
																error("An error occurred while running the launcher!\n\nException: "
																		+ e.getClass().getTypeName()
																		+ (e.getMessage() != null
																				? ": " + e.getMessage()
																				: "")
																		+ ":" + stackTr, "Launcher error");
																return;
															}
														} else {
															// Check error
															String data = new String(
																	IoUtil.readAllBytes(res.bodyStream), "UTF-8");
															res.bodyStream.close();
															JsonObject resp = new JsonParser().parse(data)
																	.getAsJsonObject();
															switch (resp.get("error").getAsString()) {
															case "invalid_credential": {
																activity.runOnUiThread(new Runnable() {
																	@Override
																	public void run() {
																		showNonFatalErrorMessage(
																				"Credentials are invalid.",
																				"Login error", new Runnable() {
																					@Override
																					public void run() {
																						showLogin();
																					}
																				});
																	}
																});
																return;
															}
															default: {
																activity.runOnUiThread(new Runnable() {
																	@Override
																	public void run() {
																		showNonFatalErrorMessage(
																				"An unknown server error occured, please contact support.",
																				"Login error", new Runnable() {
																					@Override
																					public void run() {
																						showLogin();
																					}
																				});
																	}
																});
																return;
															}
															}
														}
													} catch (Exception e) {
														activity.runOnUiThread(new Runnable() {
															@Override
															public void run() {
																Throwable t = e;
																String stackTr = "";
																while (t != null) {
																	for (StackTraceElement ele : e.getStackTrace())
																		stackTr += "\n  at: " + ele;
																	t = t.getCause();
																}
																showNonFatalErrorMessage(
																		"An unknown error occured, please check your internet connection. If the error persists, please open a support ticket\n\nException: "
																				+ e.getClass().getTypeName()
																				+ (e.getMessage() != null
																						? ": " + e.getMessage()
																						: "")
																				+ ":" + stackTr,
																		"Login error", new Runnable() {
																			@Override
																			public void run() {
																				showLogin();
																			}
																		});
															}
														});
														return;
													}
												}
											}, "Launcher thread");
											th.setDaemon(true);
											th.start();
										}
									});
									builder.setNegativeButton("Cancel", new DialogInterface.OnClickListener() {

										public void onClick(DialogInterface dialog, int id) {
											if (!isManuallySelectedF)
												activity.finishAndRemoveTask();
											else {
												Thread th = new Thread(new Runnable() {

													@Override
													public void run() {
														try {
															// Run launcher post-auth
															postAuth(apiDataF, accountIDF, authTokenF);
														} catch (Throwable e) {
															Throwable t = e;
															String stackTr = "";
															while (t != null) {
																for (StackTraceElement ele : e.getStackTrace())
																	stackTr += "\n  at: " + ele;
																t = t.getCause();
															}
															error("An error occurred while running the launcher!\n\nException: "
																	+ e.getClass().getTypeName()
																	+ (e.getMessage() != null ? ": " + e.getMessage()
																			: "")
																	+ ":" + stackTr, "Launcher error");
															return;
														}
													}
												}, "Launcher thread");
												th.setDaemon(true);
												th.start();
											}
										}
									});

									// Show
									builder.create().show();
								}
							});
							return;
						}

						// Run launcher post-auth
						postAuth(apiData, accountID, authToken);
					} else {
						// Run game
						startGame(apiData, false, null);
					}
				} catch (Throwable e) {
					Throwable t = e;
					String stackTr = "";
					while (t != null) {
						for (StackTraceElement ele : e.getStackTrace())
							stackTr += "\n  at: " + ele;
						t = t.getCause();
					}
					error("An error occurred while running the launcher!\n\nException: " + e.getClass().getTypeName()
							+ (e.getMessage() != null ? ": " + e.getMessage() : "") + ":" + stackTr, "Launcher error");
					return;
				}
			}

			private void loadNativeLibraries(Activity activity) {
				String nativesDir = activity.getApplicationInfo().nativeLibraryDir;

				// Load windowsill
				try {
					if (windowsillLoaded)
						return;
					if (new File(new File(launcherDir, "launcher-binaries"), "libwindowsill.so").exists())
						System.load(new File(new File(launcherDir, "launcher-binaries"), "libwindowsill.so")
								.getAbsolutePath());
					else if (new File(launcherDir, "libwindowsill.so").exists())
						System.load(new File(launcherDir, "libwindowsill.so").getAbsolutePath());
					else if (new File(nativesDir, "libwindowsill.so").exists())
						System.load(new File(nativesDir, "libwindowsill.so").getAbsolutePath());
					windowsillLoaded = true;
				} catch (Throwable e) {
					// Due to how we mod, we cannot determine if the library is already loaded, so
					// we need to prevent errors crashing the app instead
				}
			}

			private void postAuth(String apiData, String accountID, String authToken) throws Throwable {
				// Success
				log("Login success!");
				FeralTweaksLauncher.this.accountID = accountID;

				// Pull details
				String api = hosts.get("api").getAsString();
				if (!api.endsWith("/"))
					api += "/";

				JsonObject payload = new JsonObject();
				payload.addProperty("id", accountID);

				// Contact API
				HashMap<String, String> headers = new HashMap<String, String>();
				headers.put("Authorization", "Bearer " + authToken);
				headers.put("Content-Type", "application/json");
				InputStream res = request(api + "centuria/getuser", "POST", headers,
						payload.toString().getBytes("UTF-8"));

				// Read response
				String dataS = new String(IoUtil.readAllBytes(res), "UTF-8");
				res.close();
				JsonObject respJ = new JsonParser().parse(dataS).getAsJsonObject();
				boolean completedTutorial = respJ.get("tutorial_completed").getAsBoolean();

				// Start game
				startGame(apiData, completedTutorial, authToken);
			}

			private void startGame(String apiData, boolean completedTutorial, String authToken) throws Throwable {
				// Check modding
				if (!disableModloader) {
					// Modloader update
					log("Checking modloader files...");

					// Read version
					String currentLoader = "";
					if (new File(launcherDir, "loaderversion.info").exists()) {
						currentLoader = readFileAsString(new File(launcherDir, "loaderversion.info"));
					}
					if (!modloader.get("version").getAsString().equals(currentLoader)) {
						// Update modloader
						log("Updating " + modloader.get("name").getAsString() + "...");
						downloadFile(processRelative(apiData, modloader.get("url").getAsString()),
								new File(launcherDir, "modloader.zip"));
						progressValue = 0;
						progressMax = 100;
						updateProgress();

						// Extract
						log("Extracting " + modloader.get("name").getAsString() + "...");
						unZip(new File(launcherDir, "modloader.zip"), loaderDir);
						copyNativeLibs(loaderDir, modloader.get("name").getAsString() + "/");

						// Save version
						writeString(new File(launcherDir, "loaderversion.info"),
								modloader.get("version").getAsString());
						progressEnabled = false;
						progressValue = 0;
						progressMax = 100;
						log("Update completed!");
					}

					// Client mod update
					log("Checking for mod updates...");

					// Read version
					String currentModVersion = "";
					String currentAssetVersion = "";
					if (new File(launcherDir, "modversion.info").exists()) {
						currentModVersion = readFileAsString(new File(launcherDir, "modversion.info"));
					}
					if (new File(launcherDir, "assetversion.info").exists()) {
						currentAssetVersion = readFileAsString(new File(launcherDir, "assetversion.info"));
					}
					if (!serverInfo.get("modVersion").getAsString().equals(currentModVersion)) {
						// Update mods
						log("Updating client mods...");

						// Download manifest
						updateMods("assemblies/index.json", modloader.get("assemblyBaseDir").getAsString(), hosts,
								authToken);

						// Save version
						writeString(new File(launcherDir, "modversion.info"),
								serverInfo.get("modVersion").getAsString());
						progressEnabled = false;
						progressValue = 0;
						progressMax = 100;
						log("Update completed!");
					}
					if (!serverInfo.get("assetVersion").getAsString().equals(currentAssetVersion)) {
						// Update mods
						log("Updating client mod assets...");

						// Download manifest
						updateMods("assets/index.json", modloader.get("assetBaseDir").getAsString(), hosts, authToken);

						// Save version
						writeString(new File(launcherDir, "assetversion.info"),
								serverInfo.get("assetVersion").getAsString());
						progressEnabled = false;
						progressValue = 0;
						progressMax = 100;
						log("Update completed!");
					}
				}

				// Ready
				log("Ready to start the game!");
				Thread.sleep(1000);

				// Prepare to start
				log("Preparing to start the game...");

				// Start client communication server
				ServerSocket serverSock = null;
				if (!disableModloader) {
					// Find a port
					ServerSocket s;
					Random rnd = new Random();
					int port;
					log("Preparing client communication...");
					while (true) {
						port = rnd.nextInt(65535);
						while (port < 1024)
							port = rnd.nextInt(65535);
						try {
							s = new ServerSocket(port, 0, InetAddress.getByName("127.0.0.1"));
							break;
						} catch (IOException e) {
						}
					}
					serverSock = s;
				}

				// Start proxies if needed
				if (useProxyMethod) {
					log("Starting proxy servers...");
					String hostDirector = hosts.get("director").getAsString();
					String hostApi = hosts.get("api").getAsString();

					// Create API proxy
					HashMap<String, String> props = new HashMap<String, String>();
					props.put("Address", "127.0.0.1");
					props.put("Port", "6970");
					ConnectiveHttpServer apiProxy = ConnectiveHttpServer.create("HTTP/1.1", props);
					apiProxy.registerProcessor(new ProxyProcessor(hostApi));
					apiProxy.start();

					// Create director proxy
					props = new HashMap<String, String>();
					props.put("Address", "127.0.0.1");
					props.put("Port", "6969");
					ConnectiveHttpServer directorProxy = ConnectiveHttpServer.create("HTTP/1.1", props);
					directorProxy.registerProcessor(new ProxyProcessor(hostDirector));
					directorProxy.start();

					// Create asset proxy
					props = new HashMap<String, String>();
					props.put("Address", "0.0.0.0");
					props.put("Port", "6967");
					ConnectiveHttpServer assetProxy = ConnectiveHttpServer.create("HTTP/1.1", props);
					assetProxy.registerProcessor(new AssetProxyProcessor(proxyAssetUrl));
					assetProxy.start();
				}

				// Start the game
				try {
					// Prepare to start
					log("Preparing game client...");

					// Setup modloader
					if (!disableModloader) {
						// Load windowsill
						log("Loading Windowsill configuration...");
						JsonObject windowsillConfig = new JsonParser()
								.parse(readFileAsString(new File(loaderDir, "windowsil.config.json")))
								.getAsJsonObject();
						File monoAssembly = new File(new File(nativeLibraryDir, modloader.get("name").getAsString()),
								windowsillConfig.get("monoAssembly").getAsString());
						File monoDir = new File(loaderDir, windowsillConfig.get("monoDir").getAsString());
						File monoLibsDir = new File(loaderDir, windowsillConfig.get("monoLibsDir").getAsString());
						File mainAssembly = new File(loaderDir, windowsillConfig.get("mainAssembly").getAsString());
						File monoEtcDir = new File(monoDir, "etc");
						monoEtcDir.mkdirs();
						String mainClass = windowsillConfig.get("mainClass").getAsString();
						String mainMethod = windowsillConfig.get("mainMethod").getAsString();
						Log.i("FT-LAUNCHER", "");
						Log.i("FT-LAUNCHER", "WINDOWSIL LOADER (WINDOWSILL) IS LOADING!");
						Log.i("FT-LAUNCHER", "");
						Log.i("FT-LAUNCHER", "Mono assembly: " + monoAssembly);
						Log.i("FT-LAUNCHER", "Mono libraries: " + monoLibsDir);
						Log.i("FT-LAUNCHER", "Mono directory: " + monoDir);
						Log.i("FT-LAUNCHER", "Main assembly: " + mainAssembly);
						Log.i("FT-LAUNCHER", "Entrypoint class: " + mainClass);
						Log.i("FT-LAUNCHER", "Entrypoint method: " + mainMethod);
						Log.i("FT-LAUNCHER", "");

						// Check files
						if (!monoDir.exists() || !monoDir.isDirectory()) {
							error("An error occurred while running the launcher!\n\nCritical windowsill error!\n\nMono folder does not exist: "
									+ windowsillConfig.get("monoDir").getAsString(), "Launcher error");
							return;
						}
						if (!monoLibsDir.exists() || !monoLibsDir.isDirectory()) {
							error("An error occurred while running the launcher!\n\nCritical windowsill error!\n\nMono libraries folder does not exist: "
									+ windowsillConfig.get("monoLibsDir").getAsString(), "Launcher error");
							return;
						}
						if (!monoAssembly.exists() || !monoAssembly.isFile()) {
							error("An error occurred while running the launcher!\n\nCritical windowsill error!\n\nMono assembly file does not exist: "
									+ windowsillConfig.get("monoAssembly").getAsString(), "Launcher error");
							return;
						}
						if (!mainAssembly.exists() || !mainAssembly.isFile()) {
							error("An error occurred while running the launcher!\n\nCritical windowsill error!\n\nMod assembly file does not exist: "
									+ windowsillConfig.get("mainAssembly").getAsString(), "Launcher error");
							return;
						}

						// Load mono
						log("Loading Mono...");
						long monoLib = WMNI.loadMonoLib(monoAssembly.getPath());
						if (monoLib == 0) {
							error("An error occurred while running the launcher!\n\nCritical windowsill error!\n\nMono assembly failed to load: "
									+ WMNI.dlLoadError(), "Launcher error");
							return;
						}

						// Init runtime
						log("Initializing Mono runtime...");
						long domain = WMNI.initRuntime(monoLib, "WINDOWSIL", loaderDir.getPath(), monoLibsDir.getPath(),
								monoEtcDir.getPath());
						log("Application domain: " + domain);
						Thread.sleep(10000);

						// TODO: windowsill setup etc
					}

					// Start client
					log("Starting client...");

					// Accept client
					if (serverSock != null) {
						final String authTokenF = authToken;
						final ServerSocket serverSockF = serverSock;
						Thread clT = new Thread(new Runnable() {
							@Override
							public void run() {
								Socket cl;
								try {
									cl = serverSockF.accept();
								} catch (IOException e) {
									return;
								}
								try {
									log("Communicating with client...");
									launcherHandoff(apiData, cl, authTokenF, hosts.get("api").getAsString(), serverInfo,
											hosts, ports, completedTutorial);
									cl.close();
									log("Finished startup!");
									Thread.sleep(1000);
									try {
										serverSockF.close();
									} catch (IOException e) {
									}
									return;
								} catch (Throwable e) {
									Throwable t = e;
									String stackTr = "";
									while (t != null) {
										for (StackTraceElement ele : e.getStackTrace())
											stackTr += "\n  at: " + ele;
										t = t.getCause();
									}
									error("An error occurred while running the launcher!\n\nException: "
											+ e.getClass().getTypeName()
											+ (e.getMessage() != null ? ": " + e.getMessage() : "") + ":" + stackTr,
											"Launcher error");
									return;
								}
							}
						}, "Client Communication Thread");
						clT.setDaemon(true);
						clT.start();
					}

					// Start
					label = null;
					startGameCallback.run();
				} catch (Throwable e) {
					try {
						serverSock.close();
					} catch (IOException e2) {
					}
					throw e;
				}
			}

			private void copyNativeLibs(File dir, String pref) throws IOException {
				if (!dir.exists())
					return;
				for (File subDir : dir.listFiles(new FileFilter() {
					@Override
					public boolean accept(File t) {
						return t.isDirectory();
					}
				})) {
					copyNativeLibs(subDir, pref + subDir.getName() + "/");
				}
				for (File file : dir.listFiles(new FileFilter() {
					@Override
					public boolean accept(File t) {
						return !t.isDirectory() && t.getName().endsWith(".so");
					}
				})) {
					// Copy to native library folder
					File nativeLib = new File(nativeLibraryDir, pref + file.getName());
					Log.i("FT-LAUNCHER", "Copying " + file.getName() + " into " + nativeLib + "...");
					nativeLib.getParentFile().mkdirs();
					InputStream strm = new FileInputStream(file);
					FileOutputStream outp = new FileOutputStream(nativeLib);
					IoUtil.transfer(strm, outp);
					outp.close();
					strm.close();
				}
			}

		}, "Launcher thread");
		th.setDaemon(true);
		th.start();
	}

	private void launcherHandoff(String apiData, Socket cl, String authToken, String api, JsonObject serverInfo,
			JsonObject hosts, JsonObject ports, boolean completedTutorial) throws Exception {
		if (!api.endsWith("/"))
			api += "/";

		// Send options
		Log.i("FT-LAUNCHER", "Downloading and sending configuration...");
		sendCommand(cl, "config",
				Base64.getEncoder()
						.encodeToString(downloadProtectedString(api + "data/feraltweaks/settings.props", authToken)
								.replace("\t", "    ").replace("\r", "").getBytes("UTF-8")));

		// Download chart patches
		Log.i("FT-LAUNCHER", "Downloading chart patches...");
		String manifest = downloadProtectedString(api + "data/feraltweaks/chartpatches/index.json", authToken);
		JsonArray patches = new JsonParser().parse(manifest).getAsJsonArray();
		for (JsonElement ele : patches) {
			String url = api + "data";
			if (!ele.getAsString().startsWith("/"))
				url += "/";
			url += ele.getAsString();

			// Download patch
			String file = ele.getAsString();
			String patch = downloadProtectedString(url, authToken);

			// Send patch
			Log.i("FT-LAUNCHER", "Sending chart patch: " + file);
			sendCommand(cl, "chartpatch", Base64.getEncoder()
					.encodeToString((file + "::" + patch.replace("\t", "    ").replace("\r", "")).getBytes("UTF-8")));
		}

		// Log
		Log.i("FT-LAUNCHER", "Sending server environment...");

		// Build command
		// The server environment commands are in order
		ArrayList<Object> serverEnv = new ArrayList<Object>();

		// API hosts
		serverEnv.add(hosts.get("director").getAsString());
		serverEnv.add(hosts.get("api").getAsString());

		// Chat
		serverEnv.add(hosts.get("chat").getAsString());
		serverEnv.add(ports.get("chat").getAsInt());

		// Gameserver
		serverEnv.add(ports.get("game").getAsInt());

		// Voicechat
		serverEnv.add(hosts.get("voiceChat").getAsString());
		serverEnv.add(ports.get("voiceChat").getAsInt());

		// Bluebox
		serverEnv.add(ports.get("bluebox").getAsInt());

		// Encryption states
		serverEnv.add(serverInfo.get("encryptedGame").getAsBoolean());
		serverEnv.add(serverInfo.get("encryptedChat").getAsBoolean());
		serverEnv.add(serverInfo.get("encryptedVoiceChat").getAsBoolean());

		// If present, send asset servers
		if (hosts.has("gameAssets")) {
			JsonObject assets = hosts.get("gameAssets").getAsJsonObject();

			// Prod
			serverEnv.add(processUrl(apiData, assets.get("prod").getAsString()));

			// If present, add stage
			if (assets.has("stage")) {
				// Stage
				serverEnv.add(processUrl(apiData, assets.get("stage").getAsString()));

				// If present, add dev
				if (assets.has("dev")) {
					// Dev
					serverEnv.add(processUrl(apiData, assets.get("dev").getAsString()));

					// If present, add S2
					if (assets.has("s2")) {
						JsonObject s2 = assets.get("s2").getAsJsonObject();

						// Prod
						serverEnv.add(processUrl(apiData, s2.get("prod").getAsString()));

						// Stage
						serverEnv.add(processUrl(apiData, s2.get("stage").getAsString()));

						// Dev
						serverEnv.add(processUrl(apiData, s2.get("dev").getAsString()));
					}
				}
			}
		}

		// Check asset hosts
		sendCommand(cl, "serverenvironment", serverEnv.toArray(new Object[0]));

		// Send autologin
		if (completedTutorial && authToken != null) {
			Log.i("FT-LAUNCHER", "Sending autologin...");
			sendCommand(cl, "autologin", authToken);
		}

		// Send end
		cl.getOutputStream().write("end\n".getBytes("UTF-8"));
	}

	private String downloadProtectedString(String url, String authToken) throws IOException {
		if (authToken == null)
			return downloadString(url);
		HashMap<String, String> headers = new HashMap<String, String>();
		headers.put("Authorization", "Bearer " + authToken);
		InputStream res = request(url, "GET", headers, null);
		String data = new String(IoUtil.readAllBytes(res), "UTF-8");
		res.close();
		return data;
	}

	/**
	 * Downloads String contents from a HTTP server
	 * 
	 * @param url URL to download from
	 * @return Response string
	 * @throws IOException If the HTTP request fails
	 */
	public String downloadString(String url) throws IOException {
		InputStream res = request(url, "GET", new HashMap<String, String>(), null);
		String data = new String(IoUtil.readAllBytes(res), "UTF-8");
		res.close();
		return data;
	}

	private void sendCommand(Socket cl, String cmd, Object... params) throws UnsupportedEncodingException, IOException {
		for (Object obj : params)
			cmd += " " + obj;
		cl.getOutputStream().write((cmd + "\n").getBytes("UTF-8"));
	}

	private void updateMods(String pth, String baseOut, JsonObject hosts, String authToken) throws Exception {
		String api = hosts.get("api").getAsString();
		if (!api.endsWith("/"))
			api += "/";
		HashMap<String, String> headers = new HashMap<String, String>();
		headers.put("Authorization", "Bearer " + authToken);
		InputStream strm = request(api + "data/clientmods/" + pth, "GET", headers, null);
		String data = new String(IoUtil.readAllBytes(strm), "UTF-8");
		strm.close();
		JsonObject resp = new JsonParser().parse(data).getAsJsonObject();
		if (resp.has("error")) {
			// Handle error
			String err = resp.get("error").getAsString();
			switch (err) {
			case "invalid_credential": {
				throw new IOException("Credentials invalid");
			}
			case "feraltweaks_not_enabled": {
				error("Client modding is not enabled on your account, unable to launch the game.", "Launcher Error");
				return;
			}
			default: {
				throw new Exception("Unknown server error: " + err);
			}
			}
		}

		// Set progress bar
		progressValue = 0;
		progressMax = resp.size();
		progressEnabled = true;
		updateProgress();

		// Download
		for (Entry<String, JsonElement> en : resp.entrySet()) {
			String path = en.getKey();
			String output = baseOut + "/" + resp.get(path).getAsString();
			if (path.startsWith("/"))
				path = path.substring(1);

			api = hosts.get("api").getAsString();
			if (!api.endsWith("/"))
				api += "/";
			api += "data/";

			// Download mod
			strm = request(api + path, "GET", headers, null);
			File outputFile = new File(loaderDir, output);
			outputFile.getParentFile().mkdirs();
			FileOutputStream outp = new FileOutputStream(outputFile);
			IoUtil.transfer(strm, outp);
			outp.close();
			strm.close();
			Log.i("FT-LAUNCHER", "Downloading " + path + " into " + output + "...");

			// Check if its a native library
			if (path.endsWith(".so")) {
				// Copy to native library folder
				File nativeLib = new File(nativeLibraryDir, output);
				Log.i("FT-LAUNCHER", "Copying " + path + " into " + nativeLib + "...");
				nativeLib.getParentFile().mkdirs();
				strm = new FileInputStream(outputFile);
				outp = new FileOutputStream(nativeLib);
				IoUtil.transfer(strm, outp);
				outp.close();
				strm.close();
			}

			// Increase progress
			progressValue++;
			updateProgress();
		}
	}

	/**
	 * Writes strings to files
	 * 
	 * @param file File object to write to
	 * @param str  File content
	 * @throws IOException If writing fails
	 */
	private static void writeString(File file, String str) throws IOException {
		writeBytes(file, str.getBytes("UTF-8"));
	}

	/**
	 * Writes files
	 * 
	 * @param file File object to write to
	 * @param d    Bytes to write
	 * @throws IOException If writing fails
	 */
	public static void writeBytes(File file, byte[] d) throws IOException {
		FileOutputStream sO = new FileOutputStream(file);
		sO.write(d);
		sO.close();
	}

	/**
	 * Reads files as strings
	 * 
	 * @param file File to read
	 * @return File contents as string
	 * @throws IOException If reading fails
	 */
	public String readFileAsString(File file) throws IOException {
		InputStream strm = new FileInputStream(file);
		String res = new String(IoUtil.readAllBytes(strm), "UTF-8");
		strm.close();
		return res;
	}

	/**
	 * Reads files
	 * 
	 * @param file File to read
	 * @return File contents as binary array
	 * @throws IOException If reading fails
	 */
	public byte[] readFileBytes(File file) throws IOException {
		InputStream strm = new FileInputStream(file);
		byte[] res = IoUtil.readAllBytes(strm);
		strm.close();
		return res;
	}

	/**
	 * Shows a fatal error message and exits the program
	 * 
	 * @param message Message to display
	 * @param title   Window title
	 */
	public void error(String message, String title) {
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

	/**
	 * Shows a non-fatal error message
	 * 
	 * @param message        Message to display
	 * @param title          Window title
	 * @param returnCallback Runnable that is invoked when the window closes
	 */
	public void showNonFatalErrorMessage(String message, String title, Runnable returnCallback) {
		AlertDialog.Builder builder = new AlertDialog.Builder(activity);
		builder.setTitle(title);
		builder.setMessage(message);
		builder.setPositiveButton("OK", new DialogInterface.OnClickListener() {
			@Override
			public void onClick(DialogInterface dialog, int id) {
			}
		});
		builder.setOnDismissListener(new DialogInterface.OnDismissListener() {
			@Override
			public void onDismiss(DialogInterface arg0) {
				returnCallback.run();
			}
		});
		builder.create().show();
	}

	private String lastMsg;

	/**
	 * Logs messages and displays them on the label
	 * 
	 * @param message Message to log
	 */
	public synchronized void log(String message) {
		if (label != null) {
			logDone = false;
			activity.runOnUiThread(new Runnable() {
				@Override
				public void run() {
					TextView lbl = label;
					if (lbl != null) {
						String suff = progressMessageSuffix();
						lbl.setText(message + suff);
						lastMsg = message;
					}
					logDone = true;
				}
			});
			while (!logDone)
				try {
					Thread.sleep(10);
				} catch (InterruptedException e) {
				}
		}
		Log.i("FT-LAUNCHER", message);
	}

	/**
	 * Updates the progress label
	 */
	public void updateProgress() {
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

	public static class ResponseData implements Closeable {
		public int statusCode;
		public String statusLine;
		public Map<String, String> headers;
		public InputStream bodyStream;
		public Object responseHolder;

		@Override
		public void close() throws IOException {
			bodyStream.close();
		}
	}

	private String processRelative(String apiData, String url) {
		if (!url.startsWith("http://") && !url.startsWith("https://")) {
			while (url.startsWith("/"))
				url = url.substring(1);
			url = apiData + url;
		}
		return url;
	}

	private String processUrl(String apiData, String url) {
		// Check
		if (!url.endsWith("/"))
			url += "/";
		if (!url.startsWith("http://") && !url.startsWith("https://")) {
			while (url.startsWith("/"))
				url = url.substring(1);
			url = apiData + url;
		}
		return url;
	}

	/**
	 * Sends HTTP requests
	 * 
	 * @param url     URL to send the request to
	 * @param method  Request method
	 * @param headers Request headers
	 * @param body    Request body
	 * @return Response stream (NEEDS TO BE CLOSED)
	 * @throws MalformedURLException If the URL is not of a valid format
	 * @throws IOException           If a transfer error occurs
	 */
	public static InputStream request(String url, String method, Map<String, String> headers, byte[] body)
			throws MalformedURLException, IOException {
		ResponseData res = requestRaw(url, method, headers, body);
		if (res.statusCode != 200) {
			res.close();
			throw new IOException("Server returned HTTP " + res.statusLine.substring("HTTP/1.1 ".length()));
		}
		return res.bodyStream;
	}

	/**
	 * Sends raw HTTP requests
	 * 
	 * @param url     URL to send the request to
	 * @param method  Request method
	 * @param headers Request headers
	 * @param body    Request body
	 * @return ResponseData instance (NEEDS TO BE CLOSED WHEN DONE)
	 * @throws MalformedURLException If the URL is not of a valid format
	 * @throws IOException           If a transfer error occurs
	 */
	public static ResponseData requestRaw(String url, String method, Map<String, String> headers, byte[] body)
			throws MalformedURLException, IOException {
		// Check URL
		InputStream data;
		if (url.startsWith("http:") || url.startsWith("https:")) {
			// Plain HTTP
			URL u = new URL(url);
			Socket conn;
			if (url.startsWith("http:")) {
				// Plain
				conn = new Socket(u.getHost(), u.getPort() == -1 ? 80 : u.getPort());
			} else {
				// TLS
				SSLSocketFactory factory = (SSLSocketFactory) SSLSocketFactory.getDefault();
				conn = factory.createSocket(u.getHost(), u.getPort() == -1 ? 443 : u.getPort());
			}
			PrependedBufferStream st = new PrependedBufferStream(conn.getInputStream());

			// Write request
			conn.getOutputStream().write((method + " " + u.getFile() + " HTTP/1.1\r\n").getBytes("UTF-8"));
			conn.getOutputStream().write(("User-Agent: ftlauncher\r\n").getBytes("UTF-8"));
			conn.getOutputStream().write(("Host: " + u.getHost() + "\r\n").getBytes("UTF-8"));
			if (body != null)
				conn.getOutputStream().write(("Content-Length: " + body.length + "\r\n").getBytes("UTF-8"));
			for (String key : headers.keySet())
				conn.getOutputStream().write((key + ": " + headers.get(key) + "\r\n").getBytes("UTF-8"));
			conn.getOutputStream().write(("\r\n").getBytes("UTF-8"));
			if (body != null)
				conn.getOutputStream().write(body);

			// Check response
			Map<String, String> responseHeadersOutput = new HashMap<String, String>();
			String line = readStreamLine(st);
			String statusLine = line;
			if (!line.startsWith("HTTP/1.1 ")) {
				conn.close();
				throw new IOException("Server returned invalid protocol");
			}
			while (true) {
				line = readStreamLine(st);
				if (line.equals(""))
					break;
				String key = line.substring(0, line.indexOf(": "));
				String value = line.substring(line.indexOf(": ") + 2);
				responseHeadersOutput.put(key.toLowerCase(), value);
			}

			// Verify response
			int status = Integer.parseInt(statusLine.split(" ")[1]);

			// Set data
			data = st;

			// Return
			ResponseData resp = new ResponseData();
			resp.statusLine = statusLine;
			resp.statusCode = status;
			resp.bodyStream = new SocketCloserStream(data, conn);
			resp.headers = responseHeadersOutput;
			if (resp.headers.containsKey("transfer-encoding")
					&& resp.headers.get("transfer-encoding").equalsIgnoreCase("chunked"))
				resp.bodyStream = new ChunkedStream(resp.bodyStream);
			else if (resp.headers.containsKey("content-length")
					&& Long.parseLong(resp.headers.get("content-length")) > 0)
				resp.bodyStream = new LengthLimitedStream(resp.bodyStream, true,
						Long.parseLong(resp.headers.get("content-length")));
			return resp;
		} else {
			// Default mode
			HttpURLConnection conn = (HttpURLConnection) new URL(url).openConnection();
			conn.setRequestMethod(method);
			for (String key : headers.keySet())
				conn.addRequestProperty(key, headers.get(key));
			conn.setDoOutput(body != null);
			if (body != null)
				conn.getOutputStream().write(body);

			// Return
			ResponseData resp = new ResponseData();
			resp.statusLine = conn.getResponseCode() + " " + conn.getResponseMessage();
			resp.statusCode = conn.getResponseCode();
			resp.bodyStream = resp.statusCode >= 400 ? conn.getErrorStream() : conn.getInputStream();
			resp.headers = new HashMap<String, String>();
			resp.responseHolder = conn;
			Map<String, List<String>> headerResp = conn.getHeaderFields();
			for (String key : headerResp.keySet())
				if (headerResp.get(key).size() != 0)
					resp.headers.put(key.toLowerCase(), headerResp.get(key).get(0));
			return resp;
		}
	}

	/**
	 * Downloads files with progress
	 * 
	 * @param url  URL of the file to download
	 * @param outp Output file object
	 * @throws MalformedURLException If the URL is not of a valid format
	 * @throws IOException           If a download error occurs
	 */
	public void downloadFile(String url, File outp) throws MalformedURLException, IOException {
		// Check URL
		InputStream data;
		Log.i("FT-LAUNCHER", "Downloading: " + url + " -> " + outp.getName());

		// Request
		ResponseData req = requestRaw(url, "GET", new HashMap<String, String>(), null);

		// Handle response
		data = req.bodyStream;
		progressValue = 0;
		progressMax = (int) (Long.parseLong(req.headers.get("content-length")) / 1000);
		progressEnabled = true;
		updateProgress();

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

	/**
	 * Unzips files with progress
	 * 
	 * @param input  Input zip file
	 * @param output Output folder
	 * @throws IOException If unzipping fails
	 */
	public void unZip(File input, File output) throws IOException {
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
				Log.i("FT-LAUNCHER", "Unzipping: " + ent.getName());
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

	private static String readStreamLine(PrependedBufferStream strm) throws IOException {
		// Read a number of bytes
		byte[] content = new byte[20480];
		int read = strm.read(content, 0, content.length);
		if (read <= -1) {
			// Failed
			return null;
		} else {
			// Trim array
			content = Arrays.copyOfRange(content, 0, read);

			// Find newline
			String newData = new String(content, "UTF-8");
			if (newData.contains("\n")) {
				// Found newline
				String line = newData.substring(0, newData.indexOf("\n"));
				int offset = line.length() + 1;
				int returnLength = content.length - offset;
				if (returnLength > 0) {
					// Return
					strm.returnToBuffer(Arrays.copyOfRange(content, offset, content.length));
				}
				return line.replace("\r", "");
			} else {
				// Read more
				while (true) {
					byte[] addition = new byte[20480];
					read = strm.read(addition, 0, addition.length);
					if (read <= -1) {
						// Failed
						strm.returnToBuffer(content);
						return null;
					}

					// Trim
					addition = Arrays.copyOfRange(addition, 0, read);

					// Append
					byte[] newContent = new byte[content.length + addition.length];
					for (int i = 0; i < content.length; i++)
						newContent[i] = content[i];
					for (int i = content.length; i < newContent.length; i++)
						newContent[i] = addition[i - content.length];
					content = newContent;

					// Find newline
					newData = new String(content, "UTF-8");
					if (newData.contains("\n")) {
						// Found newline
						String line = newData.substring(0, newData.indexOf("\n"));
						int offset = line.length() + 1;
						int returnLength = content.length - offset;
						if (returnLength > 0) {
							// Return
							strm.returnToBuffer(Arrays.copyOfRange(content, offset, content.length));
						}
						return line.replace("\r", "");
					}
				}
			}
		}
	}

	/**
	 * Retrieves the launcher instance
	 * 
	 * @return FeralTweaksLauncher instance
	 */
	public static FeralTweaksLauncher getInstance() {
		return instance;
	}

	/**
	 * Retrieves the launcher storage directory
	 * 
	 * @return Launcher storage directory object
	 */
	public File getLauncherDirectory() {
		return launcherDir;
	}

	/**
	 * Retrieves the launcher native library storage directory
	 * 
	 * @return Native library directory object
	 */
	public File getNativeLibraryDirectory() {
		return nativeLibraryDir;
	}

	/**
	 * Retrieves the FeralTweaks persistent data directory
	 * 
	 * @return Persistent data directory object
	 */
	public File getFtPersistentDataDirectory() {
		return modPersistentDataDir;
	}

	/**
	 * Retrieves the persistent data directory of the game
	 * 
	 * @return Persistent data directory object
	 */
	public File getUnityPersistentDataDirectory() {
		return persistentDataDir;
	}

	/**
	 * Retrieves the modloader
	 * 
	 * @return Modloader directory object
	 */
	public File getModloaderDirectory() {
		return loaderDir;
	}

	/**
	 * Checks if the login system is enabled
	 * 
	 * @return True if enabled, false otherwise
	 */
	public boolean isLoginEnabled() {
		return !disableLogin;
	}

	/**
	 * Retrieves the account ID currently logged into
	 * 
	 * @return Account ID string
	 */
	public String getAccountID() {
		return accountID;
	}

	/**
	 * Checks if modding is supported
	 * 
	 * @return True if supported, false otherwise
	 */
	public boolean isModdingSupported() {
		return !disableModloader;
	}

}
