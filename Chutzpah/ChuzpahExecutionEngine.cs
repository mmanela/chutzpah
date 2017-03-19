using Chutzpah.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Linq;
using System;
using System.Threading.Tasks;
using Chutzpah.Server.Models;
using Chutzpah.BatchProcessor;
using Chutzpah.Server;
using Chutzpah.Transformers;
using System.Diagnostics;
using System.IO;
using Chutzpah.Exceptions;
using Chutzpah.Utility;

namespace Chutzpah
{
    public class ChutzpahExeuctionState
    {
        public TestOptions TestOptions { get; set; } = new TestOptions();
        public CancellationTokenSource CacellationSource { get; set; } = new CancellationTokenSource();
        public ConcurrentBag<TestContext> TestContexts { get; set; } = new ConcurrentBag<TestContext>();
        public ParallelOptions ParallelOptions { get; set; } = new ParallelOptions();

        public ITestMethodRunnerCallback Callback { get; set; } = RunnerCallback.Empty;
        
        public ConcurrentQueue<TestFileSummary> TestFileSummaries { get; set; } = new ConcurrentQueue<TestFileSummary>();

        public IChutzpahWebServerHost WebServerHost { get; set; } = ChutzpahWebServerHost.Empty;

        public List<TestError> TestErrors { get; internal set; }
    }

    public class ChuzpahExecutionEngine
    {
        private IChutzpahWebServerHost m_activeWebServerHost;


        public IChutzpahWebServerHost ActiveWebServerHost
        {
            get
            {
                if (m_activeWebServerHost != null && m_activeWebServerHost.IsRunning)
                {
                    return m_activeWebServerHost;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                m_activeWebServerHost = value;
            }
        }
        public static string HeadlessBrowserName = "phantomjs.exe";
        public static string TestRunnerJsName = @"ChutzpahJSRunners\chutzpahRunner.js";

        private readonly Stopwatch stopWatch;
        private readonly IProcessHelper process;
        private readonly ITestCaseStreamReaderFactory testCaseStreamReaderFactory;
        private readonly IFileProbe fileProbe;
        private readonly IBatchCompilerService batchCompilerService;
        private readonly ITestHarnessBuilder testHarnessBuilder;
        private readonly ITestContextBuilder testContextBuilder;
        private readonly IChutzpahTestSettingsService testSettingsService;
        private readonly ITransformProcessor transformProcessor;
        private readonly IChutzpahWebServerFactory webServerFactory;
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

        readonly IUrlBuilder urlBuilder;
        private string headlessBrowserPath;

        public ChuzpahExecutionEngine(IProcessHelper process,
                          ITestCaseStreamReaderFactory testCaseStreamReaderFactory,
                          IFileProbe fileProbe,
                          IBatchCompilerService batchCompilerService,
                          ITestHarnessBuilder testHarnessBuilder,
                          ITestContextBuilder htmlTestFileCreator,
                          IChutzpahTestSettingsService testSettingsService,
                          ITransformProcessor transformProcessor,
                          IChutzpahWebServerFactory webServerFactory,
                          IUrlBuilder urlBuilder)
        {
            this.urlBuilder = urlBuilder;
            this.process = process;
            this.testCaseStreamReaderFactory = testCaseStreamReaderFactory;
            this.fileProbe = fileProbe;
            this.batchCompilerService = batchCompilerService;
            this.testHarnessBuilder = testHarnessBuilder;
            stopWatch = new Stopwatch();
            testContextBuilder = htmlTestFileCreator;
            this.testSettingsService = testSettingsService;
            this.transformProcessor = transformProcessor;
            this.webServerFactory = webServerFactory;

            headlessBrowserPath = fileProbe.FindFilePath(HeadlessBrowserName);

            if (headlessBrowserPath == null)
            {
                throw new FileNotFoundException("Unable to find headless browser: " + HeadlessBrowserName);
            }
        }

        public bool BuildTestContexts(ChutzpahExeuctionState executionState, IEnumerable<string> testPaths)
        {
            // Given the input paths discover the potential test files
            var scriptPaths = FindTestFiles(testPaths, executionState.TestOptions);

            // Group the test files by their chutzpah.json files. Then check if those settings file have batching mode enabled.
            // If so, we keep those tests in a group together to be used in one context
            // Otherwise, we put each file in its own test group so each get their own context
            var testRunConfiguration = BuildTestRunConfiguration(scriptPaths, executionState.TestOptions);

            ConfigureTracing(testRunConfiguration);

            var parallelism = testRunConfiguration.MaxDegreeOfParallelism.HasValue
                                ? Math.Min(executionState.TestOptions.MaxDegreeOfParallelism, testRunConfiguration.MaxDegreeOfParallelism.Value)
                                : executionState.TestOptions.MaxDegreeOfParallelism;

            executionState.ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = parallelism, CancellationToken = executionState.CacellationSource.Token };

            ChutzpahTracer.TraceInformation("Parallelism set to {0}", executionState.ParallelOptions.MaxDegreeOfParallelism);

            // Build test contexts in parallel given a list of files each
            CreateTestContexts(executionState, testRunConfiguration.TestGroups);

            // Match input -> output files and compile if configured
            if (!PerformBatchCompile(executionState.Callback, executionState.TestContexts))
            {
                return false;
            }

            // Find the first test context with a web server configuration and use it
            // This will not create a new server if one is already running
            var webServerHost = SetupWebServerHost(executionState.TestContexts);
            ActiveWebServerHost = webServerHost;

            return true;
        }

