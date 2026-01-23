package org.asf.centuria.launcher.feraltweaks;

import java.awt.EventQueue;

import javax.imageio.ImageIO;
import javax.swing.JFrame;
import javax.swing.JPanel;
import java.awt.BorderLayout;
import javax.swing.JProgressBar;
import java.awt.Dimension;
import javax.swing.JLabel;
import javax.swing.JOptionPane;
import javax.swing.SwingUtilities;
import javax.swing.UIManager;
import javax.swing.UnsupportedLookAndFeelException;

import org.apache.commons.compress.archivers.ArchiveEntry;
import org.apache.commons.compress.archivers.sevenz.SevenZArchiveEntry;
import org.apache.commons.compress.archivers.sevenz.SevenZFile;
import org.apache.commons.compress.archivers.tar.TarArchiveInputStream;
import org.apache.hc.client5.http.DnsResolver;
import org.apache.hc.client5.http.classic.methods.HttpGet;
import org.apache.hc.client5.http.classic.methods.HttpPost;
import org.apache.hc.client5.http.impl.classic.CloseableHttpClient;
import org.apache.hc.client5.http.impl.classic.HttpClientBuilder;
import org.apache.hc.client5.http.impl.io.BasicHttpClientConnectionManager;
import org.apache.hc.client5.http.socket.ConnectionSocketFactory;
import org.apache.hc.client5.http.socket.PlainConnectionSocketFactory;
import org.apache.hc.client5.http.ssl.SSLConnectionSocketFactory;
import org.apache.hc.core5.http.HttpEntity;
import org.apache.hc.core5.http.config.RegistryBuilder;
import org.apache.hc.core5.http.io.entity.StringEntity;
import org.apache.hc.core5.http.message.BasicHeader;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

import java.awt.image.BufferedImage;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.UnsupportedEncodingException;
import java.lang.reflect.InvocationTargetException;
import java.net.InetAddress;
import java.net.MalformedURLException;
import java.net.ServerSocket;
import java.net.Socket;
import java.net.URL;
import java.net.URLEncoder;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Base64;
import java.util.Enumeration;
import java.util.HashMap;
import java.util.List;
import java.util.Random;
import java.util.zip.GZIPInputStream;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;
import java.awt.Color;
import javax.swing.SwingConstants;
import java.awt.event.MouseAdapter;
import java.awt.event.MouseEvent;
import java.awt.Font;
import java.awt.event.KeyAdapter;
import java.awt.event.KeyEvent;
import javax.swing.border.BevelBorder;

public class LauncherMain {

	private JFrame frmCenturiaLauncher;
	private JLabel lblNewLabel;
	private static String[] args;
	private boolean shiftDown;
	private boolean connected;

	public static final String PREFERRED_DXVK_VERSION_MACOS = "v1.10.3";

	private CloseableHttpClient clientBase;

	/**
	 * Launch the application.
	 */
	public static void main(String[] args) {
		LauncherMain.args = args;
		EventQueue.invokeLater(new Runnable() {
			public void run() {
				try {
					LauncherMain window = new LauncherMain();
					window.frmCenturiaLauncher.setVisible(true);
				} catch (Exception e) {
					e.printStackTrace();
				}
			}
		});
	}

	/**
	 * Create the application.
	 */
	public LauncherMain() {
		initialize();
	}

