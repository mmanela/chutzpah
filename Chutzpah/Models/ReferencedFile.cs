using System.Collections.Generic;

namespace Chutzpah.Models
{
    public class ReferencedFile
    {
        public ReferencedFile()
        {
            FilePositions = new FilePositions();
            ReferencedFiles = new List<ReferencedFile>();
        }

        public bool IsFileUnderTest { get; set; }

        /// <summary>
        /// The path to the reference file
        /// </summary>
        public string Path { get; set; }

        public bool IsLocal { get; set; }
        public FilePositions FilePositions { get; set; }
        public IList<ReferencedFile> ReferencedFiles { get; set; }

        /// <summary>
        /// This is a path the the generated version of this referenced file. 
        /// This will be used when a file is in a different language like CoffeeScript
        /// </summary>
        public string GeneratedFilePath { get; set; }
    }
}