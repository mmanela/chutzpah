using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestCaseStreamReader
    {
        TestFileSummary Read(ProcessStream processStream, TestOptions testOptions, TestContext testContext, ITestMethodRunnerCallback callback, bool debugEnabled);
    }
}