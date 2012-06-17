using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TestCase = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase;

namespace Chutzpah.VS11
{
    [ExtensionUri(Constants.ExecutorUriString)]
	public class JsTestExecutor : ITestExecutor
	{
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
