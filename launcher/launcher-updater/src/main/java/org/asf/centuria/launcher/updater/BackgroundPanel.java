package org.asf.centuria.launcher.updater;

import java.awt.Graphics;
import java.awt.Graphics2D;
import java.awt.RenderingHints;
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

		// Enable antialiasing
		Graphics2D gr = (Graphics2D) g;
		gr.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON);
		gr.setRenderingHint(RenderingHints.KEY_RENDERING, RenderingHints.VALUE_RENDER_QUALITY);

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
