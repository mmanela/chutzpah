using System;
using System.IO;
using System.Linq;
using Chutzpah.Coverage;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah.RunnerCallbacks
{
    public class ConsoleRunnerCallback : RunnerCallback
    {
        public override void ExceptionThrown(Exception exception, string fileName)
        {
            Console.Error.Write(GetExceptionThrownMessage(exception, fileName));
        }

        public override void FileError(TestContext context, TestError error)
        {
            var errorMessage = GetFileErrorMessage(error);
            Console.Write(errorMessage);
        }

        public override void TestSuiteFinished(TestContext context, TestCaseSummary testResultsSummary)
        {
            base.TestSuiteFinished(context, testResultsSummary);
        }
    }
}