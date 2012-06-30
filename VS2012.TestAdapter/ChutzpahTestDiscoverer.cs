using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Chutzpah.VS2012.TestAdapter
{
    [FileExtension(".js")]
    [FileExtension(".htm")]
    [FileExtension(".html")]
    [DefaultExecutorUri(Constants.ExecutorUriString)]
    public class ChutzpahTestDiscoverer :ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            var settingsProvider = discoveryContext.RunSettings.GetSettings(ChutzpahAdapterSettings.SettingsName) as ChutzpahAdapterSettingsService;
            var settings = settingsProvider != null ? settingsProvider.Settings : new ChutzpahAdapterSettings();
            var testOptions = new TestOptions { TimeOutMilliseconds = settings.TimeoutMilliseconds, TestingMode = settings.TestingMode };

            var chutzpahRunner = TestRunner.Create();
            foreach (var testCase in chutzpahRunner.DiscoverTests(sources, testOptions))
            {
                var vsTestCase = testCase.ToVsTestCase();
                discoverySink.SendTestCase(vsTestCase);
            }
        } 
    }
}