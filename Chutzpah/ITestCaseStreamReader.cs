using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestCaseStreamReader
    {
        TestCaseStreamReadResult Read(TestCaseSource<string> testCaseSource, TestOptions testOptions, TestContext testContext, ITestMethodRunnerCallback callback);
    }
}