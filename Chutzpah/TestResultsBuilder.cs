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


        public IEnumerable<TestCase> Build(BrowserTestFileResult browserTestFileResult, TestRunnerMode testRunnerMode)
        {
            if (browserTestFileResult == null) throw new ArgumentNullException("browserTestFileResult");
            if (string.IsNullOrWhiteSpace(browserTestFileResult.BrowserOutput))
                throw new ArgumentNullException("browserTestFileResult.BrowserOutput");

            var referencedFile = browserTestFileResult.TestContext.ReferencedJavaScriptFiles.SingleOrDefault(x => x.IsFileUnderTest);
            var testResults = new List<TestCase>();

            string json = ParseJsonResultFromBrowserOutput(browserTestFileResult.BrowserOutput);

            var rawResults = jsonSerializer.Deserialize<JsonTestOutput>(json);
            var testIndex = 0;

            foreach (JsonTestCase rawTest in rawResults.TestCases)
            {
                var result =   testRunnerMode == TestRunnerMode.Execution
                    ? BuildTestResult(browserTestFileResult, rawTest)
                    : BuildTestCase<TestCase>(browserTestFileResult, rawTest);


                if (referencedFile != null && referencedFile.FilePositions.Contains(testIndex))
                {
                    var position = referencedFile.FilePositions[testIndex];
                    result.Line = position.Line;
                    result.Column = position.Column;
                }

                testIndex++;
                testResults.Add(result);
            }


            return testResults;
        }

        private TestResult BuildTestResult(BrowserTestFileResult browserTestFileResult, JsonTestCase rawTest)
        {
            var test = BuildTestCase<TestResult>(browserTestFileResult, rawTest);
            test.Actual = htmlUtility.DecodeJavaScript(rawTest.Actual);
            test.Expected = htmlUtility.DecodeJavaScript(rawTest.Expected);
            test.Message = htmlUtility.DecodeJavaScript(rawTest.Message);
            test.Passed = rawTest.Passed;
            return test;
        }

        private T BuildTestCase<T>(BrowserTestFileResult browserTestFileResult, JsonTestCase rawTest) where T : TestCase, new()
        {
            var test = new T();
            test.InputTestFile = browserTestFileResult.TestContext.InputTestFile;
            test.HtmlTestFile = browserTestFileResult.TestContext.TestHarnessPath;
            test.ModuleName = htmlUtility.DecodeJavaScript(rawTest.Module);
            test.TestName = htmlUtility.DecodeJavaScript(rawTest.Name);
            return test;
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