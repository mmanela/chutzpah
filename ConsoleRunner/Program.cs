using System;
using System.IO;
using System.Reflection;
using Chutzpah.Callbacks;
using Chutzpah.Models;
using Chutzpah.RunnerCallbacks;
using System.Linq;
using Chutzpah.Transformers;
using Chutzpah.Wrappers;
using System.Collections.Generic;
using System.Diagnostics;

namespace Chutzpah
{
    class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            if (Environment.GetEnvironmentVariable("ATTACH_DEBUGGER_CHUTZPAH") != null)
            {
                Debugger.Launch();
            }


            var transformers = new SummaryTransformerProvider().GetTransformers(new FileSystemWrapper());

            if (args.Length == 0 || args[0] == "/?")
            {
                PrintHeader();
                PrintUsage(transformers);
                return -1;
            }

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            try
            {

                CommandLine commandLine = CommandLine.Parse(args, transformers.Select(x => x.Name));

                if (!commandLine.NoLogo)
                {
                    PrintHeader();
                }

                int failCount = RunTests(commandLine, transformers);

                if (commandLine.Wait)
                {
                    Console.WriteLine();
                    Console.Write("Press any key to continue...");
                    Console.ReadKey();
                    Console.WriteLine();
                }

                return failCount;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("error: {0}", ex.Message);
                Console.ResetColor();
                return -1;
            }
        }

        private static void PrintHeader()
        {
            Console.WriteLine("Chutzpah console test runner  ({0}-bit .NET {1})", IntPtr.Size * 8, Environment.Version);
            Console.WriteLine("Version {0}", Assembly.GetEntryAssembly().GetName().Version);
            Console.WriteLine("Copyright (C) 2016 Matthew Manela (http://matthewmanela.com).");
        }

        static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;

            if (ex != null)
                Console.WriteLine(ex.ToString());
            else
                Console.WriteLine("Error of unknown type thrown in applicaton domain");

            Environment.Exit(1);
        }

        static void PrintUsage(IEnumerable<SummaryTransformer> transformers)
        {
            string executableName = Path.GetFileNameWithoutExtension(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);

            Console.WriteLine();
            Console.WriteLine("usage: {0} [options]", executableName);
            Console.WriteLine("usage: {0} <testFile> [options]", executableName);
            Console.WriteLine();
            Console.WriteLine("Valid options:");
            Console.WriteLine("  /nologo                      : Do not show the copyright message");
            Console.WriteLine("  /silent                      : Do not output running test count");
            Console.WriteLine("  /teamcity                    : Forces TeamCity mode (normally auto-detected)");
            Console.WriteLine("  /wait                        : Wait for input after completion");
            Console.WriteLine("  /failOnError                 : Return a non-zero exit code if any script errors or timeouts occurs");
            Console.WriteLine("  /debug                       : Print debugging information and tracing to console");
            Console.WriteLine("  /trace                       : Logs tracing information to chutzpah.log");
            Console.WriteLine("  /openInBrowser [name]        : Launch the tests in a browser.");
            Console.WriteLine("                               : If optional name is provided will try to launch in that browser.");
            Console.WriteLine("  /browserArgs                 : Optional arguments used when launching browser, e.g. --allow-file-access-from-files for chrome.");
            Console.WriteLine("                               : Name can be IE, Firefox, Chrome.");
            Console.WriteLine("  /parallelism [n]             : Max degree of parallelism for Chutzpah. Defaults to number of CPUs + 1");
            Console.WriteLine("                               : If you specify more than 1 the test output may be a bit jumbled");
            Console.WriteLine("  /path path                   : Adds a path to a folder or file to the list of test paths to run.");
            Console.WriteLine("                               : Specify more than one to add multiple paths.");
            Console.WriteLine("                               : If you give a folder, it will be scanned for testable files.");
            Console.WriteLine("                               : (e.g. /path test1.html /path testFolder)");
            Console.WriteLine("  /vsoutput                    : Print output in a format that the VS error list recognizes");
            Console.WriteLine("  /coverage                    : Enable coverage collection");
            Console.WriteLine("  /showFailureReport           : Show a failure report after the test run. Usefull if you have a large number of tests.");
            Console.WriteLine("  /settingsFileEnvironment     : Sets the environment properties for a chutzpah.json settings file.");
            Console.WriteLine("                               : Specify more than one to add multiple environments.");
            Console.WriteLine("                               : (e.g. settingsFilePath;prop1=val1;prop2=val2).");



            foreach (var transformer in transformers)
            {
                Console.WriteLine("  /{0} filename              : {1}", transformer.Name, transformer.Description);
            }

            Console.WriteLine();
        }

        static int RunTests(CommandLine commandLine, IEnumerable<SummaryTransformer> transformers)
        {

            var testRunner = TestRunner.Create(debugEnabled: commandLine.Debug);

            if (commandLine.Trace)
            {
                ChutzpahTracer.AddFileListener();
            }


            Console.WriteLine();

            TestCaseSummary testResultsSummary = null;
            try
            {
                var callback = commandLine.TeamCity 
                                ? (ITestMethodRunnerCallback)new TeamCityConsoleRunnerCallback() 
                                : new StandardConsoleRunnerCallback(commandLine.Silent, commandLine.VsOutput, commandLine.ShowFailureReport, commandLine.FailOnError);

                callback = new ParallelRunnerCallbackAdapter(callback);

                var testOptions = new TestOptions
                    {
                        TestLaunchMode = commandLine.OpenInBrowser ? TestLaunchMode.FullBrowser : TestLaunchMode.HeadlessBrowser,
                        BrowserName = commandLine.BrowserName,
                        BrowserArgs = commandLine.BrowserArgs,
                        TestFileTimeoutMilliseconds = commandLine.TimeOutMilliseconds,
                        ChutzpahSettingsFileEnvironments = commandLine.SettingsFileEnvironments,
                        CoverageOptions = new CoverageOptions
                                              {
                                                  Enabled = commandLine.Coverage,
                                                  IncludePatterns = (commandLine.CoverageIncludePatterns ?? "").Split(new[]{','},StringSplitOptions.RemoveEmptyEntries),
                                                  ExcludePatterns = (commandLine.CoverageExcludePatterns ?? "").Split(new[]{','},StringSplitOptions.RemoveEmptyEntries),
                                                  IgnorePatterns = (commandLine.CoverageIgnorePatterns ?? "").Split(new[]{','},StringSplitOptions.RemoveEmptyEntries)
                                              }
                    };

                if(commandLine.Parallelism > 0)
                {
                    testOptions.MaxDegreeOfParallelism = commandLine.Parallelism;
                }

                if (!commandLine.Discovery)
                {
                    testResultsSummary = testRunner.RunTests(commandLine.Files, testOptions, callback);
                    ProcessTestSummaryTransformers(transformers, commandLine, testResultsSummary);
                }
                else
                {
                    Console.WriteLine("Test Discovery");
                    var tests = testRunner.DiscoverTests(commandLine.Files, testOptions).ToList();
                    Console.WriteLine("\nDiscovered {0} tests", tests.Count);

                    foreach (var test in tests)
                    {
                        Console.WriteLine("Test '{0}:{1}' from '{2}'", test.ModuleName, test.TestName, test.InputTestFile);
                    }
                    return 0;
                }

            }
            catch (ArgumentException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

            if (testRunner.ActiveWebServerHost != null)
            {
                Console.WriteLine("Press any key to end Chutzpah...");
                Console.ReadKey();
                testRunner.ActiveWebServerHost.Dispose();
            }


            var failedCount = testResultsSummary.FailedCount;
            if (commandLine.FailOnError && testResultsSummary.Errors.Any())
            {
                return failedCount > 0 ? failedCount : 1;
            }


            return failedCount;
        }

        private static void ProcessTestSummaryTransformers(IEnumerable<SummaryTransformer>  transformers, CommandLine commandLine, TestCaseSummary testResultsSummary)
        {
            foreach (var transformer in transformers.Where(x => commandLine.TransformersRequested.ContainsKey(x.Name)))
            {
                var path = commandLine.TransformersRequested[transformer.Name];
                transformer.Transform(testResultsSummary, path);

            }
        }
    }
}