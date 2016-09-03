using System.Collections.Generic;
using System.Linq;
using Chutzpah.Callbacks;
using Chutzpah.Models;
using Chutzpah.VS.Common;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.Diagnostics;
using System;

namespace Chutzpah.VS2012.TestAdapter
{
    [ExtensionUri(AdapterConstants.ExecutorUriString)]
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
            if (Environment.GetEnvironmentVariable("ATTACH_DEBUGGER_CHUTZPAH") != null)
            {
                Debugger.Launch();
            }
            
            ChutzpahTracer.TraceInformation("Begin Test Adapter Run Tests");

            var settingsProvider = runContext.RunSettings.GetSettings(AdapterConstants.SettingsName) as ChutzpahAdapterSettingsProvider;
            var settings = settingsProvider != null ? settingsProvider.Settings : new ChutzpahAdapterSettings();

            ChutzpahTracingHelper.Toggle(settings.EnabledTracing);

            var testOptions = new TestOptions
                {
                    TestLaunchMode =
                        runContext.IsBeingDebugged ? TestLaunchMode.Custom:
                        settings.OpenInBrowser ? TestLaunchMode.FullBrowser:
                        TestLaunchMode.HeadlessBrowser,
                    CustomTestLauncher     = runContext.IsBeingDebugged ? ChutzpahContainer.Get<VsDebuggerTestLauncher>() : null,
                    MaxDegreeOfParallelism = runContext.IsBeingDebugged ? 1 : settings.MaxDegreeOfParallelism,
                    ChutzpahSettingsFileEnvironments = new ChutzpahSettingsFileEnvironments(settings.ChutzpahSettingsFileEnvironments)
                };

            testOptions.CoverageOptions.Enabled = runContext.IsDataCollectionEnabled;

            var callback = new ParallelRunnerCallbackAdapter(new ExecutionCallback(frameworkHandle, runContext));
            testRunner.RunTests(sources, testOptions, callback);

            ChutzpahTracer.TraceInformation("End Test Adapter Run Tests");

        }

        public void RunTests(IEnumerable<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            // We'll just punt and run everything in each file that contains the selected tests
            var sources = tests.Select(test => test.Source).Distinct();
            RunTests(sources, runContext, frameworkHandle);
        }
    }
}