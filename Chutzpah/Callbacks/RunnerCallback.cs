using System;
using System.Linq;
using Chutzpah.Models;

namespace Chutzpah
{
    public abstract class RunnerCallback : ITestMethodRunnerCallback
    {
        public static ITestMethodRunnerCallback Empty = new EmptyRunnerCallback();
        private sealed class EmptyRunnerCallback : RunnerCallback
        {
            public override void ExceptionThrown(Exception exception, string fileName)
            {
                // Default runner callback will re-throw exception. In other implementations 
                // we don't do this since we know they can report the error
                base.ExceptionThrown(exception, fileName);

                throw exception;
            }
        }

        public virtual void TestContextStarted(TestContext context) { }
        public virtual void TestContextFinished(TestContext context) { }

        public virtual void TestSuiteStarted(TestContext context) { }
        public virtual void TestSuiteFinished(TestContext context,TestCaseSummary testResultsSummary) { }

        public virtual void FileStarted(TestContext context) { }
        public virtual void FileFinished(TestContext context, TestFileSummary testResultsSummary) { }

        public virtual void ExceptionThrown(Exception exception, string fileName) { }

        public virtual void FileError(TestContext context, TestError error) { }
        public virtual void FileLog(TestContext context, TestLog log) { }

        public virtual void TestStarted(TestContext context, TestCase testCase) { }
        public virtual void TestFinished(TestContext context, TestCase testCase)
        {
            switch (testCase.TestOutcome)
            {
                case TestOutcome.Passed:
                    ChutzpahTracer.TraceInformation("File {0}, Test {1} passed", testCase.InputTestFile, testCase.TestName);
                    TestPassed(context, testCase);
                    break;
                case TestOutcome.Failed: 
                    ChutzpahTracer.TraceInformation("File {0}, Test {1} failed", testCase.InputTestFile, testCase.TestName);
                    TestFailed(context, testCase);
                    break;
                case TestOutcome.Skipped:
                    ChutzpahTracer.TraceInformation("File {0}, Test {1} skipped", testCase.InputTestFile, testCase.TestName);
                    TestSkipped(context, testCase);
                    break;
                default:
                    break;
            }

            TestComplete(context, testCase);
        }

        protected virtual void TestComplete(TestContext context, TestCase testCase) { }
        protected virtual void TestFailed(TestContext context, TestCase testCase) { }
        protected virtual void TestPassed(TestContext context, TestCase testCase) { }
        protected virtual void TestSkipped(TestContext context, TestCase testCase) { }

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

        protected virtual string GetExceptionThrownMessage(Exception exception, string fileName=null)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                return string.Format("Chutzpah Error: {0}\n While Running:{1}\n\n", exception, fileName);
            }
            else
            {

                return string.Format("Chutzpah Error: {0}\n\n", exception);
            }
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