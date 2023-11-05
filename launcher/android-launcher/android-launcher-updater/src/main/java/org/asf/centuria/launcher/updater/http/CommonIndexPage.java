package org.asf.centuria.launcher.updater.http;

import java.io.File;
import java.io.FileFilter;
import java.io.IOException;
import java.io.InputStream;
import java.util.function.IntFunction;
import java.util.stream.Stream;

import org.asf.centuria.launcher.updater.LauncherUpdaterMain;
import org.asf.connective.io.IoUtil;
import org.asf.connective.objects.HttpRequest;
import org.asf.connective.objects.HttpResponse;

public class CommonIndexPage {

	public static void index(File requestedFile, HttpRequest request, HttpResponse response) {
		index(requestedFile, request, response, Stream.of(requestedFile.listFiles(new FileFilter() {
			@Override
			public boolean accept(File t) {
				return t.isDirectory();
			}
		})).sorted().toArray(new IntFunction<File[]>() {
			@Override
			public File[] apply(int t) {
				return new File[t];
			}
		}), Stream.of(requestedFile.listFiles(new FileFilter() {
			@Override
			public boolean accept(File t) {
				return t.isFile();
			}
		})).sorted().toArray(new IntFunction<File[]>() {
			@Override
			public File[] apply(int t) {
				return new File[t];
			}
		}));
	}

	public static void index(File requestedFile, HttpRequest request, HttpResponse response, File[] dirs,
			File[] files) {
		try {
			// Read template index page
			InputStream strm;
			if (LauncherUpdaterMain.getApplicationContext() != null)
				strm = LauncherUpdaterMain.getApplicationContext().getAssets().open("connective/index.template.html");
			else
				strm = CommonIndexPage.class.getResource("/assets/connective/index.template.html").openStream();

			// Process and set body
			response.setContent("text/html", processIndex(new String(IoUtil.readAllBytes(strm), "UTF-8"),
					request.getRequestPath(), requestedFile.getName(), null, dirs, files));
		} catch (IOException e) {
		}
	}

