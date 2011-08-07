using System;
using Chutzpah.Models;

namespace Chutzpah.RunnerCallbacks
{
    public class RunnerCallback : ITestMethodRunnerCallback
    {
        public virtual void TestSuiteStarted()
        {
        }

        public virtual void TestSuiteFinished(TestResultsSummary testResultsSummary)
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

        public virtual bool FileStart(string fileName)
        {
            return true;
        }

        public virtual bool FileFinished(string fileName, TestResultsSummary testResultsSummary)
        {
            return true;
        }

        public void TestFinished(TestResult result)
        {
            TestStarted(result);
            if (result.Passed)
                TestPassed(result);
            if (!result.Passed)
                TestFailed(result);

            TestComplete(result);
        }

        protected virtual void TestComplete(TestResult result)
        {
            
        }

        protected virtual void TestFailed(TestResult result)
        {
        }

        protected virtual void TestStarted(TestResult result)
        {
        }        
        
        protected virtual void TestPassed(TestResult result)
        {
        }


        protected string GetTestDisplayText(TestResult result)
        {
            return string.IsNullOrWhiteSpace(result.ModuleName) ? result.TestName : string.Format("{0}:{1}", result.ModuleName, result.TestName);
        }

        protected string GetTestFailureMessage(TestResult result)
        {
            var errorString = "";
            if (result.Expected != null || result.Actual != null)
            {
                errorString += string.Format("Expected: {0}, Actual: {1}", result.Expected, result.Actual);
            }
            else if(!string.IsNullOrWhiteSpace(result.Message))
            {
                errorString += string.Format("{0}", result.Message);
            }

            errorString += string.Format("\n\tin {0}({1},{2}) at {3}\n\n", result.InputTestFile, result.Line, result.Column, GetTestDisplayText(result));

            return errorString;
        }
    }
}