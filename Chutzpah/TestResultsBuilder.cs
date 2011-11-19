using System;
using System.Collections.Generic;
using System.Linq;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah
{
    public class TestResultsBuilder : ITestResultsBuilder
    {
        private readonly IJsonSerializer jsonSerializer;
        private readonly IHtmlUtility htmlUtility;
        private const string StartJsonDelimiter = "#_#Begin#_#";
        private const string EndJsonDelimiter = "#_#End#_#";

        public TestResultsBuilder(IJsonSerializer jsonSerializer, IHtmlUtility htmlUtility)
        {
            this.jsonSerializer = jsonSerializer;
            this.htmlUtility = htmlUtility;
        }

        public IEnumerable<TestResult> Build(BrowserTestFileResult browserTestFileResult)
        {
            if (browserTestFileResult == null) throw new ArgumentNullException("browserTestFileResult");
            if (string.IsNullOrWhiteSpace(browserTestFileResult.BrowserOutput)) 
                throw new ArgumentNullException("browserTestFileResult.BrowserOutput");

            var referencedFile = browserTestFileResult.TestContext.ReferencedJavaScriptFiles.SingleOrDefault(x => x.IsFileUnderTest);
            var testResults = new List<TestResult>();

            string json = ParseJsonResultFromBrowserOutput(browserTestFileResult.BrowserOutput);

            var rawResults = jsonSerializer.Deserialize<JsonTestOutput>(json);
            var testIndex = 0;

            foreach (JsonTestCase rawTest in rawResults.Results)
            {
                var test = new TestResult();
                test.InputTestFile = browserTestFileResult.TestContext.InputTestFile;
                test.HtmlTestFile = browserTestFileResult.TestContext.TestHarnessPath;
                test.ModuleName = htmlUtility.DecodeJavaScript(rawTest.Module);
                test.TestName = htmlUtility.DecodeJavaScript(rawTest.Name);
                test.Actual = htmlUtility.DecodeJavaScript(rawTest.Actual);
                test.Expected = htmlUtility.DecodeJavaScript(rawTest.Expected);
                test.Message = htmlUtility.DecodeJavaScript(rawTest.Message);
                test.Passed = rawTest.Passed;

                if (referencedFile != null)
                {
                    var position = referencedFile.FilePositions[testIndex];
                    test.Line = position.Line;
                    test.Column = position.Column;
                }

                testIndex++;
                testResults.Add(test);
            }


            return testResults;
        }

        private static string ParseJsonResultFromBrowserOutput(string browserResult)
        {
            var json = browserResult;
            var startIndex = browserResult.IndexOf(StartJsonDelimiter);
            var endIndex = browserResult.IndexOf(EndJsonDelimiter);
            if (startIndex >= 0)
            {
                startIndex += StartJsonDelimiter.Length;
                json = json.Substring(startIndex, endIndex - startIndex).Trim();
            }
            return json;
        }
    }
}