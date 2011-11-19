using System;
using System.Collections.Generic;
using System.IO;
using Chutzpah.Exceptions;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah
{
    public class TestRunner : ITestRunner
    {
        private const int TestableFileSearchLimit = 100;

        public static string HeadlessBrowserName = "phantomjs.exe";
        public static string TestRunnerJsName = @"JSRunners\chutzpah.js";

        private readonly IProcessHelper process;
        private readonly ITestResultsBuilder testResultsBuilder;
        private readonly IFileProbe fileProbe;
        private readonly ITestContextBuilder testContextBuilder;

        public bool DebugEnabled { get; set; }

        public static ITestRunner Create(bool debugEnabled = false)
        {
            var runner = ChutzpahContainer.Current.GetInstance<TestRunner>();
            runner.DebugEnabled = debugEnabled;
            return runner;
        }

        public TestRunner(IProcessHelper process, ITestResultsBuilder testResultsBuilder, IFileProbe fileProbe, ITestContextBuilder htmlTestFileCreator)
        {
            this.process = process;
            this.testResultsBuilder = testResultsBuilder;
            this.fileProbe = fileProbe;
            this.testContextBuilder = htmlTestFileCreator;
        }

        public TestContext GetTestContext(string testFile, TestOptions options)
        {
            if (string.IsNullOrEmpty(testFile)) return null;

            return testContextBuilder.BuildContext(testFile, options.StagingFolder);
        }

        public TestContext GetTestContext(string testFile)
        {
            return GetTestContext(testFile, new TestOptions());
        }

        public TestResultsSummary RunTests(IEnumerable<string> testPaths, ITestMethodRunnerCallback callback = null)
        {
            return RunTests(testPaths, new TestOptions(), callback);
        }

        public TestResultsSummary RunTests(IEnumerable<string> testPaths, TestOptions options, ITestMethodRunnerCallback callback = null)
        {
            string headlessBrowserPath = fileProbe.FindFilePath(HeadlessBrowserName);

            if (testPaths == null)
                throw new ArgumentNullException("testPaths");
            if (headlessBrowserPath == null)
                throw new FileNotFoundException("Unable to find headless browser: " + HeadlessBrowserName);
            if (fileProbe.FindFilePath(TestRunnerJsName) == null)
                throw new FileNotFoundException("Unable to find test runner base js file: " + TestRunnerJsName);

            if (callback != null) callback.TestSuiteStarted();

            var testResults = new List<TestResult>();
            var resultCount = 1;

            foreach (string testFile in fileProbe.FindScriptFiles(testPaths))
            {
                try
                {
                    TestContext testContext;

                    if (testContextBuilder.TryBuildContext(testFile, options.StagingFolder, out testContext))
                    {
                        resultCount++;
                        bool result = RunTestsFromHtmlFile(headlessBrowserPath, options, testContext, testResults, callback);

                        if (options.OpenInBrowser)
                        {
                            process.LaunchFileInBrowser(testContext.TestHarnessPath);
                        }

                        if (!result || resultCount >= TestableFileSearchLimit) break;
                    }

                }
                catch (Exception e)
                {
                    if (callback != null)
                        callback.ExceptionThrown(e, testFile);
                }
            }

            var summary = new TestResultsSummary(testResults);
            if (callback != null) callback.TestSuiteFinished(summary);
            return summary;
        }

        public TestResultsSummary RunTests(string testPath, ITestMethodRunnerCallback callback = null)
        {
            return RunTests(testPath, new TestOptions(), callback);
        }

        public TestResultsSummary RunTests(string testPath, TestOptions options, ITestMethodRunnerCallback callback = null)
        {
            return RunTests(new[] { testPath }, options, callback);
        }

        private bool RunTestsFromHtmlFile(string headlessBrowserPath,
                                          TestOptions options,
                                          TestContext testContext,
                                          List<TestResult> testResults,
                                          ITestMethodRunnerCallback callback)
        {
            if (callback != null && !callback.FileStart(testContext.InputTestFile)) return false;

            string runnerPath = fileProbe.FindFilePath(testContext.TestRunner);
            string fileUrl = BuildFileUrl(testContext.TestHarnessPath);

            string runnerArgs = BuildRunnerArgs(options, fileUrl, runnerPath);

            var result = process.RunExecutableAndCaptureOutput(headlessBrowserPath, runnerArgs);

            if (DebugEnabled)
                Console.WriteLine(result.StandardOutput);


            HandleTestProcessExitCode(result.ExitCode, testContext.InputTestFile);


            IEnumerable<TestResult> fileTests = testResultsBuilder.Build(new BrowserTestFileResult(testContext, result.StandardOutput));
            testResults.AddRange(fileTests);

            if (callback != null)
            {
                foreach (TestResult test in fileTests)
                    callback.TestFinished(test);
            }

            if (callback != null && !callback.FileFinished(testContext.InputTestFile, new TestResultsSummary(fileTests))) return false;

            return true;
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

        private static string BuildRunnerArgs(TestOptions options, string fileUrl, string runnerPath)
        {
            string runnerArgs;
            if (options.TimeOutMilliseconds.HasValue && options.TimeOutMilliseconds > 0)
            {
                runnerArgs = string.Format("\"{0}\" {1} {2}", runnerPath, fileUrl, options.TimeOutMilliseconds.Value);
            }
            else
            {
                runnerArgs = string.Format("\"{0}\" {1}", runnerPath, fileUrl);
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