package org.asf.centuria.feraltweaks.utils;

import java.util.ArrayList;

/**
 * 
 * Version utility
 * 
 * @author Sky Swimmer
 * 
 */
public class VersionUtils {

	/**
	 * Verifies version requirements
	 * 
	 * @param version      Version string
	 * @param versionCheck Version check string
	 * @return True if valid, false otherwise
	 */
	public static boolean verifyVersionRequirement(String version, String versionCheck) {
		for (String filter : versionCheck.split("\\|\\|")) {
			filter = filter.trim();
			if (verifyVersionRequirementPart(version, filter))
				return true;
		}
		return false;
	}

	private static boolean verifyVersionRequirementPart(String version, String versionCheck) {
		// Handle versions
		for (String filter : versionCheck.split("&")) {
			filter = filter.trim();

			// Verify filter string
			if (filter.startsWith("!=")) {
				// Not equal
				if (version.equals(filter.substring(2)))
					return false;
			} else if (filter.startsWith("==")) {
				// Equal to
				if (!version.equals(filter.substring(2)))
					return false;
			} else if (filter.startsWith(">=")) {
				int[] valuesVersionCurrent = parseVersionValues(version);
				int[] valuesVersionCheck = parseVersionValues(filter.substring(2));

				// Handle each
				for (int i = 0; i < valuesVersionCheck.length; i++) {
					int val = valuesVersionCheck[i];

					// Verify lengths
					if (i > valuesVersionCurrent.length)
						break;

					// Verify value
					int i2 = 0;
					if (i < valuesVersionCurrent.length)
						i2 = valuesVersionCurrent[i];
					if (i2 < val)
						return false;
				}
			} else if (filter.startsWith("<=")) {
				int[] valuesVersionCurrent = parseVersionValues(version);
				int[] valuesVersionCheck = parseVersionValues(filter.substring(2));

				// Handle each
				for (int i = 0; i < valuesVersionCheck.length; i++) {
					int val = valuesVersionCheck[i];

					// Verify lengths
					if (i > valuesVersionCurrent.length)
						break;

					// Verify value
					int i2 = 0;
					if (i < valuesVersionCurrent.length)
						i2 = valuesVersionCurrent[i];
					if (i2 > val)
						return false;
				}
			} else if (filter.startsWith(">")) {
				int[] valuesVersionCurrent = parseVersionValues(version);
				int[] valuesVersionCheck = parseVersionValues(filter.substring(1));

				// Handle each
				for (int i = 0; i < valuesVersionCheck.length; i++) {
					int val = valuesVersionCheck[i];

					// Verify lengths
					if (i > valuesVersionCurrent.length)
						break;

					// Verify value
					int i2 = 0;
					if (i < valuesVersionCurrent.length)
						i2 = valuesVersionCurrent[i];
					if (i2 <= val)
						return false;
				}
			} else if (filter.startsWith("<")) {
				int[] valuesVersionCurrent = parseVersionValues(version);
				int[] valuesVersionCheck = parseVersionValues(filter.substring(1));

				// Handle each
				for (int i = 0; i < valuesVersionCheck.length; i++) {
					int val = valuesVersionCheck[i];

					// Verify lengths
					if (i > valuesVersionCurrent.length)
						break;

					// Verify value
					int i2 = 0;
					if (i < valuesVersionCurrent.length)
						i2 = valuesVersionCurrent[i];
					if (i2 >= val)
						return false;
				}
			} else {
				// Equal to
				if (!version.equals(filter))
					return false;
			}
		}

		// Valid
		return true;
	}

	private static int[] parseVersionValues(String version) {
		ArrayList<Integer> values = new ArrayList<Integer>();

		// Parse version string
		String buffer = "";
		for (char ch : version.toCharArray()) {
			if (ch == '-' || ch == '.') {
				// Handle segment
				if (!buffer.isEmpty()) {
					// Check if its a number
					if (buffer.matches("^[0-9]+$")) {
						// Add value
						try {
							values.add(Integer.parseInt(buffer));
						} catch (Exception e) {
							// ... okay... add first char value instead
							values.add((int) buffer.charAt(0));
						}
					} else {
						// Check if its a full word and doesnt contain numbers
						if (buffer.matches("^[^0-9]+$")) {
							// It is, add first char value
							values.add((int) buffer.charAt(0));
						} else {
							// Add each value
							for (char ch2 : buffer.toCharArray())
								values.add((int) ch2);
						}
					}
				}
				buffer = "";
			} else {
				// Add to segment buffer
				buffer += ch;
			}
		}
		if (!buffer.isEmpty()) {
			// Check if its a number
			if (buffer.matches("^[0-9]+$")) {
				// Add value
				try {
					values.add(Integer.parseInt(buffer));
				} catch (Exception e) {
					// ... okay... add first char value instead
					values.add((int) buffer.charAt(0));
				}
			} else {
				// Check if its a full word and doesnt contain numbers
				if (buffer.matches("^[^0-9]+$")) {
					// It is, add first char value
					values.add((int) buffer.charAt(0));
				} else {
					// Add each value
					for (char ch : buffer.toCharArray())
						values.add((int) ch);
				}
			}
		}

		int[] arr = new int[values.size()];
		for (int i = 0; i < arr.length; i++)
			arr[i] = values.get(i);
		return arr;
	}

}
