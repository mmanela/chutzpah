using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chutzpah;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace VS11.Plugin
{
	[FileExtension(".js")]
	[ExtensionUri(Constants.ExecutorUriString)]
	[DefaultExecutorUri(Constants.ExecutorUriString)]
	public class VsTestRunner : ITestExecutor, ITestDiscoverer
	{
		class DiscoveryCallback : ITestMethodRunnerCallback
		{
			private IMessageLogger logger;
			private ITestCaseDiscoverySink discoverySink;

			public DiscoveryCallback(ITestCaseDiscoverySink discoverySink, IMessageLogger logger)
			{
				this.discoverySink = discoverySink;
				this.logger = logger;
			}

			// We're basically gonna ignore these for discovery purposes
			public void TestSuiteStarted() { }
			public void TestSuiteFinished(Chutzpah.Models.TestResultsSummary testResultsSummary) { }
			public void ExceptionThrown(Exception exception, string fileName) { }
			public bool FileStart(string fileName) { return true; }
			public bool FileFinished(string fileName, Chutzpah.Models.TestResultsSummary testResultsSummary) { return true; }

			public void TestFinished(Chutzpah.Models.TestResult result)
			{
				var testCase = result.ToVsTestCase();

				discoverySink.SendTestCase(testCase);
			}
		}

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

				// The test case is starting
				frameworkHandle.RecordStart(testCase);
	
				// Record a result (there can be many)
				frameworkHandle.RecordResult(vsresult);

				// The test case is done
				frameworkHandle.RecordEnd(testCase, result.ToVsTestOutcome());
			}

		}


		// The parameters on this might not match what you see if you are on a //build/ drop
		public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
		{
			var chutzpahRunner = TestRunner.Create(false);
			var callback = new DiscoveryCallback(discoverySink, logger);
			chutzpahRunner.RunTests(sources, callback);
		}

		public void Cancel()
		{
			// Noop - Not sure if we can do this...
		}

		// The parameters on this might not match what you see if you are on a //build/ drop
		public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
		{
			var chutzpahRunner = TestRunner.Create(false);
			var callback = new ExecutionCallback(frameworkHandle);
			chutzpahRunner.RunTests(sources, callback);
		}

		// The parameters on this might not match what you see if you are on a //build/ drop
		public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
		{
			// We'll just punt and run everything in each file that contains the selected tests
			var sources = tests.Select(test => test.Source).Distinct();
			RunTests(sources, runContext, frameworkHandle);
		}
	}
}
