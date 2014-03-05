using System;
using System.Collections.Generic;

namespace Chutzpah.Models
{
    public class ReferencedFile
    {
        public ReferencedFile()
        {
            FilePositions = new FilePositions();
            ReferencedFiles = new List<ReferencedFile>();
            IncludeInTestHarness = true;

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

        /// <summary>
        /// Gets if this is a dependency of the test framework
        /// </summary>
        public bool IsTestFrameworkFile { get; set; }

        /// <summary>
        /// Gets if this is a dependency of the code coverage framework
        /// </summary>
        public bool IsCodeCoverageDependency { get; set; }

        /// <summary>
        /// Should this reference be included into the test harness.
        /// </summary>
        public bool IncludeInTestHarness { get; set; }

        /// <summary>
        /// The path a AMD loader would use to find this file
        /// </summary>
        public string AmdFilePath { get; set; }

        /// <summary>
        /// The path a AMD loader would use to find the generated file
        /// </summary>
        public string AmdGeneratedFilePath { get; set; }

        public override int GetHashCode()
        {
            return Path.ToLowerInvariant().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var referencedFile = obj as ReferencedFile;
            if (referencedFile == null)
            {
                return false;
            }

            return Path != null && Path.Equals(referencedFile.Path, StringComparison.OrdinalIgnoreCase);
        }
    }
}