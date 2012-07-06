namespace Chutzpah.Models
{
    using System.Collections.Generic;

    public class TestContext
    {
        public TestContext()
        {
            this.ReferencedJavaScriptFiles = new List<ReferencedFile>();
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
        /// The list of referenced JavaScript files
        /// </summary>
        public IEnumerable<ReferencedFile> ReferencedJavaScriptFiles { get; set; }
    }
}