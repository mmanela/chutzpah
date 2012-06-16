using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chutzpah.Exceptions;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah
{
    public class TestRunner : ITestRunner
    {
        public static string HeadlessBrowserName = "phantomjs.exe";
        public static string TestRunnerJsName = @"JSRunners\chutzpahRunner.js";

        private readonly IProcessHelper process;
        private readonly ITestCaseStreamReader testCaseStreamReader;
        private readonly IFileProbe fileProbe;
        private readonly ITestContextBuilder testContextBuilder;

        public bool DebugEnabled { get; set; }

        public static ITestRunner Create(bool debugEnabled = false)
        {
            var runner = ChutzpahContainer.Current.GetInstance<TestRunner>();
            runner.DebugEnabled = debugEnabled;
            return runner;
        }

        public TestRunner(IProcessHelper process,
                          ITestCaseStreamReader testCaseStreamReader,
                          IFileProbe fileProbe,
                          ITestContextBuilder htmlTestFileCreator)
        {
            this.process = process;
            this.testCaseStreamReader = testCaseStreamReader;
            this.fileProbe = fileProbe;
            testContextBuilder = htmlTestFileCreator;
        }

        public TestContext GetTestContext(string testFile, TestOptions options)
        {
            if (string.IsNullOrEmpty(testFile)) return null;

            return testContextBuilder.BuildContext(testFile);
        }

        public TestContext GetTestContext(string testFile)
        {
            return GetTestContext(testFile, new TestOptions());
        }

        public bool IsTestFile(string testFile)
        {
            return testContextBuilder.IsTestFile(testFile);
        }

        public IEnumerable<TestCase> DiscoverTests(string testPath)
        {
            return DiscoverTests(new[] { testPath });
        }

        public IEnumerable<TestCase> DiscoverTests(IEnumerable<string> testPaths)
        {
            var summary = ProcessTestPaths(testPaths, new TestOptions(), TestRunnerMode.Discovery, new EmptyRunnerCallback());
            return summary.Tests;
        }

        public TestCaseSummary RunTests(string testPath, ITestMethodRunnerCallback callback = null)
        {
            return RunTests(testPath, new TestOptions(), callback);
        }

        public TestCaseSummary RunTests(string testPath,
                                           TestOptions options,
                                           ITestMethodRunnerCallback callback = null)
        {
            return RunTests(new[] { testPath }, options, callback);
        }


        public TestCaseSummary RunTests(IEnumerable<string> testPaths, ITestMethodRunnerCallback callback = null)
        {
            return RunTests(testPaths, new TestOptions(), callback);
        }

        public TestCaseSummary RunTests(IEnumerable<string> testPaths,
                                           TestOptions options,
                                           ITestMethodRunnerCallback callback = null)
        {

            callback = callback ?? new EmptyRunnerCallback();
            callback.TestSuiteStarted();

            var summary = ProcessTestPaths(testPaths, options, TestRunnerMode.Execution, callback);

            callback.TestSuiteFinished(summary);
            return summary;
        }

        private TestCaseSummary ProcessTestPaths(IEnumerable<string> testPaths, TestOptions options, TestRunnerMode testRunnerMode, ITestMethodRunnerCallback callback)
        {
            string headlessBrowserPath = fileProbe.FindFilePath(HeadlessBrowserName);
            if (testPaths == null)
                throw new ArgumentNullException("testPaths");
            if (headlessBrowserPath == null)
                throw new FileNotFoundException("Unable to find headless browser: " + HeadlessBrowserName);
            if (fileProbe.FindFilePath(TestRunnerJsName) == null)
                throw new FileNotFoundException("Unable to find test runner base js file: " + TestRunnerJsName);

            var overallSummary = new TestCaseSummary();
            var resultCount = 1;
            foreach (string testFile in fileProbe.FindScriptFiles(testPaths))
            {
                try
                {
                    TestContext testContext;

                    if (testContextBuilder.TryBuildContext(testFile, out testContext))
                    {
                        resultCount++;
                        var testSummary = InvokeTestRunner(headlessBrowserPath,
                                                        options,
                                                        testContext,
                                                        testRunnerMode,
                                                        callback);
                        overallSummary.Append(testSummary);

                        if (options.OpenInBrowser)
                        {
                            process.LaunchFileInBrowser(testContext.TestHarnessPath);
                        }

                        // Limit the number of files we can scan to attempt to build a context for
                        // This is important in the case of folder scanning where many JS files may not be
                        // test files.
                        if (resultCount >= options.FileSearchLimit) break;
                    }
                }
                catch (Exception e)
                {
                    callback.ExceptionThrown(e, testFile);
                }
            }

            return overallSummary;
        }

        private TestCaseSummary InvokeTestRunner(string headlessBrowserPath,
                                       TestOptions options,
                                       TestContext testContext,
                                       TestRunnerMode testRunnerMode,
                                       ITestMethodRunnerCallback callback)
        {
            if (callback != null) callback.FileStarted(testContext.InputTestFile);

            string runnerPath = fileProbe.FindFilePath(testContext.TestRunner);
            string fileUrl = BuildFileUrl(testContext.TestHarnessPath);

            string runnerArgs = BuildRunnerArgs(options, fileUrl, runnerPath, testRunnerMode);

            Func<StreamReader, TestCaseSummary> streamProcessor =
                stream => testCaseStreamReader.Read(stream, testContext, callback, DebugEnabled);
            var processResult = process.RunExecutableAndProcessOutput(headlessBrowserPath, runnerArgs, streamProcessor);

            HandleTestProcessExitCode(processResult.ExitCode, testContext.InputTestFile);

            return processResult.Model;
        }

        private static void HandleTestProcessExitCode(int exitCode, string inputTestFile)
        {
            switch ((TestProcessExitCode)exitCode)
            {
                case TestProcessExitCode.AllPassed:
                case TestProcessExitCode.SomeFailed:
                    return;
                case TestProcessExitCode.Timeout:
                    throw new ChutzpahTimeoutException("Timeout occured when running " + inputTestFile);
                default:
                    throw new ChutzpahException("Unknown error occured when running " + inputTestFile);
            }
        }

        private static string BuildRunnerArgs(TestOptions options, string fileUrl, string runnerPath, TestRunnerMode testRunnerMode)
        {
            string runnerArgs;
            var testModeStr = testRunnerMode.ToString().ToLowerInvariant();
            if (options.TimeOutMilliseconds.HasValue && options.TimeOutMilliseconds > 0)
            {
                runnerArgs = string.Format("\"{0}\" {1} {2} {3}",
                                           runnerPath,
                                           fileUrl,
                                           testModeStr,
                                           options.TimeOutMilliseconds.Value);
            }
            else
            {
                runnerArgs = string.Format("\"{0}\" {1} {2}", runnerPath, fileUrl, testModeStr);
            }

            return runnerArgs;
        }

        private static string BuildFileUrl(string absolutePath)
        {
            const string fileUrlFormat = "\"file:///{0}\"";
            return string.Format(fileUrlFormat, absolutePath.Replace("\\", "/"));
        }
    }
}