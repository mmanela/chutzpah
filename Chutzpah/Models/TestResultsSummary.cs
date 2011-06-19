using System.Collections.Generic;
using System.Linq;

namespace Chutzpah.Models
{
    public class TestResultsSummary
    {
        public TestResultsSummary(IEnumerable<TestResult> tests)
        {
            Tests = tests;
        }

        public IEnumerable<TestResult> Tests { get; set; }

        public int TotalCount
        {
            get { return Tests.Count(); }
        }
        
        public int PassedCount
        {
            get { return Tests.Count(x => x.Passed); }
        }
        
        public int FailedCount
        {
            get { return Tests.Count(x => !x.Passed); }
        }

    }
}