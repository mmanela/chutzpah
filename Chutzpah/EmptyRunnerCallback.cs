using System;
using Chutzpah.Models;

namespace Chutzpah
{
    public sealed class EmptyRunnerCallback : ITestMethodRunnerCallback
    {
        public void TestSuiteStarted(){}
        public void TestSuiteFinished(TestCaseSummary testResultsSummary){}
        public void FileStarted(string fileName){}
        public void FileFinished(string fileName, TestCaseSummary testResultsSummary){}
        public void TestStarted(TestCase testCase){}
        public void TestFinished(TestCase testCase){}
        public void ExceptionThrown(Exception exception, string fileName) { }
        public void FileError(TestError error){}
        public void FileLog(TestLog log){}
    }
}