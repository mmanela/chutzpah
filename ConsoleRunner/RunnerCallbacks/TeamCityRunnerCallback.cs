using System;
using Chutzpah.Models;

namespace Chutzpah.RunnerCallbacks
{
    public class TeamCityRunnerCallback : RunnerCallback
    {
        private const string ChutzpahJavascriptTestSuiteName = "JavaScript Tests";

        public override void TestSuiteFinished(TestResultsSummary summary)
        {
            base.TestSuiteFinished(summary);

            Console.WriteLine("##teamcity[testSuiteFinished name='{0}']",Escape(ChutzpahJavascriptTestSuiteName));
        }

        public override void TestSuiteStarted()
        {
            Console.WriteLine("##teamcity[testSuiteStarted name='{0}']",Escape(ChutzpahJavascriptTestSuiteName));
        }

        protected override void TestFailed(TestResult result)
        {
            Console.WriteLine(
                "##teamcity[testFailed name='{0}' details='{1}']",
                Escape(GetTestDisplayText(result)),
                Escape(GetTestFailureMessage(result))
                );

            WriteOutput(GetTestDisplayText(result), GetTestFailureMessage(result));
        }

        protected override void TestComplete(TestResult result)
        {
            WriteFinished(GetTestDisplayText(result), 0);
        }

        protected override void TestPassed(TestResult result)
        {
            WriteOutput(GetTestDisplayText(result), "Passed");
        }

        protected override void TestStarted(TestResult result)
        {
            Console.WriteLine(
                "##teamcity[testStarted name='{0}']", Escape(GetTestDisplayText(result)));
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