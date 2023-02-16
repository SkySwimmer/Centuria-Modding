package org.asf.centuria.launcher.updater;

import java.awt.Graphics;
import java.awt.image.BufferedImage;

import javax.swing.JPanel;

public class BackgroundPanel extends JPanel {

	/**
	 * 
	 */
	private static final long serialVersionUID = 1L;
	private BufferedImage img;

	public void setImage(BufferedImage img) {
		this.img = img;
	}

	@Override
	public void paintComponent(Graphics g) {
		super.paintComponent(g);
		if (img == null)
			return;
		// Calculate sizes
		double wrh = (double) img.getWidth() / (double) img.getHeight();
		int newWidth = getWidth();
		int newHeight = (int) (newWidth / wrh);
		if (newHeight > getHeight()) {
			newHeight = getHeight();
			newWidth = (int) (newHeight * wrh);
		}

		// Calculate offset
		int offX = getWidth() - newWidth;
		int offY = getHeight() - newHeight;

		// Draw
		g.drawImage(img, offX / 2, offY / 2, newWidth, newHeight, null);
	}

}
