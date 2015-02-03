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
                ChutzpahTracer.TraceInformation("Finished reading stream from test file '{0}'", testContext.InputTestFile);
                return readerTask.Result;
            }
            else
            {
                // We timed out so kill the process and return an empty test file summary
                ChutzpahTracer.TraceError("Test file '{0}' timed out after running for {1} milliseconds", testContext.InputTestFile, (DateTime.Now - lastTestEvent).TotalMilliseconds);

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

            public TestFileContext(ReferencedFile referencedFile, TestContext testContext, bool coverageEnabled)
            {
                ReferencedFile = referencedFile;
                TestContext = testContext;
                TestFileSummary = new TestFileSummary(referencedFile.Path);

                if (coverageEnabled)
                {
                    TestFileSummary.CoverageObject = new CoverageData();
                }

            }
        }


        private void FireTestStarted(ITestMethodRunnerCallback callback, TestFileContext testFileContext, JsRunnerOutput jsRunnerOutput)
        {
            var jsTestCase = jsRunnerOutput as JsTestCase;
            jsTestCase.TestCase.InputTestFile = testFileContext.ReferencedFile.Path;
            callback.TestStarted(jsTestCase.TestCase);
        }

        private void FireTestFinished(ITestMethodRunnerCallback callback, TestFileContext testFileContext, JsRunnerOutput jsRunnerOutput, int testIndex)
        {
            var jsTestCase = jsRunnerOutput as JsTestCase;
            jsTestCase.TestCase.InputTestFile = testFileContext.ReferencedFile.Path;
            AddLineNumber(testFileContext.ReferencedFile, testIndex, jsTestCase);
            callback.TestFinished(jsTestCase.TestCase);
            testFileContext.TestFileSummary.AddTestCase(jsTestCase.TestCase);
        }

        private void FireFileStarted(ITestMethodRunnerCallback callback, TestFileContext testFileContext)
        {
            callback.FileStarted(testFileContext.ReferencedFile.Path);
        }

        private void FireCoverageObject(ITestMethodRunnerCallback callback, TestFileContext testFileContext, JsRunnerOutput jsRunnerOutput)
        {
            var jsCov = jsRunnerOutput as JsCoverage;
            testFileContext.TestFileSummary.CoverageObject = coverageEngine.DeserializeCoverageObject(jsCov.Object, testFileContext.TestContext);
        }

        private void FireFileFinished(ITestMethodRunnerCallback callback, TestFileContext testFileContext, JsRunnerOutput jsRunnerOutput)
        {
            var jsFileDone = jsRunnerOutput as JsFileDone;
            testFileContext.TestFileSummary.TimeTaken = jsFileDone.TimeTaken;
            callback.FileFinished(testFileContext.ReferencedFile.Path, testFileContext.TestFileSummary);
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
                            if(currentTestFileContext == null)
                            {
                                deferredEvents.Add((fileContext) => FireFileStarted(callback, fileContext));
                            }
                            else
                            {
                                FireFileStarted(callback, currentTestFileContext);
                            }

                            break;

                        case "CoverageObject":

                            var jsCov = jsonSerializer.Deserialize<JsCoverage>(json);

                            if (currentTestFileContext == null)
                            {
                                deferredEvents.Add((fileContext) => FireCoverageObject(callback,fileContext,jsCov));
                            }
                            else
                            {
                                FireCoverageObject(callback, currentTestFileContext, jsCov);
                            }

                            break;

                        case "FileDone":

                            if (currentTestFileContext == null)
                            {
                                // If we got here and still couldn't figure out which file these tests belong to then we
                                // just assume the first file not used yet is the one.
                                // NOTE: In the future we could be much smarted and deferr these tests until the very end and then do further analysis
                                var greedyFileContext = testFileContexts.FirstOrDefault(x => !x.IsUsed);

                                currentTestFileContext = greedyFileContext;
                                if(greedyFileContext == null)
                                {
                                    ChutzpahTracer.TraceError("Chutzpah was unable to figure out what path to associate of file of tests with. Skipping file!");
                                }
                                else
                                {
                                    ChutzpahTracer.TraceError("Chutzpah was unable to figure out what path to associate of file of tests with. Assuming {0}", greedyFileContext.ReferencedFile.Path);
                                }
                                
                            }

                            if (currentTestFileContext != null)
                            {
                                var jsFileDone = jsonSerializer.Deserialize<JsFileDone>(json);
                                FireFileFinished(callback, currentTestFileContext, jsFileDone);
                            }

                            // Rest test index for next file
                            testIndex = 0;
                            currentTestFileContext = null;
                            deferredEvents.Clear();
                            break;

                        case "TestStart":
                            var jsTestCaseStart = jsonSerializer.Deserialize<JsTestCase>(json);


                            if(currentTestFileContext != null)
                            {
                                FireTestStarted(callback, currentTestFileContext,jsTestCaseStart);
                            }
                            else
                            {
                                var fileContexts = GetFileMatches(jsTestCaseStart.TestCase.TestName, testFileContexts);
                                if(fileContexts.Count == 0 || fileContexts.Count > 1)
                                {
                                    // Either we couldnt figure out which files this test name can be for
                                    // or we found more than one possible match
                                    deferredEvents.Add((fileContext) => FireTestStarted(callback, fileContext, jsTestCaseStart));
                                }
                                else if(fileContexts.Count == 1)
                                {
                                    // We found a unique match
                                    currentTestFileContext = fileContexts[0];
                                    currentTestFileContext.IsUsed = true;

                                    PlayDeferredEvents(currentTestFileContext, deferredEvents);

                                    FireTestStarted(callback, currentTestFileContext, jsTestCaseStart);
                                }
                            }

                            break;

                        case "TestDone":
                            var jsTestCaseDone = jsonSerializer.Deserialize<JsTestCase>(json);
                            var currentTestIndex = testIndex;

                            if(currentTestFileContext != null)
                            {
                                FireTestFinished(callback, currentTestFileContext, jsTestCaseDone, currentTestIndex);
                            }
                            else
                            {
                                deferredEvents.Add((fileContext) => FireTestFinished(callback, fileContext, jsTestCaseDone, currentTestIndex));
                            }


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