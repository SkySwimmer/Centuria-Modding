# The Centuria Modding Project
This is the repository for the official [Centuria](https://github.com/CPeers1/Centuria) modding project. This repository contains client modding code and server modules designed to patch up and modify parts of the original game.

Centuria is a work-in-progress server emulator for the now-defunct MMORPG Fer.al. The main server project can be found [here](https://github.com/CPeers1/Centuria). Centuria is developed by a group of developers from the Fer.ever discord. The software was originally released by the AerialWorks Software Foundation (SkySwimmer's small organization) but is now owned [Owenvii](https://github.com/CPeers1).

Note that this project is a HEAVY WIP and subject to change. Currently the launcher, server modules and android mod is not yet functional, only the desktop mod PARTIALLY works.

<br/>

# Building the client mods
The project is split up into multiple parts, `feraltweaks`, the launcher and the server modules needed for the mod handshake and mod download system.

<br/>

## Building the Desktop client mod (feraltweaks)
Feraltweaks is a BepInEx module, in order to build it you will need .NET cli installed.
You will also need to have a original fer.al client in order to mod it.

1. Firstly, you need a Fer.al client, you can download it yourself from the WildWorks servers or by using the one downloaded by the EmuFeral launcher.

2. If you do not have a Fer.al client, go to https://download.fer.al/win64/launcher.ini

3. Search for the line containing `ApplicationDownloadUrl`, open the link on that line in your browser.

4. After downloading, extract the `.7z` file and the client should be in the folder named `build`.

5. After that, you will need to install BepInEx on your Fer.al client, currently only BepInEx 6 (Bleeding Edge) build #577 works, newer builds fail due to a bug in BepInEx.

6. After the first run, BepInEx will have unhollowed the Fer.al client in `BepInEx/unhollowed`. These are the client assemblies, which are needed to build the mod, copy the contents of that folder to `feraltweaks/lib/feral` (you may need to create this folder)

7. Create the folder `run` in `feraltweaks`, and copy your client to it (make sure to include BepInEx while copying), it should end up looking like this:<br/>
-- feraltweaks<br/>
--- lib<br/>
---- feral<br/>
----- Assembly-CSharp.dll<br/>
----- Assembly-CSharp-firstpass.dll<br/>
----- etc...<br/>
--- run<br/>
---- BepInEx<br/>
---- Fer.al_Data<br/>
---- mono<br/>
---- Fer.al.exe<br/>
---- etc...<br/>

8. Run the following command in the root of the project
```
dotnet build
```

9. You can find the plugin at `feraltweaks/run/plugins/netstandard2.1/feraltweaks.dll`

<br/>

## Using the Desktop mod
After putting it in the `BepInEx/plugins/feraltweaks` (you may need to create this directory) folder of your client (needs BepInEx), it should load automatically. Note that the client mod doesn't have content, content is streamed (or will stream) from the  server.

You can however change client-specific options in `BepInEx/feraltweaks/settings.props` which contains some settings you may find useful. Note that client settings are overriden by the server when playing on a feral-tweaks enabled Centuria server.
