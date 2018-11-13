using System;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestMethodRunnerCallback
    {
        /// <summary>
        /// Started the TestContext in which the test is run
        /// </summary>
        /// <param name="context"></param>
        void TestContextStarted(TestContext context);

        /// <summary>
        /// Finished the TestContext in which the test is run
        /// </summary>
        /// <param name="context"></param>
        void TestContextFinished(TestContext context);

        /// <summary>
        /// Started running all files containing tests
        /// </summary>
        void TestSuiteStarted(TestContext testContext);

        /// <summary>
        /// Finished running all files containing tests
        /// </summary>
        /// <param name="testContext"></param>
        /// <param name="testResultsSummary"></param>
        void TestSuiteFinished(TestContext testContext, TestCaseSummary testResultsSummary);

        /// <summary>
        /// Began executing tests in a file
        /// </summary>
        /// <param name="testContext"></param>
        /// <param name="fileName"></param>
        void FileStarted(TestContext testContext);

        /// <summary>
        /// All tests in a file have finished
        /// </summary>
        /// <param name="testContext"></param>
        /// <param name="fileName"></param>
        /// <param name="testResultsSummary"></param>
        void FileFinished(TestContext testContext, TestFileSummary testResultsSummary);

        /// <summary>
        /// A test started execution
        /// </summary>
        /// <param name="testContext"></param>
        /// <param name="testCase"></param>
        void TestStarted(TestContext testContext, TestCase testCase);

        /// <summary>
        /// A test finished executing
        /// </summary>
        /// <param name="testContext"></param>
        /// <param name="testCase"></param>
        void TestFinished(TestContext testContext, TestCase testCase);

        /// <summary>
        /// An exception occurred in Chutzpah while running tests
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="fileName"></param>
        void ExceptionThrown(Exception exception, string fileName=null);

        /// <summary>
        /// An error that occurred in a test file 
        /// </summary>
        /// <param name="testContext"></param>
        /// <param name="error">Test file error</param>
        void FileError(TestContext testContext, TestError error);

        /// <summary>
        /// An log message sent from a test file 
        /// </summary>
        /// <param name="testContext"></param>
        /// <param name="log">Test file log message</param>
        void FileLog(TestContext testContext, TestLog log);
    }
}