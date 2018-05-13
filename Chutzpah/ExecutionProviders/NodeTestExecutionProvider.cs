using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chutzpah.Exceptions;
using Chutzpah.Models;

namespace Chutzpah
{
    public class NodeTestExecutionProvider : ITestExecutionProvider
    {
        public static string HeadlessBrowserName = "node.exe";
        private const string PackagesPath = @"Node\packages\node_modules";
        private readonly IProcessHelper processTools;
        private readonly IFileProbe fileProbe;
        private readonly IUrlBuilder urlBuilder;
        private readonly string headlessBrowserPath;
        private readonly ITestCaseStreamReaderFactory readerFactory;
        private readonly bool isRunningElevated;

        public bool CanHandleBrowser(Engine engine) => engine == Engine.Chrome || engine == Engine.JsDom;

        public NodeTestExecutionProvider(IProcessHelper process, IFileProbe fileProbe,
                                       IUrlBuilder urlBuilder, ITestCaseStreamReaderFactory readerFactory)
        {
            this.processTools = process;
            this.fileProbe = fileProbe;
            this.urlBuilder = urlBuilder;

            var path = Path.Combine("Node", Environment.Is64BitProcess ? "x64" : "x86", HeadlessBrowserName);
            this.headlessBrowserPath = fileProbe.FindFilePath(path);

            if (path == null)
                throw new FileNotFoundException("Unable to find node: " + path);

            this.readerFactory = readerFactory;

            isRunningElevated = process.IsRunningElevated();
        }

        public void SetupEnvironment(TestOptions testOptions, TestContext testContext)
        {
            if (testContext.TestFileSettings.Engine != Engine.Chrome || 
                (testContext.TestFileSettings.EngineOptions != null && testContext.TestFileSettings.EngineOptions.PreventDownloadOfEngineDepenedencies))
            {
                return;
            }

            
            string runnerPath = Directory.GetParent(fileProbe.FindFilePath(testContext.TestRunner)) + @"\setupRunner.js";
            var environmentVariables = BuildEnvironmentVariables();
            var processResult = processTools.RunExecutableAndProcessOutput(headlessBrowserPath, runnerPath, environmentVariables, out string standardOutput, out string standardError);

            if (!processResult)
            {
                throw new ChutzpahException($"Unable to install Chromium: Output: {standardOutput},   Error: {standardError}");
            }

        }

        public IList<TestFileSummary> Execute(TestOptions testOptions,
                            TestContext testContext,
                            TestExecutionMode testExecutionMode,
                            ITestMethodRunnerCallback callback)
        {

            string runnerPath = fileProbe.FindFilePath(testContext.TestRunner);
            string fileUrl = BuildHarnessUrl(testContext);
            bool preventDownloadOfEngineDepenedencies = testContext.TestFileSettings.EngineOptions != null && testContext.TestFileSettings.EngineOptions.PreventDownloadOfEngineDepenedencies;
            string runnerArgs = BuildRunnerArgs(testOptions, testContext, fileUrl, runnerPath, testExecutionMode, isRunningElevated, preventDownloadOfEngineDepenedencies);

            var streamTimeout = ((testContext.TestFileSettings.TestFileTimeout ?? testOptions.TestFileTimeoutMilliseconds) + 500).GetValueOrDefault(); // Add buffer to timeout to account for serialization

            TestCaseStreamReadResult streamProcessor(ProcessStreamStringSource processStream) => readerFactory.Create().Read(processStream, testOptions, testContext, callback);

            var environmentVariables = BuildEnvironmentVariables();
            var processResult = processTools.RunExecutableAndProcessOutput(headlessBrowserPath, runnerArgs, streamProcessor, streamTimeout, environmentVariables);

            HandleTestProcessExitCode(processResult.ExitCode, testContext.FirstInputTestFile, processResult.Model.TestFileSummaries.Select(x => x.Errors).FirstOrDefault(), callback);

            return processResult.Model.TestFileSummaries;
        }

        private IDictionary<string, string> BuildEnvironmentVariables()
        {
            var envVars = new Dictionary<string, string>();

            var chutzpahNodeModules = fileProbe.FindFolderPath(PackagesPath);
            envVars.Add("NODE_PATH", chutzpahNodeModules);
            return envVars;
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

        private static string BuildRunnerArgs(TestOptions options, TestContext context, string fileUrl, string runnerPath, TestExecutionMode testExecutionMode, bool isRunningElevated, bool tryToFindChrome)
        {
            string runnerArgs;
            var testModeStr = testExecutionMode.ToString().ToLowerInvariant();
            var timeout = context.TestFileSettings.TestFileTimeout ?? options.TestFileTimeoutMilliseconds ?? Constants.DefaultTestFileTimeout;
            runnerArgs = string.Format("{0} {1} {2} {3} {4} {5} {6}",
                                        runnerPath,
                                        fileUrl,
                                        testModeStr,
                                        timeout,
                                        isRunningElevated,
                                        context.TestFileSettings.IgnoreResourceLoadingErrors.Value,
                                        context.TestFileSettings.UserAgent,
                                        tryToFindChrome);

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
    }
}