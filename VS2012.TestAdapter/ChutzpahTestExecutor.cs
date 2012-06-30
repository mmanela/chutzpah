using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Chutzpah.VS2012.TestAdapter
{
    [ExtensionUri(Constants.ExecutorUriString)]
	public class ChutzpahTestExecutor : ITestExecutor
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

            var settingsProvider = runContext.RunSettings.GetSettings(ChutzpahAdapterSettings.SettingsName) as ChutzpahAdapterSettingsService;
            var settings = settingsProvider != null ? settingsProvider.Settings : new ChutzpahAdapterSettings();
            var testOptions = new TestOptions { TimeOutMilliseconds = settings.TimeoutMilliseconds, TestingMode = settings.TestingMode };

			var chutzpahRunner = TestRunner.Create();
			var callback = new ExecutionCallback(frameworkHandle);
			chutzpahRunner.RunTests(sources,testOptions, callback);
		}

		public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
		{
			// We'll just punt and run everything in each file that contains the selected tests
			var sources = tests.Select(test => test.Source).Distinct();
			RunTests(sources, runContext, frameworkHandle);
		}
	}
}
