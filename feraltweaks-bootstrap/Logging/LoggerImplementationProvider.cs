
namespace FeralTweaks.Logging {

    public interface ILoggerImplementationProvider
    {
        /// <summary>
        /// Creates a logger instance
        /// </summary>
        /// <param name="name">Logger name</param>
        /// <returns>New Logger instance</returns>
        public Logger CreateInstance(string name);
    }

}