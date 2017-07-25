using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestExecutionProvider
    {
        Browser Name { get; }

        IList<TestFileSummary> Execute(TestOptions testOptions,
                     TestContext testContext,
                     TestExecutionMode testExecutionMode,
                     ITestMethodRunnerCallback callback);
    }
}