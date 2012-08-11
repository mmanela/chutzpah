using System;
using System.Linq;
using Chutzpah.Models;

namespace Chutzpah
{
    public abstract class RunnerCallback : ITestMethodRunnerCallback
    {
        public static ITestMethodRunnerCallback Empty = new EmptyRunnerCallback(); 
        private sealed class EmptyRunnerCallback : RunnerCallback{}

        public virtual void TestSuiteStarted(){}
        public virtual void TestSuiteFinished(TestCaseSummary testResultsSummary){}
        public virtual void FileStarted(string fileName){}
        public virtual void FileFinished(string fileName, TestFileSummary testResultsSummary){}
        public virtual void TestStarted(TestCase testCase){}
        public virtual void ExceptionThrown(Exception exception, string fileName){}
        public virtual void FileError(TestError error){}
        public virtual void FileLog(TestLog log){}
        public virtual void TestFinished(TestCase testCase)
        {
            if (testCase.Passed)
                TestPassed(testCase);
            if (!testCase.Passed)
                TestFailed(testCase);

            TestComplete(testCase);
        }

        protected virtual void TestComplete(TestCase testCase){}
        protected virtual void TestFailed(TestCase testCase){}
        protected virtual void TestPassed(TestCase testCase){}

        protected virtual string GetFileLogMessage(TestLog log)
        {
            return string.Format("Log Message: {0} from {1}\n", log.Message, log.InputTestFile);
        }

        protected virtual string GetExceptionThrownMessage(Exception exception, string fileName)
        {
            return string.Format("Chutzpah Error: {0}\n While Running:{1}\n\n", exception, fileName);
        }

        protected virtual string GetFileErrorMessage(TestError error)
        {
            var stack = "";
            if (!error.Stack.Any()) stack += "\n";
            foreach (var item in error.Stack)
            {
                if (!string.IsNullOrEmpty(item.Function))
                {
                    stack += "at " + item.Function + " ";
                }
                if (!string.IsNullOrEmpty(item.File))
                {
                    stack += "in " + item.File;
                }
                if (!string.IsNullOrEmpty(item.Line))
                {
                    stack += string.Format(" (line {0})", item.Line);
                }
                stack += "\n";
            }

            return string.Format("JS Error: {0}\n {1}While Running:{2}\n\n", error.Message, stack, error.InputTestFile);
        }



        protected virtual string GetTestFailureMessage(TestCase testCase)
        {

            var errorString = "";

            errorString += string.Format("Test '{0}' failed\n", testCase.GetDisplayName());

            foreach (var result in testCase.TestResults.Where(x => !x.Passed))
            {
                errorString += string.Format("\t{0}\n", result.GetFailureMessage());
            }

            errorString += string.Format("in {0} (line {1})\n\n", testCase.InputTestFile, testCase.Line);

            return errorString;
        }
    }
}