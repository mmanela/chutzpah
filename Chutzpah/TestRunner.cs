using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Chutzpah.Exceptions;
using Chutzpah.Models;
using Chutzpah.Utility;

namespace Chutzpah
{
    public class TestRunner : ITestRunner
    {
        public static string HeadlessBrowserName = "phantomjs.exe";
        public static string TestRunnerJsName = @"JSRunners\chutzpahRunner.js";
        private readonly Stopwatch stopWatch;
        private readonly IProcessHelper process;
        private readonly ITestCaseStreamReaderFactory testCaseStreamReaderFactory;
        private readonly IFileProbe fileProbe;
        private readonly ITestContextBuilder testContextBuilder;
        private readonly ICompilerCache compilerCache;


        public bool DebugEnabled { get; set; }

        public static ITestRunner Create(bool debugEnabled = false)
        {
            var runner = ChutzpahContainer.Current.GetInstance<TestRunner>();
            runner.DebugEnabled = debugEnabled;
            return runner;
        }

        public TestRunner(IProcessHelper process,
                          ITestCaseStreamReaderFactory testCaseStreamReaderFactory,
                          IFileProbe fileProbe,
                          ITestContextBuilder htmlTestFileCreator,
                          ICompilerCache compilerCache)
        {
            this.process = process;
            this.testCaseStreamReaderFactory = testCaseStreamReaderFactory;
            this.fileProbe = fileProbe;
            stopWatch = new Stopwatch();
            testContextBuilder = htmlTestFileCreator;
            this.compilerCache = compilerCache;
        }


        public void CleanTestContext(TestContext context)
        {
            testContextBuilder.CleanupContext(context);
        }

        public TestContext GetTestContext(string testFile, TestOptions options)
        {
            if (string.IsNullOrEmpty(testFile)) return null;

            return testContextBuilder.BuildContext(testFile, options);
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
            var summary = ProcessTestPaths(testPaths, new TestOptions(), TestRunnerMode.Discovery, RunnerCallback.Empty);
            return summary.Tests;
        }

        public IEnumerable<TestCase> DiscoverTests(IEnumerable<string> testPaths, TestOptions options)
        {
            var summary = ProcessTestPaths(testPaths, options, TestRunnerMode.Discovery, RunnerCallback.Empty);
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
            callback = options.OpenInBrowser || callback == null ? RunnerCallback.Empty : callback;
            callback.TestSuiteStarted();

            var summary = ProcessTestPaths(testPaths, options, TestRunnerMode.Execution, callback);

            callback.TestSuiteFinished(summary);
            return summary;
        }

        private TestCaseSummary ProcessTestPaths(IEnumerable<string> testPaths,
                                                 TestOptions options,
                                                 TestRunnerMode testRunnerMode,
                                                 ITestMethodRunnerCallback callback)
        {
            stopWatch.Start();
            string headlessBrowserPath = fileProbe.FindFilePath(HeadlessBrowserName);
            if (testPaths == null)
                throw new ArgumentNullException("testPaths");
            if (headlessBrowserPath == null)
                throw new FileNotFoundException("Unable to find headless browser: " + HeadlessBrowserName);
            if (fileProbe.FindFilePath(TestRunnerJsName) == null)
                throw new FileNotFoundException("Unable to find test runner base js file: " + TestRunnerJsName);

            var overallSummary = new TestCaseSummary();
            
            // Concurrent collection used to gather the parallel results from
            var testFileSummaries = new ConcurrentQueue<TestFileSummary>();
            var resultCount = 0;
            var cancellationSource = new CancellationTokenSource();
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = options.MaxDegreeOfParallelism, CancellationToken = cancellationSource.Token };
            Parallel.ForEach(fileProbe.FindScriptFiles(testPaths, options.TestingMode), parallelOptions, testFile =>
            {
                try
                {
                    if (cancellationSource.IsCancellationRequested) return;
                    TestContext testContext;

                    resultCount++;
                    if (testContextBuilder.TryBuildContext(testFile, options, out testContext))
                    {
                        if (options.OpenInBrowser)
                        {
                            process.LaunchFileInBrowser(testContext.TestHarnessPath);
                        }
                        else
                        {
                            var testSummary = InvokeTestRunner(headlessBrowserPath,
                                                               options,
                                                               testContext,
                                                               testRunnerMode,
                                                               callback);
                            testFileSummaries.Enqueue(testSummary);
                        }

                        
                        if(!DebugEnabled && !options.OpenInBrowser)
                        {
                            // Don't clean up context if you open in browser since we need the files around
                            // for the browser to use
                            testContextBuilder.CleanupContext(testContext);
                        }

                    }

                    // Limit the number of files we can scan to attempt to build a context for
                    // This is important in the case of folder scanning where many JS files may not be
                    // test files.
                    if (resultCount >= options.FileSearchLimit)
                    {
                        cancellationSource.Cancel();
                    }
                }
                catch (Exception e)
                {
                    callback.ExceptionThrown(e, testFile.FullPath);
                }
            });


            // Gather TestFileSummaries into TaseCaseSummary
            foreach(var fileSummary in testFileSummaries)
            {
                overallSummary.Append(fileSummary);
            }
            stopWatch.Stop();
            overallSummary.SetTotalRunTime((int)stopWatch.Elapsed.TotalMilliseconds);
            compilerCache.Save();
            return overallSummary;
        }

        private TestFileSummary InvokeTestRunner(string headlessBrowserPath,
                                                 TestOptions options,
                                                 TestContext testContext,
                                                 TestRunnerMode testRunnerMode,
                                                 ITestMethodRunnerCallback callback)
        {
            string runnerPath = fileProbe.FindFilePath(testContext.TestRunner);
            string fileUrl = BuildFileUrl(testContext.TestHarnessPath);

            string runnerArgs = BuildRunnerArgs(options, fileUrl, runnerPath, testRunnerMode);
            Func<ProcessStream, TestFileSummary> streamProcessor =
                processStream => testCaseStreamReaderFactory.Create().Read(processStream, options, testContext, callback, DebugEnabled);
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
            if (options.TestFileTimeoutMilliseconds.HasValue && options.TestFileTimeoutMilliseconds > 0)
            {
                runnerArgs = string.Format("\"{0}\" {1} {2} {3}",
                                           runnerPath,
                                           fileUrl,
                                           testModeStr,
                                           options.TestFileTimeoutMilliseconds.Value);
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