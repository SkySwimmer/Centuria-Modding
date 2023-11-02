package org.asf.windowsill;

public class WMNI {

	/**
	 * Loads CoreCLR
	 * 
	 * @param path Path to library of coreclr
	 * @return CoreCLR pointer
	 */
	public static native long loadCoreCLR(String path);

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
