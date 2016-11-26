namespace Chutzpah.Models
{
    public class ChutzpahWebServerConfiguration
    {
        public ChutzpahWebServerConfiguration()
        {

        }

        public ChutzpahWebServerConfiguration(ChutzpahWebServerConfiguration configurationToCopy)
        {
            Enabled = configurationToCopy.Enabled;
            DefaultPort = configurationToCopy.DefaultPort;
            RootPath = configurationToCopy.RootPath;
            FileCachingEnabled = configurationToCopy.FileCachingEnabled;
        }

        /// <summary>
        /// Determines if the web server is enabled for this chutzpah.json
        /// </summary>
        public bool? Enabled { get; set; }

        /// <summary>
        /// The default port to use. If this port is taken Chutzpah will try incrementing until it finds an available one.
        /// If a parent settings file has already set this it cannot be overriden. If not set, default to start at 9876
        /// </summary>
        public int? DefaultPort { get; set; }

        /// <summary>
        /// The root path of the server. All file paths are relative to this and should be in a directory below or equal to this.
        /// Defaults to settings file drive root path
        /// </summary>
        public string RootPath { get; set; }

        /// <summary>
        /// Set large experation on files served by appending a hash value on the urls. Defaults to true.
        /// </summary>
        public bool? FileCachingEnabled { get; set; }
    }   
}