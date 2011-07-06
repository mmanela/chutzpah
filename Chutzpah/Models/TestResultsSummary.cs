using System.Collections.Generic;
using System.Linq;

namespace Chutzpah.Models
{
    /// <summary>
    /// Summary of the results of tests
    /// </summary>
    public class TestResultsSummary
    {
        public TestResultsSummary(IEnumerable<TestResult> tests)
        {
            Tests = tests;
        }

        /// <summary>
        /// Collection of test results
        /// </summary>
        public IEnumerable<TestResult> Tests { get; set; }

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

    }
}