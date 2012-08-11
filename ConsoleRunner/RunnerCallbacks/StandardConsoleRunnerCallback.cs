using System;
using System.Globalization;
using System.Linq;
using Chutzpah.Models;

namespace Chutzpah.RunnerCallbacks
{
    public class StandardConsoleRunnerCallback : ConsoleRunnerCallback
    {
        readonly bool silent;
        readonly bool vsoutput;
        int testCount;

        public StandardConsoleRunnerCallback(bool silent, bool vsoutput)
        {
            this.silent = silent;
            this.vsoutput = vsoutput;
        }

        public override void TestSuiteFinished(TestCaseSummary testResultsSummary)
        {
            Console.WriteLine();
            var seconds = testResultsSummary.TimeTaken / 1000.0;
            Console.WriteLine("=== {0} total, {1} failed, took {2:n} seconds ===", testResultsSummary.TotalCount, testResultsSummary.FailedCount, seconds);

            base.TestSuiteFinished(testResultsSummary);
        }

        public override void FileFinished(string fileName, TestFileSummary testResultsSummary)
        {
            ClearCounter();

            Console.WriteLine("File: {0}", fileName);
            var seconds = testResultsSummary.TimeTaken / 1000.0;
            Console.WriteLine(Indent("{0} total, {1} failed, took {2:n} seconds", 2), testResultsSummary.TotalCount, testResultsSummary.FailedCount, seconds);
            Console.WriteLine();


            PrintRunningTestCount();

            base.FileFinished(fileName, testResultsSummary);
        }

        protected override void TestFailed(TestCase testCase)
        {            
            ClearCounter();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("{0} [FAIL]", testCase.GetDisplayName());
            Console.ResetColor();

            Console.WriteLine(Indent(GetTestFailureMessage(testCase)));

            Console.WriteLine();
        }

        protected override string GetTestFailureLocationString(TestCase testCase)
        {            
            if (vsoutput)
            {
                string s = String.Empty;

                foreach (var result in testCase.TestResults.Where(x => !x.Passed))
                {
                    s += string.Format("{0}({1},{2}):{3} {4} {5}: {6}: {7}\n",
                        testCase.InputTestFile,
                        testCase.Line,
                        testCase.Column,
                        "",
                        "error",
                        "C0001",
                        string.Format("Test '{0}' failed", GetTestDisplayText(testCase)),
                        GetTestResultsString(result));
                }

                return s;
            }

            return base.GetTestFailureLocationString(testCase);
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