package org.asf.centuria.launcher.updater;

import javax.swing.JFrame;
import javax.swing.JLabel;
import javax.swing.JOptionPane;

import java.awt.BorderLayout;

import javax.swing.SwingConstants;
import javax.swing.SwingUtilities;

import java.awt.Font;
import java.awt.Dimension;
import java.awt.EventQueue;

import javax.swing.JPanel;
import javax.swing.ComboBoxModel;
import javax.swing.JButton;
import javax.swing.JDialog;
import javax.swing.JFileChooser;
import javax.swing.border.BevelBorder;

import java.awt.event.ActionListener;
import java.awt.event.WindowEvent;
import java.awt.image.BufferedImage;
import java.io.File;
import java.io.IOException;
import java.lang.reflect.InvocationTargetException;
import java.util.ArrayList;
import java.awt.event.ActionEvent;
import javax.swing.JTextField;
import java.awt.FlowLayout;
import javax.swing.border.EtchedBorder;
import javax.swing.event.ListDataListener;
import javax.swing.JCheckBox;
import javax.swing.JComboBox;

public class InstallOptionsOverviewWindow {

	private JDialog frame;

	private JTextField textFieldInstallDir;
	private BufferedImage img;

	private String instDirName;

	private File installDir;
	private String initialInstallDir;

	private boolean lock;
	private boolean createShortcutDesktop = true;
	private boolean createStartMenu = true;

	private boolean useWine = false;
	private boolean preferProton = false;
	private boolean bundledSupported = false;
	private boolean useBundled = false;
	private boolean supportShortcutDesktop = false;

	private WineInstallation[] wineInstalls;
	private WineInstallation selectedWine;

	private WineInstallation selectedWinePrev = selectedWine;

	public boolean useBundledWine() {
		return useBundled;
	}

	public boolean preferProton() {
		return preferProton;
	}

	public WineInstallation getSelectedWine() {
		return selectedWine;
	}

	public File getInstallPath() {
		return installDir;
	}

	public boolean createShortcutDesktop() {
		return createShortcutDesktop;
	}

	public boolean createStartMenu() {
		return createStartMenu;
	}

	public String getInstallationPath() {
		return textFieldInstallDir.getText();
	}

	public InstallOptionsOverviewWindow(JFrame parent, boolean preferProton, WineInstallation selectedWine,
			WineInstallation[] wineInstalls, boolean useWine, boolean bundledWine, boolean useBundled,
			BufferedImage img, boolean lock, String installDir, String instDirName) {
		this(parent, preferProton, selectedWine, wineInstalls, useWine, bundledWine, useBundled, img, lock, installDir,
				instDirName,
				true, true, true);
	}

	public InstallOptionsOverviewWindow(JFrame parent, boolean preferProton, WineInstallation selectedWine,
			WineInstallation[] wineInstalls, boolean useWine, boolean bundledWine, boolean useBundled,
			BufferedImage img, boolean lock, String installDir, String instDirName, boolean createShortcutDesktop,
			boolean createStartMenu,
			boolean supportShortcutDesktop) {
		try {
			this.initialInstallDir = installDir;
			this.instDirName = instDirName;
			this.selectedWine = selectedWine;
			this.preferProton = preferProton;
			this.useBundled = useBundled;
			this.wineInstalls = wineInstalls;
			this.useWine = useWine;
			this.bundledSupported = bundledWine;
			this.lock = lock;
			this.img = img;
			this.supportShortcutDesktop = supportShortcutDesktop;
			this.createShortcutDesktop = createShortcutDesktop;
			this.createStartMenu = createStartMenu;
			EventQueue.invokeAndWait(new Runnable() {
				public void run() {
					initialize(parent);
					textFieldInstallDir.setText(installDir);
					frame.setLocationRelativeTo(parent);
					frame.setModal(true);
					frame.setVisible(true);
				}
			});
		} catch (InvocationTargetException | InterruptedException e) {
			e.printStackTrace();
		}
	}

