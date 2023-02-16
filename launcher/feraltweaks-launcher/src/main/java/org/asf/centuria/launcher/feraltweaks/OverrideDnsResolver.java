package org.asf.centuria.launcher.feraltweaks;

import java.net.InetAddress;
import java.net.UnknownHostException;

import org.apache.hc.client5.http.SystemDefaultDnsResolver;

public class OverrideDnsResolver extends SystemDefaultDnsResolver {

	private String host;
	private String ip;

	public OverrideDnsResolver(String host, String ip) {
		this.host = host;
		this.ip = ip;
	}

	@Override
	public InetAddress[] resolve(String host) throws UnknownHostException {
		// Check host
		if (host.equals(this.host)) {
			try {
				return new InetAddress[] { InetAddress.getByName(ip) };
			} catch (Exception e) {
			}
		}

		return super.resolve(host);
	}

}