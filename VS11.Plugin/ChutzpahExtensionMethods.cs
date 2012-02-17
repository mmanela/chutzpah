using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VS11.Plugin
{
	public static class ChutzpahExtensionMethods
	{
		public static Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase ToVsTestCase(this Chutzpah.Models.TestCase testCase)
		{
			return new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase(testCase.ModuleName + "." + testCase.TestName, Constants.ExecutorUri, testCase.InputTestFile)
			{
				CodeFilePath = testCase.InputTestFile,
				DisplayName = testCase.ModuleName + " " + testCase.TestName,
				LineNumber = testCase.Line,
			};
		}

		public static Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult ToVsTestResult(this Chutzpah.Models.TestResult result)
		{
			var testCase = result.ToVsTestCase();
			return new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(testCase)
			{
				DisplayName = testCase.DisplayName,
				ErrorMessage = result.Message,
				Outcome = result.ToVsTestOutcome()
			};
		}

		public static Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome ToVsTestOutcome(this Chutzpah.Models.TestResult result)
		{
			return result.Passed ? Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Passed : Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Failed;
		}
	}
}
