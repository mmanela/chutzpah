using System;
using Chutzpah.Models;

namespace Chutzpah.PerfTester
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("########################");
            Console.WriteLine("# Chutzpah Perf Tester #");
            Console.WriteLine("########################\n");

            Console.WriteLine("Samples: {0}\n",PerfRunner.Samples);

            var testRunner = TestRunner.Create();

            Console.WriteLine("Javascript:");
            PerfRunner.Run(() => testRunner.RunTests(@"JS\test.js"));
            Console.WriteLine("\nCoffee without cache:");
            PerfRunner.Run(() => testRunner.RunTests(@"Coffee\runner.coffee"));
            
            var globalOptions = GlobalOptions.Instance;
            globalOptions.EnableCompilerCache = true;
            globalOptions.CompilerCacheFile = "js.cache";
            Console.WriteLine("\nCoffee with cache:");
            PerfRunner.Run(() => testRunner.RunTests(@"Coffee\runner.coffee"));
        }
    }
}