	/**
	 * Initialize the contents of the frame.
	 */
	private void initialize() {
		// Create client
		clientBase = HttpClientBuilder.create().setDefaultHeaders(List.of(new BasicHeader("Keep-Alive", "timeout=30")))
				.build();

		try {
			try {
				UIManager.setLookAndFeel("com.sun.java.swing.plaf.gtk.GTKLookAndFeel");
			} catch (ClassNotFoundException | InstantiationException | IllegalAccessException
					| UnsupportedLookAndFeelException e1) {
				UIManager.setLookAndFeel(UIManager.getSystemLookAndFeelClassName());
			}
		} catch (ClassNotFoundException | InstantiationException | IllegalAccessException
				| UnsupportedLookAndFeelException e1) {
		}

		frmCenturiaLauncher = new JFrame();
		frmCenturiaLauncher.addKeyListener(new KeyAdapter() {
			@Override
			public void keyPressed(KeyEvent e) {
				if (e.getKeyCode() == KeyEvent.VK_SHIFT)
					shiftDown = true;
			}

			@Override
			public void keyReleased(KeyEvent e) {
				if (e.getKeyCode() == KeyEvent.VK_SHIFT)
					shiftDown = false;
			}
		});
		frmCenturiaLauncher.setUndecorated(true);
		frmCenturiaLauncher.setResizable(false);
		frmCenturiaLauncher.setBounds(100, 100, 1042, 408);
		frmCenturiaLauncher.setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
		frmCenturiaLauncher.setLocationRelativeTo(null);
		try {
			InputStream strmi = getClass().getClassLoader().getResourceAsStream("emulogo_purple.png");
			frmCenturiaLauncher.setIconImage(ImageIO.read(strmi));
			strmi.close();
		} catch (IOException e1) {
		}

		BackgroundPanel panel_1 = new BackgroundPanel();
		panel_1.setForeground(Color.WHITE);
		frmCenturiaLauncher.getContentPane().add(panel_1, BorderLayout.CENTER);
		panel_1.setLayout(new BorderLayout(0, 0));

		JPanel panel_2 = new JPanel();
		panel_2.setPreferredSize(new Dimension(10, 30));
		panel_2.setBackground(new Color(10, 10, 10, 100));
		panel_1.add(panel_2, BorderLayout.NORTH);
		panel_2.setLayout(new BorderLayout(0, 0));

		JPanel panel = new JPanel();
		panel.setBackground(new Color(255, 255, 255, 0));
		panel_1.add(panel, BorderLayout.CENTER);
		panel.setLayout(new BorderLayout(0, 0));

		JPanel panel_4 = new JPanel();
		panel_4.setPreferredSize(new Dimension(10, 30));
		panel.add(panel_4, BorderLayout.SOUTH);
		panel_4.setBackground(new Color(10, 10, 10, 100));
		panel_4.setLayout(new BorderLayout(0, 0));

		lblNewLabel = new JLabel("New label");
		panel_4.add(lblNewLabel, BorderLayout.CENTER);
		lblNewLabel.setFont(new Font("Tahoma", Font.PLAIN, 14));
		lblNewLabel.setForeground(Color.WHITE);
		lblNewLabel.setPreferredSize(new Dimension(46, 20));

		JPanel panel_5 = new JPanel();
		panel_5.setBorder(new BevelBorder(BevelBorder.LOWERED, null, null, null, null));
		panel_5.setPreferredSize(new Dimension(510, 10));
		panel_5.setBackground(Color.DARK_GRAY);
		panel_4.add(panel_5, BorderLayout.EAST);

		JProgressBar progressBar = new JProgressBar();
		panel_5.add(progressBar);
		progressBar.setPreferredSize(new Dimension(500, 18));
		progressBar.setBackground(new Color(240, 240, 240, 100));
		panel_5.setVisible(false);

		JPanel panel_3 = new JPanel();
		panel_3.setBackground(new Color(90, 90, 90, 190));
		panel_2.add(panel_3, BorderLayout.EAST);
		panel_3.setLayout(new BorderLayout(0, 0));

		JLabel lblNewLabel_2 = new JLabel("X");
		panel_3.add(lblNewLabel_2);
		lblNewLabel_2.setFont(new Font("Tahoma", Font.PLAIN, 15));
		lblNewLabel_2.setForeground(Color.RED);
		lblNewLabel_2.addMouseListener(new MouseAdapter() {
			@Override
			public void mouseClicked(MouseEvent e) {
				System.exit(0);
			}

			@Override
			public void mouseEntered(MouseEvent e) {
				lblNewLabel_2.setForeground(new Color(200, 0, 0));
				panel_3.invalidate();
			}

			@Override
			public void mouseExited(MouseEvent e) {
				lblNewLabel_2.setForeground(Color.RED);
				panel_3.invalidate();
			}
		});
		lblNewLabel_2.setHorizontalAlignment(SwingConstants.CENTER);
		lblNewLabel_2.setPreferredSize(new Dimension(28, 14));

		JLabel lblNewLabel_1 = new JLabel("New label");
		lblNewLabel_1.setForeground(Color.WHITE);
		lblNewLabel_1.setFont(new Font("Tahoma", Font.PLAIN, 16));
		lblNewLabel_1.setHorizontalAlignment(SwingConstants.CENTER);
		panel_2.add(lblNewLabel_1, BorderLayout.CENTER);

		// Contact server
		String os;
		String feralPlat;
		String srvName;
		JsonObject serverInfo;
		JsonObject hosts;
		JsonObject ports;
		JsonObject client;
		JsonObject modloader;
		String manifest;
		boolean useWineMethodOSX;
		try {
			// Read server info
			String url;
			try {
				srvName = args[0];
				url = args[1];
			} catch (Exception e) {
				JOptionPane.showMessageDialog(null,
						"Missing required arguments, please use the updater to start the launcher.", "Launcher Error",
						JOptionPane.ERROR_MESSAGE);
				System.exit(1);
				return;
			}
			ArrayList<String> arguments = new ArrayList<String>(Arrays.asList(args));
			arguments.remove(0);
			arguments.remove(0);
			args = arguments.toArray(t -> new String[t]);
			frmCenturiaLauncher.setTitle(srvName + " Launcher");
			lblNewLabel_1.setText(srvName + " Launcher");

			// Download data
			HttpGet get = new HttpGet(url);
			String data = clientBase.execute(get, t -> {
				if (t.getCode() != 200) {
					return null;
				}
				InputStream strm = t.getEntity().getContent();
				String d = new String(strm.readAllBytes(), "UTF-8");
				strm.close();
				return d;
			});
			JsonObject info = JsonParser.parseString(data).getAsJsonObject();
			JsonObject launcher = info.get("launcher").getAsJsonObject();
			String banner = launcher.get("banner").getAsString();
			url = launcher.get("url").getAsString();
			serverInfo = info.get("server").getAsJsonObject();
			hosts = serverInfo.get("hosts").getAsJsonObject();
			ports = serverInfo.get("ports").getAsJsonObject();
			client = launcher.get("client").getAsJsonObject();
			JsonObject loader = launcher.get("modloader").getAsJsonObject();
			useWineMethodOSX = launcher.has("osxUseWineMethod") && launcher.get("osxUseWineMethod").getAsBoolean();

			// Handle relative paths for banner
			String api = hosts.get("api").getAsString();
			if (!api.endsWith("/"))
				api += "/";
			String apiData = api + "data/";
			if (hosts.has("launcherDataSource")) {
				apiData = hosts.get("launcherDataSource").getAsString();
				if (!apiData.endsWith("/"))
					apiData += "/";
			}
			banner = processRelative(apiData, banner);

			// Determine platform
			if (System.getProperty("os.name").toLowerCase().contains("win")
					&& !System.getProperty("os.name").toLowerCase().contains("darwin")) { // Windows
				os = "win64";
				feralPlat = "win64";
				if (!loader.has(os)) {
					JOptionPane.showMessageDialog(null,
							"Unsupported platform!\nThe launcher cannot load on your device due to there being no modloader for your platform in the server configuration. Please wait until your device is supported.\n\nOS Name: "
									+ System.getProperty("os.name"),
							"Launcher Error", JOptionPane.ERROR_MESSAGE);
					System.exit(1);
					return;
				}
				manifest = client.get(os).getAsString();
			} else if (System.getProperty("os.name").toLowerCase().contains("darwin")
					|| System.getProperty("os.name").toLowerCase().contains("mac")) { // MacOS
				os = "osx";
				if (useWineMethodOSX) {
					feralPlat = "win64";
					if (!loader.has("win64")) {
						JOptionPane.showMessageDialog(null,
								"Unsupported platform!\nThe launcher cannot load on your device due to there being no modloader for your platform in the server configuration. Please wait until your device is supported.\n\nOS Name: "
										+ System.getProperty("os.name"),
								"Launcher Error", JOptionPane.ERROR_MESSAGE);
						System.exit(1);
						return;
					}
					manifest = client.get("win64").getAsString();
				} else {
					feralPlat = "osx";
					if (!loader.has(os)) {
						JOptionPane.showMessageDialog(null,
								"Unsupported platform!\nThe launcher cannot load on your device due to there being no modloader for your platform in the server configuration. Please wait until your device is supported.\n\nOS Name: "
										+ System.getProperty("os.name"),
								"Launcher Error", JOptionPane.ERROR_MESSAGE);
						System.exit(1);
						return;
					}
					manifest = client.get(os).getAsString();
				}
			} else if (System.getProperty("os.name").toLowerCase().contains("linux")) { // Linux
				os = "linux";
				feralPlat = "win64";
				if (!loader.has("win64")) {
					JOptionPane.showMessageDialog(null,
							"Unsupported platform!\nThe launcher cannot load on your device due to there being no modloader for your platform in the server configuration. Please wait until your device is supported.\n\nOS Name: "
									+ System.getProperty("os.name"),
							"Launcher Error", JOptionPane.ERROR_MESSAGE);
					System.exit(1);
					return;
				}
				manifest = client.get("win64").getAsString();
			} else {
				JOptionPane.showMessageDialog(null,
						"Unsupported platform!\n\nThe launcher cannot load on your device, please contact support for more info.\n\nOS Name: "
								+ System.getProperty("os.name"),
						"Launcher Error", JOptionPane.ERROR_MESSAGE);
				System.exit(1);
				return;
			}
			modloader = loader.get(feralPlat).getAsJsonObject();

			// Download splash and set image
			BufferedImage img = ImageIO.read(new URL(banner));
			panel_1.setImage(img);
		} catch (Exception e) {
			JOptionPane.showMessageDialog(null,
					"Could not connect with the launcher servers, please check your internet connection. If you are connected, please wait a few minutes and try again.\n\nIf the issue remains and you are connected to the internet, please contact support.",
					"Launcher Error", JOptionPane.ERROR_MESSAGE);
			System.exit(1);
			return;
		}

		Thread th = new Thread(() -> {
			// Set progress bar status
			try {
				// Set label
				SwingUtilities.invokeAndWait(() -> {
					log("Preparing...");
					progressBar.setMaximum(100);
					progressBar.setValue(0);
					panel_1.repaint();
				});

				// Check credentials
				SwingUtilities.invokeAndWait(() -> {
					log("Verifying login... (hold shift to switch accounts)");
					progressBar.setMaximum(100);
					progressBar.setValue(0);
					panel_1.repaint();
				});
				String lastAccountName = "";
				String lastToken = "";
				String authToken = "";
				String accountID = "";
				if (new File("login.json").exists()) {
					try {
						JsonObject login = JsonParser.parseString(Files.readString(Path.of("login.json")))
								.getAsJsonObject();
						lastToken = login.get("token").getAsString();
						lastAccountName = login.get("loginName").getAsString();

						// Check if shift is down
						for (int i = 0; i < 30; i++) {
							if (shiftDown) {
								lastToken = "";
								break;
							}
							Thread.sleep(100);
						}
					} catch (Exception e) {
						// Corrupted login data file likely
					}
				}
				boolean requireRelogin = true;
				if (!lastToken.isBlank()) {
					// Contact API
					try {
						String api = hosts.get("api").getAsString();
						if (!api.endsWith("/"))
							api += "/";

						HttpPost post = new HttpPost(api + "centuria/refreshtoken");
						post.addHeader("Authorization", "Bearer " + lastToken);
						String data = clientBase.execute(post, resp -> {
							if (resp.getCode() != 200) {
								return null;
							}
							InputStream strm = resp.getEntity().getContent();
							String d = new String(strm.readAllBytes(), "UTF-8");
							strm.close();
							return d;
						});
						if (data != null) {
							JsonObject resp = JsonParser.parseString(data).getAsJsonObject();
							authToken = resp.get("auth_token").getAsString();
							lastToken = resp.get("refresh_token").getAsString();
							accountID = resp.get("uuid").getAsString();

							// Save
							requireRelogin = false;
							JsonObject login = new JsonObject();
							login.addProperty("token", lastToken);
							login.addProperty("loginName", lastAccountName);
							Files.writeString(Path.of("login.json"), login.toString());
						}
					} catch (Exception e) {
					}
				}

				// Check
				if (requireRelogin) {
					// Show login
					final String accNmF = lastAccountName;
					SwingUtilities.invokeAndWait(() -> {
						log("Opened login window");
						progressBar.setMaximum(100);
						progressBar.setValue(0);
						panel_1.repaint();
					});
					LoginWindow window = new LoginWindow(frmCenturiaLauncher, hosts.get("api").getAsString(), accNmF);
					AccountHolder acc = window.getAccount();
					if (acc == null) {
						System.exit(0);
						return;
					}

					// Save
					authToken = acc.authToken;
					lastToken = acc.refreshToken;
					lastAccountName = acc.accountName;
					accountID = acc.accountID;
					JsonObject login = new JsonObject();
					login.addProperty("token", lastToken);
					login.addProperty("loginName", lastAccountName);
					Files.writeString(Path.of("login.json"), login.toString());
				}
				SwingUtilities.invokeAndWait(() -> {
					log("Login success!");
					progressBar.setMaximum(100);
					progressBar.setValue(0);
					panel_1.repaint();
				});

				// Pull details
				String api = hosts.get("api").getAsString();
				if (!api.endsWith("/"))
					api += "/";
				String apiData = api + "data/";
				if (hosts.has("launcherDataSource")) {
					apiData = hosts.get("launcherDataSource").getAsString();
					if (!apiData.endsWith("/"))
						apiData += "/";
				}
				HttpPost post = new HttpPost(api + "centuria/getuser");
				post.addHeader("Authorization", "Bearer " + lastToken);
				JsonObject payload = new JsonObject();
				payload.addProperty("id", accountID);
				post.setHeader("Content-type", "application/json");
				post.setEntity(new StringEntity(payload.toString()));
				String dataS = clientBase.execute(post, resp -> {
					InputStream strm = resp.getEntity().getContent();
					String d = new String(strm.readAllBytes(), "UTF-8");
					strm.close();
					return d;
				});
				JsonObject respJ = JsonParser.parseString(dataS).getAsJsonObject();
				boolean completedTutorial = respJ.get("tutorial_completed").getAsBoolean();

				// Check client modding
				try {
					HttpGet get = new HttpGet(apiData + "clientmods/testendpoint");
					get.addHeader("Authorization", "Bearer " + authToken);
					String data = clientBase.execute(get, resp -> {
						InputStream strm = resp.getEntity().getContent();
						String d = new String(strm.readAllBytes(), "UTF-8");
						strm.close();
						return d;
					});
					JsonObject resp = JsonParser.parseString(data).getAsJsonObject();
					if (resp.has("error")) {
						// Handle error
						String err = resp.get("error").getAsString();
						if (err.equals("feraltweaks_not_enabled")) {
							JOptionPane.showMessageDialog(frmCenturiaLauncher,
									"Client modding is not enabled on your account, unable to launch the game.",
									"Launcher Error", JOptionPane.ERROR_MESSAGE);
							System.exit(1);
							return;
						} else if (resp.has("errorMessage")) {
							JOptionPane.showMessageDialog(frmCenturiaLauncher, resp.get("errorMessage").getAsString(),
									"Launcher Error", JOptionPane.ERROR_MESSAGE);
							System.exit(1);
							return;
						}
					}
				} catch (IOException e) {
				}

				// Check client
				SwingUtilities.invokeAndWait(() -> {
					log("Checking client files...");
					progressBar.setMaximum(100);
					progressBar.setValue(0);
					panel_1.repaint();
				});

				// Verify method switch
				if (!useWineMethodOSX) {
					// Check if last time was wine method
					if (new File("macosusewine").exists()) {
						// Clean up
						if (new File("client").exists())
							deleteDir(new File("client"));
						if (new File("clientversion.info").exists())
							new File("clientversion.info").delete();
						if (new File("loaderversion.info").exists())
							new File("loaderversion.info").delete();
						if (new File("modversion.info").exists())
							new File("modversion.info").delete();
						if (new File("assetversion.info").exists())
							new File("assetversion.info").delete();
						new File("macosusewine").delete();
					}
				} else if (os.equals("osx")) {
					// Check if last time was non-wine method
					if (!new File("macosusewine").exists()) {
						// Clean up
						if (new File("client").exists())
							deleteDir(new File("client"));
						if (new File("clientversion.info").exists())
							new File("clientversion.info").delete();
						if (new File("loaderversion.info").exists())
							new File("loaderversion.info").delete();
						if (new File("modversion.info").exists())
							new File("modversion.info").delete();
						if (new File("assetversion.info").exists())
							new File("assetversion.info").delete();
						new File("macosusewine").createNewFile();
					}
				}

				// Read version
				String currentClient = "";
				if (new File("clientversion.info").exists()) {
					currentClient = Files.readString(Path.of("clientversion.info"));
				}

				// Download manifest
				CloseableHttpClient http;
				String manifestF = processRelative(apiData, manifest);
				URL oUrl = new URL(manifestF);
				String ip = oUrl.getHost();
				String hostname = oUrl.getHost();
				if (client.has("manifestDownloadIP")) {
					ip = client.get("manifestDownloadIP").getAsString();

					// Create resolver
					DnsResolver resolver = new OverrideDnsResolver(oUrl.getHost(), ip);

					// Create client
					BasicHttpClientConnectionManager connManager = new BasicHttpClientConnectionManager(
							RegistryBuilder.<ConnectionSocketFactory>create()
									.register("http", PlainConnectionSocketFactory.getSocketFactory())
									.register("https", SSLConnectionSocketFactory.getSocketFactory()).build(),
							null, null, resolver);
					http = HttpClientBuilder.create().setConnectionManager(connManager).build();
				} else {
					// Create client
					http = HttpClientBuilder.create().build();
				}

				// Create request
				HttpGet req = new HttpGet(manifestF);
				req.addHeader("Host", hostname);

				// Get response
				String propFile = http.execute(req, t -> {
					InputStream strm = t.getEntity().getContent();
					String resp = new String(strm.readAllBytes(), "UTF-8");
					strm.close();
					t.close();
					return resp;
				});
				http.close();

				// Parse ini (ish)
				HashMap<String, String> properties = new HashMap<String, String>();
				for (String line : propFile.split("\n")) {
					String key = line;
					String value = "";
					if (key.contains("=")) {
						value = key.substring(key.indexOf("=") + 1);
						key = key.substring(0, key.indexOf("="));
					}
					properties.put(key, value);
				}

				// Load version and download URL
				String cVer = properties.get("ApplicationVersion");
				String url = properties.get("ApplicationDownloadUrl");
				boolean resetAttrs = false;
				if (!currentClient.equals(cVer)) {
					// Download new client
					CloseableHttpClient httpCl;
					if (client.has("clientDownloadIP")) {
						ip = client.get("clientDownloadIP").getAsString();

						// Create resolver
						DnsResolver resolver = new OverrideDnsResolver(oUrl.getHost(), ip);

						// Create client
						BasicHttpClientConnectionManager connManager = new BasicHttpClientConnectionManager(
								RegistryBuilder.<ConnectionSocketFactory>create()
										.register("http", PlainConnectionSocketFactory.getSocketFactory())
										.register("https", SSLConnectionSocketFactory.getSocketFactory()).build(),
								null, null, resolver);
						httpCl = HttpClientBuilder.create().setConnectionManager(connManager).build();
					} else {
						// Create client
						httpCl = HttpClientBuilder.create().build();
					}

					// Create request
					url = processRelative(apiData, url);
					req = new HttpGet(url);
					URL oUrl2 = new URL(url);
					String hostname2 = oUrl2.getHost();
					req.addHeader("Host", hostname2);
					httpCl.execute(req, t -> {
						HttpEntity ent = t.getEntity();

						try {
							SwingUtilities.invokeAndWait(() -> {
								log("Updating Fer.al client...");
								progressBar.setMaximum((int) (ent.getContentLength() / 1000));
								progressBar.setValue(0);
								panel_5.setVisible(true);
								panel_1.repaint();
							});
						} catch (InvocationTargetException | InterruptedException e) {
						}

						File tmpOut = new File("client.archive");
						InputStream data = ent.getContent();
						FileOutputStream out = new FileOutputStream(tmpOut);
						while (true) {
							byte[] b = data.readNBytes(1000);
							if (b.length == 0)
								break;
							else {
								out.write(b);
								SwingUtilities.invokeLater(() -> {
									progressBar.setValue(progressBar.getValue() + 1);
									panel_1.repaint();
								});
							}
						}
						out.close();
						data.close();
						ent.close();
						http.close();

						SwingUtilities.invokeLater(() -> {
							progressBar.setValue(progressBar.getMaximum());
							panel_1.repaint();
						});
						try {
							SwingUtilities.invokeAndWait(() -> {
								log("Extracting Fer.al client...");
								progressBar.setMaximum(100);
								progressBar.setValue(0);
								panel_1.repaint();
							});
						} catch (InvocationTargetException | InterruptedException e) {
						}

						// Check OS
						if (feralPlat.equals("osx")) {
							unZip(tmpOut, new File("client"), progressBar, panel_1); // OSX
						} else {
							unzip7z(tmpOut, new File("client"), progressBar, panel_1); // Windows or linux
						}

						// Delete FTL assembly caches
						if (new File("client/build/FeralTweaks/cache/dummy").exists()) {
							try {
								SwingUtilities.invokeAndWait(() -> {
									log("Erasing dummy assembly cache for regeneration...");
									progressBar.setMaximum(100);
									progressBar.setValue(0);
									panel_5.setVisible(false);
									panel_1.repaint();
								});
							} catch (InvocationTargetException | InterruptedException e) {
							}
							deleteDir(new File("client/build/FeralTweaks/cache/dummy"));
						}
						if (new File("client/build/FeralTweaks/cache/assemblies").exists()) {
							try {
								SwingUtilities.invokeAndWait(() -> {
									log("Erasing proxy assembly cache for regeneration...");
									progressBar.setMaximum(100);
									progressBar.setValue(0);
									panel_5.setVisible(false);
									panel_1.repaint();
								});
							} catch (InvocationTargetException | InterruptedException e) {
							}
							deleteDir(new File("client/build/FeralTweaks/cache/assemblies"));
						}

						// Save version
						Files.writeString(Path.of("clientversion.info"), cVer);

						try {
							SwingUtilities.invokeAndWait(() -> {
								log("Update completed!");
								progressBar.setMaximum(100);
								progressBar.setValue(0);
								panel_5.setVisible(false);
								panel_1.repaint();
							});
						} catch (InvocationTargetException | InterruptedException e) {
						}

						return null;
					});

					// OSX stuff
					if (feralPlat.equals("osx")) {
						resetAttrs = true;
					}
				}

				// Modloader update
				SwingUtilities.invokeAndWait(() -> {
					log("Checking modloader files...");
					progressBar.setMaximum(100);
					progressBar.setValue(0);
					panel_1.repaint();
				});

				// Read version
				String currentLoader = "";
				if (new File("loaderversion.info").exists()) {
					currentLoader = Files.readString(Path.of("loaderversion.info"));
				}
				if (!modloader.get("version").getAsString().equals(currentLoader)) {
					// Update modloader
					SwingUtilities.invokeAndWait(() -> {
						log("Updating " + modloader.get("name").getAsString() + "...");
						panel_5.setVisible(true);
						panel_1.repaint();
					});
					downloadFile(processRelative(apiData, modloader.get("url").getAsString()),
							new File("modloader.zip"), progressBar, panel_1);

					// Extract
					try {
						SwingUtilities.invokeAndWait(() -> {
							log("Extracting " + modloader.get("name").getAsString() + "...");
							progressBar.setMaximum(100);
							progressBar.setValue(0);
							panel_1.repaint();
						});
					} catch (InvocationTargetException | InterruptedException e) {
					}
					unZip(new File("modloader.zip"), new File("client/build"), progressBar, panel_1);

					// Save version
					Files.writeString(Path.of("loaderversion.info"), modloader.get("version").getAsString());

					// OSX stuff
					if (feralPlat.equals("osx")) {
						resetAttrs = true;
					}

					try {
						SwingUtilities.invokeAndWait(() -> {
							log("Update completed!");
							progressBar.setMaximum(100);
							progressBar.setValue(0);
							panel_5.setVisible(false);
							panel_1.repaint();
						});
					} catch (InvocationTargetException | InterruptedException e) {
					}
				}

				// Client mod update
				SwingUtilities.invokeAndWait(() -> {
					log("Checking for mod updates...");
					progressBar.setMaximum(100);
					progressBar.setValue(0);
					panel_1.repaint();
				});

				// Read version
				String currentModVersion = "";
				String currentAssetVersion = "";
				if (new File("modversion.info").exists()) {
					currentModVersion = Files.readString(Path.of("modversion.info"));
				}
				if (new File("assetversion.info").exists()) {
					currentAssetVersion = Files.readString(Path.of("assetversion.info"));
				}
				if (!serverInfo.get("modVersion").getAsString().equals(currentModVersion)) {
					// Update mods
					SwingUtilities.invokeAndWait(() -> {
						log("Updating client mods...");
						panel_5.setVisible(true);
						panel_1.repaint();
					});

					// Download manifest
					updateMods("assemblies/index.json", apiData, modloader.get("assemblyBaseDir").getAsString(), hosts,
							authToken, progressBar, panel_1);

					// Check if platformspecific mods exist
					// Tho if on linux we use windows unless linux separately exists
					if (checkExists(apiData + "assemblies-" + os + "/index.json", authToken)) {
						// Download assets
						SwingUtilities.invokeAndWait(() -> {
							log("Updating platform-specific client mods...");
							panel_5.setVisible(true);
							panel_1.repaint();
						});
						updateMods("assemblies-" + os + "/index.json", apiData,
								modloader.get("assemblyBaseDir").getAsString(), hosts, authToken, progressBar, panel_1);

					} else if (os.equals("linux") && checkExists(apiData + "assemblies-win64/index.json", authToken)) {
						// Download assets
						SwingUtilities.invokeAndWait(() -> {
							log("Updating platform-specific client mods...");
							panel_5.setVisible(true);
							panel_1.repaint();
						});
						updateMods("assemblies-win64/index.json", apiData,
								modloader.get("assemblyBaseDir").getAsString(), hosts, authToken, progressBar, panel_1);
					}

					// Save version
					Files.writeString(Path.of("modversion.info"), serverInfo.get("modVersion").getAsString());

					try {
						SwingUtilities.invokeAndWait(() -> {
							log("Update completed!");
							progressBar.setMaximum(100);
							progressBar.setValue(0);
							panel_5.setVisible(false);
							panel_1.repaint();
						});
					} catch (InvocationTargetException | InterruptedException e) {
					}
				}
				if (!serverInfo.get("assetVersion").getAsString().equals(currentAssetVersion)) {
					// Update mods
					SwingUtilities.invokeAndWait(() -> {
						log("Updating client mod assets...");
						panel_5.setVisible(true);
						panel_1.repaint();
					});

					// Download manifest
					updateMods("assets/index.json", apiData, modloader.get("assetBaseDir").getAsString(), hosts,
							authToken, progressBar, panel_1);

					// Check if platformspecific mods exist
					// Tho if on linux we use windows unless linux separately exists
					if (checkExists(apiData + "assets-" + os + "/index.json", authToken)) {
						// Download assets
						SwingUtilities.invokeAndWait(() -> {
							log("Updating platform-specific client mod assets...");
							panel_5.setVisible(true);
							panel_1.repaint();
						});
						updateMods("assets-" + os + "/index.json", apiData, modloader.get("assetBaseDir").getAsString(),
								hosts, authToken, progressBar, panel_1);

					} else if (os.equals("linux") && checkExists(apiData + "assets-win64/index.json", authToken)) {
						// Download assets
						SwingUtilities.invokeAndWait(() -> {
							log("Updating platform-specific client mod assets...");
							panel_5.setVisible(true);
							panel_1.repaint();
						});
						updateMods("assets-win64/index.json", apiData, modloader.get("assetBaseDir").getAsString(),
								hosts, authToken, progressBar, panel_1);
					}

					// Save version
					Files.writeString(Path.of("assetversion.info"), serverInfo.get("assetVersion").getAsString());

					try {
						SwingUtilities.invokeAndWait(() -> {
							log("Update completed!");
							progressBar.setMaximum(100);
							progressBar.setValue(0);
							panel_5.setVisible(false);
							panel_1.repaint();
						});
					} catch (InvocationTargetException | InterruptedException e) {
					}
				}

				// Prepare to start
				SwingUtilities.invokeAndWait(() -> {
					log("Preparing to start the game...");
					panel_1.repaint();
				});

				// Check OS
				File clientFile;
				if (feralPlat.equals("osx")) {
					clientFile = new File("client/build/run.sh"); // MacOS
				} else {
					clientFile = new File("client/build/Fer.al.exe"); // Linux or Windows
				}
				if (!clientFile.exists()) {
					JOptionPane.showMessageDialog(null, "Failed to download the fer.al client!", "Download Failure",
							JOptionPane.ERROR_MESSAGE);
					System.exit(1);
				}

				// Start client
				ProcessBuilder builder;

				// Log
				try {
					SwingUtilities.invokeAndWait(() -> {
						log("Preparing client communication...");
						progressBar.setMaximum(100);
						progressBar.setValue(0);
						panel_1.repaint();
					});
				} catch (InvocationTargetException | InterruptedException e) {
				}

				// Find a port
				ServerSocket s;
				Random rnd = new Random();
				int port;
				while (true) {
					port = rnd.nextInt(1024, 65535);
					try {
						s = new ServerSocket(port, 0, InetAddress.getByName("127.0.0.1"));
						break;
					} catch (IOException e) {
					}
				}
				ServerSocket serverSock = s;

				// Prepare to start
				SwingUtilities.invokeAndWait(() -> {
					log("Preparing game client...");
					panel_1.repaint();
				});

				// OSX stuff
				if (resetAttrs) {
					ProcessBuilder proc = new ProcessBuilder("xattr", "-cr",
							new File("client/build").getCanonicalPath());
					proc.inheritIO();
					try {
						proc.start().waitFor();
					} catch (InterruptedException e1) {
					}
				}

				try {
					// Build startup command
					ArrayList<String> arguments = new ArrayList<String>();

					// Start command
					if (os.equals("win64")) {
						// Windows
						arguments.add(clientFile.getAbsolutePath());
					} else if (os.equals("osx") && !useWineMethodOSX) {
						// MacOS
						arguments.add("sh");
						arguments.add(clientFile.getAbsolutePath());
					} else if (os.equals("linux") || useWineMethodOSX) {
						// Linux or wine-method-macos
						arguments.add("wine");
						arguments.add(clientFile.getAbsolutePath());
					} else
						throw new Exception("Invalid platform: " + os);

					// Handoff
					arguments.add("--launcher-handoff");
					arguments.add(Integer.toString(port));

					// User arguments
					for (String arg : args)
						arguments.add(arg);

					// Builder
					builder = new ProcessBuilder(arguments.toArray(t -> new String[t]));

					// Wine
					if (os.equals("linux") || (useWineMethodOSX && os.equals("osx"))) {
						// Check prefix
						File prefix = new File("wineprefix");
						if (!new File(prefix, "completed").exists() && !new File(prefix, "completedwine").exists()) {
							if (prefix.exists())
								deleteDir(prefix);
							prefix.mkdirs();

							// Set overrides
							SwingUtilities.invokeAndWait(() -> {
								log("Configuring wine...");
								progressBar.setMaximum(100);
								progressBar.setValue(0);
								panel_1.repaint();
							});
							try {
								ProcessBuilder proc = new ProcessBuilder("wine", "wineboot", "--init", "-f");
								proc.environment().put("WINEPREFIX", prefix.getCanonicalPath());
								proc.inheritIO();
								if (proc.start().waitFor() != 0)
									prefixConfigureError(prefix, serverSock);
								proc = new ProcessBuilder("wine", "reg", "add",
										"HKEY_CURRENT_USER\\Software\\Wine\\DllOverrides", "/v", "winhttp", "/d",
										"native,builtin", "/f");
								proc.environment().put("WINEPREFIX", prefix.getCanonicalPath());
								proc.inheritIO();
								if (proc.start().waitFor() != 0)
									prefixConfigureError(prefix, serverSock);
								proc = new ProcessBuilder("wine", "reg", "add",
										"HKEY_CURRENT_USER\\Software\\Wine\\DllOverrides", "/v", "d3d11", "/d",
										"native", "/f");
								proc.environment().put("WINEPREFIX", prefix.getCanonicalPath());
								proc.inheritIO();
								if (proc.start().waitFor() != 0)
									prefixConfigureError(prefix, serverSock);
								proc = new ProcessBuilder("wine", "reg", "add",
										"HKEY_CURRENT_USER\\Software\\Wine\\DllOverrides", "/v", "d3d10core", "/d",
										"native", "/f");
								proc.environment().put("WINEPREFIX", prefix.getCanonicalPath());
								proc.inheritIO();
								proc.start().waitFor();
								proc = new ProcessBuilder("wine", "reg", "add",
										"HKEY_CURRENT_USER\\Software\\Wine\\DllOverrides", "/v", "dxgi", "/d", "native",
										"/f");
								proc.environment().put("WINEPREFIX", prefix.getCanonicalPath());
								proc.inheritIO();
								if (proc.start().waitFor() != 0)
									prefixConfigureError(prefix, serverSock);
								proc = new ProcessBuilder("wine", "reg", "add",
										"HKEY_CURRENT_USER\\Software\\Wine\\DllOverrides", "/v", "d3d9", "/d", "native",
										"/f");
								proc.environment().put("WINEPREFIX", prefix.getCanonicalPath());
								proc.inheritIO();
								if (proc.start().waitFor() != 0)
									prefixConfigureError(prefix, serverSock);
							} catch (Exception e) {
								prefix.delete();
								SwingUtilities.invokeAndWait(() -> {
									JOptionPane.showMessageDialog(frmCenturiaLauncher,
											"Failed to configure wine, please make sure you have wine installed.",
											"Launcher Error", JOptionPane.ERROR_MESSAGE);
									try {
										serverSock.close();
									} catch (IOException e2) {
									}
									System.exit(1);
								});
							}

							// Mark done
							new File(prefix, "completed").createNewFile();
						}
						builder.environment().put("WINEPREFIX", prefix.getCanonicalPath());
					}
					if (os.equals("linux") || (useWineMethodOSX && os.equals("osx"))) {
						// Check prefix
						File dxvkD = new File("dxvk");
						File prefix = new File("wineprefix");
						if (!new File(dxvkD, "completed").exists()) {
							// Download DXVK
							SwingUtilities.invokeAndWait(() -> {
								log("Downloading DXVK...");
								progressBar.setMaximum(100);
								progressBar.setValue(0);
								panel_5.setVisible(true);
								panel_1.repaint();
							});

							// Get url
							String macosURL = null;
							JsonObject rel = null;
							if (os.equalsIgnoreCase("osx")) {
								String man = downloadString("https://api.github.com/repos/Gcenx/DXVK-macOS/releases");
								JsonArray versions = JsonParser.parseString(man).getAsJsonArray();
								for (JsonElement ele : versions) {
									JsonObject r = ele.getAsJsonObject();
									if (r.get("tag_name").getAsString().equals(PREFERRED_DXVK_VERSION_MACOS)) {
										rel = r;
										break;
									}
								}
								if (rel == null)
									throw new IOException(
											"Could not find DXVK version " + PREFERRED_DXVK_VERSION_MACOS);
							} else {
								String man = downloadString(os.equalsIgnoreCase("linux")
										? "https://api.github.com/repos/doitsujin/dxvk/releases/latest"
										: macosURL);
								rel = JsonParser.parseString(man).getAsJsonObject();
							}
							JsonArray assets = rel.get("assets").getAsJsonArray();
							String dxvk = null;
							for (JsonElement ele : assets) {
								JsonObject asset = ele.getAsJsonObject();
								if (asset.get("name").getAsString().endsWith(".tar.gz")
										&& !asset.get("name").getAsString().contains("steam")) {
									dxvk = asset.get("browser_download_url").getAsString();
									break;
								}
							}
							if (dxvk == null)
								throw new Exception("Failed to find a DXVK download.");
							downloadFile(dxvk, new File("dxvk.tar.gz"), progressBar, panel_1);

							// Extract
							try {
								SwingUtilities.invokeAndWait(() -> {
									log("Extracting DXVK...");
									progressBar.setMaximum(100);
									progressBar.setValue(0);
									panel_1.repaint();
								});
							} catch (InvocationTargetException | InterruptedException e) {
							}
							unTarGz(new File("dxvk.tar.gz"), new File("dxvk"), progressBar, panel_1);

							// Install
							try {
								SwingUtilities.invokeAndWait(() -> {
									log("Installing DXVK...");
									progressBar.setMaximum(100);
									progressBar.setValue(0);
									panel_5.setVisible(false);
									panel_1.repaint();
								});
							} catch (InvocationTargetException | InterruptedException e) {
							}
							File dxvkDir = new File("dxvk").listFiles()[0];
							File wineSys = new File(prefix, "drive_c/windows");
							wineSys.mkdirs();

							// Install for x32
							new File(wineSys, "syswow64").mkdirs();
							for (File f : new File(dxvkDir, "x32").listFiles()) {
								Files.copy(f.toPath(), new File(wineSys, "syswow64/" + f.getName()).toPath(),
										StandardCopyOption.REPLACE_EXISTING);
							}

							// Install for x64
							new File(wineSys, "system32").mkdirs();
							for (File f : new File(dxvkDir, "x64").listFiles()) {
								Files.copy(f.toPath(), new File(wineSys, "system32/" + f.getName()).toPath(),
										StandardCopyOption.REPLACE_EXISTING);
							}

							// Mark done
							new File(dxvkD, "completed").createNewFile();
						}
					}
					builder.inheritIO();
					builder.directory(new File("client/build"));

					// Log
					try {
						SwingUtilities.invokeAndWait(() -> {
							log("Starting client...");
							progressBar.setMaximum(100);
							progressBar.setValue(0);
							panel_1.repaint();
						});
					} catch (InvocationTargetException | InterruptedException e) {
					}

					// Start
					Process proc = builder.start();
					SwingUtilities.invokeAndWait(() -> {
						log("Waiting for client startup...");
						progressBar.setMaximum(100);
						progressBar.setValue(0);
						panel_1.repaint();
					});
					proc.onExit().thenAccept(t -> {
						if (!connected) {
							try {
								try {
									serverSock.close();
								} catch (IOException e) {
								}
								SwingUtilities.invokeAndWait(() -> {
									JOptionPane.showMessageDialog(frmCenturiaLauncher,
											"Client process exited before the launch was completed!\nExit code: "
													+ proc.exitValue() + "\n\nPlease contact support!",
											"Launcher Error", JOptionPane.ERROR_MESSAGE);
									System.exit(proc.exitValue());
								});
							} catch (InvocationTargetException | InterruptedException e1) {
							}
						}
					});

					// Accept client
					final String authTokenF = authToken;
					final String apiDataF = apiData;
					Thread clT = new Thread(() -> {
						Socket cl;
						try {
							cl = serverSock.accept();
						} catch (IOException e) {
							return;
						}
						try {
							connected = true;
							SwingUtilities.invokeAndWait(() -> {
								log("Communicating with client...");
								progressBar.setMaximum(100);
								progressBar.setValue(0);
								panel_1.repaint();
							});
							launcherHandoff(cl, authTokenF, hosts.get("api").getAsString(), apiDataF, serverInfo, hosts,
									ports, completedTutorial);
							cl.close();
							SwingUtilities.invokeAndWait(() -> {
								log("Finished startup!");
								progressBar.setMaximum(100);
								progressBar.setValue(0);
								panel_1.repaint();
							});
							Thread.sleep(1000);
							SwingUtilities.invokeAndWait(() -> {
								frmCenturiaLauncher.dispose();
							});
							proc.waitFor();
							try {
								serverSock.close();
							} catch (IOException e) {
							}
							System.exit(proc.exitValue());
							return;
						} catch (Exception e) {
							try {
								SwingUtilities.invokeAndWait(() -> {
									String stackTrace = "";
									for (StackTraceElement ele : e.getStackTrace())
										stackTrace += "\n     At: " + ele;
									try {
										sendCommand(cl, "crash");
									} catch (IOException e2) {
									}
									JOptionPane.showMessageDialog(frmCenturiaLauncher,
											"An error occured while running the launcher.\nUnable to continue, the launcher will now close.\n\nError details: "
													+ e + stackTrace
													+ "\nPlease report this error to the server operators.",
											"Launcher Error", JOptionPane.ERROR_MESSAGE);
									try {
										cl.close();
									} catch (IOException e2) {
									}
									try {
										serverSock.close();
									} catch (IOException e2) {
									}
									if (proc != null && proc.isAlive())
										proc.destroyForcibly();
									System.exit(1);
								});
							} catch (InvocationTargetException | InterruptedException e1) {
							}
						}
					}, "Client Communication Thread");
					clT.setDaemon(true);
					clT.start();
				} catch (Exception e) {
					try {
						serverSock.close();
					} catch (IOException e2) {
					}
					throw e;
				}
			} catch (Exception e) {
				try {
					SwingUtilities.invokeAndWait(() -> {
						String stackTrace = "";
						for (StackTraceElement ele : e.getStackTrace())
							stackTrace += "\n     At: " + ele;
						JOptionPane.showMessageDialog(frmCenturiaLauncher,
								"An error occured while running the launcher.\nUnable to continue, the launcher will now close.\n\nError details: "
										+ e + stackTrace + "\nPlease report this error to the server operators.",
								"Launcher Error", JOptionPane.ERROR_MESSAGE);
						System.exit(1);
					});
				} catch (InvocationTargetException | InterruptedException e1) {
				}
			}
		}, "Launcher Thread");
		th.setDaemon(true);
		th.start();
	}