        public void ExecuteTestContexts(ChutzpahExeuctionState executionState)
        {
            Parallel.ForEach(
                executionState.TestContexts,
                executionState.ParallelOptions,
                testContext =>
                {
                    ChutzpahTracer.TraceInformation("Start test run for {0} in {1} mode", testContext.FirstInputTestFile, executionState.TestOptions.TestExecutionMode);

                    try
                    {
                        try
                        {
                            testHarnessBuilder.CreateTestHarness(testContext, executionState.TestOptions);
                        }
                        catch (IOException)
                        {
                            // Mark this creation failed so we do not try to clean it up later
                            // This is to work around a bug in TestExplorer that runs chutzpah in parallel on 
                            // the same files
                            // TODO(mmanela): Re-evalute if this is needed once they fix that bug
                            testContext.TestHarnessCreationFailed = true;
                            ChutzpahTracer.TraceWarning("Marking test harness creation failed for harness {0} and test file {1}", testContext.TestHarnessPath, testContext.FirstInputTestFile);
                            throw;
                        }

                        if (executionState.TestOptions.TestLaunchMode == TestLaunchMode.FullBrowser)
                        {
                            ChutzpahTracer.TraceInformation(
                                "Launching test harness '{0}' for file '{1}' in a browser",
                                testContext.TestHarnessPath,
                                testContext.FirstInputTestFile);

                            // Allow override from command line.
                            var browserArgs = testContext.TestFileSettings.BrowserArguments;
                            if (!string.IsNullOrWhiteSpace(executionState.TestOptions.BrowserArgs))
                            {
                                var path = BrowserPathHelper.GetBrowserPath(executionState.TestOptions.BrowserName);
                                browserArgs = new Dictionary<string, string>
                                {
                                    { Path.GetFileNameWithoutExtension(path), executionState.TestOptions.BrowserArgs }
                                };
                            }

                            process.LaunchFileInBrowser(testContext, testContext.TestHarnessPath, executionState.TestOptions.BrowserName, browserArgs);
                        }
                        else if (executionState.TestOptions.TestLaunchMode == TestLaunchMode.HeadlessBrowser)
                        {
                            ChutzpahTracer.TraceInformation(
                                "Invoking headless browser on test harness '{0}' for file '{1}'",
                                testContext.TestHarnessPath,
                                testContext.FirstInputTestFile);

                            var testSummaries = InvokeTestRunner(
                                headlessBrowserPath,
                                executionState.TestOptions,
                                testContext,
                                executionState.Callback);

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

                                executionState.TestFileSummaries.Enqueue(testSummary);
                            }
                        }
                        else if (executionState.TestOptions.TestLaunchMode == TestLaunchMode.Custom)
                        {
                            if (executionState.TestOptions.CustomTestLauncher == null)
                            {
                                throw new ArgumentNullException("TestOptions.CustomTestLauncher");
                            }
                            ChutzpahTracer.TraceInformation(
                                "Launching custom test on test harness '{0}' for file '{1}'",
                                testContext.TestHarnessPath,
                                testContext.FirstInputTestFile);
                            executionState.TestOptions.CustomTestLauncher.LaunchTest(testContext);
                        }
                        else
                        {
                            Debug.Fail("Unknown testing mode");
                        }
                    }
                    catch (Exception e)
                    {
                        var error = new TestError
                        {
                            InputTestFile = testContext.InputTestFiles.FirstOrDefault(),
                            Message = e.ToString()
                        };

                        executionState.TestErrors.Add(error);
                        executionState.Callback.FileError(error);

                        ChutzpahTracer.TraceError(e, "Error during test execution of {0}", testContext.FirstInputTestFile);
                    }
                    finally
                    {
                        ChutzpahTracer.TraceInformation("Finished test run for {0} in {1} mode", testContext.FirstInputTestFile, executionState.TestOptions.TestExecutionMode);
                    }
                });
        }


