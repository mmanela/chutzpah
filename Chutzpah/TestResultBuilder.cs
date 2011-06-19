using System;
using System.Collections.Generic;
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

        public TestResultsBuilder()
            : this(new JsonSerializer(), new HtmlUtility())
        {
        }

        public IEnumerable<TestResult> Build(BrowserTestFileResult browserTestFileResult)
        {
            if (browserTestFileResult == null) throw new ArgumentNullException("browserTestFileResult");
            if (string.IsNullOrWhiteSpace(browserTestFileResult.BrowserOutput)) throw new ArgumentNullException("browserTestFileResult.BrowserOutput");

            var testResults = new List<TestResult>();

            string json = ParseJsonResultFromBrowserOutput(browserTestFileResult.BrowserOutput);

            var rawResults = jsonSerializer.Deserialize<JsonTestOutput>(json);
            foreach (JsonTestCase rawTest in rawResults.Results)
            {
                var test = new TestResult();
                test.InputTestFile = browserTestFileResult.InputTestFile;
                test.HtmlTestFile = browserTestFileResult.HtmlTestFile;
                test.ModuleName = htmlUtility.DecodeJavaScript(rawTest.Module);
                test.TestName = htmlUtility.DecodeJavaScript(rawTest.Name);
                test.Actual = htmlUtility.DecodeJavaScript(rawTest.Actual);
                test.Expected = htmlUtility.DecodeJavaScript(rawTest.Expected);
                test.Message = htmlUtility.DecodeJavaScript(rawTest.Message);
                test.Passed = rawTest.State != null && rawTest.State.Equals("pass", StringComparison.OrdinalIgnoreCase);
                testResults.Add(test);
            }


            return testResults;
        }

        private static string ParseJsonResultFromBrowserOutput(string browserResult)
        {
            var json = browserResult;
            var startIndex = browserResult.IndexOf(StartJsonDelimiter);
            var endIndex = browserResult.IndexOf(EndJsonDelimiter);
            if(startIndex >= 0)
            {
                startIndex += StartJsonDelimiter.Length;
                json = json.Substring(startIndex, endIndex - startIndex).Trim();
            }
            return json;
        }
    }
}