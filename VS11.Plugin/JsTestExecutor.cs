using System;
using System.Collections.Generic;
using System.Linq;
using Chutzpah.Models;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TestCase = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase;

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
            
			public void TestSuiteStarted() { }
			public void TestSuiteFinished(TestCaseSummary testResultsSummary) { }


            public void FileError(TestError error)
            {
                var stack = "";
                foreach (var item in error.Stack)
                {
                    if (!string.IsNullOrEmpty(item.Function))
                    {
                        stack += "at " + item.Function + " ";
                    }
                    if (!string.IsNullOrEmpty(item.File))
                    {
                        stack += "in " + item.File;
                    }
                    if (!string.IsNullOrEmpty(item.Line))
                    {
                        stack += ":line " + item.Line;
                    }
                }

               frameworkHandle.SendMessage(TestMessageLevel.Error,string.Format("Test File Error:\n{0}\n {1}\nWhile Running:{2}\n\n", error.Message, stack, error.InputTestFile));
            }

            public void FileLog(TestLog log)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Informational,string.Format("Log Message: {0} from {1}\n", log.Message, log.InputTestFile));
            }

            public void ExceptionThrown(Exception exception, string fileName)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error,string.Format("Chutzpah Error:\n{0}\n While Running:{1}\n\n", exception, fileName));
            }

		    public void FileStarted(string fileName) { }
			public void FileFinished(string fileName, TestCaseSummary testResultsSummary) { }

            public void TestStarted(Models.TestCase test)
            {
                var testCase = test.ToVsTestCase();

                // The test case is starting
                frameworkHandle.RecordStart(testCase);
			    
			}
			public void TestFinished(Models.TestCase test)
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