	private boolean checkExists(String url, String authToken) {
		HttpGet get = new HttpGet(url);
		get.addHeader("Authorization", "Bearer " + authToken);
		try {
			return clientBase.execute(get, resp -> {
				if (resp.getCode() != 200)
					return null;
				InputStream strm = resp.getEntity().getContent();
				String d = new String(strm.readAllBytes(), "UTF-8");
				strm.close();
				return d;
			}) != null;
		} catch (IOException e) {
			return false;
		}
	}

	private void prefixConfigureError(File prefix, ServerSocket serverSock) {
		prefix.delete();
		try {
			SwingUtilities.invokeAndWait(() -> {
				JOptionPane.showMessageDialog(frmCenturiaLauncher,
						"Failed to configure wine, please contact support, an error occurred while setting up the wine prefix.",
						"Launcher Error", JOptionPane.ERROR_MESSAGE);
				try {
					serverSock.close();
				} catch (IOException e2) {
				}
				System.exit(1);
			});
		} catch (InvocationTargetException | InterruptedException e) {
			;
		}
	}

	private void launcherHandoff(Socket cl, String authToken, String api, String apiData, JsonObject serverInfo,
			JsonObject hosts, JsonObject ports, boolean completedTutorial) throws Exception {
		if (!api.endsWith("/"))
			api += "/";
		if (!apiData.endsWith("/"))
			apiData += "/";

		// Send options
		System.out.println("[LAUNCHER] [FERALTWEAKS LAUNCHER] Downloading and sending configuration...");
		sendCommand(cl, "config",
				Base64.getEncoder()
						.encodeToString(downloadProtectedString(apiData + "feraltweaks/settings.props", authToken)
								.replace("\t", "    ").replace("\r", "").getBytes("UTF-8")));

		// Download chart patches
		System.out.println("[LAUNCHER] [FERALTWEAKS LAUNCHER] Downloading chart patches...");
		String manifest = downloadProtectedString(apiData + "feraltweaks/chartpatches/index.json", authToken);
		JsonArray patches = JsonParser.parseString(manifest).getAsJsonArray();
		for (JsonElement ele : patches) {
			String url = api + "data";
			if (!ele.getAsString().startsWith("/"))
				url += "/";
			url += URLEncoder.encode(ele.getAsString(), "UTF-8");

			// Download patch
			String file = ele.getAsString();
			String patch = downloadProtectedString(url, authToken);

			// Send patch
			System.out.println("[LAUNCHER] [FERALTWEAKS LAUNCHER] Sending chart patch: " + file);
			sendCommand(cl, "chartpatch", Base64.getEncoder()
					.encodeToString((file + "::" + patch.replace("\t", "    ").replace("\r", "")).getBytes("UTF-8")));
		}

		// Send server environment
		System.out.println("[LAUNCHER] [FERALTWEAKS LAUNCHER] Sending server environment...");

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
		sendCommand(cl, "serverenvironment", serverEnv.toArray(t -> new Object[t]));

		// Send autologin
		if (completedTutorial) {
			System.out.println("[LAUNCHER] [FERALTWEAKS LAUNCHER] Sending autologin...");
			sendCommand(cl, "autologin", authToken);
		}

		// Send end
		cl.getOutputStream().write("end\n".getBytes("UTF-8"));
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

	private String downloadProtectedString(String url, String authToken) throws IOException {
		HttpGet get = new HttpGet(url);
		get.addHeader("Authorization", "Bearer " + authToken);
		return clientBase.execute(get, resp -> {
			InputStream strm = resp.getEntity().getContent();
			String d = new String(strm.readAllBytes(), "UTF-8");
			strm.close();
			return d;
		});
	}

	private String downloadString(String url) throws IOException {
		return clientBase.execute(new HttpGet(url), resp -> {
			InputStream strm = resp.getEntity().getContent();
			String d = new String(strm.readAllBytes(), "UTF-8");
			strm.close();
			return d;
		});
	}

	private void sendCommand(Socket cl, String cmd, Object... params) throws UnsupportedEncodingException, IOException {
		for (Object obj : params)
			cmd += " " + obj;
		cl.getOutputStream().write((cmd + "\n").getBytes("UTF-8"));
	}

	private void deleteDir(File dir) {
		if (Files.isSymbolicLink(dir.toPath())) {
			// DO NOT RECURSE
			dir.delete();
			return;
		}
		for (File subDir : dir.listFiles(t -> t.isDirectory())) {
			deleteDir(subDir);
		}
		for (File file : dir.listFiles(t -> !t.isDirectory())) {
			file.delete();
		}
		dir.delete();
	}

	private void updateMods(String pth, String apiData, String baseOut, JsonObject hosts, String authToken,
			JProgressBar progressBar, JPanel panel_1) throws Exception {
		String api = hosts.get("api").getAsString();
		if (!api.endsWith("/"))
			api += "/";
		HttpGet get = new HttpGet(apiData + "clientmods/" + pth);
		get.addHeader("Authorization", "Bearer " + authToken);
		String data = clientBase.execute(get, resp -> {
			InputStream strm = resp.getEntity().getContent();
			String d = new String(strm.readAllBytes(), "UTF-8");
			strm.close();
			return d;
		});
		JsonObject resp = JsonParser.parseString(data).getAsJsonObject();
		if (resp.has("error")) {
			// Handle error
			String err = resp.get("error").getAsString();
			switch (err) {
			case "invalid_credential": {
				throw new IOException("Credentials invalid");
			}
			case "feraltweaks_not_enabled": {
				JOptionPane.showMessageDialog(frmCenturiaLauncher,
						"Client modding is not enabled on your account, unable to launch the game.", "Launcher Error",
						JOptionPane.ERROR_MESSAGE);
				System.exit(1);
				return;
			}
			default: {
				if (resp.has("errorMessage")) {
					JOptionPane.showMessageDialog(frmCenturiaLauncher, resp.get("errorMessage").getAsString(),
							"Launcher Error", JOptionPane.ERROR_MESSAGE);
					System.exit(1);
					return;
				}
				throw new Exception("Unknown server error: " + err);
			}
			}
		}

		// Set progress bar
		SwingUtilities.invokeAndWait(() -> {
			progressBar.setMaximum(resp.size() * 100);
			progressBar.setValue(0);
			panel_1.repaint();
		});

		// Download
		for (String path : resp.keySet()) {
			String output = "client/build/" + baseOut + "/" + resp.get(path).getAsString();
			if (path.startsWith("/"))
				path = path.substring(1);

			// Download mod
			System.out.println("[LAUNCHER] [FERALTWEAKS LAUNCHER] Downloading " + path + " into " + output + "...");
			get = new HttpGet(api + "data/" + path);
			get.addHeader("Authorization", "Bearer " + authToken);
			clientBase.execute(get, response -> {
				// Check code
				if (response.getCode() != 200)
					throw new IOException("Unexpected response code: " + response.getCode());

				// Output
				HttpEntity entity = response.getEntity();
				long size = entity.getContentLength();
				InputStream strm = entity.getContent();
				File outputFile = new File(output);
				outputFile.getParentFile().mkdirs();
				FileOutputStream outp = new FileOutputStream(outputFile);
				if (size == -1) {
					// Transfer
					strm.transferTo(outp);

					// Increase progress
					SwingUtilities.invokeLater(() -> {
						progressBar.setValue(progressBar.getValue() + 100);
						panel_1.repaint();
					});
				} else {
					// Block transfer
					long c = 0;
					float step = 100f / (float) size;
					int valStart = progressBar.getValue();
					while (c < size) {
						// Read block
						byte[] buffer = new byte[2048];
						int r = strm.read(buffer, 0, buffer.length);
						if (r == -1)
							throw new IOException("Stream closed before end of document");
						c += r;

						// Write
						outp.write(buffer, 0, r);

						// Set progress
						long cF = c;
						SwingUtilities.invokeLater(() -> {
							progressBar.setValue(valStart + (int) ((float) step * cF));
							panel_1.repaint();
						});
					}

					// Set progress
					SwingUtilities.invokeLater(() -> {
						progressBar.setValue(valStart + 100);
						panel_1.repaint();
					});
				}
				outp.close();
				return null;
			});
		}

		// Set progress
		SwingUtilities.invokeLater(() -> {
			progressBar.setValue(progressBar.getMaximum());
			panel_1.repaint();
		});
	}

	private void log(String message) {
		lblNewLabel.setText(" " + message);
		System.out.println("[LAUNCHER] [FERALTWEAKS LAUNCHER] " + message);
	}

	private void unTarGz(File input, File output, JProgressBar bar, JPanel panel_1) throws IOException {
		output.mkdirs();

		// count entries
		InputStream file = new FileInputStream(input);
		GZIPInputStream gzip = new GZIPInputStream(file);
		TarArchiveInputStream tar = new TarArchiveInputStream(gzip);
		int count = 0;
		while (tar.getNextEntry() != null) {
			count++;
		}
		tar.close();
		gzip.close();
		file.close();

		// prepare and log
		file = new FileInputStream(input);
		gzip = new GZIPInputStream(file);
		tar = new TarArchiveInputStream(gzip);
		try {
			int fcount = count;
			SwingUtilities.invokeAndWait(() -> {
				bar.setMaximum(fcount);
				bar.setValue(0);
				panel_1.repaint();
			});
		} catch (InvocationTargetException | InterruptedException e) {
		}

		// extract
		while (true) {
			ArchiveEntry ent = tar.getNextEntry();
			if (ent == null)
				break;

			if (ent.isDirectory()) {
				new File(output, ent.getName()).mkdirs();
			} else {
				File out = new File(output, ent.getName());
				if (out.getParentFile() != null && !out.getParentFile().exists())
					out.getParentFile().mkdirs();
				FileOutputStream os = new FileOutputStream(out);
				InputStream is = tar;
				is.transferTo(os);
				os.close();
			}

			SwingUtilities.invokeLater(() -> {
				bar.setValue(bar.getValue() + 1);
				panel_1.repaint();
			});
		}

		// finish progress
		SwingUtilities.invokeLater(() -> {
			bar.setValue(bar.getValue() + 1);
			panel_1.repaint();
		});
		tar.close();
		gzip.close();
		file.close();
	}

	private void unzip7z(File input, File output, JProgressBar bar, JPanel panel_1) throws IOException {
		output.mkdirs();

		// count entries
		SevenZFile archive = new SevenZFile(input);
		int count = 0;
		while (archive.getNextEntry() != null) {
			count++;
		}
		archive.close();

		// prepare and log
		archive = new SevenZFile(input);
		try {
			int fcount = count;
			SwingUtilities.invokeAndWait(() -> {
				bar.setMaximum(fcount);
				bar.setValue(0);
				panel_1.repaint();
			});
		} catch (InvocationTargetException | InterruptedException e) {
		}

		// extract
		while (true) {
			SevenZArchiveEntry ent = archive.getNextEntry();
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
				is.transferTo(os);
				is.close();
				os.close();
			}

			SwingUtilities.invokeLater(() -> {
				bar.setValue(bar.getValue() + 1);
				panel_1.repaint();
			});
		}

		// finish progress
		SwingUtilities.invokeLater(() -> {
			bar.setValue(bar.getValue() + 1);
			panel_1.repaint();
		});
		archive.close();
	}

