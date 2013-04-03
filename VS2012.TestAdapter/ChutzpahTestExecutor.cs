using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chutzpah.Callbacks;
using Chutzpah.Coverage;
using Chutzpah.Wrappers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Chutzpah.VS2012.TestAdapter
{
    [ExtensionUri(Constants.ExecutorUriString)]
    public class ChutzpahTestExecutor : ITestExecutor
    {
        private readonly ITestRunner testRunner;

        public ChutzpahTestExecutor()
        {
            testRunner = TestRunner.Create();
        }

        public void Cancel()
        {
        }


        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {

            var settingsProvider = runContext.RunSettings.GetSettings(ChutzpahAdapterSettings.SettingsName) as ChutzpahAdapterSettingsService;
            var settings = settingsProvider != null ? settingsProvider.Settings : new ChutzpahAdapterSettings();
            var testOptions = new TestOptions
                {
                    TestFileTimeoutMilliseconds = settings.TimeoutMilliseconds,
                    TestingMode = settings.TestingMode,
                    MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism,
                };

            testOptions.CoverageOptions.Enabled = runContext.IsDataCollectionEnabled;

            var callback = new ParallelRunnerCallbackAdapter(new ExecutionCallback(frameworkHandle, runContext));
            testRunner.RunTests(sources, testOptions, callback);
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            // We'll just punt and run everything in each file that contains the selected tests
            var sources = tests.Select(test => test.Source).Distinct();
            RunTests(sources, runContext, frameworkHandle);
        }
    }
}