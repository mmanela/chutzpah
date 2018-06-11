using System.Collections.Generic;
using System.Linq;

namespace Chutzpah.Models
{
    public enum TestOutcome
    {
        None,
        Passed,
        Failed,
        Skipped
    }

    public class TestCase
    {
        public TestCase()
        {
            TestResults = new List<TestResult>();
        }

        public string HtmlTestFile { get; set; }
        public string InputTestFile { get; set; }
        public string PathFromTestSettingsDirectory { get; set; }
        public string ModuleName { get; set; }
        public string TestName { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public bool Skipped { get; set; }
        public bool NotRan { get; set; }
        public IList<TestResult> TestResults { get; set; }


        /// <summary>
        /// The time in milliseconds to complete the test
        /// </summary>
        public int TimeTaken { get; set; }

        public bool ResultsAllPassed { get { return TestResults.Count() != 0 && TestResults.All(x => x.Passed); } }    

        public TestOutcome TestOutcome
        {
            get
            {
                if (Skipped)
                {
                    return TestOutcome.Skipped;
                }
                else if (NotRan)
                {
                    return TestOutcome.None;
                }
                else if (ResultsAllPassed)
                {
                    return TestOutcome.Passed;
                }
                else
                {
                    return TestOutcome.Failed;
                }
            }
        }

        public string GetDisplayName()
        {
            return string.IsNullOrWhiteSpace(ModuleName) ? TestName : string.Format("{0}:{1}", ModuleName, TestName);
        }
    }
}