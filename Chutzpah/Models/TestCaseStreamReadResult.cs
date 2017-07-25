using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah
{
    public class TestCaseStreamReadResult
    {
        public IList<TestFileSummary> TestFileSummaries { get; set; }

        public bool TimedOut { get; set; }
    }
}