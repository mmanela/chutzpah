using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Chutzpah.PerfTester
{
    public class PerfRunner
    {
        public const int Samples = 25;

        public static void Run(Action action)
        {
            var stopwatch = new Stopwatch();
            var results = new List<double>();

            // Warm everything up to make sure it is all jitted
            action();


            for(var i =0; i < Samples;i++)
            {
                stopwatch.Start();
                action();
                results.Add(stopwatch.ElapsedMilliseconds / 1000.0);
                stopwatch.Reset();
            }


            double averageTime = results.Average();
            double maxTime = results.Max();
            double minTime = results.Min();
            double variance = results.Select(x => Math.Pow(averageTime - x, 2)).Sum()/Samples;
            double stndDev = Math.Sqrt(variance);

            Console.WriteLine("Average Time:   {0:0.000} seconds", averageTime);
            Console.WriteLine("Fastest Time:   {0:0.000} seconds", minTime);
            Console.WriteLine("Slowest Time:   {0:0.000} seconds", maxTime);
            Console.WriteLine("Std Deviation:  {0:0.000} seconds", stndDev);
        }
    }
}