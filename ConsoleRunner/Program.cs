using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Chutzpah.Models;
using Chutzpah.RunnerCallbacks;

namespace Chutzpah
{
    class Program
    {
        static void Main2(string[] args)
        {
            var testRunner = new TestRunner();
            var testFile = Path.GetFullPath(@"JS\test.html");
            var res = testRunner.RunTests(testFile);

            Console.WriteLine("Running tests for {0}", testFile);
            Console.WriteLine(string.Format("Passed: {0}  Failed:{1}", res.PassedCount, res.FailedCount));
        }

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
            Console.WriteLine("  /silent                : do not output running test count");
            Console.WriteLine("  /teamcity              : forces TeamCity mode (normally auto-detected)");
            Console.WriteLine("  /wait                  : wait for input after completion");
            Console.WriteLine("  /debug                 : print debugging information");
            Console.WriteLine("  /file fileName         : adds fileName to list of test files");
            Console.WriteLine("                         : sepcify more than once to add multiple files");
            Console.WriteLine("                         : (e.g. /file test1.html /file test2.html)");
            Console.WriteLine();
        }

        static int RunTests(CommandLine commandLine)
        {
 
            var testRunner = new TestRunner {DebugEnabled = commandLine.Debug};

            var chutzpahAssemblyName = testRunner.GetType().Assembly.GetName();
            

            Console.WriteLine();
            Console.WriteLine("chutzpah.dll:     Version {0}", chutzpahAssemblyName.Version);
            Console.WriteLine();

            TestResultsSummary testResultsSummary = null;
            try
            {

                var callback = commandLine.TeamCity ? (RunnerCallback)new TeamCityRunnerCallback() : new StandardRunnerCallback(commandLine.Silent);

                testResultsSummary = testRunner.RunTests(commandLine.Files, callback);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return testResultsSummary.FailedCount;
        }
    }
}