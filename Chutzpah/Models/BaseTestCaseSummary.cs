using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Chutzpah.Models
{
    /// <summary>
    /// Base class for a summary of test case results
    /// </summary>
    public abstract class BaseTestCaseSummary
    {
        private readonly List<TestCase> tests;

        protected BaseTestCaseSummary()
        {
            TestGroups = new Dictionary<string, IList<TestCase>>();
            tests = new List<TestCase>();
            Tests = new ReadOnlyCollection<TestCase>(tests);
            Logs = new List<TestLog>();
            Errors = new List<TestError>();
        }

        /// <summary>
        /// A mapping from group name (suite/module) to test case list
        /// </summary>
        public Dictionary<string, IList<TestCase>> TestGroups { get; private set; }

        /// <summary>
        /// Collection of test results
        /// </summary>
        public ReadOnlyCollection<TestCase> Tests { get; set; }

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
        /// The total runtime in milliseconds to complete all tests
        /// </summary>
        public int TotalRuntime { get; set; }

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
            get { return Tests.Count(x => x.TestOutcome == TestOutcome.Passed); }
        }

        /// <summary>
        /// Number of tests which failed
        /// </summary>
        public int FailedCount
        {
            get { return Tests.Count(x => x.TestOutcome == TestOutcome.Failed); }
        }

        /// <summary>
        /// Number of tests which skipped
        /// </summary>
        public int SkippedCount
        {
            get { return Tests.Count(x => x.TestOutcome == TestOutcome.Skipped); }
        }

        /// <summary>
        /// Number of tests which not ran
        /// </summary>
        public int NotRanCount
        {
            get { return Tests.Count(x => x.TestOutcome == TestOutcome.None); }
        }

        /// <summary>
        /// Add a test case
        /// </summary>
        /// <param name="testCase"></param>
        public void AddTestCase(TestCase testCase)
        {
            tests.Add(testCase);
            var module = testCase.ModuleName ?? "";
            if (!TestGroups.ContainsKey(module))
            {
                TestGroups[module] = new List<TestCase>();
            }

            TestGroups[module].Add(testCase);

        }

        public void AddTestCases(IEnumerable<TestCase> testCases)
        {
            foreach (var testCase in testCases)
            {
                AddTestCase(testCase);
            }
        }
    }
}