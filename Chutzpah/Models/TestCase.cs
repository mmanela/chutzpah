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

        public string HtmlTestFile { get; set; }
        public string InputTestFile { get; set; }
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

        public string GetDisplayName()
        {
            return string.IsNullOrWhiteSpace(ModuleName) ? TestName : string.Format("{0}:{1}", ModuleName, TestName);
        }
    }
}