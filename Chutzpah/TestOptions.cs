using System;
using Chutzpah.Models;

namespace Chutzpah
{
    public class TestOptions
    {
        private int testFileTimeoutMilliseconds;
        private int maxDegreeOfParallelism;

        public TestOptions()
        {
            FileSearchLimit = Constants.DefaultFileSeachLimit;
            TestFileTimeoutMilliseconds = Constants.DefaultTestFileTimeout;
            MaxDegreeOfParallelism = Environment.ProcessorCount;
            CoverageOptions = new CoverageOptions();
            TestExecutionMode = TestExecutionMode.Execution;

            ChutzpahSettingsFileEnvironments = new ChutzpahSettingsFileEnvironments();

        }

        /// <summary>
        /// Describes the ways in which the test is to be launched.
        /// </summary>
        public TestLaunchMode TestLaunchMode { get; set; }

        /// <summary>
        /// The browser to run the headless tests with
        /// </summary>
        public Engine? Engine { get; set; }

        /// <summary>
        /// The name of browser which will be opened when TestLaunchMode.FullBrowser is set, this value is optional
        /// </summary>
        public string OpenInBrowserName { get; set; }

        /// <summary>
        /// The arguments for the corresponding browser which will be opened when TestLaunchMode.FullBrowser is set, this value is optional
        /// </summary>
        public string OpenInBrowserArgs { get; set; }

        /// <summary>
        /// Test launch object implementing the Custom test launch logic.
        /// Required when TestLaunchMode == TestLaunchMode.Custom.
        /// </summary>
        public ITestLauncher CustomTestLauncher { get; set; }

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

        /// <summary>
        /// Optional proxy settings in format of [address]:[port]
        /// </summary>
        public string Proxy { get; set; }
        public bool DebugEnabled { get; internal set; }
    }
}