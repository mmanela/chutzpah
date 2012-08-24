using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Chutzpah.VS2012.TestAdapter
{
    [FileExtension(".coffee")]
    [FileExtension(".js")]
    [FileExtension(".htm")]
    [FileExtension(".html")]
    [DefaultExecutorUri(Constants.ExecutorUriString)]
    public class ChutzpahTestDiscoverer : ITestDiscoverer
    {
        private readonly ITestRunner testRunner;

        public ChutzpahTestDiscoverer()
        {
            testRunner = TestRunner.Create();
        }

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            var settingsProvider = discoveryContext.RunSettings.GetSettings(ChutzpahAdapterSettings.SettingsName) as ChutzpahAdapterSettingsService;
            var settings = settingsProvider != null ? settingsProvider.Settings : new ChutzpahAdapterSettings();
            var testOptions = new TestOptions
                {
                    TestFileTimeoutMilliseconds = settings.TimeoutMilliseconds,
                    TestingMode = settings.TestingMode,
                    MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism
                };

            foreach (var testCase in testRunner.DiscoverTests(sources, testOptions))
            {
                var vsTestCase = testCase.ToVsTestCase();
                discoverySink.SendTestCase(vsTestCase);
            }
        }
    }
}