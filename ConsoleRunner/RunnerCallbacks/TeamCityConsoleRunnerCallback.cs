using System;
using System.Linq;
using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah.RunnerCallbacks
{
    public class TeamCityConsoleRunnerCallback : ConsoleRunnerCallback
    {
        private const string ChutzpahJavascriptTestSuiteName = "JavaScript Tests";
        private readonly IList<string> _testCaseMessages = new List<string>();

        public override void FileLog(TestLog log)
        {
            _testCaseMessages.Add(GetFileLogMessage(log));
        }

        public override void TestSuiteFinished(TestCaseSummary summary)
        {
            base.TestSuiteFinished(summary);

            Console.WriteLine("##teamcity[testSuiteFinished name='{0}']",Escape(ChutzpahJavascriptTestSuiteName));
        }

        public override void TestSuiteStarted()
        {
            Console.WriteLine("##teamcity[testSuiteStarted name='{0}']",Escape(ChutzpahJavascriptTestSuiteName));
        }

        private string CombineWithTestCaseMessages(string output)
        {
            return string.Join("\n", _testCaseMessages.Concat(Enumerable.Repeat(output, 1)));
        }

        protected override void TestFailed(TestCase testCase)
        {
            Console.WriteLine(
                "##teamcity[testFailed name='{0}' details='{1}']",
                Escape(testCase.GetDisplayName()),
                Escape(GetTestFailureMessage(testCase))
                );

            WriteOutput(testCase.GetDisplayName(), CombineWithTestCaseMessages(GetTestFailureMessage(testCase)));
        }

        protected override void TestComplete(TestCase testCase)
        {
            WriteFinished(testCase.GetDisplayName(), testCase.TimeTaken);
        }

        protected override void TestPassed(TestCase testCase)
        {
            WriteOutput(testCase.GetDisplayName(), CombineWithTestCaseMessages("Passed"));
        }

        public override void TestStarted(TestCase testCase)
        {
            _testCaseMessages.Clear();
            Console.WriteLine(
                "##teamcity[testStarted name='{0}']", Escape(testCase.GetDisplayName()));
        }

        // Helpers

        static string Escape(string value)
        {
            return value.Replace("|", "||")
                .Replace("'", "|'")
                .Replace("\r", "|r")
                .Replace("\n", "|n")
                .Replace("]", "|]");
        }

        static void WriteFinished(string name, double duration)
        {
            Console.WriteLine("##teamcity[testFinished name='{0}' duration='{1}']",
                                          Escape(name), duration);
        }

        static void WriteOutput(string name, string output)
        {
            if (output != null)
                Console.WriteLine("##teamcity[testStdOut name='{0}' out='{1}']", Escape(name), Escape(output));
        }
    }
}