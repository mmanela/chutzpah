using System;
using System.Linq;
using Chutzpah.Models;

namespace Chutzpah.RunnerCallbacks
{
    public class ConsoleRunnerCallback : RunnerCallback
    {
        public override void TestSuiteStarted()
        {
        }

        public override void TestSuiteFinished(TestCaseSummary testResultsSummary)
        {
        }

        public override void ExceptionThrown(Exception exception, string fileName)
        {
            Console.Write(GetExceptionThrownMessage(exception, fileName));
        }

        public override void FileError(TestError error)
        {
            var errorMessage = GetFileErrorMessage(error);
            Console.Write(errorMessage);
        }

        public override void FileLog(TestLog log)
        {
            Console.Write(GetFileLogMessage(log));
        }

    }
}