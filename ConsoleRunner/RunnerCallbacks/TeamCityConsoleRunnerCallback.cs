using System;
using Chutzpah.Models;

namespace Chutzpah.RunnerCallbacks
{
    public class TeamCityConsoleRunnerCallback : ConsoleRunnerCallback
    {
        private const string ChutzpahJavascriptTestSuiteName = "JavaScript Tests";

        public override void TestSuiteFinished(TestCaseSummary summary)
        {
            base.TestSuiteFinished(summary);

            Console.WriteLine("##teamcity[testSuiteFinished name='{0}']",Escape(ChutzpahJavascriptTestSuiteName));
        }

        public override void TestSuiteStarted()
        {
            Console.WriteLine("##teamcity[testSuiteStarted name='{0}']",Escape(ChutzpahJavascriptTestSuiteName));
        }

        protected override void TestFailed(TestCase testCase)
        {
            Console.WriteLine(
                "##teamcity[testFailed name='{0}' details='{1}']",
                Escape(GetTestDisplayText(testCase)),
                Escape(GetTestFailureMessage(testCase))
                );

            WriteOutput(GetTestDisplayText(testCase), GetTestFailureMessage(testCase));
        }

        protected override void TestComplete(TestCase testCase)
        {
            WriteFinished(GetTestDisplayText(testCase), 0);
        }

        protected override void TestPassed(TestCase testCase)
        {
            WriteOutput(GetTestDisplayText(testCase), "Passed");
        }

        public override void TestStarted(TestCase testCase)
        {
            Console.WriteLine(
                "##teamcity[testStarted name='{0}']", Escape(GetTestDisplayText(testCase)));
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
                                          Escape(name), (int)(duration * 1000D));
        }

        static void WriteOutput(string name, string output)
        {
            if (output != null)
                Console.WriteLine("##teamcity[testStdOut name='{0}' out='{1}']", Escape(name), Escape(output));
        }
    }
}