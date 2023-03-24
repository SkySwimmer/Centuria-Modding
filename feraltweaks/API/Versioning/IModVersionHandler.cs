using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeralTweaks.Versioning
{
    /// <summary>
    /// Interface for handling mod version handshake rules
    /// </summary>
    public interface IModVersionHandler
    {
        /// <summary>
        /// Retrieves the server mod handshake rules
        /// </summary>
        /// <returns>Map of handshake rules expected for server mods (id and version check string pairs)</returns>
        public Dictionary<string, string> GetServerModVersionRules();
    }
}
