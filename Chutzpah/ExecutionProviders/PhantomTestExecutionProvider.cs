using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chutzpah.Models;

namespace Chutzpah
{
    public class PhantomTestExecutionProvider : ITestExecutionProvider
    {
        public static string HeadlessBrowserName = "phantomjs.exe";
        private readonly IProcessHelper processTools;
        private readonly IFileProbe fileProbe;
        private readonly IUrlBuilder urlBuilder;
        private readonly string headlessBrowserPath;
        private readonly ITestCaseStreamReaderFactory readerFactory;

        public bool CanHandleBrowser(Engine engine) => engine == Engine.Phantom;

        public PhantomTestExecutionProvider(IProcessHelper process, IFileProbe fileProbe,
                                       IUrlBuilder urlBuilder, ITestCaseStreamReaderFactory readerFactory)
        {
            this.processTools = process;
            this.fileProbe = fileProbe;
            this.urlBuilder = urlBuilder;
            this.headlessBrowserPath = fileProbe.FindFilePath(HeadlessBrowserName);

            if (headlessBrowserPath == null)
                throw new FileNotFoundException("Unable to find headless browser: " + HeadlessBrowserName);

            this.readerFactory = readerFactory;
        }

        public IList<TestFileSummary> Execute(TestOptions testOptions,
                            TestContext testContext,
                            TestExecutionMode testExecutionMode,
                            ITestMethodRunnerCallback callback)
        {

            string runnerPath = fileProbe.FindFilePath(testContext.TestRunner);
            string fileUrl = BuildHarnessUrl(testContext);
            string runnerArgs = BuildRunnerArgs(testOptions, testContext, fileUrl, runnerPath, testExecutionMode);

            var streamTimeout = ((testContext.TestFileSettings.TestFileTimeout ?? testOptions.TestFileTimeoutMilliseconds) + 500).GetValueOrDefault(); // Add buffer to timeout to account for serialization

            Func<ProcessStreamStringSource, TestCaseStreamReadResult> streamProcessor =
                processStream => readerFactory.Create().Read(processStream, testOptions, testContext, callback);

            var processResult = processTools.RunExecutableAndProcessOutput(headlessBrowserPath, runnerArgs, streamProcessor, streamTimeout, null);

            HandleTestProcessExitCode(testContext, processResult.ExitCode, testContext.FirstInputTestFile, processResult.Model.TestFileSummaries.Select(x => x.Errors).FirstOrDefault(), callback);

            return processResult.Model.TestFileSummaries;
        }


        private static void HandleTestProcessExitCode(TestContext context, int exitCode, string inputTestFile, IList<TestError> errors, ITestMethodRunnerCallback callback)
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

                callback.FileError(context, error);
                ChutzpahTracer.TraceError("Headless browser returned with an error: {0}", errorMessage);
            }
        }

        private static string BuildRunnerArgs(TestOptions options, TestContext context, string fileUrl, string runnerPath, TestExecutionMode testExecutionMode)
        {
            string runnerArgs;
            var testModeStr = testExecutionMode.ToString().ToLowerInvariant();
            var timeout = context.TestFileSettings.TestFileTimeout ?? options.TestFileTimeoutMilliseconds ?? Constants.DefaultTestFileTimeout;
            var proxy = options.Proxy ?? context.TestFileSettings.Proxy;
            var proxySetting = string.IsNullOrEmpty(proxy) ? "--proxy-type=none" : string.Format("--proxy={0}", proxy);
            runnerArgs = string.Format("--ignore-ssl-errors=true {0} --ssl-protocol=any \"{1}\" {2} {3} {4} {5} {6}",
                                       proxySetting,
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

        public void SetupEnvironment(TestOptions testOptions, TestContext testContext)
        {
        }
    }
}