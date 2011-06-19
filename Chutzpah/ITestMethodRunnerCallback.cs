using System;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestMethodRunnerCallback
    {
        void TestSuiteStarted();

        void TestSuiteFinished(TestResultsSummary testResultsSummary);

        void ExceptionThrown(Exception exception, string fileName);

        bool FileStart(string fileName);

        bool FileFinished(string fileName, TestResultsSummary testResultsSummary);

        void TestFinished(TestResult result);
    }
}