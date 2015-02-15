using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    /// Reads from the stream of test results writen by our phantom test runner. As events from this stream arrive we 
    /// will derserialize them and publish them to the runner callback.
    /// The reader keeps track of how long it has been since the last event has been revieved from the stream. If this is longer
    /// than the configured test file timeout then we kill phantom since it is likely stuck in a infinite loop or error.
    /// We make this timeout the test file timeout plus a small (generous) delay time to account for serialization. 
    /// </summary>
    public class TestCaseStreamReader : ITestCaseStreamReader
    {
        private readonly IJsonSerializer jsonSerializer;
        private readonly Regex prefixRegex = new Regex("^#_#(?<type>[a-z]+)#_#(?<json>.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private const string internalLogPrefix = "!!_!!";

        // Tracks the last time we got an event/update from phantom. 
        private DateTime lastTestEvent;
        private readonly ICoverageEngine coverageEngine;

        public TestCaseStreamReader(ICoverageEngine coverageEngine)
        {
            jsonSerializer = new JsonSerializer();
            this.coverageEngine = coverageEngine;
        }

        public IList<TestFileSummary> Read(ProcessStream processStream, TestOptions testOptions, TestContext testContext, ITestMethodRunnerCallback callback, bool debugEnabled)
        {
            if (processStream == null) throw new ArgumentNullException("processStream");
            if (testOptions == null) throw new ArgumentNullException("testOptions");
            if (testContext == null) throw new ArgumentNullException("testContext");

            lastTestEvent = DateTime.Now;
            var timeout = (testContext.TestFileSettings.TestFileTimeout ?? testOptions.TestFileTimeoutMilliseconds) + 500; // Add buffer to timeout to account for serialization
            var readerTask = Task<IList<TestFileSummary>>.Factory.StartNew(() => ReadFromStream(processStream.StreamReader, testContext, testOptions, callback, debugEnabled));
            while (readerTask.Status == TaskStatus.WaitingToRun
               || (readerTask.Status == TaskStatus.Running && (DateTime.Now - lastTestEvent).TotalMilliseconds < timeout))
            {
                Thread.Sleep(100);
            }

            if (readerTask.IsCompleted)
            {
                ChutzpahTracer.TraceInformation("Finished reading stream from test file '{0}'", testContext.FirstInputTestFile);
                return readerTask.Result;
            }
            else
            {
                // We timed out so kill the process and return an empty test file summary
                ChutzpahTracer.TraceError("Test file '{0}' timed out after running for {1} milliseconds", testContext.FirstInputTestFile, (DateTime.Now - lastTestEvent).TotalMilliseconds);

                processStream.TimedOut = true;
                processStream.KillProcess();
                return testContext.ReferencedFiles.Where(x => x.IsFileUnderTest).Select(file => new TestFileSummary(file.Path)).ToList();
            }
        }

        class TestFileContext
        {
            public ReferencedFile ReferencedFile { get; set; }
            public TestFileSummary TestFileSummary { get; set; }

            public TestContext TestContext { get; set; }

            public bool IsUsed { get; set; }

            public HashSet<Tuple<string, string>> SeenTests { get; set; }

            public TestFileContext(ReferencedFile referencedFile, TestContext testContext, bool coverageEnabled)
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

        private void FireTestFinished(ITestMethodRunnerCallback callback, TestFileContext testFileContext, JsRunnerOutput jsRunnerOutput, int testIndex)
        {
            var jsTestCase = jsRunnerOutput as JsTestCase;
            jsTestCase.TestCase.InputTestFile = testFileContext.ReferencedFile.Path;
            AddLineNumber(testFileContext.ReferencedFile, testIndex, jsTestCase);
            callback.TestFinished(jsTestCase.TestCase);
            testFileContext.TestFileSummary.AddTestCase(jsTestCase.TestCase);
        }

        private void FireFileStarted(ITestMethodRunnerCallback callback, TestContext testContext)
        {
            callback.FileStarted(testContext.InputTestFilesString);
        }

        private void FireCoverageObject(ITestMethodRunnerCallback callback, TestFileContext testFileContext, JsRunnerOutput jsRunnerOutput)
        {
            var jsCov = jsRunnerOutput as JsCoverage;
            testFileContext.TestFileSummary.CoverageObject = coverageEngine.DeserializeCoverageObject(jsCov.Object, testFileContext.TestContext);
        }

        private void FireFileFinished(ITestMethodRunnerCallback callback, string testFilesString, IEnumerable<TestFileContext> testFileContexts, JsRunnerOutput jsRunnerOutput)
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

        private void FireLogOutput(ITestMethodRunnerCallback callback, TestFileContext testFileContext, JsRunnerOutput jsRunnerOutput)
        {
            var log = jsRunnerOutput as JsLog;

            // This is an internal log message
            if (log.Log.Message.StartsWith(internalLogPrefix))
            {
                ChutzpahTracer.TraceInformation("Phantom Log - {0}", log.Log.Message.Substring(internalLogPrefix.Length).Trim());
                return;
            }

            log.Log.InputTestFile = testFileContext.ReferencedFile.Path;
            callback.FileLog(log.Log);
            testFileContext.TestFileSummary.Logs.Add(log.Log);
        }

        private void FireErrorOutput(ITestMethodRunnerCallback callback, TestFileContext testFileContext, JsRunnerOutput jsRunnerOutput)
        {
            var error = jsRunnerOutput as JsError;

            error.Error.InputTestFile = testFileContext.ReferencedFile.Path;
            callback.FileError(error.Error);
            testFileContext.TestFileSummary.Errors.Add(error.Error);

            ChutzpahTracer.TraceError("Eror recieved from Phantom {0}", error.Error.Message);
        }

        private IList<TestFileSummary> ReadFromStream(StreamReader stream, TestContext testContext, TestOptions testOptions, ITestMethodRunnerCallback callback, bool debugEnabled)
        {
            var codeCoverageEnabled = (!testContext.TestFileSettings.EnableCodeCoverage.HasValue && testOptions.CoverageOptions.Enabled)
                                      || (testContext.TestFileSettings.EnableCodeCoverage.HasValue && testContext.TestFileSettings.EnableCodeCoverage.Value);

            var testFileContexts = testContext.ReferencedFiles
                                              .Where(x => x.IsFileUnderTest)
                                              .Select(x => new TestFileContext(x, testContext, codeCoverageEnabled))
                                              .ToList();


            var testIndex = 0;

            string line;
            TestFileContext currentTestFileContext = null;

            if (testFileContexts.Count == 1)
            {
                currentTestFileContext = testFileContexts.First();
            }

            var deferredEvents = new List<Action<TestFileContext>>();

            while ((line = stream.ReadLine()) != null)
            {
                if (debugEnabled) Console.WriteLine(line);

                var match = prefixRegex.Match(line);
                if (!match.Success) continue;
                var type = match.Groups["type"].Value;
                var json = match.Groups["json"].Value;

                // Only update last event timestamp if it is an important event.
                // Log and error could happen even though no test progress is made
                if (!type.Equals("Log") && !type.Equals("Error"))
                {
                    lastTestEvent = DateTime.Now;
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
                                deferredEvents.Add((fileContext) => FireCoverageObject(callback, fileContext, jsCov));
                            }
                            else
                            {
                                FireCoverageObject(callback, currentTestFileContext, jsCov);
                            }

                            break;

                        case "FileDone":

                            var jsFileDone = jsonSerializer.Deserialize<JsFileDone>(json);
                            FireFileFinished(callback, testContext.InputTestFilesString, testFileContexts, jsFileDone);

                            break;

                        case "TestStart":
                            var jsTestCaseStart = jsonSerializer.Deserialize<JsTestCase>(json);
                            TestFileContext newContext = null;
                            var testName = jsTestCaseStart.TestCase.TestName.Trim();
                            var moduleName = (jsTestCaseStart.TestCase.ModuleName ?? "").Trim();
                            
                            
                            var fileContexts = GetFileMatches(testName, testFileContexts);
                            if (fileContexts.Count == 0 && currentTestFileContext == null)
                            {
                                // If there are no matches and not file context has been used yet
                                // then just choose the first context
                                newContext = testFileContexts[0];

                                // If there is already a current context and no matches we just keep using that
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
                            callback.TestStarted(jsTestCaseStart.TestCase);

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
                                deferredEvents.Add((fileContext) => FireLogOutput(callback, fileContext, log));
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
                                deferredEvents.Add((fileContext) => FireErrorOutput(callback, fileContext, error));
                            }

                            break;
                    }
                }
                catch (SerializationException e)
                {
                    // Ignore malformed json and move on
                    ChutzpahTracer.TraceError(e, "Recieved malformed json from Phantom in this line: '{0}'", line);
                }
            }

            return testFileContexts.Select(x => x.TestFileSummary).ToList();
        }

        private static void PlayDeferredEvents(TestFileContext currentTestFileContext, List<Action<TestFileContext>> deferredEvents)
        {
            // Since we found a unique match we need to reply and log the events that came before this 
            // using this file context
            foreach (var deferredEvent in deferredEvents)
            {
                deferredEvent(currentTestFileContext);
            }

            deferredEvents.Clear();
        }

        private static IList<TestFileContext> GetFileMatches(string testName, IEnumerable<TestFileContext> testFileContexts)
        {
            var contextMatches = testFileContexts.Where(x => x.ReferencedFile.FilePositions.Contains(testName)).ToList();
            return contextMatches;
        }

        private static void AddLineNumber(ReferencedFile referencedFile, int testIndex, JsTestCase jsTestCase)
        {
            if (referencedFile != null && referencedFile.FilePositions.Contains(testIndex))
            {
                var position = referencedFile.FilePositions[testIndex];
                jsTestCase.TestCase.Line = position.Line;
                jsTestCase.TestCase.Column = position.Column;
            }
        }
    }
}