using System.Collections.Generic;
using System.Linq;

namespace Chutzpah.Models
{
    /// <summary>
    /// Summary of the test cases aggregated from all test files
    /// </summary>
    public class TestCaseSummary : BaseTestCaseSummary
    {
        public TestCaseSummary()
        {
            TestFileSummaries = new List<TestFileSummary>();
        }

        /// <summary>
        /// A mapping from module name to test case list
        /// </summary>
        public IList<TestFileSummary> TestFileSummaries { get; private set; }

        /// <summary>
        /// Appends another test case summary into the current instnace.
        /// This will combines the tests, logs and errors collections
        /// </summary>
        /// <param name="summary"></param>
        public void Append(TestFileSummary summary)
        {
            TestFileSummaries.Add(summary);
            AppendTests(summary.Tests);
            AppendLogs(summary.Logs);
            AppendErrors(summary.Errors);
            TimeTaken += summary.TimeTaken;
        }

        internal void AppendTests(IEnumerable<TestCase> tests)
        {
            foreach (var test in tests)
            {
                AddTestCase(test);
            }
        }

        internal void AppendLogs(IEnumerable<TestLog> logs)
        {
            Logs = Logs.Concat(logs).ToList();
        }

        internal void AppendErrors(IEnumerable<TestError> errors)
        {
            Errors = Errors.Concat(errors).ToList();
        }
    }
}