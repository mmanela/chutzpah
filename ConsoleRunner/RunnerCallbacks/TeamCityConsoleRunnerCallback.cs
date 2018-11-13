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

        public override void FileLog(TestContext context, TestLog log)
        {
            _testCaseMessages.Add(GetFileLogMessage(log));
        }

        public override void TestContextStarted(TestContext context)
        {
            WriteTeamCity($"flowStarted flowId='{context?.TaskId ?? 0}'");
        }

        public override void TestContextFinished(TestContext context)
        {
            WriteTeamCity($"flowFinished flowId='{context?.TaskId ?? 0}'");
        }

        public override void TestSuiteFinished(TestContext context, TestCaseSummary summary)
        {
            base.TestSuiteFinished(context, summary);

            WriteTeamCity($"testSuiteFinished {NameAndFlowId(Escape(ChutzpahJavascriptTestSuiteName), context)}");
        }

        public override void TestSuiteStarted(TestContext context)
        {
            WriteTeamCity($"testSuiteStarted {NameAndFlowId(Escape(ChutzpahJavascriptTestSuiteName), context)}");
        }

        protected override void TestFailed(TestContext context, TestCase testCase)
        {
            WriteTeamCity($"testFailed {NameAndFlowId(Escape(testCase.GetDisplayName()), context)} details='{Escape(GetTestFailureMessage(testCase))}'");

            WriteOutput(context, testCase, CombineWithTestCaseMessages(GetTestFailureMessage(testCase)));
        }

        protected override void TestComplete(TestContext context, TestCase testCase)
        {
            WriteTeamCity($"testFinished {NameAndFlowId(Escape(testCase.GetDisplayName()), context)} duration='{(double)testCase.TimeTaken}'");
        }

        protected override void TestPassed(TestContext context, TestCase testCase)
        {
            WriteOutput(context, testCase, CombineWithTestCaseMessages("Passed"));
        }

        protected override void TestSkipped(TestContext context, TestCase testCase)
        {
            WriteTeamCity($"testIgnored {NameAndFlowId(Escape(testCase.GetDisplayName()), context)}");
        }

        public override void TestStarted(TestContext context, TestCase testCase)
        {
            _testCaseMessages.Clear();
            WriteTeamCity($"testStarted {NameAndFlowId(Escape(testCase.GetDisplayName()), context)}");
        }

        private string CombineWithTestCaseMessages(string output)
        {
            return string.Join("\n", _testCaseMessages.Concat(Enumerable.Repeat(output, 1)));
        }

        // Helpers

        static string NameAndFlowId(string name, TestContext context)
        {
            return $"name='{name}' flowId='{context?.TaskId ?? 0}'";
        }

        static string Escape(string value)
        {
            return value.Replace("|", "||")
                .Replace("'", "|'")
                .Replace("\r", "|r")
                .Replace("\n", "|n")
                .Replace("]", "|]");
        }

        static void WriteOutput(TestContext context, TestCase testCase, string output)
        {
            if (output != null)
                WriteTeamCity($"testStdOut {NameAndFlowId(Escape(testCase.GetDisplayName()), context)} out='{Escape(output)}'");
        }

        static void WriteTeamCity(string content)
        {
            Console.WriteLine("##teamcity[{0}]", content);
        }
       
    }
}