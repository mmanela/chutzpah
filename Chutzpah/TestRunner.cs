using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chutzpah.BatchProcessor;
using Chutzpah.Exceptions;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Transformers;

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
        private readonly IBatchCompilerService batchCompilerService;
        private readonly ITestHarnessBuilder testHarnessBuilder;
        private readonly ITestContextBuilder testContextBuilder;
        private readonly IChutzpahTestSettingsService testSettingsService;
        private readonly ITransformProcessor transformProcessor;
        private bool m_debugEnabled;

        public static ITestRunner Create(bool debugEnabled = false)
        {
            var runner = ChutzpahContainer.Current.GetInstance<TestRunner>();
            if (debugEnabled)
            {
                runner.EnableDebugMode();
            }

            return runner;
        }

        public TestRunner(IProcessHelper process,
                          ITestCaseStreamReaderFactory testCaseStreamReaderFactory,
                          IFileProbe fileProbe,
                          IBatchCompilerService batchCompilerService,
                          ITestHarnessBuilder testHarnessBuilder,
                          ITestContextBuilder htmlTestFileCreator,
                          IChutzpahTestSettingsService testSettingsService,
                          ITransformProcessor transformProcessor)
        {
            this.process = process;
            this.testCaseStreamReaderFactory = testCaseStreamReaderFactory;
            this.fileProbe = fileProbe;
            this.batchCompilerService = batchCompilerService;
            this.testHarnessBuilder = testHarnessBuilder;
            stopWatch = new Stopwatch();
            testContextBuilder = htmlTestFileCreator;
            this.testSettingsService = testSettingsService;
            this.transformProcessor = transformProcessor;
        }


        public void EnableDebugMode()
        {
            m_debugEnabled = true;

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

        public bool IsTestFile(string testFile, ChutzpahSettingsFileEnvironments environments)
        {
            return testContextBuilder.IsTestFile(testFile, environments);
        }

        public IEnumerable<TestCase> DiscoverTests(string testPath)
        {
            return DiscoverTests(new[] { testPath });
        }

        public IEnumerable<TestCase> DiscoverTests(IEnumerable<string> testPaths)
        {
            return DiscoverTests(testPaths, new TestOptions());
        }

        public IEnumerable<TestCase> DiscoverTests(IEnumerable<string> testPaths, TestOptions options)
        {
            IList<TestError> testErrors;
            return DiscoverTests(testPaths, options, out testErrors);
        }


        public IEnumerable<TestCase> DiscoverTests(IEnumerable<string> testPaths, TestOptions options, out IList<TestError> errors)
        {
            var summary = ProcessTestPaths(testPaths, options, TestExecutionMode.Discovery, RunnerCallback.Empty);
            errors = summary.Errors;
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
            callback = options.TestLaunchMode == TestLaunchMode.FullBrowser || callback == null ? RunnerCallback.Empty : callback;
            callback.TestSuiteStarted();

            var summary = ProcessTestPaths(testPaths, options, TestExecutionMode.Execution, callback);

            callback.TestSuiteFinished(summary);
            return summary;
        }

        private TestCaseSummary ProcessTestPaths(IEnumerable<string> testPaths,
                                                 TestOptions options,
                                                 TestExecutionMode testExecutionMode,
                                                 ITestMethodRunnerCallback callback)
        {

            ChutzpahTracer.TraceInformation("Chutzpah run started in mode {0} with parallelism set to {1}", testExecutionMode, options.MaxDegreeOfParallelism);

            options.TestExecutionMode = testExecutionMode;

            stopWatch.Start();
            string headlessBrowserPath = fileProbe.FindFilePath(HeadlessBrowserName);
            if (testPaths == null)
                throw new ArgumentNullException("testPaths");
            if (headlessBrowserPath == null)
                throw new FileNotFoundException("Unable to find headless browser: " + HeadlessBrowserName);
            if (fileProbe.FindFilePath(TestRunnerJsName) == null)
                throw new FileNotFoundException("Unable to find test runner base js file: " + TestRunnerJsName);

            var overallSummary = new TestCaseSummary();

            // Concurrent list to collect test contexts
            var testContexts = new ConcurrentBag<TestContext>();

            // Concurrent collection used to gather the parallel results from
            var testFileSummaries = new ConcurrentQueue<TestFileSummary>();
            var resultCount = 0;
            var cancellationSource = new CancellationTokenSource();
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = options.MaxDegreeOfParallelism, CancellationToken = cancellationSource.Token };


            // Given the input paths discover the potential test files
            var scriptPaths = FindTestFiles(testPaths, options);

            // Group the test files by their chutzpah.json files. Then check if those settings file have batching mode enabled.
            // If so, we keep those tests in a group together to be used in one context
            // Otherwise, we put each file in its own test group so each get their own context
            var testGroups = BuildTestingGroups(scriptPaths, options);

            // Build test contexts in parallel given a list of files each
            BuildTestContexts(options, testGroups, parallelOptions, cancellationSource, resultCount, testContexts, callback, overallSummary);


            // Compile the test contexts
            if (!PerformBatchCompile(callback, testContexts))
            {
                return overallSummary;
            }

            // Build test harness for each context and execute it in parallel
            ExecuteTestContexts(options, testExecutionMode, callback, testContexts, parallelOptions, headlessBrowserPath, testFileSummaries, overallSummary);


            // Gather TestFileSummaries into TaseCaseSummary
            foreach (var fileSummary in testFileSummaries)
            {
                overallSummary.Append(fileSummary);
            }

            stopWatch.Stop();
            overallSummary.SetTotalRunTime((int)stopWatch.Elapsed.TotalMilliseconds);

            transformProcessor.ProcessTransforms(testContexts, overallSummary);

            // Clear the settings file cache since in VS Chutzpah is not unloaded from memory.
            // If we don't clear then the user can never update the file.
            testSettingsService.ClearCache();


            ChutzpahTracer.TraceInformation(
                "Chutzpah run finished with {0} passed, {1} failed and {2} errors",
                overallSummary.PassedCount,
                overallSummary.FailedCount,
                overallSummary.Errors.Count);

            return overallSummary;
        }

        private List<List<PathInfo>> BuildTestingGroups(IEnumerable<PathInfo> scriptPaths, TestOptions testOptions)
        {
            // Find all chutzpah.json files for the input files
            // Then group files by their respective settings file
            var testGroups = new List<List<PathInfo>>();
            var fileSettingGroups = from path in scriptPaths
                                    let settingsFile = testSettingsService.FindSettingsFile(Path.GetDirectoryName(path.FullPath), testOptions.ChutzpahSettingsFileEnvironments)
                                    group path by settingsFile;

            // Scan over the grouped test files and if this file is set up for batching we add those files
            // as a group to be tested. Otherwise, we will explode them out individually so they get run in their
            // own context
            foreach (var group in fileSettingGroups)
            {
                if (group.Key.EnableTestFileBatching.Value)
                {
                    testGroups.Add(group.ToList());
                }
                else
                {
                    foreach (var path in group)
                    {
                        testGroups.Add(new List<PathInfo> { path });
                    }
                }
            }
            return testGroups;
        }

        private bool PerformBatchCompile(ITestMethodRunnerCallback callback, IEnumerable<TestContext> testContexts)
        {
            try
            {
                batchCompilerService.Compile(testContexts);
            }
            catch (ChutzpahCompilationFailedException e)
            {
                callback.ExceptionThrown(e, e.SettingsFile);

                ChutzpahTracer.TraceError(e, "Error during batch compile from {0}", e.SettingsFile);
                return false;
            }

            return true;
        }

        private void ExecuteTestContexts(
            TestOptions options,
            TestExecutionMode testExecutionMode,
            ITestMethodRunnerCallback callback,
            ConcurrentBag<TestContext> testContexts,
            ParallelOptions parallelOptions,
            string headlessBrowserPath,
            ConcurrentQueue<TestFileSummary> testFileSummaries,
            TestCaseSummary overallSummary)
        {
            Parallel.ForEach(
                testContexts,
                parallelOptions,
                testContext =>
                {
                    ChutzpahTracer.TraceInformation("Start test run for {0} in {1} mode", testContext.FirstInputTestFile, testExecutionMode);

                    try
                    {
                        testHarnessBuilder.CreateTestHarness(testContext, options);

                        if (options.TestLaunchMode == TestLaunchMode.FullBrowser)
                        {
                            ChutzpahTracer.TraceInformation(
                                "Launching test harness '{0}' for file '{1}' in a browser",
                                testContext.TestHarnessPath,
                                testContext.FirstInputTestFile);
                            process.LaunchFileInBrowser(testContext.TestHarnessPath, options.BrowserName);
                        }
                        else if (options.TestLaunchMode == TestLaunchMode.HeadlessBrowser)
                        {
                            ChutzpahTracer.TraceInformation(
                                "Invoking headless browser on test harness '{0}' for file '{1}'",
                                testContext.TestHarnessPath,
                                testContext.FirstInputTestFile);

                            var testSummaries = InvokeTestRunner(
                                headlessBrowserPath,
                                options,
                                testContext,
                                testExecutionMode,
                                callback);

                            foreach (var testSummary in testSummaries)
                            {

                                ChutzpahTracer.TraceInformation(
                                    "Test harness '{0}' for file '{1}' finished with {2} passed, {3} failed and {4} errors",
                                    testContext.TestHarnessPath,
                                    testSummary.Path,
                                    testSummary.PassedCount,
                                    testSummary.FailedCount,
                                    testSummary.Errors.Count);

                                ChutzpahTracer.TraceInformation(
                                    "Finished running headless browser on test harness '{0}' for file '{1}'",
                                    testContext.TestHarnessPath,
                                    testSummary.Path);

                                testFileSummaries.Enqueue(testSummary);
                            }
                        }
                        else if (options.TestLaunchMode == TestLaunchMode.Custom)
                        {
                            if (options.CustomTestLauncher == null)
                            {
                                throw new ArgumentNullException("TestOptions.CustomTestLauncher");
                            }
                            ChutzpahTracer.TraceInformation(
                                "Launching custom test on test harness '{0}' for file '{1}'",
                                testContext.TestHarnessPath,
                                testContext.FirstInputTestFile);
                            options.CustomTestLauncher.LaunchTest(testContext);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    catch (Exception e)
                    {
                        var error = new TestError
                        {
                            InputTestFile = testContext.InputTestFiles.FirstOrDefault(),
                            Message = e.ToString()
                        };

                        overallSummary.Errors.Add(error);
                        callback.FileError(error);

                        ChutzpahTracer.TraceError(e, "Error during test execution of {0}", testContext.FirstInputTestFile);
                    }
                    finally
                    {
                        ChutzpahTracer.TraceInformation("Finished test run for {0} in {1} mode", testContext.FirstInputTestFile, testExecutionMode);
                    }
                });


            // Clean up test context
            foreach (var testContext in testContexts)
            {
                // Don't clean up context if in debug mode
                if (!m_debugEnabled
                    && options.TestLaunchMode != TestLaunchMode.FullBrowser
                    && options.TestLaunchMode != TestLaunchMode.Custom)
                {
                    try
                    {
                        ChutzpahTracer.TraceInformation("Cleaning up test context for {0}", testContext.FirstInputTestFile);
                        testContextBuilder.CleanupContext(testContext);

                    }
                    catch (Exception e)
                    {
                        ChutzpahTracer.TraceError(e, "Error cleaning up test context for {0}", testContext.FirstInputTestFile);
                    }
                }
            }
        }

        private void BuildTestContexts(
            TestOptions options,
            List<List<PathInfo>> scriptPathGroups,
            ParallelOptions parallelOptions,
            CancellationTokenSource cancellationSource,
            int resultCount,
            ConcurrentBag<TestContext> testContexts,
            ITestMethodRunnerCallback callback,
            TestCaseSummary overallSummary)
        {
            Parallel.ForEach(scriptPathGroups, parallelOptions, testFiles =>
            {
                var pathString = string.Join(",", testFiles.Select(x => x.FullPath));
                ChutzpahTracer.TraceInformation("Building test context for {0}", pathString);

                try
                {
                    if (cancellationSource.IsCancellationRequested) return;
                    TestContext testContext;

                    resultCount++;
                    if (testContextBuilder.TryBuildContext(testFiles, options, out testContext))
                    {
                        testContexts.Add(testContext);
                    }
                    else
                    {
                        ChutzpahTracer.TraceWarning("Unable to build test context for {0}", pathString);
                    }

                    // Limit the number of files we can scan to attempt to build a context for
                    // This is important in the case of folder scanning where many JS files may not be
                    // test files.
                    if (resultCount >= options.FileSearchLimit)
                    {
                        ChutzpahTracer.TraceError("File search limit hit!!!");
                        cancellationSource.Cancel();
                    }
                }
                catch (Exception e)
                {
                    var error = new TestError
                    {
                        InputTestFile = testFiles.Select(x => x.FullPath).FirstOrDefault(),
                        Message = e.ToString()
                    };

                    overallSummary.Errors.Add(error);
                    callback.FileError(error);

                    ChutzpahTracer.TraceError(e, "Error during building test context for {0}", pathString);
                }
                finally
                {
                    ChutzpahTracer.TraceInformation("Finished building test context for {0}", pathString);
                }
            });
        }

        private IEnumerable<PathInfo> FindTestFiles(IEnumerable<string> testPaths, TestOptions options)
        {
            IEnumerable<PathInfo> scriptPaths = Enumerable.Empty<PathInfo>();

            // If the path list contains only chutzpah.json files then use those files for getting the list of test paths
            var testPathList = testPaths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (testPathList.All(testPath => Path.GetFileName(testPath).Equals(Constants.SettingsFileName, StringComparison.OrdinalIgnoreCase)))
            {
                ChutzpahTracer.TraceInformation("Using Chutzpah.json files to find tests");
                foreach (var path in testPathList)
                {
                    var chutzpahJsonPath = fileProbe.FindFilePath(path);
                    if (chutzpahJsonPath == null)
                    {
                        ChutzpahTracer.TraceWarning("Supplied chutzpah.json path {0} does not exist", path);
                    }

                    // The FindSettingsFile api takes the directory of the file since it caches this for use in later test runs
                    // this could be cleaned up to have two APIS one which lets you give the direct file
                    var settingsFile = testSettingsService.FindSettingsFile(Path.GetDirectoryName(chutzpahJsonPath), options.ChutzpahSettingsFileEnvironments);
                    var pathInfos = fileProbe.FindScriptFiles(settingsFile);
                    scriptPaths = scriptPaths.Concat(pathInfos);
                }
            }
            else
            {
                scriptPaths = fileProbe.FindScriptFiles(testPathList);
            }
            return scriptPaths
                    .Where(x => x.FullPath != null)
                    .ToList(); ;
        }

        private IList<TestFileSummary> InvokeTestRunner(string headlessBrowserPath,
                                                 TestOptions options,
                                                 TestContext testContext,
                                                 TestExecutionMode testExecutionMode,
                                                 ITestMethodRunnerCallback callback)
        {
            string runnerPath = fileProbe.FindFilePath(testContext.TestRunner);
            string fileUrl = BuildHarnessUrl(testContext.TestHarnessPath, testContext.IsRemoteHarness);

            string runnerArgs = BuildRunnerArgs(options, testContext, fileUrl, runnerPath, testExecutionMode);
            Func<ProcessStream, IList<TestFileSummary>> streamProcessor =
                processStream => testCaseStreamReaderFactory.Create().Read(processStream, options, testContext, callback, m_debugEnabled);
            var processResult = process.RunExecutableAndProcessOutput(headlessBrowserPath, runnerArgs, streamProcessor);

            HandleTestProcessExitCode(processResult.ExitCode, testContext.FirstInputTestFile, processResult.Model.Select(x => x.Errors).FirstOrDefault(), callback);

            return processResult.Model;
        }

        private static void HandleTestProcessExitCode(int exitCode, string inputTestFile, IList<TestError> errors, ITestMethodRunnerCallback callback)
        {
            string errorMessage = null;

            switch ((TestProcessExitCode)exitCode)
            {
                case TestProcessExitCode.AllPassed:
                case TestProcessExitCode.SomeFailed:
                    return;
                case TestProcessExitCode.Timeout:
                    errorMessage = "Timeout occurred when executing test file";
                    break;
                default:
                    errorMessage = "Unknown error occurred when executing test file. Received exit code of " + exitCode;
                    break;
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                var error = new TestError
                {
                    InputTestFile = inputTestFile,
                    Message = errorMessage
                };

                errors.Add(error);

                callback.FileError(error);
                ChutzpahTracer.TraceError("Headless browser returned with an error: {0}", errorMessage);
            }
        }

        private static string BuildRunnerArgs(TestOptions options, TestContext context, string fileUrl, string runnerPath, TestExecutionMode testExecutionMode)
        {
            string runnerArgs;
            var testModeStr = testExecutionMode.ToString().ToLowerInvariant();
            var timeout = context.TestFileSettings.TestFileTimeout ?? options.TestFileTimeoutMilliseconds ?? Constants.DefaultTestFileTimeout;

            runnerArgs = string.Format("--ignore-ssl-errors=true --proxy-type=none \"{0}\" {1} {2} {3} {4} {5}",
                                       runnerPath,
                                       fileUrl,
                                       testModeStr,
                                       timeout,
                                       context.TestFileSettings.IgnoreResourceLoadingErrors.Value,
                                       context.TestFileSettings.UserAgent);


            return runnerArgs;
        }

        private static string BuildHarnessUrl(string absolutePath, bool isRemoteHarness)
        {
            if (isRemoteHarness)
            {
                return absolutePath;
            }
            else
            {
                return string.Format("\"{0}\"", FileProbe.GenerateFileUrl(absolutePath));
            }
        }
    }
}