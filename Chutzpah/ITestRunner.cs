using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestRunner
    {
        bool DebugEnabled { get; set; }
        TestContext GetTestContext(string testFile);
        TestContext GetTestContext(string testFile, TestOptions options);
        TestResultsSummary RunTests(string testFile, ITestMethodRunnerCallback callback = null);
        TestResultsSummary RunTests(string testFile, TestOptions options, ITestMethodRunnerCallback callback = null);
        TestResultsSummary RunTests(IEnumerable<string> testFiles, TestOptions options, ITestMethodRunnerCallback callback = null);
        TestResultsSummary RunTests(IEnumerable<string> testFile, ITestMethodRunnerCallback callback = null);
    }
}