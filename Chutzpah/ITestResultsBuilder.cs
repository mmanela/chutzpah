using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestResultsBuilder
    {
        IEnumerable<TestResult> Build(BrowserTestFileResult browserTestFileResult);
    }
}