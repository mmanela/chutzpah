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
        }
    }
}
