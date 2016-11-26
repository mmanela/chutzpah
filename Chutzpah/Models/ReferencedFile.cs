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
            FrameworkReplacements = new Dictionary<string, string>();
            TemplateOptions = new TemplateOptions();
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
        /// Gets or sets the path to the map file that translates the generated file (if set)
        /// to its original source.
        /// </summary>
        public string SourceMapFilePath { get; set; }

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


        /// <summary>
        /// Determines if we should expand references to other files found as reference comments
        /// </summary>
        public bool ExpandReferenceComments { get; set; }

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

        /// <summary>
        /// A mapping of tokens and values to replace in the harness specific to this file
        /// </summary>
        public Dictionary<string, string> FrameworkReplacements { get; set; }

        public TemplateOptions TemplateOptions { get; set; }

        /// <summary>
        /// Determines if this is a file added by the built in frameworks
        /// </summary>
        public bool IsBuiltInDependency { get; set; }

        /// <summary>
        /// The path of the file when running from the server
        /// </summary>
        public string PathForUseInTestHarness { get; set; }

        public string AbsoluteServerUrl { get; set; }

        /// <summary>
        /// A cached hash string for the file user for user generation
        /// </summary>
        public string Hash { get; set; }
    }
}