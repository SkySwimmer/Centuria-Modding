package org.asf.centuria.launcher.feraltweaks;

import javax.swing.JFrame;
import javax.swing.JLabel;
import java.awt.BorderLayout;
import java.awt.Color;

import javax.swing.SwingConstants;
import javax.swing.SwingUtilities;

import java.awt.Font;
import java.awt.Dimension;
import java.awt.EventQueue;

import javax.swing.JPanel;
import javax.swing.JPasswordField;
import javax.swing.JButton;
import javax.swing.JDialog;
import javax.swing.border.BevelBorder;

import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

import java.awt.event.ActionListener;
import java.awt.event.WindowEvent;
import java.lang.reflect.InvocationTargetException;
import java.net.HttpURLConnection;
import java.net.URL;
import java.awt.event.ActionEvent;
import javax.swing.JTextField;
import java.awt.FlowLayout;

public class LoginWindow {

	private JDialog frame;
	private String api;
	private JTextField textField;
	private JPasswordField textField_1;
	private JLabel lblNewLabel_2;
	private AccountHolder info = null;

	public AccountHolder getAccount() {
		return info;
	}

	public LoginWindow(JFrame parent, String api) {
		this(parent, api, "");
	}

	public LoginWindow(JFrame parent, String api, String startingUsername) {
		this(parent, api, startingUsername, "");
	}

	public LoginWindow(JFrame parent, String api, String startingUsername, String startingError) {
		try {
			this.api = api;
			EventQueue.invokeAndWait(new Runnable() {
				public void run() {
					initialize(parent);
					textField.setText(startingUsername);
					lblNewLabel_2.setText(startingError);
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
		frame.setBounds(100, 100, 671, 336);
		frame.setDefaultCloseOperation(JFrame.DISPOSE_ON_CLOSE);
		frame.setResizable(false);
		frame.setLocationRelativeTo(null);
		frame.setTitle("Player Login");

		JLabel lblTitle = new JLabel("Player Login");
		lblTitle.setPreferredSize(new Dimension(326, 30));
		lblTitle.setFont(new Font("Dialog", Font.BOLD, 20));
		lblTitle.setHorizontalAlignment(SwingConstants.CENTER);
		frame.getContentPane().add(lblTitle, BorderLayout.NORTH);

		JPanel panel = new JPanel();
		panel.setBorder(new BevelBorder(BevelBorder.LOWERED, null, null, null, null));
		frame.getContentPane().add(panel, BorderLayout.CENTER);
		panel.setLayout(new FlowLayout(FlowLayout.CENTER, 5, 5));

		JPanel panel_2 = new JPanel();
		panel_2.setPreferredSize(new Dimension(600, 195));
		panel_2.setLayout(null);

		textField = new JTextField();
		textField.setBounds(10, 65, 580, 19);
		panel_2.add(textField);
		textField.setColumns(10);

		JLabel lblNewLabel = new JLabel("Login name");
		lblNewLabel.setBounds(10, 44, 580, 15);
		panel_2.add(lblNewLabel);

		JLabel lblNewLabel_1 = new JLabel("Password");
		lblNewLabel_1.setBounds(10, 90, 580, 15);
		panel_2.add(lblNewLabel_1);

		textField_1 = new JPasswordField();
		textField_1.setBounds(10, 111, 580, 19);
		panel_2.add(textField_1);
		textField_1.setColumns(10);

		lblNewLabel_2 = new JLabel("");
		lblNewLabel_2.setForeground(Color.RED);
		lblNewLabel_2.setBounds(10, 142, 580, 42);
		panel_2.add(lblNewLabel_2);
		panel.add(panel_2);

		JPanel panel_1 = new JPanel();
		frame.getContentPane().add(panel_1, BorderLayout.SOUTH);

		JButton btnNewButton_1 = new JButton("Cancel");
		btnNewButton_1.addActionListener(new ActionListener() {
			public void actionPerformed(ActionEvent arg0) {
				frame.dispatchEvent(new WindowEvent(frame, WindowEvent.WINDOW_CLOSING));
			}
		});
		panel_1.add(btnNewButton_1);

		JButton btnNewButton = new JButton("Login");
		btnNewButton.addActionListener(new ActionListener() {
			public void actionPerformed(ActionEvent arg0) {
				info = null;
				lblNewLabel_2.setText("");
				textField.setEnabled(false);
				textField_1.setEnabled(false);
				if (!textField.getText().isEmpty()) {
					Thread th = new Thread(() -> {
						// Contact API
						try {
							String api = LoginWindow.this.api;
							if (!api.endsWith("/"))
								api += "/";
							HttpURLConnection conn = (HttpURLConnection) new URL(api + "a/authenticate")
									.openConnection();
							conn.setDoOutput(true);
							JsonObject payload = new JsonObject();
							payload.addProperty("username", textField.getText());
							payload.addProperty("password", new String(textField_1.getPassword()));
							conn.setRequestMethod("POST");
							conn.getOutputStream().write(payload.toString().getBytes("UTF-8"));
							if (conn.getResponseCode() == 200) {
								// Read response
								String data = new String(conn.getInputStream().readAllBytes(), "UTF-8");
								JsonObject resp = JsonParser.parseString(data).getAsJsonObject();
								String auth = resp.get("auth_token").getAsString();
								String refresh = resp.get("refresh_token").getAsString();

								// Set info
								info = new AccountHolder();
								info.accountID = resp.get("uuid").getAsString();
								info.accountName = textField.getText();
								info.authToken = auth;
								info.refreshToken = refresh;

								// Close
								frame.dispatchEvent(new WindowEvent(frame, WindowEvent.WINDOW_CLOSING));
							} else {
								// Check error
								String data = new String(conn.getErrorStream().readAllBytes(), "UTF-8");
								JsonObject resp = JsonParser.parseString(data).getAsJsonObject();
								switch (resp.get("error").getAsString()) {
								case "invalid_credential": {
									SwingUtilities.invokeLater(() -> {
										lblNewLabel_2.setText("Invalid credentials.");
										textField.setEnabled(true);
										textField_1.setEnabled(true);
										textField.requestFocus();
									});
									break;
								}
								default: {
									SwingUtilities.invokeLater(() -> {
										lblNewLabel_2
												.setText("An unknown server error occured, please contact support.");
										textField.setEnabled(true);
										textField_1.setEnabled(true);
										textField.requestFocus();
									});
									break;
								}
								}
							}
						} catch (Exception e) {
							SwingUtilities.invokeLater(() -> {
								lblNewLabel_2.setText(
										"An unknown error occured, please check your internet connection. If the error persists, please open a support ticket");
								textField.setEnabled(true);
								textField_1.setEnabled(true);
								textField.requestFocus();
							});
						}
					});
					th.setDaemon(true);
					th.start();
				} else if (textField.getText().isEmpty()) {
					lblNewLabel_2.setText("Missing login name!");
					textField.setEnabled(true);
					textField_1.setEnabled(true);
					textField.requestFocus();
				}
			}
		});
		panel_1.add(btnNewButton);

		frame.getRootPane().setDefaultButton(btnNewButton);
	}
}
