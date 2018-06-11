using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Chutzpah.Coverage;
using Chutzpah.Models;
using Chutzpah.Models.JS;
using Chutzpah.Transformers;
using Chutzpah.Wrappers;
using JsonSerializer = Chutzpah.Wrappers.JsonSerializer;

namespace Chutzpah
{
    /// <summary>
    /// Reads from the stream of test results written by our phantom test runner. As events from this stream arrive we 
    /// will deserialize them and publish them to the runner callback.
    /// The reader keeps track of how long it has been since the last event has been received from the stream. If this is longer
    /// than the configured test file timeout then we kill phantom since it is likely stuck in a infinite loop or error.
    /// We make this timeout the test file timeout plus a small (generous) delay time to account for serialization. 
    /// </summary>
    public class TestCaseStreamStringReader : ITestCaseStreamReader
    {
        private readonly IJsonSerializer jsonSerializer;
        private readonly Regex prefixRegex = new Regex("^#_#(?<type>[a-z]+)#_#(?<json>.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private const string internalLogPrefix = "!!_!!";

        // Tracks the last time we got an event/update from phantom. 
        private int testIndex;
        StreamingTestFileContext currentTestFileContext = null;

        public TestCaseStreamStringReader()
        {
            jsonSerializer = new JsonSerializer();
        }

        public TestCaseStreamReadResult Read(TestCaseSource<string> testCaseSource, TestOptions testOptions, TestContext testContext, ITestMethodRunnerCallback callback)
        {
            if (testCaseSource == null) throw new ArgumentNullException(nameof(testCaseSource));
            if (testOptions == null) throw new ArgumentNullException(nameof(testOptions));
            if (testContext == null) throw new ArgumentNullException(nameof(testContext));

            var testCaseStreamReadResult = new TestCaseStreamReadResult();


            var codeCoverageEnabled = testOptions.CoverageOptions.ShouldRunCoverage(testContext.TestFileSettings.CodeCoverageExecutionMode);

            var streamingTestFileContexts = testContext.ReferencedFiles
                                              .Where(x => x.IsFileUnderTest)
                                              .Select(x => new StreamingTestFileContext(x, testContext, codeCoverageEnabled))
                                              .ToList();

            var deferredEvents = new List<Action<StreamingTestFileContext>>();


            if (streamingTestFileContexts.Count == 1)
            {
                currentTestFileContext = streamingTestFileContexts.First();
            }

            testCaseSource.Subscribe((line) => ProcessLine(line, testContext, streamingTestFileContexts, deferredEvents, callback, testOptions.DebugEnabled));

            var readerTask = testCaseSource.Open();
            while ((readerTask.Status == TaskStatus.WaitingToRun
                || readerTask.Status == TaskStatus.WaitingForActivation
               || readerTask.Status == TaskStatus.Running) && testCaseSource.IsAlive)
            {
                Thread.Sleep(100);
            }

            if (readerTask.IsCompleted)
            {
                ChutzpahTracer.TraceInformation("Finished reading stream from test file '{0}'", testContext.FirstInputTestFile);
            }
            else
            {

                // Since we timed out make sure we play the deferred events so we do not lose errors
                // We will just attach these events to the first test context at this point since we do
                // not know where they belong
                PlayDeferredEvents(streamingTestFileContexts.FirstOrDefault(), deferredEvents);

                // We timed out so kill the process
                ChutzpahTracer.TraceError("Test file '{0}' timed out after running for {1} milliseconds", testContext.FirstInputTestFile, (DateTime.Now - testCaseSource.LastTestEvent).TotalMilliseconds);


                testCaseSource.Dispose();
                testCaseStreamReadResult.TimedOut = true;
                FailTimedoutOutput(callback, streamingTestFileContexts);
            }

            FileNotRanTests(streamingTestFileContexts);
            testCaseStreamReadResult.TestFileSummaries = streamingTestFileContexts.Select(x => x.TestFileSummary).ToList();
            return testCaseStreamReadResult;
        }

        /// <summary>
        /// Fail the timed out test
        /// </summary>
        /// <param name="streamingTestFileContexts">List of StreamingTestFileContext</param>
        private void FailTimedoutOutput(ITestMethodRunnerCallback callback, IList<StreamingTestFileContext> streamingTestFileContexts)
        {
            foreach (var streamingTestFileContext in streamingTestFileContexts)
            {
                var failedTest = new TestCase();
                var timedOutTest = FindTimedoutTest(streamingTestFileContext);
                if (timedOutTest != null)
                {
                    failedTest.InputTestFile = streamingTestFileContext.ReferencedFile.Path;
                    failedTest.ModuleName = timedOutTest.Item1;
                    failedTest.TestName = timedOutTest.Item2;
                    failedTest.Line = streamingTestFileContext.ReferencedFile.FilePositions[timedOutTest.Item2].Line;
                    failedTest.Column = streamingTestFileContext.ReferencedFile.FilePositions[timedOutTest.Item2].Column;
                    failedTest.TestResults.Add(new TestResult { Passed = false, Message = "Timeout occurred when executing test file." });
                    streamingTestFileContext.TestFileSummary.AddTestCase(failedTest);

                    callback.TestStarted(failedTest);
                    callback.TestFinished(failedTest);
                }
            }
        }

        /// <summary>
        /// Find tests which are not ran
        /// </summary>
        /// <param name="streamingTestFileContexts">List of StreamingTestFileContext</param>
        private void FileNotRanTests(IList<StreamingTestFileContext> streamingTestFileContexts)
        {
            foreach (var streamingTestFileContext in streamingTestFileContexts)
            {
                int totalTestsCountInFile = streamingTestFileContext.ReferencedFile.FilePositions.TotalTestsCount();
                for (int i = 0; i < totalTestsCountInFile; i++)
                {
                    string testNameInFile = streamingTestFileContext.ReferencedFile.FilePositions[i].TestName;
                    if (!streamingTestFileContext.SeenTests.Any(t => (t.Item2 == testNameInFile)))
                    {
                        var notRanTest = new TestCase();
                        notRanTest.InputTestFile = streamingTestFileContext.ReferencedFile.Path;
                        notRanTest.TestName = testNameInFile;
                        notRanTest.Line = streamingTestFileContext.ReferencedFile.FilePositions[i].Line;
                        notRanTest.Column = streamingTestFileContext.ReferencedFile.FilePositions[i].Column;
                        notRanTest.NotRan = true;
                        streamingTestFileContext.TestFileSummary.AddTestCase(notRanTest);
                    }
                }
            }
        }
        class StreamingTestFileContext
        {
            public ReferencedFile ReferencedFile { get; set; }
            public TestFileSummary TestFileSummary { get; set; }

            public TestContext TestContext { get; set; }

            public bool IsUsed { get; set; }

            public HashSet<Tuple<string, string>> SeenTests { get; set; }

            public StreamingTestFileContext(ReferencedFile referencedFile, TestContext testContext, bool coverageEnabled)
            {
                SeenTests = new HashSet<Tuple<string, string>>();
                ReferencedFile = referencedFile;
                TestContext = testContext;
                TestFileSummary = new TestFileSummary(referencedFile.Path);

                if (coverageEnabled)
                {
                    TestFileSummary.CoverageObject = new CoverageData();
                }

            }

            public bool HasTestBeenSeen(string module, string test)
            {
                return SeenTests.Contains(Tuple.Create(module, test));
            }

            public void MarkTestSeen(string module, string test)
            {
                SeenTests.Add(Tuple.Create(module ?? "", test));
            }
        }

        private void FireTestFinished(ITestMethodRunnerCallback callback, StreamingTestFileContext testFileContext, JsRunnerOutput jsRunnerOutput, int testIndex)
        {

            var jsTestCase = jsRunnerOutput as JsTestCase;
            jsTestCase.TestCase.InputTestFile = testFileContext.ReferencedFile.Path;
            jsTestCase.TestCase.PathFromTestSettingsDirectory = currentTestFileContext.ReferencedFile.PathFromTestSettingsDirectory;
            AddLineNumber(testFileContext.ReferencedFile, testIndex, jsTestCase);
            callback.TestFinished(jsTestCase.TestCase);
            testFileContext.TestFileSummary.AddTestCase(jsTestCase.TestCase);


            ChutzpahTracer.TraceInformation("Test Case Finished:'{0}'", jsTestCase.TestCase.GetDisplayName());

        }

        private void FireFileStarted(ITestMethodRunnerCallback callback, TestContext testContext)
        {
            callback.FileStarted(testContext.InputTestFilesString);
        }

        private void FireCoverageObject(ITestMethodRunnerCallback callback, StreamingTestFileContext testFileContext, JsRunnerOutput jsRunnerOutput)
        {
            var jsCov = jsRunnerOutput as JsCoverage;
            testFileContext.TestFileSummary.CoverageObject = testFileContext.TestContext.CoverageEngine.DeserializeCoverageObject(jsCov.Object, testFileContext.TestContext);
        }

        private void FireFileFinished(ITestMethodRunnerCallback callback, string testFilesString, IEnumerable<StreamingTestFileContext> testFileContexts, JsRunnerOutput jsRunnerOutput)
        {
            var jsFileDone = jsRunnerOutput as JsFileDone;

            var testFileSummary = new TestFileSummary(testFilesString);
            testFileSummary.TimeTaken = jsFileDone.TimeTaken;

            foreach (var context in testFileContexts)
            {

                context.TestFileSummary.TimeTaken = jsFileDone.TimeTaken;
                testFileSummary.AddTestCases(context.TestFileSummary.Tests);
            }

            callback.FileFinished(testFilesString, testFileSummary);
        }

        private void FireLogOutput(ITestMethodRunnerCallback callback, StreamingTestFileContext testFileContext, JsRunnerOutput jsRunnerOutput)
        {
            var log = jsRunnerOutput as JsLog;

            // This is an internal log message
            if (log.Log.Message.StartsWith(internalLogPrefix))
            {
                ChutzpahTracer.TraceInformation("Phantom Log - {0}", log.Log.Message.Substring(internalLogPrefix.Length).Trim());
                return;
            }

            log.Log.InputTestFile = testFileContext.ReferencedFile.Path;
            log.Log.PathFromTestSettingsDirectory = testFileContext.ReferencedFile.PathFromTestSettingsDirectory;
            callback.FileLog(log.Log);
            testFileContext.TestFileSummary.Logs.Add(log.Log);
        }

        private void FireErrorOutput(ITestMethodRunnerCallback callback, StreamingTestFileContext testFileContext, JsRunnerOutput jsRunnerOutput)
        {
            var error = jsRunnerOutput as JsError;

            error.Error.InputTestFile = testFileContext.ReferencedFile.Path;
            error.Error.PathFromTestSettingsDirectory = testFileContext.ReferencedFile.PathFromTestSettingsDirectory;
            callback.FileError(error.Error);
            testFileContext.TestFileSummary.Errors.Add(error.Error);

            if (testFileContext.TestContext.TestFileSettings.CreateFailedTestForFileError.GetValueOrDefault())
            {
                var fileErrorTest = new TestCase();
                fileErrorTest.InputTestFile = testFileContext.ReferencedFile.Path;
                fileErrorTest.PathFromTestSettingsDirectory = testFileContext.ReferencedFile.PathFromTestSettingsDirectory;
                fileErrorTest.TestName = string.Format("!! File Error #{0} - Error encountered outside of test case execution !!", testFileContext.TestFileSummary.Errors.Count);
                fileErrorTest.TestResults.Add(new TestResult { Passed = false, StackTrace = error.Error.StackAsString ?? error.Error.FormatStackObject(), Message = error.Error.Message });
                callback.TestStarted(fileErrorTest);
                callback.TestFinished(fileErrorTest);

                testFileContext.TestFileSummary.AddTestCase(fileErrorTest);
            }

            ChutzpahTracer.TraceError("Error received from Phantom {0}", error.Error.Message);
        }

        private Tuple<string, string> FindTimedoutTest(StreamingTestFileContext testFileContext)
        {
            foreach (var seenTest in testFileContext.SeenTests)
            {
                if (!(testFileContext.TestFileSummary.Tests.Any(t => (t.ModuleName == seenTest.Item1 && t.TestName == seenTest.Item2))))
                {
                    return seenTest;
                }
            }

            return null;
        }
        private bool ProcessLine(string line, TestContext testContext, IList<StreamingTestFileContext> streamingTestFileContexts, IList<Action<StreamingTestFileContext>> deferredEvents, ITestMethodRunnerCallback callback, bool debugEnabled)
        {

            bool isTestEvent = true;
            if (debugEnabled)
            {
                Console.WriteLine(line);
            }

            var match = prefixRegex.Match(line);
            if (!match.Success) { return false; }

            var type = match.Groups["type"].Value;
            var json = match.Groups["json"].Value;

            // Only update last event timestamp if it is an important event.
            // Log and error could happen even though no test progress is made
            if (!type.Equals("Log") && !type.Equals("Error"))
            {
                isTestEvent = false;
            }


            try
            {
                switch (type)
                {
                    case "FileStart":

                        FireFileStarted(callback, testContext);

                        break;

                    case "CoverageObject":

                        var jsCov = jsonSerializer.Deserialize<JsCoverage>(json);

                        if (currentTestFileContext == null)
                        {
                            AddDeferredEvent((fileContext) => FireCoverageObject(callback, fileContext, jsCov), deferredEvents);
                        }
                        else
                        {
                            FireCoverageObject(callback, currentTestFileContext, jsCov);
                        }

                        break;

                    case "FileDone":
                        var jsFileDone = jsonSerializer.Deserialize<JsFileDone>(json);
                        
                        FireFileFinished(callback, testContext.InputTestFilesString, streamingTestFileContexts, jsFileDone);
                        
                        break;

                    case "TestStart":
                        var jsTestCaseStart = jsonSerializer.Deserialize<JsTestCase>(json);
                        StreamingTestFileContext newContext = null;
                        var testName = jsTestCaseStart.TestCase.TestName.Trim();
                        var moduleName = (jsTestCaseStart.TestCase.ModuleName ?? "").Trim();


                        var fileContexts = GetFileMatches(testName, streamingTestFileContexts);
                        if (fileContexts.Count == 0 && currentTestFileContext == null)
                        {
                            // If there are no matches and not file context has been used yet
                            // then just choose the first context
                            newContext = streamingTestFileContexts[0];

                        }
                        else if (fileContexts.Count == 0)
                        {
                            // If there is already a current context and no matches we just keep using that context
                            // unless this test name has been used already in the current context. In that case
                            // move to the next one that hasn't seen this file yet

                            var testAlreadySeenInCurrentContext = currentTestFileContext.HasTestBeenSeen(moduleName, testName);
                            if (testAlreadySeenInCurrentContext)
                            {
                                newContext = streamingTestFileContexts.FirstOrDefault(x => !x.HasTestBeenSeen(moduleName, testName)) ?? currentTestFileContext;
                            }

                        }
                        else if (fileContexts.Count > 1)
                        {
                            // If we found the test has more than one file match
                            // try to choose the best match, otherwise just choose the first one

                            // If we have no file context yet take the first one
                            if (currentTestFileContext == null)
                            {
                                newContext = fileContexts.First();
                            }
                            else
                            {
                                // In this case we have an existing file context so we need to
                                // 1. Check to see if this test has been seen already on that context
                                //    if so we need to try the next file context that matches it
                                // 2. If it is not seen yet in the current context and the current context
                                //    is one of the matches then keep using it

                                var testAlreadySeenInCurrentContext = currentTestFileContext.HasTestBeenSeen(moduleName, testName);
                                var currentContextInFileMatches = fileContexts.Any(x => x == currentTestFileContext);
                                if (!testAlreadySeenInCurrentContext && currentContextInFileMatches)
                                {
                                    // Keep the current context
                                    newContext = currentTestFileContext;
                                }
                                else
                                {
                                    // Either take first not used context OR the first one
                                    newContext = fileContexts.Where(x => !x.IsUsed).FirstOrDefault() ?? fileContexts.First();
                                }
                            }
                        }
                        else if (fileContexts.Count == 1)
                        {
                            // We found a unique match
                            newContext = fileContexts[0];
                        }


                        if (newContext != null && newContext != currentTestFileContext)
                        {
                            currentTestFileContext = newContext;
                            testIndex = 0;
                        }

                        currentTestFileContext.IsUsed = true;

                        currentTestFileContext.MarkTestSeen(moduleName, testName);

                        PlayDeferredEvents(currentTestFileContext, deferredEvents);

                        jsTestCaseStart.TestCase.InputTestFile = currentTestFileContext.ReferencedFile.Path;
                        jsTestCaseStart.TestCase.PathFromTestSettingsDirectory = currentTestFileContext.ReferencedFile.PathFromTestSettingsDirectory;
                        callback.TestStarted(jsTestCaseStart.TestCase);


                        ChutzpahTracer.TraceInformation("Test Case Started:'{0}'", jsTestCaseStart.TestCase.GetDisplayName());

                        break;

                    case "TestDone":
                        var jsTestCaseDone = jsonSerializer.Deserialize<JsTestCase>(json);
                        var currentTestIndex = testIndex;

                        FireTestFinished(callback, currentTestFileContext, jsTestCaseDone, currentTestIndex);

                        testIndex++;

                        break;

                    case "Log":
                        var log = jsonSerializer.Deserialize<JsLog>(json);

                        if (currentTestFileContext != null)
                        {
                            FireLogOutput(callback, currentTestFileContext, log);
                        }
                        else
                        {
                            AddDeferredEvent((fileContext) => FireLogOutput(callback, fileContext, log), deferredEvents);
                        }
                        break;

                    case "Error":
                        var error = jsonSerializer.Deserialize<JsError>(json);
                        if (currentTestFileContext != null)
                        {
                            FireErrorOutput(callback, currentTestFileContext, error);
                        }
                        else
                        {
                            AddDeferredEvent((fileContext) => FireErrorOutput(callback, fileContext, error), deferredEvents);
                        }

                        break;
                }
            }
            catch (SerializationException e)
            {
                // Ignore malformed json and move on
                ChutzpahTracer.TraceError(e, "Received malformed json from Phantom in this line: '{0}'", line);
            }


            return isTestEvent;
        }


        private static void AddDeferredEvent(Action<StreamingTestFileContext> deferredEvent, IList<Action<StreamingTestFileContext>> deferredEvents)
        {
            lock (deferredEvents)
            {
                deferredEvents.Add(deferredEvent);
            }
        }

        private static void PlayDeferredEvents(StreamingTestFileContext currentTestFileContext, IList<Action<StreamingTestFileContext>> deferredEvents)
        {
            try
            {
                if (currentTestFileContext == null)
                {
                    return;
                }

                // Since we found a unique match we need to reply and log the events that came before this 
                // using this file context

                // We lock here since in the event of a timeout this may be run from the timeout handler while the phantom
                // process is still running
                lock (deferredEvents)
                {
                    foreach (var deferredEvent in deferredEvents)
                    {
                        deferredEvent(currentTestFileContext);
                    }

                    deferredEvents.Clear();
                }
            }
            catch (Exception e)
            {
                ChutzpahTracer.TraceError(e, "Unable to play deferred events");
            }
        }

        private static IList<StreamingTestFileContext> GetFileMatches(string testName, IEnumerable<StreamingTestFileContext> testFileContexts)
        {
            var contextMatches = testFileContexts.Where(x => x.ReferencedFile.FilePositions.Contains(testName)).ToList();
            return contextMatches;
        }

        private static void AddLineNumber(ReferencedFile referencedFile, int testIndex, JsTestCase jsTestCase)
        {
            if (referencedFile != null)
            {
                var position = referencedFile.FilePositions[jsTestCase.TestCase.TestName];
                if (position != null)
                {
                    jsTestCase.TestCase.Line = position.Line;
                    jsTestCase.TestCase.Column = position.Column;
                }
                else if (referencedFile.FilePositions.Contains(testIndex))
                {
                    position = referencedFile.FilePositions[testIndex];
                    jsTestCase.TestCase.Line = position.Line;
                    jsTestCase.TestCase.Column = position.Column;
                }
            }
        }
    }
}