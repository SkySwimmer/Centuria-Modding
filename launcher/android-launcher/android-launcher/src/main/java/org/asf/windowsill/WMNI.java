package org.asf.windowsill;

public class WMNI {

	/**
	 * Initializes the mono runtime
	 * 
	 * @param monoPtr      Mono library pointer
	 * @param domainName   Name of the app domain
	 * @param root         Root folder
	 * @param monoLibsPath Mono library folder
	 * @param monoEtcPath  Mono 'etc' folder
	 * @return Mono domain pointer
	 */
	public static native long initRuntime(long monoPtr, String domainName, String root, String monoLibsPath,
			String monoEtcPath);

	/**
	 * Loads the Mono library
	 * 
	 * @param path Path to the Mono library
	 * @return Mono library pointer
	 */
	public static native long loadMonoLib(String path);

	/**
	 * Loads libraries
	 * 
	 * @param path Path to the library to load
	 * @return Library pointer
	 */
	public static native long loadLibrary(String path);

	/**
	 * Closes libraries
	 * 
	 * @param ptr Library pointer
	 */
	public static native void closeLibrary(long ptr);

	/**
	 * Retrieves the load error
	 * 
	 * @return Error message
	 */
	public static native String dlLoadError();

}
