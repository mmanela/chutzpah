using System;
using System.Globalization;
using Chutzpah.Models;

namespace Chutzpah.RunnerCallbacks
{
    public class StandardConsoleRunnerCallback : ConsoleRunnerCallback
    {
        readonly bool silent;
        int testCount;

        public StandardConsoleRunnerCallback(bool silent)
        {
            this.silent = silent;
        }

        public override void TestSuiteFinished(TestCaseSummary testResultsSummary)
        {
            Console.WriteLine();
            var seconds = testResultsSummary.TimeTakenMilliseconds / 1000.0;
            Console.WriteLine("=== {0} total, {1} failed, took {2:n} seconds ===", testResultsSummary.TotalCount, testResultsSummary.FailedCount, seconds);

            base.TestSuiteFinished(testResultsSummary);
        }

        public override void FileFinished(string fileName, TestCaseSummary testResultsSummary)
        {
            ClearCounter();

            Console.WriteLine("File: {0}", fileName);
            var seconds = testResultsSummary.TimeTakenMilliseconds / 1000.0;
            Console.WriteLine(Indent("{0} total, {1} failed, took {2:n} seconds", 2), testResultsSummary.TotalCount, testResultsSummary.FailedCount, seconds);
            Console.WriteLine();


            PrintRunningTestCount();

            base.FileFinished(fileName, testResultsSummary);
        }

        protected override void TestFailed(TestCase testCase)
        {
            ClearCounter();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("{0} [FAIL]", GetTestDisplayText(testCase));
            Console.ResetColor();

            Console.WriteLine(Indent(GetTestFailureMessage(testCase)));

            Console.WriteLine();
        }

        public override void ExceptionThrown(Exception exception, string fileName)
        {
            ClearCounter();

            Console.ForegroundColor = ConsoleColor.Red;

            var errorMessage = GetExceptionThrownMessage(exception, fileName);
            Console.WriteLine(errorMessage);
            Console.ResetColor();
        }

        public override void FileError(TestError error)
        {
            ClearCounter();

            Console.ForegroundColor = ConsoleColor.Red;

            var errorMessage = GetFileErrorMessage(error);
            Console.WriteLine(errorMessage);
            Console.ResetColor();
        }

        protected override void TestComplete(TestCase testCase)
        {
            ++testCount;
            PrintRunningTestCount();
        }

        void ClearCounter()
        {
            if (!silent)
            {

                Console.Write("\r");
                Console.Write(" ".PadLeft(Console.BufferWidth));
                Console.Write("\r");
            }
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