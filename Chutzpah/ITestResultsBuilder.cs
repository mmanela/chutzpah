using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestResultsBuilder
    {
        IEnumerable<TestCase> Build(BrowserTestFileResult browserTestFileResult, TestRunnerMode testRunnerMode);
    }
}