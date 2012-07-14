using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Chutzpah.Callbacks;
using Chutzpah.Models;
using Chutzpah.RunnerCallbacks;
using System.Linq;

namespace Chutzpah
{
    class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            Console.WriteLine("Chutzpah console test runner ({0}-bit .NET {1})", IntPtr.Size * 8, Environment.Version);
            Console.WriteLine("Copyright (C) 2011 Matthew Manela (http://matthewmanela.com).");

            if (args.Length == 0 || args[0] == "/?")
            {
                PrintUsage();
                return -1;
            }

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            try
            {
                CommandLine commandLine = CommandLine.Parse(args);

                int failCount = RunTests(commandLine);

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
                Console.WriteLine("error: {0}", ex.Message);
                return -1;
            }
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

        static void PrintUsage()
        {
            string executableName = Path.GetFileNameWithoutExtension(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);

            Console.WriteLine();
            Console.WriteLine("usage: {0} [options]", executableName);
            Console.WriteLine("usage: {0} <testFile> [options]", executableName);
            Console.WriteLine();
            Console.WriteLine("Valid options:");
            Console.WriteLine("  /silent                : Do not output running test count");
            Console.WriteLine("  /teamcity              : Forces TeamCity mode (normally auto-detected)");
            Console.WriteLine("  /wait                  : Wait for input after completion");
            Console.WriteLine("  /debug                 : Print debugging information");
            Console.WriteLine("  /openInBrowser         : Launch the tests in the default browser");
            Console.WriteLine("  /timeoutMilliseconds   : Amount of time to wait for a test file to finish before failing. (Defaults to {0})",Constants.DefaultTestFileTimeout);
            Console.WriteLine("  /parallelism n         : Max degree of parallelism for Chutzpah. (Defaults to 1)");
            Console.WriteLine("                         : If you specify more than 1 the test output may be a bit jumbled");
            Console.WriteLine("  /path path             : Adds a path to a folder or file to the list of test paths to run.");
            Console.WriteLine("                         : Specify more than one to add multiple paths.");
            Console.WriteLine("                         : If you give a folder, it will be scanned for testable files.");
            Console.WriteLine("                         : (e.g. /path test1.html /path testFolder)");
            Console.WriteLine("  /file path             : Alias for /path");
            Console.WriteLine();
        }

        static int RunTests(CommandLine commandLine)
        {

            var testRunner = TestRunner.Create(debugEnabled: commandLine.Debug);

            var chutzpahAssemblyName = testRunner.GetType().Assembly.GetName();
            

            Console.WriteLine();
            Console.WriteLine("chutzpah.dll:     Version {0}", chutzpahAssemblyName.Version);
            Console.WriteLine();

            TestCaseSummary testResultsSummary = null;
            try
            {

                var callback = commandLine.TeamCity ? (ITestMethodRunnerCallback)new TeamCityConsoleRunnerCallback() : new StandardConsoleRunnerCallback(commandLine.Silent);
                callback = new ParallelRunnerCallbackAdapter(callback);

                var testOptions = new TestOptions
                    {
                        OpenInBrowser = commandLine.OpenInBrowser, 
                        TestFileTimeoutMilliseconds = commandLine.TimeOutMilliseconds,
                        MaxDegreeOfParallelism = commandLine.Parallelism
                    };

                testResultsSummary = testRunner.RunTests(commandLine.Files, testOptions, callback);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return testResultsSummary.FailedCount;
        }
    }
}