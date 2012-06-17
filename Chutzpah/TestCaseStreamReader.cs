using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Chutzpah.Models;
using Chutzpah.Models.JS;
using Chutzpah.Wrappers;
using Newtonsoft.Json;
using JsonSerializer = Chutzpah.Wrappers.JsonSerializer;

namespace Chutzpah
{
    public class TestCaseStreamReader : ITestCaseStreamReader
    {
        private readonly IJsonSerializer jsonSerializer;
        private readonly Regex prefixRegex = new Regex("^#_#(?<type>[a-z]+)#_#(?<json>.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public TestCaseStreamReader()
        {
            jsonSerializer = new JsonSerializer();
        }

        public TestCaseSummary Read(StreamReader stream, TestContext testContext, ITestMethodRunnerCallback callback, bool debugEnabled)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (testContext == null) throw new ArgumentNullException("testContext");
            
            var referencedFile = testContext.ReferencedJavaScriptFiles.SingleOrDefault(x => x.IsFileUnderTest);
            var testIndex = 0;
            var summary = new TestCaseSummary();
            string line;
            while ((line = stream.ReadLine()) != null)
            {
                if(debugEnabled) Console.WriteLine(line);

                var match = prefixRegex.Match(line);
                if (!match.Success) continue;
                var type = match.Groups["type"].Value;
                var json = match.Groups["json"].Value;

                try
                {
                    JsTestCase jsTestCase = null;
                    switch (type)
                    {
                        case "FileStart":
                            callback.FileStarted(testContext.InputTestFile);
                            break;

                        case "FileDone":
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
                            summary.Tests.Add(jsTestCase.TestCase);
                            break;

                        case "Log":
                            var log = jsonSerializer.Deserialize<JsLog>(json);
                            log.Log.InputTestFile = testContext.InputTestFile;
                            summary.Logs.Add(log.Log);
                            break;

                        case "Error":
                            var error = jsonSerializer.Deserialize<JsError>(json);
                            error.Error.InputTestFile = testContext.InputTestFile;
                            summary.Errors.Add(error.Error);
                            break;
                    }
                }
                catch (JsonSerializationException)
                {
                    // Ignore malformed json and move on
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