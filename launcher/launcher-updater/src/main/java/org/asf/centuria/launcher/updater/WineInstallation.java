package org.asf.centuria.launcher.updater;

import java.io.File;
import java.io.IOException;
import java.util.ArrayList;

public class WineInstallation {

	public static WineInstallation[] findAllWineInstallations() {
		ArrayList<WineInstallation> installs = new ArrayList<WineInstallation>();

		// On linux or mac
		if (LauncherUpdaterMain.os != 1) {
			File steamappsCommonDefault = new File(
					System.getProperty("user.home") + "/.local/share/Steam/steamapps/common");

			// Find proton installations matching supported
			String[] preferredProton = new String[] { "Proton 8.0", "Proton 9.0 (Beta)" };
			ArrayList<String> protonVersions = new ArrayList<String>();
			for (String proton : preferredProton) {
				File game = new File(steamappsCommonDefault, proton);
				if (game.exists()) {
					File wineBinary = new File(game, "dist/bin/wineserver");
					if (!wineBinary.exists()) {
						wineBinary = new File(game, "files/bin/wineserver");
					}

					// Check
					if (wineBinary.exists() && !protonVersions.contains(game.getName())) {
						// Found proton
						findWine(game.getName(), wineBinary.getParentFile(), null, installs, true);
						protonVersions.add(game.getName());
					}
				}
			}

			// Find remaining versions
			for (File game : steamappsCommonDefault.listFiles(t -> t.isDirectory())) {
				File wineBinary = new File(game, "dist/bin/wineserver");
				if (!wineBinary.exists()) {
					wineBinary = new File(game, "files/bin/wineserver");
				}

				// Check
				if (wineBinary.exists() && !protonVersions.contains(game.getName())) {
					// Found proton
					findWine(game.getName(), wineBinary.getParentFile(), null, installs, true);
					protonVersions.add(game.getName());
				}
			}
		}

		// Find system wine
		findWine("System wine (/usr/bin)", new File("/usr/bin"), null, installs, false);
		findWine("System wine (/usr/local/bin)", new File("/usr/local/bin"), null, installs, false);
		findWine("System wine (/opt/bin)", new File("/opt/bin"), null, installs, false);
		findWine("System wine (/opt/homebrew/bin)", new File("/opt/homebrew/bin"), null, installs, false);

		// Bundled wine
		findWine("Bundled wine (builtin)", new File("syslibs/bin"), "syslibs/bin", installs, false);
		findWine("Bundled wine (builtin)", new File("installerdata/syslibs/bin"), "syslibs/bin", installs, false);

		// Return
		return installs.toArray(t -> new WineInstallation[t]);
	}

	public static String getWineVersion(File wineBin) {
		ProcessBuilder proc = new ProcessBuilder(wineBin.getAbsolutePath(), "--version");
		try {
			Process inst = proc.start();
			String res = new String(inst.getInputStream().readAllBytes(), "UTF-8").replace("\n", "").replace("\r", "");
			if (res.isEmpty())
				res = new String(inst.getErrorStream().readAllBytes(), "UTF-8").replace("\n", "").replace("\r", "");
			inst.waitFor();
			return res;
		} catch (IOException | InterruptedException e) {
			return null;
		}
	}

	private static void findWine(String name, File source, String path, ArrayList<WineInstallation> installs,
			boolean isProton) {
		// Check
		File wineBinary = new File(source, "wineserver");
		if (wineBinary.exists()) {
			// Check
			String wineVersion = getWineVersion(wineBinary);
			if (wineVersion != null) {
				// Add
				WineInstallation install = new WineInstallation(path == null ? source.getAbsolutePath() : path,
						name + ": " + wineVersion, false, false);
				install.isProton = isProton;
				installs.add(install);
			}
		}
	}

	public WineInstallation(String path, String display, boolean isUserPicked, boolean isAuto) {
		this.path = path;
		this.display = display;
		this.isUserPicked = isUserPicked;
		this.isAuto = isAuto;
	}

	public boolean isProton;
	public String path;
	public String display;

	public boolean isUserPicked;
	public boolean isAuto;

	public String toString() {
		return display;
	}
}
