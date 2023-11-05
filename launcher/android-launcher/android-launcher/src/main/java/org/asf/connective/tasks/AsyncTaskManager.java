package org.asf.connective.tasks;

import java.util.ArrayList;
import java.util.function.Predicate;
import java.util.stream.Stream;

/**
 * 
 * Internal task thread manager
 * 
 * @author Sky Swimmer
 *
 */
public class AsyncTaskManager {

	private static ArrayList<AsyncTaskThreadHandler> threads = new ArrayList<AsyncTaskThreadHandler>();
	private static ArrayList<AsyncTask> queuedActions = new ArrayList<AsyncTask>();

	static AsyncTask obtainNext() {
		synchronized (queuedActions) {
			if (queuedActions.size() == 0)
				return null;
			return queuedActions.remove(0);
		}
	}

	public static AsyncTask runAsync(Runnable action) {
		AsyncTask tsk = new AsyncTask(action);

		synchronized (threads) {
			// Check if a thread is available, if not, start a new one
			AsyncTaskThreadHandler[] ths = threads.toArray(new AsyncTaskThreadHandler[0]);
			if (!Stream.of(ths).anyMatch(new Predicate<AsyncTaskThreadHandler>() {

				@Override
				public boolean test(AsyncTaskThreadHandler t) {
					return t.isAvailable();
				}

			})) {
				// Start new thread
				AsyncTaskThreadHandler handler = new AsyncTaskThreadHandler();
				threads.add(handler);
				Thread th = new Thread(new Runnable() {
					@Override
					public void run() {
						// Start
						handler.run();

						// End
						synchronized (threads) {
							threads.remove(handler);
						}
					}
				}, "Async task thread");
				th.setDaemon(true);
				th.start();
			}

			// Add task
			synchronized (queuedActions) {
				queuedActions.add(tsk);
			}

			// Return
			return tsk;
		}
	}

}
