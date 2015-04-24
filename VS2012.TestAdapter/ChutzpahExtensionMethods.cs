using System;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Chutzpah.VS2012.TestAdapter
{
    public static class ChutzpahExtensionMethods
    {
        public static TestCase ToVsTestCase(this Models.TestCase test)
        {
            var normalizedPath = test.InputTestFile.ToLowerInvariant();
            var testCase = new TestCase(BuildFullyQualifiedName(test), AdapterConstants.ExecutorUri, normalizedPath)
                {
                    CodeFilePath = normalizedPath,
                    DisplayName = GetTestDisplayText(test),
                    LineNumber = test.Line,
                };


            testCase.Traits.Add("Module", test.ModuleName);
            return testCase;
        }

        public static TestResult ToVsTestResult(this Models.TestCase test)
        {
            var testCase = test.ToVsTestCase();
            TestResult result;
            if (test.Passed)
            {
                result = new TestResult(testCase)
                    {
                        Duration = TimeSpan.FromMilliseconds(test.TimeTaken),
                        DisplayName = testCase.DisplayName,
                        Outcome = ToVsTestOutcome(true)
                    };
            }
            else
            {
                var failureResult = test.TestResults.FirstOrDefault(x => !x.Passed);
                result = new TestResult(testCase)
                    {
                        Duration = TimeSpan.FromMilliseconds(test.TimeTaken),
                        DisplayName = testCase.DisplayName,
                        ErrorMessage = GetTestFailureMessage(failureResult),
                        Outcome = ToVsTestOutcome(false),
                        ErrorStackTrace = BuildVirtualStackTrace(test)
                    };
            }

            return result;
        }

        public static TestOutcome ToVsTestOutcome(bool passed)
        {
            return passed ? TestOutcome.Passed : TestOutcome.Failed;
        }

        private static string BuildFullyQualifiedName(Models.TestCase testCase)
        {
            var parts = new[] { testCase.InputTestFile, testCase.ModuleName, testCase.TestName };
            return String.Join("::", parts).ToLowerInvariant();
        }

        private static string GetTestDisplayText(Models.TestCase testCase)
        {
            return string.IsNullOrWhiteSpace(testCase.ModuleName) ? testCase.TestName : string.Format("{0} {1}", testCase.ModuleName, testCase.TestName);
        }

        private static string GetTestFailureMessage(Models.TestResult result)
        {
            if (result == null) return "";
            return result.GetFailureMessage();
        }

        /// <summary>
        /// Builds an artifical stack trace so that errors coming from test adapter contains a reference to the file and test name
        /// without needing to read the whole log
        /// </summary>
        private static string BuildVirtualStackTrace(Models.TestCase testCase)
        {
            return string.Format("at {0} in {1}:line {2}{3}", GetTestDisplayText(testCase), testCase.InputTestFile, testCase.Line, Environment.NewLine);
        }

    }
}