using System.Collections.Generic;
using System.Linq;

namespace Chutzpah.Models
{
    /// <summary>
    /// Summary of the test cases
    /// </summary>
    public class TestCaseSummary
    {
        public TestCaseSummary()
        {
            Tests = new List<TestCase>();
            Logs = new List<TestLog>();
            Errors = new List<TestError>();
        }

        /// <summary>
        /// Collection of test results
        /// </summary>
        public IList<TestCase> Tests { get; set; }

        /// <summary>
        /// Log out put from test files
        /// </summary>
        public IList<TestLog> Logs { get; set; }

        /// <summary>
        /// Source errors from test files
        /// </summary>
        public IList<TestError> Errors { get; set; }

        /// <summary>
        /// The time in milliseconds to complete the tests
        /// </summary>
        public int TimeTaken { get; set; }

        /// <summary>
        /// Total count of all tests
        /// </summary>
        public int TotalCount
        {
            get { return Tests.Count(); }
        }
        
        /// <summary>
        /// Number of tests which passed
        /// </summary>
        public int PassedCount
        {
            get { return Tests.Count(x => x.Passed); }
        }
 
        /// <summary>
        /// Number of tests which failed
        /// </summary>
        public int FailedCount
        {
            get { return Tests.Count(x => !x.Passed); }
        }

        /// <summary>
        /// Appends another test case summary into the current instnace.
        /// This will combines the tests, logs and errors collections
        /// </summary>
        /// <param name="summary"></param>
        internal void Append(TestCaseSummary summary)
        {
            AppendTests(summary.Tests);
            AppendLogs(summary.Logs);
            AppendErrors(summary.Errors);
            TimeTaken += summary.TimeTaken;
        }

        internal void AppendTests(IEnumerable<TestCase> tests)
        {
            Tests = Tests.Concat(tests).ToList();
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