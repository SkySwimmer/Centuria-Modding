package org.asf.centuria.launcher;

import java.io.File;
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
import java.net.URLConnection;
import java.util.Base64;
import java.util.Enumeration;
import java.util.HashMap;
import java.util.Map;
import java.util.Map.Entry;
import java.util.Random;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;

import org.asf.centuria.launcher.io.IoUtil;
import org.asf.windowsill.WMNI;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.DialogInterface;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Color;
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

	private boolean tappedStatus;
	private File launcherDir;

	private TextView label;
	private Activity activity;

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

	@Override
	public void startLauncher(Activity activity, File launcherDir, Runnable startGameCallback, String dataUrl,
			String srvName) {
		// Assign
		this.activity = activity;
		this.launcherDir = launcherDir;

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

					// Download data
					InputStream strm = new URL(url).openStream();
					String data = new String(IoUtil.readAllBytes(strm), "UTF-8");
					strm.close();
					JsonObject info = new JsonParser().parse(data).getAsJsonObject();
					JsonObject launcher = info.get("launcher").getAsJsonObject();
					String banner = launcher.get("banner").getAsString();
					url = launcher.get("url").getAsString();
					serverInfo = info.get("server").getAsJsonObject();
					hosts = serverInfo.get("hosts").getAsJsonObject();
					ports = serverInfo.get("ports").getAsJsonObject();
					JsonObject loader = launcher.get("modloader").getAsJsonObject();

					// Handle relative paths for banner
					if (!banner.startsWith("http://") && !banner.startsWith("https://")) {
						String api = hosts.get("api").getAsString();
						if (!api.endsWith("/"))
							api += "/";
						while (banner.startsWith("/"))
							banner = banner.substring(1);
						banner = api + banner;
					}

					// Assign platform
					os = "android-";
					if (android.os.Process.is64Bit())
						os += "arm64";
					else
						os += "armeabi";
					feralPlat = os;
					if (!loader.has(os)) {
						error("Unsupported platform!\n\nThe launcher cannot load on your device due to there being no modloader for your platform in the server configuration. Please wait until your device is supported.\n\nOS Name: "
								+ os, "Launcher Error");
						return;
					}
					modloader = loader.get(feralPlat).getAsJsonObject();

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
					log("Verifying login... (tap this label to switch accounts)");
					String lastAccountName = "";
					String lastToken = "";
					String authToken = "";
					String accountID = "";
					tappedStatus = false;
					boolean isManuallySelected = false;
					if (new File(launcherDir, "login.json").exists()) {
						try {
							JsonObject login = new JsonParser().parse(readString(new File(launcherDir, "login.json")))
									.getAsJsonObject();
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
							String api = hosts.get("api").getAsString();
							if (!api.endsWith("/"))
								api += "/";
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
								pass.setInputType(InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PASSWORD);
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
														String data = new String(IoUtil.readAllBytes(res.bodyStream),
																"UTF-8");
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
															postAuth(accountID, authToken);
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
													} else {
														// Check error
														String data = new String(IoUtil.readAllBytes(res.bodyStream),
																"UTF-8");
														res.bodyStream.close();
														JsonObject resp = new JsonParser().parse(data)
																.getAsJsonObject();
														switch (resp.get("error").getAsString()) {
														case "invalid_credential": {
															activity.runOnUiThread(new Runnable() {
																@Override
																public void run() {
																	nonFatalError("Credentials are invalid.",
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
																	nonFatalError(
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
															nonFatalError(
																	"An unknown error occured, please check your internet connection. If the error persists, please open a support ticket",
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
														postAuth(accountIDF, authTokenF);
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
																+ (e.getMessage() != null ? ": " + e.getMessage() : "")
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
					postAuth(accountID, authToken);
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
				System.load(nativesDir + "/libwindowsill.so");
			}

			private void postAuth(String accountID, String authToken) throws Throwable {
				// Success
				log("Login success!");

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

				// Modloader update
				log("Checking modloader files...");

				// Read version
				String currentLoader = "";
				if (new File(launcherDir, "loaderversion.info").exists()) {
					currentLoader = readString(new File(launcherDir, "loaderversion.info"));
				}
				if (!modloader.get("version").getAsString().equals(currentLoader)) {
					// Update modloader
					log("Updating " + modloader.get("name").getAsString() + "...");
					downloadFile(modloader.get("url").getAsString(), new File(launcherDir, "modloader.zip"));
					progressValue = 0;
					progressMax = 100;
					updateProgress();

					// Extract
					log("Extracting " + modloader.get("name").getAsString() + "...");
					unZip(new File(launcherDir, "modloader.zip"), new File(activity.getApplicationInfo().dataDir));

					// Save version
					writeString(new File(launcherDir, "loaderversion.info"), modloader.get("version").getAsString());
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
					currentModVersion = readString(new File(launcherDir, "modversion.info"));
				}
				if (new File(launcherDir, "assetversion.info").exists()) {
					currentAssetVersion = readString(new File(launcherDir, "assetversion.info"));
				}
				if (!serverInfo.get("modVersion").getAsString().equals(currentModVersion)) {
					// Update mods
					log("Updating client mods...");

					// Download manifest
					updateMods("assemblies/index.json", modloader.get("assemblyBaseDir").getAsString(), hosts,
							authToken);

					// Save version
					writeString(new File(launcherDir, "modversion.info"), serverInfo.get("modVersion").getAsString());
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

				// Ready
				log("Ready to start the game!");
				Thread.sleep(1000);

				// Prepare to start
				log("Preparing to start the game...");

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
				ServerSocket serverSock = s;

				// Start the game
				try {
					// Prepare to start
					log("Preparing game client...");

					// Load windowsill
					log("Loading Windowsill configuration...");
					File clientDir = new File(activity.getApplicationInfo().dataDir);
					JsonObject windowsillConfig = new JsonParser()
							.parse(readString(new File(clientDir, "windowsil.config.json"))).getAsJsonObject();
					File monoAssembly = new File(clientDir, windowsillConfig.get("monoAssembly").getAsString());
					File monoDir = new File(clientDir, windowsillConfig.get("monoDir").getAsString());
					File monoLibsDir = new File(clientDir, windowsillConfig.get("monoLibsDir").getAsString());
					File mainAssembly = new File(clientDir, windowsillConfig.get("mainAssembly").getAsString());
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
					long domain = WMNI.initRuntime(monoLib, "WINDOWSIL", clientDir.getPath(), monoLibsDir.getPath(),
							monoEtcDir.getPath());
					log("Application domain: " + domain);
					Thread.sleep(10000);

					// TODO: windowsill setup etc

					// Start client
					log("Starting client...");

					// Accept client
					final String authTokenF = authToken;
					Thread clT = new Thread(new Runnable() {
						@Override
						public void run() {
							Socket cl;
							try {
								cl = serverSock.accept();
							} catch (IOException e) {
								return;
							}
							try {
								log("Communicating with client...");
								launcherHandoff(cl, authTokenF, hosts.get("api").getAsString(), serverInfo, hosts,
										ports, completedTutorial);
								cl.close();
								log("Finished startup!");
								Thread.sleep(1000);
								try {
									serverSock.close();
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

		}, "Launcher thread");
		th.setDaemon(true);
		th.start();
	}

	private void launcherHandoff(Socket cl, String authToken, String api, JsonObject serverInfo, JsonObject hosts,
			JsonObject ports, boolean completedTutorial) throws Exception {
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

		// Send server environment
		Log.i("FT-LAUNCHER", "Sending configuration...");
		sendCommand(cl, "serverenvironment", hosts.get("director").getAsString(), hosts.get("api").getAsString(),
				hosts.get("chat").getAsString(), ports.get("chat").getAsInt(), ports.get("game").getAsInt(),
				hosts.get("voiceChat").getAsString(), ports.get("voiceChat").getAsInt(),
				ports.get("bluebox").getAsInt(), serverInfo.get("encryptedGame").getAsBoolean(),
				serverInfo.get("encryptedChat").getAsBoolean());

		// Send autologin
		if (completedTutorial) {
			Log.i("FT-LAUNCHER", "Sending autologin...");
			sendCommand(cl, "autologin", authToken);
		}

		// Send end
		cl.getOutputStream().write("end\n".getBytes("UTF-8"));
	}

	private String downloadProtectedString(String url, String authToken) throws IOException {
		HashMap<String, String> headers = new HashMap<String, String>();
		headers.put("Authorization", "Bearer " + authToken);
		InputStream res = request(url, "GET", headers, null);
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
			File outputFile = new File(new File(activity.getApplicationInfo().dataDir), output);
			outputFile.getParentFile().mkdirs();
			FileOutputStream outp = new FileOutputStream(outputFile);
			IoUtil.transfer(strm, outp);
			outp.close();
			strm.close();
			Log.i("FT-LAUNCHER", "Downloading " + path + " into " + output + "...");

			// Increase progress
			progressValue++;
			updateProgress();
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

	private void error(String message, String title) {
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

	private void nonFatalError(String message, String title, Runnable returnCallback) {
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

	private synchronized void log(String message) {
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

	private static class ResponseData {
		public int statusCode;
		public String statusLine;
		public InputStream bodyStream;
	}

	public static InputStream request(String url, String method, Map<String, String> headers, byte[] body)
			throws MalformedURLException, IOException {
		ResponseData res = requestRaw(url, method, headers, body);
		if (res.statusCode != 200) {
			res.bodyStream.close();
			throw new IOException("Server returned HTTP " + res.statusLine.substring("HTTP/1.1 ".length()));
		}
		return res.bodyStream;
	}

	public static ResponseData requestRaw(String url, String method, Map<String, String> headers, byte[] body)
			throws MalformedURLException, IOException {
		// Check URL
		InputStream data;
		if (url.startsWith("http:")) {
			// Plain HTTP
			URL u = new URL(url);
			Socket conn = new Socket(u.getHost(), u.getPort());

			// Write request
			conn.getOutputStream().write((method + " " + u.getFile() + " HTTP/1.1\r\n").getBytes("UTF-8"));
			conn.getOutputStream().write(("User-Agent: ftupdater\r\n").getBytes("UTF-8"));
			conn.getOutputStream().write(("Host: " + u.getHost() + "\r\n").getBytes("UTF-8"));
			if (body != null)
				conn.getOutputStream().write(("Content-Length: " + body.length + "\r\n").getBytes("UTF-8"));
			for (String key : headers.keySet())
				conn.getOutputStream().write((key + ": " + headers.get(key) + "\r\n").getBytes("UTF-8"));
			conn.getOutputStream().write(("\r\n").getBytes("UTF-8"));
			conn.getOutputStream().write(body);

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

			// Set data
			data = conn.getInputStream();

			// Return
			ResponseData resp = new ResponseData();
			resp.statusLine = statusLine;
			resp.statusCode = status;
			resp.bodyStream = data;
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
			return resp;
		}
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