        public void CleanupTestRun(ChutzpahExeuctionState executionState)
        {
            // Clean up test context
            foreach (var testContext in executionState.TestContexts)
            {
                // Don't clean up context if in debug mode
                if (!m_debugEnabled
                    && !testContext.TestHarnessCreationFailed
                    && executionState.TestOptions.TestLaunchMode != TestLaunchMode.FullBrowser
                    && executionState.TestOptions.TestLaunchMode != TestLaunchMode.Custom)
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

            if (executionState.WebServerHost != null
                && executionState.TestOptions.TestLaunchMode != TestLaunchMode.FullBrowser
                && executionState.TestOptions.TestLaunchMode != TestLaunchMode.Custom)
            {
                executionState.WebServerHost.Dispose();
            }
        }


        private void CreateTestContexts(
                      ChutzpahExeuctionState executionState,
                      List<List<PathInfo>> scriptPathGroups)
        {
            var resultCount = 0;
            Parallel.ForEach(scriptPathGroups, executionState.ParallelOptions, testFiles =>
            {
                var pathString = string.Join(",", testFiles.Select(x => x.FullPath));
                ChutzpahTracer.TraceInformation("Trying to build test context for {0}", pathString);

                try
                {
                    if (executionState.CacellationSource.IsCancellationRequested) return;
                    TestContext testContext;

                    resultCount++;
                    if (testContextBuilder.TryBuildContext(testFiles, executionState.TestOptions, out testContext))
                    {
                        executionState.TestContexts.Add(testContext);
                    }
                    else
                    {
                        ChutzpahTracer.TraceWarning("Unable to build test context for {0}", pathString);
                    }

                    // Limit the number of files we can scan to attempt to build a context for
                    // This is important in the case of folder scanning where many JS files may not be
                    // test files.
                    if (resultCount >= executionState.TestOptions.FileSearchLimit)
                    {
                        ChutzpahTracer.TraceError("File search limit hit!!!");
                        executionState.CacellationSource.Cancel();
                    }
                }
                catch (Exception e)
                {
                    var error = new TestError
                    {
                        InputTestFile = testFiles.Select(x => x.FullPath).FirstOrDefault(),
                        Message = e.ToString()
                    };

                    executionState.TestErrors.Add(error);
                    executionState.Callback.FileError(error);

                    ChutzpahTracer.TraceError(e, "Error during building test context for {0}", pathString);
                }
                finally
                {
                    ChutzpahTracer.TraceInformation("Finished building test context for {0}", pathString);
                }
            });
        }

        private IList<TestFileSummary> InvokeTestRunner(string headlessBrowserPath,
                                                 TestOptions options,
                                                 TestContext testContext,
                                                 ITestMethodRunnerCallback callback)
        {
            string runnerPath = fileProbe.FindFilePath(testContext.TestRunner);
            string fileUrl = BuildHarnessUrl(testContext);

            string runnerArgs = BuildRunnerArgs(options, testContext, fileUrl, runnerPath);
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

        private static string BuildRunnerArgs(TestOptions options, TestContext context, string fileUrl, string runnerPath)
        {
            string runnerArgs;
            var testModeStr = options.TestExecutionMode.ToString().ToLowerInvariant();
            var timeout = context.TestFileSettings.TestFileTimeout ?? options.TestFileTimeoutMilliseconds ?? Constants.DefaultTestFileTimeout;

            runnerArgs = string.Format("--ignore-ssl-errors=true --proxy-type=none --ssl-protocol=any \"{0}\" {1} {2} {3} {4} {5}",
                                       runnerPath,
                                       fileUrl,
                                       testModeStr,
                                       timeout,
                                       context.TestFileSettings.IgnoreResourceLoadingErrors.Value,
                                       context.TestFileSettings.UserAgent);


            return runnerArgs;
        }

        private string BuildHarnessUrl(TestContext testContext)
        {

            if (testContext.IsRemoteHarness)
            {
                return testContext.TestHarnessPath;
            }
            else
            {
                return string.Format("\"{0}\"", urlBuilder.GenerateFileUrl(testContext, testContext.TestHarnessPath, fullyQualified: true));
            }
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

                    var settingsFile = testSettingsService.FindSettingsFile(chutzpahJsonPath, options.ChutzpahSettingsFileEnvironments);
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

        private TestRunConfiguration BuildTestRunConfiguration(IEnumerable<PathInfo> scriptPaths, TestOptions testOptions)
        {
            var testRunConfiguration = new TestRunConfiguration();

            // Find all chutzpah.json files for the input files
            // Then group files by their respective settings file
            var testGroups = new List<List<PathInfo>>();
            var fileSettingGroups = from path in scriptPaths
                                    let settingsFile = testSettingsService.FindSettingsFile(path.FullPath, testOptions.ChutzpahSettingsFileEnvironments)
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

            testRunConfiguration.TestGroups = testGroups;

            // Take the parallelism degree to be the minimum of any non-null setting in chutzpah.json 
            testRunConfiguration.MaxDegreeOfParallelism = fileSettingGroups.Min(x => x.Key.Parallelism);

            // Enable tracing if any setting is true
            testRunConfiguration.EnableTracing = fileSettingGroups.Any(x => x.Key.EnableTracing.HasValue && x.Key.EnableTracing.Value);

            testRunConfiguration.TraceFilePath = fileSettingGroups.Select(x => x.Key.TraceFilePath).FirstOrDefault(x => !string.IsNullOrEmpty(x)) ?? testRunConfiguration.TraceFilePath;

            return testRunConfiguration;
        }

        private bool PerformBatchCompile(ITestMethodRunnerCallback callback, IEnumerable<TestContext> testContexts)
        {
            try
            {
                batchCompilerService.Compile(testContexts, callback);
            }
            catch (FileNotFoundException e)
            {
                callback.ExceptionThrown(e);

                ChutzpahTracer.TraceError(e, "Error during batch compile");

                return false;
            }
            catch (ChutzpahCompilationFailedException e)
            {
                callback.ExceptionThrown(e, e.SettingsFile);

                ChutzpahTracer.TraceError(e, "Error during batch compile from {0}", e.SettingsFile);
                return false;
            }

            return true;
        }

        private void ConfigureTracing(TestRunConfiguration testRunConfiguration)
        {
            var path = testRunConfiguration.TraceFilePath;
            if (testRunConfiguration.EnableTracing)
            {
                ChutzpahTracer.AddFileListener(path);
            }
            else
            {
                // TODO (mmanela): There is a known issue with this if the user is running chutzpah in VS and changes their trace path
                // This will result in that path not getting removed until the VS is restarted. To fix this we need to keep trace of previous paths 
                // and clear them all out.
                ChutzpahTracer.RemoveFileListener(path);
            }
        }

        private IChutzpahWebServerHost SetupWebServerHost(ConcurrentBag<TestContext> testContexts)
        {
            IChutzpahWebServerHost webServerHost = null;
            var contextUsingWebServer = testContexts.Where(x => x.TestFileSettings.Server != null && x.TestFileSettings.Server.Enabled.GetValueOrDefault()).ToList();
            var contextWithChosenServerConfiguration = contextUsingWebServer.FirstOrDefault();
            if (contextWithChosenServerConfiguration != null)
            {
                var webServerConfiguration = contextWithChosenServerConfiguration.TestFileSettings.Server;
                webServerHost = webServerFactory.CreateServer(webServerConfiguration, ActiveWebServerHost);

                // Stash host object on context for use in url generation
                contextUsingWebServer.ForEach(x => x.WebServerHost = webServerHost);
            }

            return webServerHost;
        }


    }
}
