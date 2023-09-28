using System.Collections.Generic;
using System.Linq;
using feraltweaks.Patches.AssemblyCSharp;

namespace FeralTweaks.Mods
{
    /// <summary>
    /// FeralTweaks server information
    /// </summary>
    public static class FeralTweaksServer
    {
        /// <summary>
        /// Checks if the client is connected to a modded server (returns true if on a FT or FT-compatible server, false if using a vanilla/unmodified emulator)
        /// </summary>
        public static bool IsModded
        {
            get
            {
                return LoginLogoutPatches.serverSoftwareName != "fer.al";
            }
        }

        /// <summary>
        /// Retrieves the server software name
        /// </summary>
        public static string SoftwareName
        {
            get
            {
                return LoginLogoutPatches.serverSoftwareName;
            }
        }

        /// <summary>
        /// Retrieves the server software version
        /// </summary>
        public static string SoftwareVersion
        {
            get
            {
                return LoginLogoutPatches.serverSoftwareVersion;
            }
        }

        /// <summary>
        /// Retrieves all server mod IDs
        /// </summary>
        public static string[] ServerModIDs
        {
            get
            {
                return LoginLogoutPatches.serverMods.Keys.ToArray();
            }
        }

        /// <summary>
        /// Checks if a server mod is loaded
        /// </summary>
        /// <param name="id">Mod ID</param>
        /// <returns>True if loaded, false otherwise</returns>
        public static bool IsModLoaded(string id)
        {
            return LoginLogoutPatches.serverMods.ContainsKey(id);
        }

        /// <summary>
        /// Retrieves versions of serve rmods
        /// </summary>
        /// <param name="id">Mod ID</param>
        /// <returns>Mod version or null if not present on server</returns>
        public static string GetModVersion(string id)
        {
            if (!IsModLoaded(id))
                return null;
            return LoginLogoutPatches.serverMods[id];
        }
    }
}