using System;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestMethodRunnerCallback
    {
        void TestSuiteStarted();

        void TestSuiteFinished(TestCaseSummary testResultsSummary);

        void ExceptionThrown(Exception exception, string fileName);

        void FileStarted(string fileName);

        void FileFinished(string fileName, TestCaseSummary testResultsSummary);

        void TestStarted(TestCase testCase);

        void TestFinished(TestCase testCase);
    }
}