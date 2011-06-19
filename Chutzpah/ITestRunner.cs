using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestRunner
    {
        bool DebugEnabled { get; set; }
        TestResultsSummary RunTests(string testFile, ITestMethodRunnerCallback callback = null);
        TestResultsSummary RunTests(IEnumerable<string> testFile, ITestMethodRunnerCallback callback = null);
    }
}