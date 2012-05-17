using System;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Chutzpah.VS11
{
    public static class ChutzpahExtensionMethods
    {
        public static TestCase ToVsTestCase(this Chutzpah.Models.TestCase testCase)
        {
            return new TestCase(BuildFullyQualifiedName(testCase), Constants.ExecutorUri, testCase.InputTestFile)
                       {
                           CodeFilePath = testCase.InputTestFile,
                           DisplayName = GetTestDisplayText(testCase),
                           LineNumber = testCase.Line,
                       };
        }

        public static TestResult ToVsTestResult(this Chutzpah.Models.TestResult result)
        {
            var testCase = result.ToVsTestCase();
            return new TestResult(testCase)
                       {
                           DisplayName = testCase.DisplayName,
                           ErrorMessage = GetTestFailureMessage(result),
                           Outcome = result.ToVsTestOutcome()
                       };
        }

        public static TestOutcome ToVsTestOutcome(this Chutzpah.Models.TestResult result)
        {
            return result.Passed ? TestOutcome.Passed : TestOutcome.Failed;
        }

        private static string BuildFullyQualifiedName(Chutzpah.Models.TestCase testCase)
        {
            var parts = new[] {testCase.ModuleName, testCase.TestName, testCase.InputTestFile}.Where(x => !String.IsNullOrEmpty(x));
            return String.Join("::", parts);
        }

        private static string GetTestDisplayText(Chutzpah.Models.TestCase testCase)
        {
            return string.IsNullOrWhiteSpace(testCase.ModuleName) ? testCase.TestName : string.Format("{0} {1}", testCase.ModuleName, testCase.TestName);
        }

        private static string GetTestFailureMessage(Chutzpah.Models.TestResult result)
        {
            var errorString = "";
            if (result.Expected != null || result.Actual != null)
            {
                errorString += string.Format("Expected: {0}, Actual: {1}", result.Expected, result.Actual);
            }
            else if (!string.IsNullOrWhiteSpace(result.Message))
            {
                errorString += string.Format("{0}", result.Message);
            }

            return errorString;
        }
    }
}