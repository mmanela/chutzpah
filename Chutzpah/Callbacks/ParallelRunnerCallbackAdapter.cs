using System;
using Chutzpah.Models;

namespace Chutzpah.Callbacks
{
    /// <summary>
    /// Takes an existing ITestMethodRunnerCallback and makes it safe to be used on multiple threads
    /// </summary>
    public class ParallelRunnerCallbackAdapter : ITestMethodRunnerCallback
    {        
        private static readonly object sync = new object();
        private readonly ITestMethodRunnerCallback nestedCallback;

        public ParallelRunnerCallbackAdapter(ITestMethodRunnerCallback nestedCallback)
        {
            this.nestedCallback = nestedCallback;
        }

        public void TestContextStarted(TestContext context)
        {
            lock (sync)
            {
                nestedCallback.TestContextStarted(context);
            }
        }

        public void TestContextFinished(TestContext context)
        {
            lock (sync)
            {
                nestedCallback.TestContextFinished(context);
            }
        }

        public void TestSuiteStarted(TestContext context)
        {
            lock (sync)
            {
                nestedCallback.TestSuiteStarted(context);
            }
        }

        public void TestSuiteFinished(TestContext context, TestCaseSummary testResultsSummary)
        {
            lock (sync)
            {
                nestedCallback.TestSuiteFinished(context, testResultsSummary);
            }
        }

        public void FileStarted(TestContext context)
        {
            lock (sync)
            {
                nestedCallback.FileStarted(context);
            }
        }

        public void FileFinished(TestContext context, TestFileSummary testResultsSummary)
        {
            lock (sync)
            {
                nestedCallback.FileFinished(context, testResultsSummary);
            }
        }

        public void TestStarted(TestContext context, TestCase testCase)
        {
            lock (sync)
            {
                nestedCallback.TestStarted(context, testCase);
            }
        }

        public void TestFinished(TestContext context, TestCase testCase)
        {
            lock (sync)
            {
                nestedCallback.TestFinished(context, testCase);
            }
        }

        public void ExceptionThrown(Exception exception, string fileName)
        {
            lock (sync)
            {
                nestedCallback.ExceptionThrown(exception, fileName);
            }
        }

        public void FileError(TestContext context, TestError error)
        {
            lock (sync)
            {
                nestedCallback.FileError(context, error);
            }
        }

        public void FileLog(TestContext context, TestLog log)
        {
            lock (sync)
            {
                nestedCallback.FileLog(context, log);
            }
        }
    }
}