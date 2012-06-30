using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Chutzpah.VS2012.TestAdapter
{
    public static class ChutzpahExtensionMethods
    {
        public static TestCase ToVsTestCase(this Models.TestCase test)
        {
            return new TestCase(BuildFullyQualifiedName(test), Constants.ExecutorUri, test.InputTestFile)
                       {
                           CodeFilePath = test.InputTestFile,
                           DisplayName = GetTestDisplayText(test),
                           LineNumber = test.Line,
                       };
        }

        public static IEnumerable<TestResult> ToVsTestResults(this Models.TestCase test)
        {
            var testCase = test.ToVsTestCase();
            var results = test.TestResults.Select(result =>
                new TestResult(testCase)
                           {
                               DisplayName = testCase.DisplayName,
                               ErrorMessage = GetTestFailureMessage(result),
                               Outcome = ToVsTestOutcome(result.Passed)
                           });

            return results;
        }

        public static TestOutcome ToVsTestOutcome(bool passed)
        {
            return passed  ? TestOutcome.Passed : TestOutcome.Failed;
        }

        private static string BuildFullyQualifiedName(Models.TestCase testCase)
        {
            var parts = new[] {testCase.ModuleName, testCase.TestName, testCase.InputTestFile}.Where(x => !String.IsNullOrEmpty(x));
            return String.Join("::", parts);
        }

        private static string GetTestDisplayText(Models.TestCase testCase)
        {
            return string.IsNullOrWhiteSpace(testCase.ModuleName) ? testCase.TestName : string.Format("{0} {1}", testCase.ModuleName, testCase.TestName);
        }

        private static string GetTestFailureMessage(Models.TestResult result)
        {
            var errorString = "";
            if (result.Passed) return errorString;
            if (!string.IsNullOrWhiteSpace(result.Message))
            {
                errorString += string.Format("{0}", result.Message);
            }
            else if (result.Expected != null || result.Actual != null)
            {
                errorString += string.Format("Expected: {0}, Actual: {1}", result.Expected, result.Actual);
            }
            else
            {
                errorString += "Assert failed";
            }

            return errorString;
        }
    }
}