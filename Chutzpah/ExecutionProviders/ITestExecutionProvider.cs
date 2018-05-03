using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestExecutionProvider
    {
        bool CanHandleBrowser(Browser browser);

        IList<TestFileSummary> Execute(TestOptions testOptions,
                     TestContext testContext,
                     TestExecutionMode testExecutionMode,
                     ITestMethodRunnerCallback callback);
    }
}