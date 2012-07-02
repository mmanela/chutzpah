using System;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestMethodRunnerCallback
    {
        /// <summary>
        /// Started running all files containing tests
        /// </summary>
        void TestSuiteStarted();

        /// <summary>
        /// Finished running all files containing tests
        /// </summary>
        /// <param name="testResultsSummary"></param>
        void TestSuiteFinished(TestCaseSummary testResultsSummary);

        /// <summary>
        /// Began executing tests in a file
        /// </summary>
        /// <param name="fileName"></param>
        void FileStarted(string fileName);

        /// <summary>
        /// All tests in a file have finished
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="testResultsSummary"></param>
        void FileFinished(string fileName, TestCaseSummary testResultsSummary);

        /// <summary>
        /// A test started execution
        /// </summary>
        /// <param name="testCase"></param>
        void TestStarted(TestCase testCase);

        /// <summary>
        /// A test finished executing
        /// </summary>
        /// <param name="testCase"></param>
        void TestFinished(TestCase testCase);

        /// <summary>
        /// An exception occured in Chutzpah while running tests
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="fileName"></param>
        void ExceptionThrown(Exception exception, string fileName);

        /// <summary>
        /// An error that occured in a test file 
        /// </summary>
        /// <param name="error">Test file error</param>
        void FileError(TestError error);

        /// <summary>
        /// An log message sent from a test file 
        /// </summary>
        /// <param name="log">Test file log message</param>
        void FileLog(TestLog log);
    }
}