	/**
	 * @wbp.parser.entryPoint
	 */
	private void initialize(JFrame parent) {
		frame = new JDialog(parent);
		frame.setBounds(100, 100, 771, 436);
		frame.setDefaultCloseOperation(JFrame.DISPOSE_ON_CLOSE);
		frame.setResizable(false);
		frame.setLocationRelativeTo(null);
		frame.setTitle("Installation Options");

		JLabel lblTitle = new JLabel("Installation Options");
		lblTitle.setPreferredSize(new Dimension(326, 30));
		lblTitle.setFont(new Font("Dialog", Font.BOLD, 20));
		lblTitle.setHorizontalAlignment(SwingConstants.CENTER);
		frame.getContentPane().add(lblTitle, BorderLayout.NORTH);

		JPanel panel = new JPanel();
		panel.setBorder(new BevelBorder(BevelBorder.LOWERED, null, null, null, null));
		frame.getContentPane().add(panel, BorderLayout.CENTER);
		panel.setLayout(new FlowLayout(FlowLayout.CENTER, 5, 5));

		JPanel panel_2 = new JPanel();
		panel_2.setBorder(new BevelBorder(BevelBorder.LOWERED, null, null, null, null));
		panel_2.setPreferredSize(new Dimension(740, 290));
		panel_2.setLayout(null);

		JLabel lblNewLabel = new JLabel("Installation path");
		lblNewLabel.setBounds(324, 26, 382, 15);
		panel_2.add(lblNewLabel);

		BackgroundPanel imagePanel = new BackgroundPanel();
		imagePanel.setBorder(new EtchedBorder(EtchedBorder.LOWERED, null, null));
		imagePanel.setSize(280, 280);
		imagePanel.setLocation(5, 5);
		imagePanel.setLayout(null);
		imagePanel.setImage(img);
		panel_2.add(imagePanel);

		textFieldInstallDir = new JTextField();
		textFieldInstallDir.setBounds(322, 47, 248, 27);
		panel_2.add(textFieldInstallDir);
		textFieldInstallDir.setColumns(10);
		if (lock)
			textFieldInstallDir.setEnabled(false);

		JButton buttonBrowse = new JButton();
		buttonBrowse.addActionListener(new ActionListener() {
			public void actionPerformed(ActionEvent e) {
				// Check
				if (lock) {
					if (JOptionPane.showConfirmDialog(frame,
							"You have already installed the launcher before!\n\nChanging the path would forcefully move the launcher to the new location, are you sure you wish to continue?",
							"Previous installation present", JOptionPane.YES_NO_OPTION,
							JOptionPane.WARNING_MESSAGE) != JOptionPane.YES_OPTION)
						return;
					lock = false;
					textFieldInstallDir.setEnabled(true);
					buttonBrowse.setText("Browse...");
					JOptionPane.showMessageDialog(frame,
							"The installation target folder has been unlocked, you can now pick another folder for installation.",
							"Invalid folder", JOptionPane.INFORMATION_MESSAGE);
					return;
				}

				// Prompt selection
				JFileChooser chooser = new JFileChooser();
				chooser.setFileSelectionMode(JFileChooser.DIRECTORIES_ONLY);
				chooser.setDialogTitle("Select installation directory...");
				if (chooser.showOpenDialog(frame) != JFileChooser.APPROVE_OPTION)
					return;

				// Check file
				if (!chooser.getSelectedFile().exists() || chooser.getSelectedFile().isFile()) {
					JOptionPane.showMessageDialog(frame, "The folder you selected is not valid or does not exist.",
							"Invalid folder", JOptionPane.ERROR_MESSAGE);
					return;
				}

				// Assign
				try {
					String pth = chooser.getSelectedFile().getCanonicalPath();
					if (!chooser.getSelectedFile().getName().equals(instDirName)) {
						if (!pth.endsWith(File.separator) && !pth.endsWith("/"))
							pth += File.separator;
						pth += instDirName;
						new File(pth).mkdirs();
					}
					textFieldInstallDir.setText(pth);
				} catch (IOException e1) {
				}
			}
		});
		buttonBrowse.setText("Browse...");
		if (lock)
			buttonBrowse.setText("Change...");
		buttonBrowse.setBounds(582, 47, 124, 27);
		panel_2.add(buttonBrowse);

		JCheckBox chckbxAppMenu = new JCheckBox("Create application menu entry");
		chckbxAppMenu.setBounds(323, 79, 383, 32);
		chckbxAppMenu.setSelected(createStartMenu);
		panel_2.add(chckbxAppMenu);

		JCheckBox chckbxDesktop = new JCheckBox("Create desktop shortcut");
		chckbxDesktop.setBounds(323, 111, 383, 32);
		chckbxDesktop.setSelected(createShortcutDesktop);
		panel_2.add(chckbxDesktop);
		chckbxDesktop.setVisible(supportShortcutDesktop);

		JPanel panel_3 = new JPanel();
		panel_3.setBounds(320, 154, 386, 133);
		panel_2.add(panel_3);
		panel_3.setLayout(null);
		if (!useWine)
			panel_3.setVisible(false);

		JLabel lblNewLabel_1 = new JLabel("Wine version to use");
		lblNewLabel_1.setBounds(5, -2, 381, 28);
		panel_3.add(lblNewLabel_1);

		// Populate wine list
		ArrayList<WineInstallation> realWineInstalls = new ArrayList<WineInstallation>();
		WineInstallation wineAuto = new WineInstallation("<auto>", "<auto>", "Use automatic selection...", false, true);
		realWineInstalls.add(wineAuto);
		for (WineInstallation wine : wineInstalls) {
			// Add
			realWineInstalls.add(wine);
		}
		WineInstallation winePick = new WineInstallation("<picker>", "<picker>", "Pick from folder...", true, true);
		realWineInstalls.add(winePick);

		// Select
		if (selectedWine != null) {
			// Find
			boolean found = false;
			for (WineInstallation wine : realWineInstalls) {
				if (selectedWine.isUserPicked == wine.isUserPicked && selectedWine.isAuto == wine.isAuto
						&& selectedWine.path.equals(wine.path)) {
					// Found
					selectedWine = wine;
					found = true;
					break;
				}
			}

			// Check user picked
			if (!found && selectedWine.isUserPicked) {
				// Just add it
				realWineInstalls.clear();
				realWineInstalls.add(wineAuto);
				for (WineInstallation wine : wineInstalls) {
					// Add
					realWineInstalls.add(wine);
				}
				realWineInstalls.add(selectedWine);
				realWineInstalls.add(winePick);
				found = true;
			}

			// Select auto as default
			if (!found)
				selectedWine = wineAuto;
		} else {
			// Select auto as default
			selectedWine = wineAuto;
		}

		JCheckBox chckbxPreferProton = new JCheckBox("Prefer proton over wine");
		chckbxPreferProton.setBounds(5, 67, 381, 32);
		chckbxPreferProton.setSelected(preferProton);
		panel_3.add(chckbxPreferProton);

		// Select
		selectedWinePrev = selectedWine;
		JComboBox<WineInstallation> comboBoxWine = new JComboBox<WineInstallation>();
		comboBoxWine.addActionListener(new ActionListener() {
			public void actionPerformed(ActionEvent e) {
				// Check selected
				if (selectedWine.isUserPicked && selectedWine.path.equals("<picker>")) {
					// Prompt selection
					comboBoxWine.setSelectedItem(selectedWinePrev);
					SwingUtilities.invokeLater(() -> {
						JFileChooser chooser = new JFileChooser();
						chooser.setFileSelectionMode(JFileChooser.DIRECTORIES_ONLY);
						chooser.setDialogTitle("Select wine folder...");
						if (chooser.showOpenDialog(frame) != JFileChooser.APPROVE_OPTION)
							return;

						// Check file
						if (!chooser.getSelectedFile().exists() || chooser.getSelectedFile().isFile()) {
							JOptionPane.showMessageDialog(frame,
									"The folder you selected is not valid or does not exist.", "Invalid folder",
									JOptionPane.ERROR_MESSAGE);
							return;
						}
						File winePath = chooser.getSelectedFile();
						WineInstallation.WineSearchResult res = WineInstallation.findWineInstallation(winePath,
								"User-selected wine: ");
						if (res.status == WineInstallation.WineSearchResultStatus.INVALID) {
							// Invalid
							JOptionPane.showMessageDialog(frame,
									"The folder you selected is not a valid wine installation, please make sure to select a folder with wine binaries.\n\nReason: unable to locate wineserver binary.",
									"Invalid folder", JOptionPane.ERROR_MESSAGE);
							return;
						} else if (res.status == WineInstallation.WineSearchResultStatus.QUERY_FAILED) {
							JOptionPane.showMessageDialog(frame,
									"The folder you selected is not a valid wine installation, please make sure to select a folder with wine binaries.\n\nReason: the wineserver binary could not be queried for wine version.",
									"Invalid folder", JOptionPane.ERROR_MESSAGE);
							return;
						} else if (res.status != WineInstallation.WineSearchResultStatus.SUCCESS) {
							// Invalid
							JOptionPane.showMessageDialog(frame,
									"The folder you selected is not a valid wine installation, please make sure to select a folder with wine binaries.\n\nReason: unknown error: "
											+ res.status + ".",
									"Invalid folder", JOptionPane.ERROR_MESSAGE);
							return;
						}

						// Update path
						winePath = res.winePath;

						// Add
						realWineInstalls.clear();
						realWineInstalls.add(wineAuto);
						for (WineInstallation wine : wineInstalls) {
							// Add
							realWineInstalls.add(wine);
						}
						realWineInstalls.add(res.installation);
						realWineInstalls.add(winePick);

						// Select
						selectedWine = res.installation;

						// Set model
						comboBoxWine.setModel(new ComboBoxModel<WineInstallation>() {

							@Override
							public int getSize() {
								return realWineInstalls.size();
							}

							@Override
							public WineInstallation getElementAt(int index) {
								return realWineInstalls.get(index);
							}

							@Override
							public void addListDataListener(ListDataListener l) {
							}

							@Override
							public void removeListDataListener(ListDataListener l) {
							}

							@Override
							public void setSelectedItem(Object anItem) {
								selectedWine = (WineInstallation) anItem;
							}

							@Override
							public Object getSelectedItem() {
								return selectedWine;
							}

						});
						comboBoxWine.setSelectedItem(selectedWine);
						chckbxPreferProton.setEnabled(selectedWine.isAuto);
						if (System.getenv("EMUFERAL_WINEDETECT_PREFER_PROTON") != null) {
							chckbxPreferProton.setEnabled(false);
							chckbxPreferProton.setSelected(
									System.getenv("EMUFERAL_WINEDETECT_PREFER_PROTON").equalsIgnoreCase("true"));
						}
					});
				}
				selectedWinePrev = selectedWine;
				chckbxPreferProton.setEnabled(selectedWine.isAuto);
				if (System.getenv("EMUFERAL_WINEDETECT_PREFER_PROTON") != null) {
					chckbxPreferProton.setEnabled(false);
					chckbxPreferProton
							.setSelected(System.getenv("EMUFERAL_WINEDETECT_PREFER_PROTON").equalsIgnoreCase("true"));
				}
			}
		});
		comboBoxWine.setBounds(5, 29, 381, 32);

		JCheckBox chckbxUseSystem = new JCheckBox("Use system wine");
		chckbxUseSystem.setBounds(5, 97, 381, 32);
		chckbxUseSystem.setSelected(!useBundled);
		panel_3.add(chckbxUseSystem);
		boolean supportBundledWine = new File("syslibs/bin/wine").exists()
				|| new File("installerdata/syslibs/bin/wine").exists()
				|| new File("installerdata/Contents/Resources/syslibs/bin/wine").exists();
		if (System.getenv("EMUFERAL_WINEDETECT_USE_BUNDLED") != null && supportBundledWine) {
			chckbxUseSystem.setEnabled(false);
			chckbxUseSystem.setSelected(!System.getenv("EMUFERAL_WINEDETECT_USE_BUNDLED").equalsIgnoreCase("true"));
		}
		if (System.getenv("EMUFERAL_WINEDETECT_PREFER_PROTON") != null) {
			chckbxPreferProton.setEnabled(false);
			chckbxPreferProton.setSelected(System.getenv("EMUFERAL_WINEDETECT_PREFER_PROTON").equalsIgnoreCase("true"));
		}
		if (!chckbxUseSystem.isSelected()) {
			// Disable
			comboBoxWine.setEnabled(false);
		}
		if (System.getenv("EMUFERAL_WINEDETECT_USE_AUTO") != null
				&& System.getenv("EMUFERAL_WINEDETECT_USE_AUTO").equalsIgnoreCase("true")) {
			chckbxUseSystem.setEnabled(false);
			chckbxUseSystem.setSelected(true);
			comboBoxWine.setEnabled(false);
			selectedWine = wineAuto;
		} else if (System.getenv("EMUFERAL_WINEDETECT_SELECTED_WINE") != null) {
			String pathFragment = System.getenv("EMUFERAL_WINEDETECT_SELECTED_WINE");
			File path = new File(pathFragment);
			if (path.exists()) {
				// Get search result
				WineInstallation.WineSearchResult res = WineInstallation.findWineInstallation(path,
						"User-selected wine: ");
				if (res.status == WineInstallation.WineSearchResultStatus.SUCCESS) {
					// Lock it
					chckbxUseSystem.setEnabled(false);
					chckbxUseSystem.setSelected(true);
					comboBoxWine.setEnabled(false);
					selectedWine = res.installation;

					// Add
					realWineInstalls.clear();
					realWineInstalls.add(wineAuto);
					for (WineInstallation wine : wineInstalls) {
						// Add
						realWineInstalls.add(wine);
					}
					realWineInstalls.add(selectedWine);
					realWineInstalls.add(winePick);
				}
			}
		}

		chckbxUseSystem.addActionListener(new ActionListener() {

			public void actionPerformed(ActionEvent e) {
				comboBoxWine.setEnabled(chckbxUseSystem.isSelected());
			}
		});

		// Set model
		comboBoxWine.setModel(new ComboBoxModel<WineInstallation>() {

			@Override
			public int getSize() {
				return realWineInstalls.size();
			}

			@Override
			public WineInstallation getElementAt(int index) {
				return realWineInstalls.get(index);
			}

			@Override
			public void addListDataListener(ListDataListener l) {
			}

			@Override
			public void removeListDataListener(ListDataListener l) {
			}

			@Override
			public void setSelectedItem(Object anItem) {
				selectedWine = (WineInstallation) anItem;
			}

			@Override
			public Object getSelectedItem() {
				return selectedWine;
			}

		});
		panel_3.add(comboBoxWine);

		// Check bundled support
		if (!bundledSupported) {
			useBundled = false;
			chckbxUseSystem.setVisible(false);
		}

		JPanel panel_1 = new JPanel();
		FlowLayout flowLayout = (FlowLayout) panel_1.getLayout();
		flowLayout.setAlignment(FlowLayout.RIGHT);
		frame.getContentPane().add(panel_1, BorderLayout.SOUTH);

		// Add panel
		panel.add(panel_2);

		JButton btnNewButton_1 = new JButton("Cancel");
		btnNewButton_1.addActionListener(new ActionListener() {
			public void actionPerformed(ActionEvent arg0) {
				frame.dispatchEvent(new WindowEvent(frame, WindowEvent.WINDOW_CLOSING));
			}
		});
		panel_1.add(btnNewButton_1);

		JButton btnNewButton = new JButton("Install");
		btnNewButton.addActionListener(new ActionListener() {
			public void actionPerformed(ActionEvent e) {
				// Check file
				installDir = new File(textFieldInstallDir.getText());
				if (!installDir.exists() || installDir.isFile()) {
					installDir = null;
					JOptionPane.showMessageDialog(frame, "The folder you selected is not valid or does not exist.",
							"Invalid folder", JOptionPane.ERROR_MESSAGE);
					return;
				}
				if (!initialInstallDir.equals(textFieldInstallDir.getText())) {
					if (!installDir.getName().equals(instDirName)) {
						String pth = textFieldInstallDir.getText();
						if (!pth.endsWith(File.separator) && !pth.endsWith("/"))
							pth += File.separator;
						pth += instDirName;
						installDir = new File(pth);
						installDir.mkdirs();
					}
				}
				useBundled = !chckbxUseSystem.isSelected();
				preferProton = chckbxPreferProton.isSelected();
				createStartMenu = chckbxAppMenu.isSelected();
				createShortcutDesktop = chckbxDesktop.isSelected();
				frame.dispatchEvent(new WindowEvent(frame, WindowEvent.WINDOW_CLOSING));
			}
		});
		panel_1.add(btnNewButton);

		frame.getRootPane().setDefaultButton(btnNewButton);
	}
}
