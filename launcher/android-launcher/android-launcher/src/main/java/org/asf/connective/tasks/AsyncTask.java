package org.asf.connective.tasks;

/**
 * 
 * Internal async task object
 * 
 * @author Sky Swimmer
 *
 */
public class AsyncTask {
	private Runnable action;
	private boolean run;

	public AsyncTask(Runnable action) {
		this.action = action;
	}

	void run() {
		try {
			action.run();
		} finally {
			run = true;
		}
	}

	public boolean hasRun() {
		return run;
	}

	public void block() {
		if (run)
			return;
		while (!run)
			try {
				Thread.sleep(1);
			} catch (InterruptedException e) {
			}
	}

}
