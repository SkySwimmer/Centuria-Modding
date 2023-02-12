package org.asf.centuria.launcher.updater;

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

import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

import java.awt.image.BufferedImage;
import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.lang.reflect.InvocationTargetException;
import java.net.MalformedURLException;
import java.net.URL;
import java.net.URLConnection;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Enumeration;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;
import java.awt.Color;

public class LauncherUpdaterMain {

	private JFrame frmCenturiaLauncher;
	private JLabel lblNewLabel;

	/**
	 * Launch the application.
	 */
	public static void main(String[] args) {
		EventQueue.invokeLater(new Runnable() {
			public void run() {
				try {
					LauncherUpdaterMain window = new LauncherUpdaterMain();
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
	public LauncherUpdaterMain() {
		initialize();
	}

	/**
	 * Initialize the contents of the frame.
	 */
	private void initialize() {
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
		frmCenturiaLauncher.setResizable(false);
		frmCenturiaLauncher.setBounds(100, 100, 555, 492);
		frmCenturiaLauncher.setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
		frmCenturiaLauncher.setLocationRelativeTo(null);
		try {
			InputStream strmi = getClass().getClassLoader().getResourceAsStream("emulogo_purple.png");
			frmCenturiaLauncher.setIconImage(ImageIO.read(strmi));
			strmi.close();
		} catch (IOException e1) {
		}

		JPanel panel = new JPanel();
		frmCenturiaLauncher.getContentPane().add(panel, BorderLayout.SOUTH);
		panel.setLayout(new BorderLayout(0, 0));

		JProgressBar progressBar = new JProgressBar();
		progressBar.setPreferredSize(new Dimension(146, 10));
		panel.add(progressBar, BorderLayout.NORTH);

		BackgroundPanel panel_1 = new BackgroundPanel();
		panel_1.setForeground(Color.WHITE);
		frmCenturiaLauncher.getContentPane().add(panel_1, BorderLayout.CENTER);
		panel_1.setLayout(new BorderLayout(0, 0));

		lblNewLabel = new JLabel("New label");
		lblNewLabel.setPreferredSize(new Dimension(46, 20));
		panel_1.add(lblNewLabel, BorderLayout.SOUTH);

		// Contact server
		String launcherVersion;
		String launcherDir;
		String launcherURL;
		String dataUrl;
		String srvName;
		try {
			// Read server info
			String dirName;
			String url;
			try {
				JsonObject conf = JsonParser.parseString(Files.readString(Path.of("server.json"))).getAsJsonObject();
				srvName = conf.get("serverName").getAsString();
				dirName = conf.get("launcherDirName").getAsString();
				url = conf.get("serverConfig").getAsString();
				dataUrl = url;
			} catch (Exception e) {
				JOptionPane.showMessageDialog(null, "Invalid launcher configuration.", "Launcher Error",
						JOptionPane.ERROR_MESSAGE);
				System.exit(1);
				return;
			}
			frmCenturiaLauncher.setTitle(srvName + " Launcher");

			// Download data
			InputStream strm = new URL(url).openStream();
			String data = new String(strm.readAllBytes(), "UTF-8");
			strm.close();
			JsonObject info = JsonParser.parseString(data).getAsJsonObject();
			JsonObject launcher = info.get("launcher").getAsJsonObject();
			String splash = launcher.get("splash").getAsString();
			url = launcher.get("url").getAsString();
			String version = launcher.get("version").getAsString();

			// Download splash and set image
			BufferedImage img = ImageIO.read(new URL(splash));
			panel_1.setImage(img);
			launcherVersion = version;
			launcherDir = dirName;
			launcherURL = url;
		} catch (Exception e) {
			JOptionPane.showMessageDialog(null,
					"Could not connect with the launcher servers, please check your internet connection. If you are connected, please wait a few minutes and try again.\n\nIf the issue remains and you are connected to the internet, please submit a support ticket.",
					"Launcher Error", JOptionPane.ERROR_MESSAGE);
			System.exit(1);
			return;
		}

		Thread th = new Thread(() -> {
			// Set progress bar status
			try {
				SwingUtilities.invokeAndWait(() -> {
					log("Checking launcher files...");
					progressBar.setMaximum(100);
					progressBar.setValue(0);
				});

				// Build folder path
				File dir;
				if (System.getenv("LOCALAPPDATA") == null) {
					dir = new File(System.getProperty("user.home") + "/.local/share");
					if (!dir.exists())
						dir = new File(System.getProperty("user.home"));
					dir = new File(dir, ".centuria-launcher");
				} else {
					dir = new File(System.getenv("LOCALAPPDATA"));
				}
				if (new File("installation.json").exists())
					dir = new File(JsonParser.parseString(Files.readString(Path.of("installation.json")))
							.getAsJsonObject().get("installationDirectory").getAsString());
				dir = new File(dir, launcherDir);
				if (!dir.exists())
					dir.mkdirs();

				// Check version file
				File verFile = new File(dir, "currentversion.info");
				String currentVersion = "";
				boolean isNew = !verFile.exists();
				if (!isNew)
					currentVersion = Files.readString(verFile.toPath());

				// Check updates
				SwingUtilities.invokeAndWait(() -> {
					log("Checking for updates...");
					progressBar.setMaximum(100);
					progressBar.setValue(0);
				});
				if (!currentVersion.equals(launcherVersion)) {
					if (isNew) {
						// Prompt
						SwingUtilities.invokeAndWait(() -> {
							if (JOptionPane.showConfirmDialog(frmCenturiaLauncher,
									"Do you wish to install the " + srvName + " launcher and client?",
									"Install Launcher", JOptionPane.YES_NO_OPTION,
									JOptionPane.QUESTION_MESSAGE) == JOptionPane.NO_OPTION) {
								System.exit(1);
							}
						});
					}

					// Update label
					try {
						SwingUtilities.invokeAndWait(() -> {
							log("Updating launcher...");
						});
					} catch (InvocationTargetException | InterruptedException e) {
					}

					// Download zip
					File tmpOut = new File(dir, "launcher.zip");
					downloadFile(launcherURL, tmpOut, progressBar);

					// Extract zip
					try {
						SwingUtilities.invokeAndWait(() -> {
							log("Extracting launcher update...");
							progressBar.setMaximum(100);
							progressBar.setValue(0);
						});
					} catch (InvocationTargetException | InterruptedException e) {
					}
					unZip(tmpOut, new File(dir, "launcher"), progressBar);
				}

				// Prepare to start launcher
				try {
					SwingUtilities.invokeAndWait(() -> {
						log("Starting...");
						progressBar.setMaximum(100);
						progressBar.setValue(0);
					});
				} catch (InvocationTargetException | InterruptedException e) {
				}
				Thread.sleep(1000);

				// Start launcher
				JsonObject startupInfo = JsonParser
						.parseString(Files.readString(new File(new File(dir, "launcher"), "startup.json").toPath()))
						.getAsJsonObject();
				ArrayList<String> cmd = new ArrayList<String>();
				cmd.add(startupInfo.get("executable").getAsString()
						.replace("$<dir>", new File(dir, "launcher").getAbsolutePath())
						.replace("$<jvm>", ProcessHandle.current().info().command().get()));
				for (JsonElement ele : startupInfo.get("arguments").getAsJsonArray())
					cmd.add(ele.getAsString().replace("$<dir>", new File(dir, "launcher").getAbsolutePath())
							.replace("$<jvm>", ProcessHandle.current().info().command().get())
							.replace("$<pathsep>", File.pathSeparator).replace("$<server>", srvName)
							.replace("$<data-url>", dataUrl));
				ProcessBuilder builder = new ProcessBuilder(cmd.toArray(t -> new String[t]));
				builder.directory(new File(dir, "launcher"));
				Process proc = builder.start();

				// Mark done
				if (!currentVersion.equals(launcherVersion))
					Files.writeString(verFile.toPath(), launcherVersion);
				SwingUtilities.invokeAndWait(() -> {
					frmCenturiaLauncher.setVisible(false);
				});
				proc.waitFor();
				System.exit(proc.exitValue());
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

	private void downloadFile(String url, File outp, JProgressBar progressBar)
			throws MalformedURLException, IOException {
		URLConnection urlConnection = new URL(url).openConnection();
		try {
			SwingUtilities.invokeAndWait(() -> {
				log("Updating launcher...");
				progressBar.setMaximum(urlConnection.getContentLength() / 1000);
				progressBar.setValue(0);
			});
		} catch (InvocationTargetException | InterruptedException e) {
		}
		InputStream data = urlConnection.getInputStream();
		FileOutputStream out = new FileOutputStream(outp);
		while (true) {
			byte[] b = data.readNBytes(1000);
			if (b.length == 0)
				break;
			else {
				out.write(b);
				SwingUtilities.invokeLater(() -> {
					progressBar.setValue(progressBar.getValue() + 1);
				});
			}
		}
		out.close();
		data.close();
		SwingUtilities.invokeLater(() -> {
			progressBar.setValue(progressBar.getMaximum());
		});
	}

	private void log(String message) {
		lblNewLabel.setText(" " + message);
		System.out.println("[LAUNCHER] [UPDATER] " + message);
	}

	private void unZip(File input, File output, JProgressBar bar) throws IOException {
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
			});
		}

		// finish progress
		SwingUtilities.invokeLater(() -> {
			bar.setValue(bar.getValue() + 1);
		});
		archive.close();
	}
}
