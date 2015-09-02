using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah
{
    /// <summary>
    /// Options for code coverage.
    /// </summary>
    public class CoverageOptions
    {
        public CoverageOptions()
        {
            IncludePatterns = new List<string>();
            ExcludePatterns = new List<string>();
        }

        /// <summary>
        /// Whether or not code coverage collection is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// If specified, pattern of files to include in the instrumentation phase.
        /// </summary>
        public ICollection<string> IncludePatterns { get; set; }

        /// <summary>
        /// If specified, pattern of files to exclude from the instrumentation phase.
        /// </summary>
        public ICollection<string> ExcludePatterns { get; set; }

        public bool ShouldRunCoverage(CodeCoverageExecutionMode? coverageExecutionModeSetting)
        {
            // If not set or set to manual honor the passed in value from the user
            if (!coverageExecutionModeSetting.HasValue || coverageExecutionModeSetting.Value == CodeCoverageExecutionMode.Manual)
            {
                return Enabled;
            }
            else if (coverageExecutionModeSetting.HasValue && coverageExecutionModeSetting.Value == CodeCoverageExecutionMode.Always)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}