using System.IO;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestCaseStreamReader
    {
        TestCaseSummary Read(StreamReader stream, TestContext testContext, ITestMethodRunnerCallback callback, bool debugEnabled);
    }
}