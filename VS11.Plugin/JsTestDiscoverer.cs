using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Chutzpah.VS11
{
    [FileExtension(".js")]
    [DefaultExecutorUri(Constants.ExecutorUriString)]
    public class JsTestDiscoverer :ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            var chutzpahRunner = TestRunner.Create();
            foreach (var testCase in chutzpahRunner.DiscoverTests(sources))
            {
                var vsTestCase = testCase.ToVsTestCase();
                discoverySink.SendTestCase(vsTestCase);
            }
        } 
    }
}