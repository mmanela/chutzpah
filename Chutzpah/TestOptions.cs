using System;
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
            TestingMode = TestingMode.All;
            defaultParallelism = 1;
            MaxDegreeOfParallelism = defaultParallelism;
            CoverageOptions = new CoverageOptions();

        }

        /// <summary>
        /// Whether or not to launch the tests in the default browser
        /// </summary>
        public bool OpenInBrowser { get; set; }

        /// <summary>
        /// The time to wait for the tests to compelte in milliseconds
        /// </summary>
        public int? TestFileTimeoutMilliseconds
        {
            get { return testFileTimeoutMilliseconds; }
            set { testFileTimeoutMilliseconds = value ?? Constants.DefaultTestFileTimeout; }
        }

        /// <summary>
        /// Determines if we are testing JavaScript files (and creating harnesses for them), 
        /// testing html test harnesses directly or both
        /// </summary>
        public TestingMode TestingMode { get; set; }

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
            return Math.Min(Math.Max(value, defaultParallelism), Environment.ProcessorCount);
        }

        /// <summary>
        /// Contains options for code coverage collection.
        /// </summary>
        public CoverageOptions CoverageOptions { get; set; }
    }
}