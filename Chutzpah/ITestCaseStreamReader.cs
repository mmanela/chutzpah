using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestCaseStreamReader
    {
        TestCaseSummary Read(ProcessStream processStream,
                             TestOptions testOptions,
                             TestContext testContext,
                             ITestMethodRunnerCallback callback,
                             bool debugEnabled);
    }
}