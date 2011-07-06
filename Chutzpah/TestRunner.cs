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

        private readonly IProcessWrapper process;
        private readonly ITestResultsBuilder testResultsBuilder;
        private readonly IFileProbe fileProbe;
        private readonly IHtmlTestFileCreator htmlTestFileCreator;

        public bool DebugEnabled { get; set; }

        public TestRunner()
            : this(
                new ProcessWrapper(),
                new TestResultsBuilder(),
                new FileProbe(new EnvironmentWrapper(), new FileSystemWrapper()),
                new HtmlTestFileCreator())
        {
        }

        public TestRunner(IProcessWrapper process, ITestResultsBuilder testResultsBuilder, IFileProbe fileProbe, IHtmlTestFileCreator htmlTestFileCreator)
        {
            this.process = process;
            this.testResultsBuilder = testResultsBuilder;
            this.fileProbe = fileProbe;
            this.htmlTestFileCreator = htmlTestFileCreator;
        }

        public string GetTestHarnessPath(string testFile)
        {
            string htmlTestFile = testFile;
            if (IsJavaScriptFile(testFile))
            {
                htmlTestFile = htmlTestFileCreator.CreateTestFile(testFile);
            }
            return htmlTestFile;
        }

        public TestResultsSummary RunTests(IEnumerable<string> testFiles, ITestMethodRunnerCallback callback = null)
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
                    string htmlTestFile = GetTestHarnessPath(testFile);

                    if (IsHtmlFile(htmlTestFile))
                    {
                        bool result = RunTestsFromHtmlFile(headlessBrowserPath, jsTestRunnerPath, htmlTestFile, testFile, testResults, callback);
                        if (!result) break;
                    }
                    else
                    {
                        //TODO: Log that fact that this is not a runnable test file
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
        
        
        public TestResultsSummary RunTests(string testFile, ITestMethodRunnerCallback callback = null)
        {
            return RunTests(new[] {testFile}, callback);
        }

        private bool RunTestsFromHtmlFile(string headlessBrowserPath,
                                          string jsTestRunnerPath,
                                          string htmlTestFile,
                                          string inputTestFile,
                                          List<TestResult> testResults,
                                          ITestMethodRunnerCallback callback)
        {
            string inputTestFilePath = fileProbe.FindPath(inputTestFile);
            string htmlTestFilePath = fileProbe.FindPath(htmlTestFile);
            if (htmlTestFilePath == null)
                throw new FileNotFoundException("Unable to find test file " + htmlTestFile);

            if (callback != null && !callback.FileStart(inputTestFilePath)) return false;

            string fileUrl = BuildFileUrl(htmlTestFilePath);
            string args = string.Format("\"{0}\" {1}", jsTestRunnerPath, fileUrl);
            string jsonResult = process.RunExecutableAndCaptureOutput(headlessBrowserPath, args);

            if (DebugEnabled)
                Console.WriteLine(jsonResult);

            IEnumerable<TestResult> fileTests = testResultsBuilder.Build(new BrowserTestFileResult(htmlTestFilePath, inputTestFilePath, jsonResult));
            testResults.AddRange(fileTests);

            if (callback != null)
            {
                foreach (TestResult test in fileTests)
                    callback.TestFinished(test);
            }

            if (callback != null && !callback.FileFinished(inputTestFilePath, new TestResultsSummary(fileTests))) return false;

            return true;
        }

        
        private static bool IsHtmlFile(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            return ext != null &&
                   (ext.Equals(".html", StringComparison.OrdinalIgnoreCase) || ext.Equals(".htm", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsJavaScriptFile(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            return ext != null && ext.Equals(".js", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildFileUrl(string absolutePath)
        {
            const string fileUrlFormat = "\"file:///{0}\"";
            return string.Format(fileUrlFormat, absolutePath.Replace("\\", "/"));
        }
    }
}