using System;
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

        public TestFileSummary Read(ProcessStream processStream, TestOptions testOptions, TestContext testContext, ITestMethodRunnerCallback callback, bool debugEnabled)
        {
            if (processStream == null) throw new ArgumentNullException("processStream");
            if (testOptions == null) throw new ArgumentNullException("testOptions");
            if (testContext == null) throw new ArgumentNullException("testContext");
            
            lastTestEvent = DateTime.Now;
            var timeout = (testContext.TestFileSettings.TestFileTimeout ?? testOptions.TestFileTimeoutMilliseconds) + 500; // Add buffer to timeout to account for serialization
            var readerTask = Task<TestFileSummary>.Factory.StartNew(() => ReadFromStream(processStream.StreamReader, testContext, testOptions, callback, debugEnabled));
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
                return new TestFileSummary(testContext.InputTestFile);
            }
        }

        private TestFileSummary ReadFromStream(StreamReader stream, TestContext testContext, TestOptions testOptions, ITestMethodRunnerCallback callback, bool debugEnabled)
        {
            var referencedFile = testContext.ReferencedFiles.SingleOrDefault(x => x.IsFileUnderTest);
            var testIndex = 0;
            var summary = new TestFileSummary(testContext.InputTestFile);
            if (testOptions.CoverageOptions.Enabled)
            {
                summary.CoverageObject = new CoverageData();
            }

            string line;
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
                    JsTestCase jsTestCase = null;
                    switch (type)
                    {
                        case "FileStart":
                            callback.FileStarted(testContext.InputTestFile);
                            break;

                        case "CoverageObject":
                            var jsCov = jsonSerializer.Deserialize<JsCoverage>(json);
                            summary.CoverageObject = coverageEngine.DeserializeCoverageObject(jsCov.Object, testContext);
                            break;

                        case "FileDone":
                            var jsFileDone = jsonSerializer.Deserialize<JsFileDone>(json);
                            summary.TimeTaken = jsFileDone.TimeTaken;
                            callback.FileFinished(testContext.InputTestFile, summary);
                            break;

                        case "TestStart":
                            jsTestCase = jsonSerializer.Deserialize<JsTestCase>(json);
                            jsTestCase.TestCase.InputTestFile = testContext.InputTestFile;
                            callback.TestStarted(jsTestCase.TestCase);
                            break;

                        case "TestDone":
                            jsTestCase = jsonSerializer.Deserialize<JsTestCase>(json);
                            jsTestCase.TestCase.InputTestFile = testContext.InputTestFile;
                            AddLineNumber(referencedFile, testIndex, jsTestCase);
                            testIndex++;
                            callback.TestFinished(jsTestCase.TestCase);
                            summary.AddTestCase(jsTestCase.TestCase);
                            break;

                        case "Log":
                            var log = jsonSerializer.Deserialize<JsLog>(json);
                            
                            // This is an internal log message
                            if (log.Log.Message.StartsWith(internalLogPrefix))
                            {
                                ChutzpahTracer.TraceInformation("Phantom Log - {0}",log.Log.Message.Substring(internalLogPrefix.Length).Trim());
                                break;
                            }

                            log.Log.InputTestFile = testContext.InputTestFile;
                            callback.FileLog(log.Log);
                            summary.Logs.Add(log.Log);
                            break;

                        case "Error":

                            var error = jsonSerializer.Deserialize<JsError>(json);
                            error.Error.InputTestFile = testContext.InputTestFile;
                            callback.FileError(error.Error);
                            summary.Errors.Add(error.Error);

                            ChutzpahTracer.TraceError("Eror recieved from Phantom {0}", error.Error.Message);

                            break;
                    }
                }
                catch (SerializationException e)
                {
                    // Ignore malformed json and move on
                    ChutzpahTracer.TraceError(e, "Recieved malformed json from Phantom in this line: '{0}'", line);
                }
            }

            return summary;
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