using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Chutzpah.VS.Common;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Chutzpah.VS2012.TestAdapter
{
    [FileExtension(Chutzpah.Constants.CoffeeScriptExtension)]
    [FileExtension(Chutzpah.Constants.TypeScriptExtension)]
    [FileExtension(Chutzpah.Constants.JavaScriptExtension)]
    [FileExtension(Chutzpah.Constants.HtmlScriptExtension)]
    [FileExtension(Chutzpah.Constants.HtmScriptExtension)]
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

            var testCases = testRunner.DiscoverTests(sources, testOptions);
            foreach (var testCase in testCases)
            {
                var vsTestCase = testCase.ToVsTestCase();
                discoverySink.SendTestCase(vsTestCase);
            }
        }
    }
}