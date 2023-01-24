package org.asf.centuria.feraltweaks.http;

import java.io.File;
import java.io.FileInputStream;
import java.net.Socket;
import java.nio.file.Files;
import java.util.Base64;

import org.asf.centuria.Centuria;
import org.asf.centuria.accounts.AccountManager;
import org.asf.centuria.accounts.CenturiaAccount;
import org.asf.centuria.feraltweaks.FeralTweaksModule;
import org.asf.centuria.modules.ModuleManager;
import org.asf.rats.processors.HttpGetProcessor;

import com.google.gson.JsonArray;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

public class CdnProcessor extends HttpGetProcessor {

	@Override
	public HttpGetProcessor createNewInstance() {
		return new CdnProcessor();
	}

	@Override
	public String path() {
		return "/cdn";
	}

	@Override
	public void process(Socket client) {
		try {
			// Retrieve path
			String path = getRequest().path.substring(path().length());

			// Sanitize path
			if (path.contains("..")) {
				setResponseCode(403);
				setResponseMessage("Access to parent directories denied");
				return;
			}

			// Parse JWT payload
			if (!getRequest().headers.containsKey("Authorization")) {
				this.setResponseCode(401);
				this.setResponseMessage("Authorization Required");
				return;
			}
			String token = this.getHeader("Authorization").substring("Bearer ".length());
			if (token.isBlank()) {
				this.setResponseCode(403);
				this.setResponseMessage("Access denied");
				this.setBody("text/json", "{\"error\":\"invalid_credential\"}");
				return;
			}

			// Parse token
			if (token.isBlank()) {
				this.setResponseCode(403);
				this.setResponseMessage("Access denied");
				this.setBody("text/json", "{\"error\":\"invalid_credential\"}");
				return;
			}

			// Verify signature
			String verifyD = token.split("\\.")[0] + "." + token.split("\\.")[1];
			String sig = token.split("\\.")[2];
			if (!Centuria.verify(verifyD.getBytes("UTF-8"), Base64.getUrlDecoder().decode(sig))) {
				this.setResponseCode(403);
				this.setResponseMessage("Access denied");
				this.setBody("text/json", "{\"error\":\"invalid_credential\"}");
				return;
			}

			// Verify expiry
			JsonObject jwtPl = JsonParser
					.parseString(new String(Base64.getUrlDecoder().decode(token.split("\\.")[1]), "UTF-8"))
					.getAsJsonObject();
			if (!jwtPl.has("exp") || jwtPl.get("exp").getAsLong() < System.currentTimeMillis() / 1000) {
				this.setResponseCode(403);
				this.setResponseMessage("Access denied");
				this.setBody("text/json", "{\"error\":\"invalid_credential\"}");
				return;
			}

			JsonObject payload = JsonParser
					.parseString(new String(Base64.getUrlDecoder().decode(token.split("\\.")[1]), "UTF-8"))
					.getAsJsonObject();

			// Find account
			String id = payload.get("uuid").getAsString();

			// Check existence
			if (id == null) {
				// Invalid details
				this.setBody("text/json", "{\"error\":\"invalid_credential\"}");
				this.setResponseCode(422);
				return;
			}

			// Find account
			CenturiaAccount acc = AccountManager.getInstance().getAccount(id);
			if (acc == null) {
				this.setResponseCode(403);
				this.setResponseMessage("Access denied");
				this.setBody("text/json", "{\"error\":\"invalid_credential\"}");
				return;
			}

			// Check if FT is enabled
			FeralTweaksModule module = (FeralTweaksModule) ModuleManager.getInstance().getModule("feraltweaks");
			if (!module.enableByDefault && !acc.getSaveSharedInventory().containsItem("feraltweaks")) {
				// No access
				this.setResponseCode(403);
				this.setResponseMessage("Access denied");
				this.setBody("text/json", "{\"error\":\"feraltweaks_not_enabled\"}");
				return;
			}

			// Check file
			File reqFile = new File(module.ftCdnPath, path);
			if ((!reqFile.exists() || (!reqFile.getParentFile().getCanonicalPath()
					.equalsIgnoreCase(new File(module.ftCdnPath).getCanonicalPath())
					&& !reqFile.getParentFile().getCanonicalPath().toLowerCase()
							.startsWith(new File(module.ftCdnPath).getCanonicalPath().toLowerCase() + File.separator)))
					&& !path.equals("/feraltweaks/chartpatches/index.json")) {
				this.setResponseCode(404);
				this.setResponseMessage("Not found");
				this.setBody("text/json", "{\"error\":\"file_not_found\"}");
				return;
			}

			// Check feraltweaks requests
			if (path.equals("/feraltweaks/settings.props")) {
				// Append to the settings properties file
				String res = Files.readString(reqFile.toPath()).replace("\r", "");
				res = res.replace("${server:version}", Centuria.SERVER_UPDATE_VERSION);

				// Add replication
				for (String obj : module.replicatingObjects.keySet()) {
					res += "OverrideReplicate-" + obj + "=" + (module.replicatingObjects.get(obj) ? "True" : "False") + "\n";
				}

				getResponse().setResponseStatus(200, "OK");
				getResponse().setContent("text/plain", res);
				return;
			} else if (path.equals("/feraltweaks/chartpatches/index.json")) {
				// Index json
				JsonArray res = new JsonArray();
				scan(new File(module.ftCdnPath, "feraltweaks/chartpatches"), res, "/feraltweaks/chartpatches/");
				getResponse().setResponseStatus(200, "OK");
				getResponse().setContent("text/json", res.toString());
				return;
			}

			// Set content
			getResponse().setResponseStatus(200, "OK");
			getResponse().setContent(MainFileMap.getInstance().getContentType(reqFile.getName()),
					new FileInputStream(reqFile));
		} catch (Exception e) {
			setResponseCode(500);
			setResponseMessage("Internal Server Error");
			Centuria.logger.error(getRequest().path + " failed: 500: Internal Server Error", e);
		}
	}

	private void scan(File source, JsonArray res, String prefix) {
		for (File f : source.listFiles()) {
			if (f.isDirectory())
				scan(f, res, prefix + f.getName() + "/");
			else
				res.add(prefix + f.getName());
		}
	}

	@Override
	public boolean supportsChildPaths() {
		return true;
	}

}
