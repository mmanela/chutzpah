using System.Collections.Generic;

namespace Chutzpah.Models
{
    public class TestContext
    {
        public TestContext()
        {
            ReferencedJavaScriptFiles = new List<ReferencedFile>();
            TemporaryFiles = new List<string>();
        }

        /// <summary>
        /// The test file given by the user
        /// </summary>
        public string InputTestFile { get; set; }

        /// <summary>
        /// The path to the test runner
        /// </summary>
        public string TestRunner { get; set; }

        /// <summary>
        /// The path to the test harness. This is either the InputTestFile when a html file or
        /// it will be the generated test harness for .js files
        /// </summary>
        public string TestHarnessPath { get; set; }

        /// <summary>
        /// Is the harness on a remote server
        /// </summary>
        public bool IsRemoteHarness { get; set; }

        /// <summary>
        /// The list of referenced JavaScript files
        /// </summary>
        public IEnumerable<ReferencedFile> ReferencedJavaScriptFiles { get; set; }

        /// <summary>
        /// A list of temporary files that should be cleaned up after the test run is finished
        /// </summary>
        public IEnumerable<string> TemporaryFiles { get; set; }
    }
}