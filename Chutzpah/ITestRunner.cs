using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestRunner
    {
        /// <summary>
        /// Puts the runner in debug mode which forces it to ouput diagnostic information
        /// </summary>
        bool DebugEnabled { get; set; }

        /// <summary>
        /// Gets a test context for a given test file. The test context contains the paths
        /// of all the items needs to run the test
        /// </summary>
        /// <param name="testFile">The file contains the tests</param>
        /// <returns>A test context</returns>
        TestContext GetTestContext(string testFile);

        /// <summary>
        /// Gets a test context for a given test file. The test context contains the paths
        /// of all the items needs to run the test
        /// </summary>
        /// <param name="testFile">The file contains the tests</param>
        /// <param name="options">The testing options</param>
        /// <returns>A test context</returns>
        TestContext GetTestContext(string testFile, TestOptions options);


        TestResultsSummary RunTests(string testPath, ITestMethodRunnerCallback callback = null);
        TestResultsSummary RunTests(string testPath, TestOptions options, ITestMethodRunnerCallback callback = null);
        TestResultsSummary RunTests(IEnumerable<string> testPaths, TestOptions options, ITestMethodRunnerCallback callback = null);
        TestResultsSummary RunTests(IEnumerable<string> testPaths, ITestMethodRunnerCallback callback = null);


        IEnumerable<TestCase> DiscoverTests(string testPath);
        IEnumerable<TestCase> DiscoverTests(IEnumerable<string> testPaths);
        bool IsTestFile(string testFile);
    }
}