using System.Diagnostics;

namespace Chutzpah.Models
{
    /// <summary>
    /// Contains information about a path like full path and type
    /// </summary>
    [DebuggerDisplay("{FullPath}")]
    public class PathInfo
    {
        /// <summary>
        /// The type of the path (e.g Folder, JavaScript, Html, etc...)
        /// </summary>
        public PathType Type { get; set; }
        
        /// <summary>
        /// The full path. This could be null if the path doesnt exist
        /// </summary>
        public string FullPath { get; set; }
        
        /// <summary>
        /// The source/raw path
        /// </summary>
        public string Path { get; set; }
    }
}