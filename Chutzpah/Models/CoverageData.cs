using System.Collections.Generic;

namespace Chutzpah.Models
{
    /// <summary>
    /// Coverage data is a dictionary that maps file paths to coverage data about a
    /// particular file.
    /// </summary>
    public class CoverageData : Dictionary<string, CoverageFileData>
    {
    }

    /// <summary>
    /// Contains coverage data for a specific file.
    /// </summary>
    public class CoverageFileData
    {
        /// <summary>
        /// The path to the file. Mostly for convenience.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Contains line execution counts for all source lines in the file. The array
        /// is 1-based, which means that the first item is always <c>null</c>. Lines not 
        /// considered executable by the coverage engine also have <c>null</c> as their
        /// values.
        /// </summary>
        public int?[] LineExecutionCounts { get; set; }

        /// <summary>
        /// Contains the converted source code of the test file if the test file wasn't plain JavaScript.
        /// Otherwise, contains <c>null</c>, in which case the path in <see cref="FilePath"/> can be
        /// used to obtain the source code. Unlike the <see cref="LineExecutionCounts"/> array, this array
        /// is 0-based, which means that the first item is the first line of the file.
        /// </summary>
        public string[] SourceLines { get; set; }
    }
}
