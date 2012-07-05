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

        public void TestSuiteStarted()
        {
            lock (sync)
            {
                nestedCallback.TestSuiteStarted();
            }
        }

        public void TestSuiteFinished(TestCaseSummary testResultsSummary)
        {
            lock (sync)
            {
                nestedCallback.TestSuiteFinished(testResultsSummary);
            }
        }

        public void FileStarted(string fileName)
        {
            lock (sync)
            {
                nestedCallback.FileStarted(fileName);
            }
        }

        public void FileFinished(string fileName, TestCaseSummary testResultsSummary)
        {
            lock (sync)
            {
                nestedCallback.FileFinished(fileName, testResultsSummary);
            }
        }

        public void TestStarted(TestCase testCase)
        {
            lock (sync)
            {
                nestedCallback.TestStarted(testCase);
            }
        }

        public void TestFinished(TestCase testCase)
        {
            lock (sync)
            {
                nestedCallback.TestFinished(testCase);
            }
        }

        public void ExceptionThrown(Exception exception, string fileName)
        {
            lock (sync)
            {
                nestedCallback.ExceptionThrown(exception, fileName);
            }
        }

        public void FileError(TestError error)
        {
            lock (sync)
            {
                nestedCallback.FileError(error);
            }
        }

        public void FileLog(TestLog log)
        {
            lock (sync)
            {
                nestedCallback.FileLog(log);
            }
        }
    }
}