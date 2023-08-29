package org.asf.centuria.feraltweaks.networking.http;

import java.io.File;
import java.io.FileInputStream;
import java.io.InputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.file.Files;
import org.asf.centuria.Centuria;
import org.asf.centuria.feraltweaks.FeralTweaksModule;
import org.asf.centuria.modules.ModuleManager;
import org.asf.connective.NetworkedConnectiveHttpServer;
import org.asf.connective.RemoteClient;
import org.asf.connective.TlsSecuredHttpServer;
import org.asf.connective.processors.HttpRequestProcessor;

import com.google.gson.Gson;
import com.google.gson.JsonArray;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

public class DataProcessor extends HttpRequestProcessor {

	@Override
	public HttpRequestProcessor createNewInstance() {
		return new DataProcessor();
	}

	@Override
	public String path() {
		return "/data";
	}

	@Override
	public void process(String pth, String method, RemoteClient client) {
		try {
			// Retrieve path
			String path = getRequest().getRequestPath().substring(path().length());

			// Sanitize path
			if (path.contains("..")) {
				setResponseStatus(403, "Access to parent directories denied");
				return;
			}

			// Retrieve module
			FeralTweaksModule module = (FeralTweaksModule) ModuleManager.getInstance().getModule("feraltweaks");

			// Check file
			File reqFile = new File(module.ftDataPath, path);
			if (reqFile.isDirectory()) {
				this.setResponseStatus(404, "Not found");
				this.setResponseContent("text/json", "{\"error\":\"file_not_found\"}");
				return;
			}
			if ((!reqFile.exists() || (!reqFile.getParentFile().getCanonicalPath()
					.equalsIgnoreCase(new File(module.ftDataPath).getCanonicalPath())
					&& !reqFile.getParentFile().getCanonicalPath().toLowerCase()
							.startsWith(new File(module.ftDataPath).getCanonicalPath().toLowerCase() + File.separator)))
					&& !path.equals("/feraltweaks/chartpatches/index.json")
					&& !path.equals("/feraltweaks/settings.props") && !path.equals("/clientmods/assemblies/index.json")
					&& !path.equals("/clientmods/assets/index.json") && !path.equals("/server.json")) {

				// Check if its a index json request of a different folder
				if (path.endsWith("/index.json")) {
					// Find path
					File parent = reqFile.getParentFile();
					if (parent.exists() && parent.isDirectory() && !parent.equals(new File(module.ftDataPath))) {
						// Handle
						JsonArray res = new JsonArray();
						scan(parent, res, path.substring(0, path.lastIndexOf("index.json")));
						getResponse().setResponseStatus(200, "OK");
						getResponse().setContent("text/json", res.toString());
						return;
					}
				}

				this.setResponseStatus(404, "Not found");
				this.setResponseContent("text/json", "{\"error\":\"file_not_found\"}");
				return;
			}

			// Check feraltweaks requests
			if (path.equals("/feraltweaks/settings.props")) {
				// Append to the settings properties file
				String res = "";
				if (reqFile.exists())
					res = Files.readString(reqFile.toPath()).replace("\r", "");
				res = res.replace("${server:version}", Centuria.SERVER_UPDATE_VERSION);

				// Add replication
				for (String obj : module.replicatingObjects.keySet()) {
					res += "OverrideReplicate-" + obj + "=" + (module.replicatingObjects.get(obj) ? "True" : "False")
							+ "\n";
				}
				res += "ServerVersion=" + module.modDataVersion + "/" + Centuria.SERVER_UPDATE_VERSION + "\n";

				getResponse().setResponseStatus(200, "OK");
				getResponse().setContent("text/plain", res);
				return;
			} else if (path.equals("/feraltweaks/chartpatches/index.json")) {
				// Index json
				JsonArray res = new JsonArray();
				scan(new File(module.ftDataPath, "feraltweaks/chartpatches"), res, "/feraltweaks/chartpatches/");
				getResponse().setResponseStatus(200, "OK");
				getResponse().setContent("text/json", res.toString());
				return;
			} else if (path.equals("/clientmods/assemblies/index.json")) {
				// Index json
				JsonObject res = new JsonObject();
				scan(new File(module.ftDataPath, "clientmods/assemblies"), res, "/clientmods/assemblies/", "/");
				getResponse().setResponseStatus(200, "OK");
				getResponse().setContent("text/json", res.toString());
				return;
			} else if (path.equals("/clientmods/assets/index.json")) {
				// Index json
				JsonObject res = new JsonObject();
				scan(new File(module.ftDataPath, "clientmods/assets"), res, "/clientmods/assets/", "/");
				getResponse().setResponseStatus(200, "OK");
				getResponse().setContent("text/json", res.toString());
				return;
			}

			// Handle server.json
			if (path.equals("/server.json")) {
				// Create cache
				File cacheDir = new File("cache/feraltweaks");
				cacheDir.mkdirs();

				// Attempt to update upstream
				JsonObject serverInfo = new JsonObject();
				boolean successfulUpdate = false;
				if (!module.upstreamServerJsonURL.isEmpty() && !module.upstreamServerJsonURL.equals("undefined")
						&& !module.upstreamServerJsonURL.equals("disabled")) {
					try {
						HttpURLConnection conn = (HttpURLConnection) new URL(module.upstreamServerJsonURL)
								.openConnection();
						InputStream strm = conn.getInputStream();
						String data = new String(strm.readAllBytes(), "UTF-8");
						Files.writeString(new File(cacheDir, "upstream.server.json").toPath(), data);
						serverInfo = JsonParser.parseString(data).getAsJsonObject();
						successfulUpdate = true;
					} catch (Exception e) {
					}
				}

				if (!successfulUpdate) {
					// Load existing
					try {
						File serverFile = new File(cacheDir, "upstream.server.json");
						if (serverFile.exists())
							serverInfo = JsonParser.parseString(Files.readString(serverFile.toPath()))
									.getAsJsonObject();
					} catch (Exception e) {
					}
				}

				// Generate data
				JsonObject serverBlock = createOrGetJsonObject(serverInfo, "server");
				JsonObject hosts = createOrGetJsonObject(serverBlock, "hosts");
				hosts.addProperty("director",
						((Centuria.directorServer instanceof TlsSecuredHttpServer) ? "https" : "http") + "://"
								+ Centuria.discoveryAddress + ":"
								+ ((NetworkedConnectiveHttpServer) Centuria.directorServer).getListenPort() + "/");
				hosts.addProperty("api",
						((this.getServer() instanceof TlsSecuredHttpServer) ? "https" : "http") + "://"
								+ Centuria.discoveryAddress + ":"
								+ ((NetworkedConnectiveHttpServer) this.getServer()).getListenPort() + "/");
				hosts.addProperty("chat", Centuria.discoveryAddress);
				hosts.addProperty("voiceChat", Centuria.discoveryAddress);
				JsonObject ports = createOrGetJsonObject(serverBlock, "ports");
				ports.addProperty("game", Centuria.gameServer.getServerSocket().getLocalPort());
				ports.addProperty("chat", Centuria.chatServer.getServerSocket().getLocalPort());
				ports.addProperty("voiceChat", -1);
				ports.addProperty("bluebox", -1);
				serverBlock.addProperty("encryptedGame", Centuria.encryptGame);
				serverBlock.addProperty("encryptedChat", Centuria.encryptChat);
				serverBlock.addProperty("modVersion", module.modDataVersion);
				serverBlock.addProperty("assetVersion", module.modDataVersion);

				// Load existing data over it
				if (reqFile.exists()) {
					try {
						JsonObject localServerInfo = JsonParser.parseString(Files.readString(reqFile.toPath()))
								.getAsJsonObject();

						// Load existing data over the server info block
						mergeObject(localServerInfo, serverInfo);
					} catch (Exception e) {
					}
				}

				// Set content
				getResponse().setResponseStatus(200, "OK");
				getResponse().setContent("application/json",
						new Gson().newBuilder().setPrettyPrinting().create().toJson(serverInfo));
				return;
			}

			// Set content
			getResponse().setResponseStatus(200, "OK");
			getResponse().setContent(MainFileMap.getInstance().getContentType(reqFile.getName()),
					new FileInputStream(reqFile), reqFile.length());
		} catch (Exception e) {
			setResponseStatus(500, "Internal Server Error");
			Centuria.logger.error(getRequest().getRequestPath() + " failed: 500: Internal Server Error", e);
		}
	}

	private void mergeObject(JsonObject source, JsonObject target) {
		// Merge each entry
		for (String key : source.keySet()) {
			if (source.get(key).isJsonObject()) {
				JsonObject chT = createOrGetJsonObject(target, key);
				mergeObject(source.get(key).getAsJsonObject(), chT);
			} else
				target.add(key, source.get(key));
		}
	}

	private JsonObject createOrGetJsonObject(JsonObject obj, String name) {
		if (obj.has(name))
			return obj.get(name).getAsJsonObject();
		JsonObject res = new JsonObject();
		obj.add(name, res);
		return res;
	}

	private void scan(File source, JsonArray res, String prefix) {
		for (File f : source.listFiles()) {
			if (f.isDirectory())
				scan(f, res, prefix + f.getName() + "/");
			else
				res.add(prefix + f.getName());
		}
	}

	private void scan(File source, JsonObject res, String prefix, String prefixOut) {
		for (File f : source.listFiles()) {
			if (f.isDirectory())
				scan(f, res, prefix + f.getName() + "/", prefixOut + f.getName() + "/");
			else
				res.addProperty(prefix + f.getName(), prefixOut + f.getName());
		}
	}

	@Override
	public boolean supportsChildPaths() {
		return true;
	}

}
