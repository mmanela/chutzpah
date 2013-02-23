namespace Chutzpah
{
    /// <summary>
    /// Options for code coverage.
    /// </summary>
    public class CoverageOptions
    {
        /// <summary>
        /// Whether or not code coverage collection is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// If specified, pattern of files to include in the instrumentation phase.
        /// </summary>
        public string IncludePattern { get; set; }

        /// <summary>
        /// If specified, pattern of files to exclude from the instrumentation phase.
        /// </summary>
        public string ExcludePattern { get; set; }
    }
}