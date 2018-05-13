using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestExecutionProvider
    {
        bool CanHandleBrowser(Engine browser);

        IList<TestFileSummary> Execute(TestOptions testOptions,
                     TestContext testContext,
                     TestExecutionMode testExecutionMode,
                     ITestMethodRunnerCallback callback);

        void SetupEnvironment(TestOptions testOptions, TestContext testContext);
    }
}