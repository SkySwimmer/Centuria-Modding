package org.asf.centuria.launcher;

import java.io.File;

import org.asf.centuria.launcher.updater.LauncherUpdaterMain;

import android.app.Activity;

/**
 * 
 * Base FT launcher interface
 * 
 * @author Sky Swimmer
 * 
 */
public interface IFeralTweaksLauncher {

	/**
	 * Starts the game
	 */
	public static void startGame() {
		LauncherUpdaterMain.startGame();
	}

	/**
	 * Called to start the launcher
	 * 
	 * @param activity          Game activity
	 * @param launcherDir       Launcher folder
	 * @param startGameCallback Game startup callback (call this to start the game)
	 * @param srvName           Server name
	 * @param dataUrl           Server information file URL
	 */
	public void startLauncher(Activity activity, File launcherDir, Runnable startGameCallback, String dataUrl,
			String srvName);

}
