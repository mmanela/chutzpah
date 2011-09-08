using System;
using System.Collections.Generic;
using System.IO;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah
{
    public class TestRunner : ITestRunner
    {
        public static string HeadlessBrowserName = "phantomjs.exe";
        public static string TestRunnerJsName = @"JSRunners\qunitRunner.js";

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

            var context = testContextBuilder.BuildContext(testFile, options.StagingFolder);
            return context;
        }

        public TestContext GetTestContext(string testFile)
        {
            return GetTestContext(testFile, new TestOptions());
        }

        public TestResultsSummary RunTests(IEnumerable<string> testFiles, ITestMethodRunnerCallback callback = null)
        {
            return RunTests(testFiles, new TestOptions(), callback);
        }

        public TestResultsSummary RunTests(IEnumerable<string> testFiles, TestOptions options, ITestMethodRunnerCallback callback = null)
        {
            string headlessBrowserPath = fileProbe.FindPath(HeadlessBrowserName);
            string jsTestRunnerPath = fileProbe.FindPath(TestRunnerJsName);

            if (testFiles == null)
                throw new ArgumentNullException("testFiles");
            if (headlessBrowserPath == null)
                throw new FileNotFoundException("Unable to find headless browser: " + HeadlessBrowserName);
            if (jsTestRunnerPath == null)
                throw new FileNotFoundException("Unable to find test runner js file: " + TestRunnerJsName);

            if (callback != null) callback.TestSuiteStarted();

            var testResults = new List<TestResult>();
            foreach (string testFile in testFiles)
            {
                try
                {
                    var testContext = GetTestContext(testFile,options);
                    bool result = RunTestsFromHtmlFile(headlessBrowserPath, jsTestRunnerPath, testContext, testResults, callback);
                    
                    if(options.OpenInBrowser) 
                    {
                        process.LaunchFileInBrowser(testContext.TestHarnessPath);
                    }

                    if (!result) break;

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


        public TestResultsSummary RunTests(string testFile, ITestMethodRunnerCallback callback = null)
        {
            return RunTests(testFile, new TestOptions(), callback);
        }

        public TestResultsSummary RunTests(string testFile, TestOptions options, ITestMethodRunnerCallback callback = null)
        {
            return RunTests(new[] { testFile }, options, callback);
        }

        private bool RunTestsFromHtmlFile(string headlessBrowserPath,
                                          string jsTestRunnerPath,
                                          TestContext testContext,
                                          List<TestResult> testResults,
                                          ITestMethodRunnerCallback callback)
        {
            if (callback != null && !callback.FileStart(testContext.InputTestFile)) return false;

            string fileUrl = BuildFileUrl(testContext.TestHarnessPath);
            string args = string.Format("\"{0}\" {1}", jsTestRunnerPath, fileUrl);
            string jsonResult = process.RunExecutableAndCaptureOutput(headlessBrowserPath, args);

            if (DebugEnabled)
                Console.WriteLine(jsonResult);

            IEnumerable<TestResult> fileTests = testResultsBuilder.Build(new BrowserTestFileResult(testContext, jsonResult));
            testResults.AddRange(fileTests);

            if (callback != null)
            {
                foreach (TestResult test in fileTests)
                    callback.TestFinished(test);
            }

            if (callback != null && !callback.FileFinished(testContext.InputTestFile, new TestResultsSummary(fileTests))) return false;

            return true;
        }

        private static string BuildFileUrl(string absolutePath)
        {
            const string fileUrlFormat = "\"file:///{0}\"";
            return string.Format(fileUrlFormat, absolutePath.Replace("\\", "/"));
        }
    }
}