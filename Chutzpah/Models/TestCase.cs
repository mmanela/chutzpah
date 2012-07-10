using System.Collections.Generic;
using System.Linq;

namespace Chutzpah.Models
{
    public class TestCase
    {
        public TestCase()
        {
            TestResults = new List<TestResult>();
        }

        /// <summary>
        /// The path to the html test harness used to run this test case
        /// </summary>
        public string HtmlTestHarness { get; set; }

        /// <summary>
        /// The path to the file the user provided to run this test. This could be the js file containing the test 
        /// or it can be the html test harness which calls run the tests
        /// </summary>
        public string InputTestFile { get; set; }

        /// <summary>
        /// The test file which contains the test
        /// </summary>
        public string TestFile { get; set; }

        public string ModuleName { get; set; }
        public string TestName { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public IList<TestResult> TestResults { get; set; }


        /// <summary>
        /// The time in milliseconds to complete the test
        /// </summary>
        public int TimeTaken { get; set; }

        public bool Passed { get { return TestResults.All(x => x.Passed); }}
    }
}