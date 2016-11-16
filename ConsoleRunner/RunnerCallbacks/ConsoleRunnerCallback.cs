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

        public override void FileError(TestError error)
        {
            var errorMessage = GetFileErrorMessage(error);
            Console.Write(errorMessage);
        }

        public override void TestSuiteFinished(TestCaseSummary testResultsSummary)
        {
            base.TestSuiteFinished(testResultsSummary);
        }
    }
}