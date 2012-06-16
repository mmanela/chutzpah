using System;
using System.Globalization;
using Chutzpah.Models;

namespace Chutzpah.RunnerCallbacks
{
    public class StandardRunnerCallback : RunnerCallback
    {
        readonly bool silent;
        int testCount;

        public StandardRunnerCallback(bool silent)
        {
            this.silent = silent;
        }

        public override void TestSuiteFinished(TestCaseSummary testResultsSummary)
        {

            if (!silent)
                Console.Write("\r");


            Console.WriteLine();
            Console.WriteLine("=== {0} total, {1} failed, took {2} seconds ===", testResultsSummary.TotalCount, testResultsSummary.FailedCount, 0);

            base.TestSuiteFinished(testResultsSummary);
        }

        public override void FileFinished(string fileName, TestCaseSummary testResultsSummary)
        {
            if (!silent)
                Console.Write("\r");

            Console.WriteLine("File: {0}", fileName);
            Console.WriteLine(Indent("{0} total, {1} failed, took {2} seconds", 2), testResultsSummary.TotalCount, testResultsSummary.FailedCount, 0);
            Console.WriteLine();


            PrintRunningTestCount();

            base.FileFinished(fileName, testResultsSummary);
        }

        protected override void TestFailed(TestCase testCase)
        {
            if (!silent)
                Console.Write("\r");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("{0} [FAIL]", GetTestDisplayText(testCase));
            Console.ResetColor();

            Console.WriteLine(Indent(GetTestFailureMessage(testCase)));

            Console.WriteLine();
        }

        protected override void TestComplete(TestCase testCase)
        {
            ++testCount;
            PrintRunningTestCount();
        }

        void PrintRunningTestCount()
        {
            if (!silent)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("\rTests complete: {0}", testCount);
                Console.ResetColor();
            }
        }

        string Indent(string message)
        {
            return Indent(message, 0);
        }

        string Indent(string message, int additionalSpaces)
        {
            string result = "";
            string indent = "".PadRight(additionalSpaces + 3);

            foreach (string line in message.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                result += indent + line + Environment.NewLine;

            return result.TrimEnd();
        }
    }
}