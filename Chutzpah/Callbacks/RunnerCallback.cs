using System;
using System.Linq;
using Chutzpah.Models;

namespace Chutzpah
{
    public abstract class RunnerCallback : ITestMethodRunnerCallback
    {
        public static ITestMethodRunnerCallback Empty = new EmptyRunnerCallback();
        private sealed class EmptyRunnerCallback : RunnerCallback { }

        public virtual void TestSuiteStarted() { }
        public virtual void TestSuiteFinished(TestCaseSummary testResultsSummary) { }
        public virtual void FileStarted(string fileName) { }

        public virtual void FileFinished(string fileName, TestFileSummary testResultsSummary){}

        public virtual void TestStarted(TestCase testCase) { }
        public virtual void ExceptionThrown(Exception exception, string fileName) { }
        public virtual void FileError(TestError error) { }
        public virtual void FileLog(TestLog log) { }
        public virtual void TestFinished(TestCase testCase)
        {
            switch (testCase.TestOutcome)
            {
                case TestOutcome.Passed:
                    ChutzpahTracer.TraceInformation("File {0}, Test {1} passed", testCase.InputTestFile, testCase.TestName);
                    TestPassed(testCase);
                    break;
                case TestOutcome.Failed: 
                    ChutzpahTracer.TraceInformation("File {0}, Test {1} failed", testCase.InputTestFile, testCase.TestName);
                    TestFailed(testCase);
                    break;
                case TestOutcome.Skipped:
                    ChutzpahTracer.TraceInformation("File {0}, Test {1} skipped", testCase.InputTestFile, testCase.TestName);
                    TestSkipped(testCase);
                    break;
                default:
                    break;
            }

            TestComplete(testCase);
        }

        protected virtual void TestComplete(TestCase testCase) { }
        protected virtual void TestFailed(TestCase testCase) { }
        protected virtual void TestPassed(TestCase testCase) { }
        protected virtual void TestSkipped(TestCase testCase) { }

        protected virtual string GetCodeCoverageMessage(CoverageData coverageData)
        {
            var message = string.Format("Code Coverage Results");
            message += string.Format("     Average Coverage: {0:0%}\n", coverageData.CoveragePercentage);
            foreach (var fileData in coverageData)
            {
                message += string.Format("     {0:0%} for {1}\n", fileData.Value.CoveragePercentage, fileData.Key);
            }

            return message;
        }

        protected virtual string GetFileLogMessage(TestLog log)
        {
            return string.Format("Log Message: {0} from {1}\n", log.Message, log.InputTestFile);
        }

        protected virtual string GetExceptionThrownMessage(Exception exception, string fileName)
        {
            return string.Format("Chutzpah Error: {0} - {1}\n While Running:{2}\n\n", exception.GetType().Name, exception, fileName);
        }

        public static string FormatFileErrorMessage(TestError error)
        {
            var stack = error.GetFormattedStackTrace();

            return string.Format("Error: {0}\n{1}While Running:{2}\n", error.Message, stack, error.InputTestFile);
        }

        protected virtual string GetFileErrorMessage(TestError error)
        {
            return FormatFileErrorMessage(error);
        }

        protected virtual string GetTestFailureMessage(TestCase testCase)
        {

            var errorString = "";

            errorString += string.Format("Test '{0}' failed\n", testCase.GetDisplayName());

            foreach (var result in testCase.TestResults.Where(x => !x.Passed))
            {
                errorString += string.Format("\t{0}\n", result.GetFailureMessage());
            }

            errorString += GetTestFailureLocationString(testCase);

            return errorString;
        }

        protected virtual string GetTestFailureLocationString(TestCase testCase)
        {
            return string.Format("in {0} (line {1})\n\n", testCase.InputTestFile, testCase.Line);
        }
    }
}