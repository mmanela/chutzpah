using System;
using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah
{
    public class TestOptions
    {
        private int testFileTimeoutMilliseconds;
        private int maxDegreeOfParallelism;
        private readonly int defaultParallelism;

        public TestOptions()
        {
            FileSearchLimit = Constants.DefaultFileSeachLimit;
            TestFileTimeoutMilliseconds = Constants.DefaultTestFileTimeout;
            defaultParallelism = Environment.ProcessorCount;
            MaxDegreeOfParallelism = defaultParallelism;
            CoverageOptions = new CoverageOptions();
            TestExecutionMode = TestExecutionMode.Execution;

            ChutzpahSettingsFileEnvironments = new ChutzpahSettingsFileEnvironments();

        }

        /// <summary>
        /// Describes the ways in which the test is to be launched.
        /// </summary>
        public TestLaunchMode TestLaunchMode { get; set; }

        /// <summary>
        /// The name of browser which will be opened when TestLaunchMode.FullBrowser is set, this value is optional
        /// </summary>
        public string BrowserName { get; set; }
        
        /// <summary>
        /// The time to wait for the tests to compelte in milliseconds
        /// </summary>
        public int? TestFileTimeoutMilliseconds
        {
            get { return testFileTimeoutMilliseconds; }
            set { testFileTimeoutMilliseconds = value ?? Constants.DefaultTestFileTimeout; }
        }

        /// <summary>
        /// Marks if we are running in exeuction or discovery mode
        /// </summary>
        public TestExecutionMode TestExecutionMode { get; set; }


        /// <summary>
        /// This is the max number of files to run tests for
        /// </summary>
        public int FileSearchLimit { get; set; }       
        
        /// <summary>
        /// The maximum degree of parallelism to process test files
        /// </summary>
        public int MaxDegreeOfParallelism
        {
            get { return maxDegreeOfParallelism; }
            set { maxDegreeOfParallelism = GetDegreeOfParallelism(value); }
        }

        /// <summary>
        /// Get the degree of parallism making sure the value is no less than 1 and not more
        /// then the number of processors
        /// </summary>
        private int GetDegreeOfParallelism(int value)
        {
            return Math.Min(Math.Max(value, 1), Environment.ProcessorCount);
        }

        /// <summary>
        /// Contains options for code coverage collection.
        /// </summary>
        public CoverageOptions CoverageOptions { get; set; }

        /// <summary>
        /// Additional per Chutzpah.json properties that can be used when resolved paths in
        /// the settings file
        /// </summary>
        public ChutzpahSettingsFileEnvironments ChutzpahSettingsFileEnvironments { get; set; }
    }
}