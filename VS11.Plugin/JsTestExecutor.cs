using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Chutzpah.VS11
{
    [ExtensionUri(Constants.ExecutorUriString)]
	public class JsTestExecutor : ITestExecutor
	{
		class ExecutionCallback : ITestMethodRunnerCallback
		{
			private IFrameworkHandle frameworkHandle;

			public ExecutionCallback(IFrameworkHandle frameworkHandle)
			{
				this.frameworkHandle = frameworkHandle;
			}

			// And we'll ignore these for execution
			// What we need, but don't have, is TestStarted
			public void TestSuiteStarted() { }
			public void TestSuiteFinished(Chutzpah.Models.TestCaseSummary testResultsSummary) { }
			public void ExceptionThrown(Exception exception, string fileName) { }
			public void FileStarted(string fileName) { }
			public void FileFinished(string fileName, Chutzpah.Models.TestCaseSummary testResultsSummary) { }

            public void TestStarted(Chutzpah.Models.TestCase test)
            {
                var testCase = test.ToVsTestCase();

                // The test case is starting
                frameworkHandle.RecordStart(testCase);
			    
			}
			public void TestFinished(Chutzpah.Models.TestCase test)
			{
                var testCase = test.ToVsTestCase();
                var results = test.ToVsTestResults();
                var outcome = ChutzpahExtensionMethods.ToVsTestOutcome(test.Passed);
	
				// Record a result (there can be many)
                foreach (var result in results)
                {
                    frameworkHandle.RecordResult(result);
                }

			    // The test case is done
                frameworkHandle.RecordEnd(testCase, outcome);
			}

		}


		public void Cancel()
		{
            // Will add code here when streaming tests is implemented
		}

		
		public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
		{
            if (runContext.IsDataCollectionEnabled)
            {
                // DataCollectors like Code Coverage are currently unavailable for JavaScript
                frameworkHandle.SendMessage(TestMessageLevel.Warning, "DataCollectors like Code Coverage are unavailable for JavaScript");
            }

			var chutzpahRunner = TestRunner.Create();
			var callback = new ExecutionCallback(frameworkHandle);
			chutzpahRunner.RunTests(sources, callback);
		}

		public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
		{
			// We'll just punt and run everything in each file that contains the selected tests
			var sources = tests.Select(test => test.Source).Distinct();
			RunTests(sources, runContext, frameworkHandle);
		}
	}
}
