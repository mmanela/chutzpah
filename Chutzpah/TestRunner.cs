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
using Chutzpah.Server;
using Chutzpah.Server.Models;

namespace Chutzpah
{
    public class TestRunner : ITestRunner
    {

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


        public TestRunner(IProcessHelper process,
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

        public IEnumerable<TestCase> DiscoverTests(IEnumerable<string> testPaths, TestOptions options, ITestMethodRunnerCallback callback)
        {
            var summary = ProcessTestPaths(testPaths, options, TestExecutionMode.Discovery, callback);
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
            
            var testCaseSummary = ProcessTestPaths(testPaths, options, TestExecutionMode.Execution, callback);

            callback.TestSuiteFinished(testCaseSummary);
            return testCaseSummary;
        }


        private TestCaseSummary ProcessTestPaths(IEnumerable<string> testPaths,
                                                 TestOptions options,
                                                 TestExecutionMode testExecutionMode,
                                                 ITestMethodRunnerCallback callback)
        {

            var overallSummary = new TestCaseSummary();
            options.TestExecutionMode = testExecutionMode;

            stopWatch.Start();
       

            // Concurrent list to collect test contexts
            var testContexts = new ConcurrentBag<TestContext>();

            // Concurrent collection used to gather the parallel results from
            var resultCount = 0;


            ChutzpahTracer.TraceInformation("Chutzpah run started in mode {0}", testExecutionMode);

            try
            {

               
                // Build test harness for each context and execute it in parallel
                ExecuteTestContexts(options, testExecutionMode, callback, testContexts, parallelOptions, headlessBrowserPath, testFileSummaries, cancellationSource, overallSummary, webServerHost);

                CleanTextContexts(options, testContexts, webServerHost);

                // Gather TestFileSummaries into TaseCaseSummary
                foreach (var fileSummary in testFileSummaries)
                {
                    overallSummary.Append(fileSummary);
                }

                stopWatch.Stop();
                overallSummary.SetTotalRunTime((int)stopWatch.Elapsed.TotalMilliseconds);

                overallSummary.TransformResult = transformProcessor.ProcessTransforms(testContexts, overallSummary);

                ChutzpahTracer.TraceInformation(
                    "Chutzpah run finished with {0} passed, {1} failed and {2} errors",
                    overallSummary.PassedCount,
                    overallSummary.FailedCount,
                    overallSummary.Errors.Count);

                return overallSummary;
            }
            catch (Exception e)
            {
                callback.ExceptionThrown(e);

                ChutzpahTracer.TraceError(e, "Unhandled exception during Chutzpah test run");

                return overallSummary;
            }
            finally
            {
                // Clear the settings file cache since in VS Chutzpah is not unloaded from memory.
                // If we don't clear then the user can never update the file.
                testSettingsService.ClearCache();
            }
        }


        

    }
}