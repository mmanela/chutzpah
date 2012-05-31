using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Chutzpah.VS11
{
	[FileExtension(".js")]
	[ExtensionUri(Constants.ExecutorUriString)]
	[DefaultExecutorUri(Constants.ExecutorUriString)]
	public class VsTestRunner : ITestExecutor, ITestDiscoverer
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
			public void TestSuiteFinished(Chutzpah.Models.TestResultsSummary testResultsSummary) { }
			public void ExceptionThrown(Exception exception, string fileName) { }
			public bool FileStart(string fileName) { return true; }
			public bool FileFinished(string fileName, Chutzpah.Models.TestResultsSummary testResultsSummary) { return true; }

			public void TestFinished(Chutzpah.Models.TestResult result)
			{
				var testCase = result.ToVsTestCase();
                var vsresult = result.ToVsTestResult();
                var outcome = result.ToVsTestOutcome();

				// The test case is starting
				frameworkHandle.RecordStart(testCase);
	
				// Record a result (there can be many)
				frameworkHandle.RecordResult(vsresult);

				// The test case is done
                frameworkHandle.RecordEnd(testCase, outcome);
			}

		}


		// The parameters on this might not match what you see if you are on a //build/ drop
		public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
		{
            var chutzpahRunner = TestRunner.Create();
            foreach (var testCase in chutzpahRunner.DiscoverTests(sources))
            {
                var vsTestCase = testCase.ToVsTestCase();
                discoverySink.SendTestCase(vsTestCase);
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