	public static String processIndex(String str, String path, String name, File data, File[] directories,
			File[] files) {
		// Remove windows line separators
		str = str.replace("\r", "");

		// Clean path a bit
		if (!path.endsWith("/")) {
			path += "/";
		}

		// If this is a entry process call, set the path and name fields
		if (data != null) {
			str = str.replace("%c-name%", name);
			str = str.replace("%c-path%", path);
		}

		// Process files and directories
		if (files != null) {
			// Parse NOTROOT block (only adds the specified entry if its not the root page
			if (str.contains("<%%PROCESS:NOTROOT:$")) {
				String buffer = "";
				String template = "";
				int percent = 0;
				boolean parsing = false;

				// Parse the block
				for (char ch : str.toCharArray()) {
					if (ch == '<' && !parsing) {
						if (buffer.isEmpty()) {
							buffer = "<";
						} else {
							buffer = "";
						}
					} else if (ch == '\n' && !parsing) {
						buffer = "";
					} else if (ch == '\n') {
						buffer += "\n";
					} else {
						if (!buffer.isEmpty() && !parsing) {
							buffer += ch;
							if (ch == '$') {
								if (!buffer.equals("<%%PROCESS:NOTROOT:$")) {
									buffer = "";
								} else {
									parsing = true;
								}
							}
						} else if (parsing) {
							buffer += ch;
							if (ch == '%' && percent < 2) {
								percent++;
							} else if (ch == '%' && percent >= 2) {
								percent = 0;
							} else if (ch == '>' && percent == 2) {
								// Finish parsing
								percent = 0;
								template = buffer;

								// Clean buffer
								buffer = buffer.substring("<%%PROCESS:NOTROOT:$".length() + "\n".length(),
										buffer.length() - 4);

								// Replace template block with result
								StringBuilder strs = new StringBuilder();

								// Check if this is the root page
								if (!path.equals("/") && !path.isEmpty()) {
									// Not in the root page, add the block
									strs.append(buffer);
								}

								// Replace template from the content and clear buffer
								str = str.replace(template, strs.toString());
								buffer = "";
								parsing = false;
							} else {
								percent = 0;
							}
						}
					}
				}
			}

			// Parse file block
			if (str.contains("<%%PROCESS:FILES:$")) {
				String buffer = "";
				String template = "";
				int percent = 0;
				boolean parsing = false;

				// Parse the block
				for (char ch : str.toCharArray()) {
					if (ch == '<' && !parsing) {
						if (buffer.isEmpty()) {
							buffer = "<";
						} else {
							buffer = "";
						}
					} else if (ch == '\n' && !parsing) {
						buffer = "";
					} else if (ch == '\n') {
						buffer += "\n";
					} else {
						if (!buffer.isEmpty() && !parsing) {
							buffer += ch;
							if (ch == '$') {
								if (!buffer.equals("<%%PROCESS:FILES:$")) {
									buffer = "";
								} else {
									parsing = true;
								}
							}
						} else if (parsing) {
							buffer += ch;
							if (ch == '%' && percent < 2) {
								percent++;
							} else if (ch == '%' && percent >= 2) {
								percent = 0;
							} else if (ch == '>' && percent == 2) {
								// Finish parsing
								percent = 0;
								template = buffer;

								// Clean the buffer
								buffer = buffer.substring("<%%PROCESS:FILES:$".length() + "\n".length(),
										buffer.length() - 4);

								// Replace template block with result
								StringBuilder strs = new StringBuilder();
								for (File f : files) {
									// Use the template to add an entry to the result
									strs.append(processIndex(buffer, path, f.getName(), f, null, null));
								}

								// Replace template from the content and clear buffer
								str = str.replace(template, strs.toString());
								buffer = "";
								parsing = false;
							} else {
								percent = 0;
							}
						}
					}
				}
			}

			// Parse directory blocks
			if (str.contains("<%%PROCESS:DIRECTORIES:$")) {
				String buffer = "";
				String template = "";
				int percent = 0;
				boolean parsing = false;

				// Parse the block
				for (char ch : str.toCharArray()) {
					if (ch == '<' && !parsing) {
						if (buffer.isEmpty()) {
							buffer = "<";
						} else {
							buffer = "";
						}
					} else if (ch == '\n' && !parsing) {
						buffer = "";
					} else if (ch == '\n') {
						buffer += "\n";
					} else {
						if (!buffer.isEmpty() && !parsing) {
							buffer += ch;
							if (ch == '$') {
								if (!buffer.equals("<%%PROCESS:DIRECTORIES:$")) {
									buffer = "";
								} else {
									parsing = true;
								}
							}
						} else if (parsing) {
							buffer += ch;
							if (ch == '%' && percent < 2) {
								percent++;
							} else if (ch == '%' && percent >= 2) {
								percent = 0;
							} else if (ch == '>' && percent == 2) {
								// Finish parsing
								percent = 0;
								template = buffer;

								// Clean the buffer
								buffer = buffer.substring("<%%PROCESS:DIRECTORIES:$".length() + "\n".length(),
										buffer.length() - 4);

								// Replace template block with the result
								StringBuilder strs = new StringBuilder();
								for (File f : directories) {
									strs.append(processIndex(buffer, path, f.getName(), f, null, null));
								}

								// Replace template from the content and clear buffer
								str = str.replace(template, strs.toString());
								buffer = "";
								parsing = false;
							} else {
								percent = 0;
							}
						}
					}
				}
			}
		}

		// Create a pretty path string
		String prettyPath = path;
		if (prettyPath.endsWith("/") && !prettyPath.equals("/"))
			prettyPath = prettyPath.substring(0, prettyPath.length() - 1);
		if (!prettyPath.equals("/"))
			prettyPath = prettyPath.substring(1);

		// Replace rest of template data
		str = str.replace("%path%", path);
		str = str.replace("%path-pretty%", prettyPath);
		str = str.replace("%name%", name);
		str = str.replace("%up-path%", (path.equals("/") || path.isEmpty()) ? "" : new File(path).getParent());

		return str;
	}

}
