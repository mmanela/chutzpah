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
            TestingMode = TestingMode.All;
            MaxDegreeOfParallelism = 1;

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
            set { maxDegreeOfParallelism = Math.Max(value, 1); }
        }
    }
}