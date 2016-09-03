using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using Chutzpah.Models;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Chutzpah.Callbacks;

namespace Chutzpah.VS2012.TestAdapter
{
    [FileExtension(Chutzpah.Constants.CoffeeScriptExtension)]
    [FileExtension(Chutzpah.Constants.TypeScriptReactExtension)]
    [FileExtension(Chutzpah.Constants.TypeScriptExtension)]
    [FileExtension(Chutzpah.Constants.JavaScriptReactExtension)]
    [FileExtension(Chutzpah.Constants.JavaScriptExtension)]
    [FileExtension(Chutzpah.Constants.HtmlScriptExtension)]
    [FileExtension(Chutzpah.Constants.HtmScriptExtension)]
    [FileExtension(Chutzpah.Constants.JsonExtension)]
    [DefaultExecutorUri(AdapterConstants.ExecutorUriString)]
    public class ChutzpahTestDiscoverer : ITestDiscoverer
    {
        private readonly ITestRunner testRunner;

        public ChutzpahTestDiscoverer()
        {
            testRunner = TestRunner.Create();
        }

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            if (Environment.GetEnvironmentVariable("ATTACH_DEBUGGER_CHUTZPAH") != null)
            {
                Debugger.Launch();
            }

            ChutzpahTracer.TraceInformation("Begin Test Adapter Discover Tests");

            var settingsProvider = discoveryContext.RunSettings.GetSettings(AdapterConstants.SettingsName) as ChutzpahAdapterSettingsProvider;
            var settings = settingsProvider != null ? settingsProvider.Settings : new ChutzpahAdapterSettings();

            ChutzpahTracingHelper.Toggle(settings.EnabledTracing);

            var testOptions = new TestOptions
            {
                MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism,
                ChutzpahSettingsFileEnvironments = new ChutzpahSettingsFileEnvironments(settings.ChutzpahSettingsFileEnvironments)
            };


            ChutzpahTracer.TraceInformation("Sending discovered tests to test case discovery sink");

            var callback = new ParallelRunnerCallbackAdapter(new DiscoveryCallback(logger, discoverySink));
            var testCases = testRunner.DiscoverTests(sources, testOptions, callback);


            ChutzpahTracer.TraceInformation("End Test Adapter Discover Tests");

        }
    }
}