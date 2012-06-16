using System;
using Chutzpah.Models;

namespace Chutzpah.RunnerCallbacks
{
    public class RunnerCallback : ITestMethodRunnerCallback
    {
        public virtual void TestSuiteStarted()
        {
        }

        public virtual void TestSuiteFinished(TestCaseSummary testResultsSummary)
        {
        }

        public virtual void ExceptionThrown(Exception exception, string fileName)
        {
            Console.WriteLine();
            Console.WriteLine("ERROR OCCURRED:");
            Console.WriteLine(exception.ToString());
            Console.WriteLine("WHILE RUNNING:");
            Console.WriteLine(fileName);
            Console.WriteLine();
        }

        public virtual void FileStarted(string fileName)
        {
        }

        public virtual void FileFinished(string fileName, TestCaseSummary testResultsSummary)
        {
        }

        public virtual void TestStarted(TestCase testCase)
        {

        }

        public virtual void TestFinished(TestCase testCase)
        {
            if (testCase.Passed)
                TestPassed(testCase);
            if (!testCase.Passed)
                TestFailed(testCase);

            TestComplete(testCase);
        }

        protected virtual void TestComplete(TestCase testCase)
        {

        }

        protected virtual void TestFailed(TestCase testCase)
        {
        }

        protected virtual void TestPassed(TestCase testCase)
        {
        }


        protected string GetTestDisplayText(TestCase testCase)
        {
            return string.IsNullOrWhiteSpace(testCase.ModuleName) ? testCase.TestName : string.Format("{0}:{1}", testCase.ModuleName, testCase.TestName);
        }

        protected string GetTestFailureMessage(TestCase testCase)
        {
            
            var errorString = "";
            foreach (var result in testCase.TestResults)
            {
                if (result.Expected != null || result.Actual != null)
                {
                    errorString += string.Format("Expected: {0}, Actual: {1}\n", result.Expected, result.Actual);
                }
                else if (!string.IsNullOrWhiteSpace(result.Message))
                {
                    errorString += string.Format("{0}", result.Message);
                }
            }
            errorString += string.Format("\n\tin {0}({1},{2}) at {3}\n\n", testCase.InputTestFile, testCase.Line, testCase.Column, GetTestDisplayText(testCase));

            return errorString;
        }
    }
}