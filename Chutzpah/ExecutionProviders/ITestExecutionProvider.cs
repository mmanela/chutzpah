using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestExecutionProvider
    {
        IList<TestFileSummary> Execute(TestOptions options,
                     TestContext testContext,
                     TestExecutionMode testExecutionMode,
                     ITestMethodRunnerCallback callback);
    }
}