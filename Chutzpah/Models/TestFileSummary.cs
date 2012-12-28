namespace Chutzpah.Models
{
    /// <summary>
    /// Summary of the test cases in one file
    /// </summary>
    public class TestFileSummary : BaseTestCaseSummary
    {
        public TestFileSummary(string path)
        {
            Path = path;
        }

        /// <summary>
        /// The file path the test file summary is for
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// If set, contains the coverage object created during coverage collection.
        /// </summary>
        public CoverageData CoverageObject { get; set; }
    }
}