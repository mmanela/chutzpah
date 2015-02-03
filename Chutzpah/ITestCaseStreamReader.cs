using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestCaseStreamReader
    {
        IList<TestFileSummary> Read(ProcessStream processStream, TestOptions testOptions, TestContext testContext, ITestMethodRunnerCallback callback, bool debugEnabled);
    }
}