	private void downloadFile(String url, File outp, JProgressBar progressBar, JPanel panel_1)
			throws MalformedURLException, IOException {
		HttpGet get = new HttpGet(url);
		clientBase.execute(get, response -> {
			// Check code
			if (response.getCode() != 200)
				throw new IOException("Unexpected response code: " + response.getCode());

			// Get entity
			HttpEntity entity = response.getEntity();
			long size = entity.getContentLength();
			InputStream data = entity.getContent();

			// Download
			try {
				SwingUtilities.invokeAndWait(() -> {
					progressBar.setMaximum((int) (size / 1000));
					progressBar.setValue(0);
					panel_1.repaint();
				});
			} catch (InvocationTargetException | InterruptedException e) {
			}
			FileOutputStream out = new FileOutputStream(outp);
			while (true) {
				byte[] b = data.readNBytes(1000);
				if (b.length == 0)
					break;
				else {
					out.write(b);
					SwingUtilities.invokeLater(() -> {
						progressBar.setValue(progressBar.getValue() + 1);
						panel_1.repaint();
					});
				}
			}
			out.close();
			data.close();
			SwingUtilities.invokeLater(() -> {
				progressBar.setValue(progressBar.getMaximum());
				panel_1.repaint();
			});
			return null;
		});
	}

	private void unZip(File input, File output, JProgressBar bar, JPanel panel_1) throws IOException {
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
		try {
			int fcount = count;
			SwingUtilities.invokeAndWait(() -> {
				bar.setMaximum(fcount);
				bar.setValue(0);
				panel_1.repaint();
			});
		} catch (InvocationTargetException | InterruptedException e) {
		}

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
				is.transferTo(os);
				is.close();
				os.close();
			}

			SwingUtilities.invokeLater(() -> {
				bar.setValue(bar.getValue() + 1);
				panel_1.repaint();
			});
		}

		// finish progress
		SwingUtilities.invokeLater(() -> {
			bar.setValue(bar.getValue() + 1);
			panel_1.repaint();
		});
		archive.close();
	}
}
