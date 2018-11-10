using System.Collections.Generic;
using Chutzpah.Coverage;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Server.Models;

namespace Chutzpah.Models
{
    public class TestContext
    {
        public TestContext()
        {
            ReferencedFiles = new List<ReferencedFile>();
            TemporaryFiles = new List<string>();
            TestFileSettings = new ChutzpahTestSettingsFile().InheritFromDefault();    
        }

        /// <summary>
        /// The test files given by the user
        /// </summary>
        public ICollection<string> InputTestFiles { get; set; }

        public string FirstInputTestFile { get; set; }

        /// <summary>
        /// The Id of the Task running the TestContext
        /// </summary>
        public int TaskId { get; set; }

        /// <summary>
        /// The path to the test runner
        /// </summary>
        public string TestRunner { get; set; }

        /// <summary>
        /// The path to the test harness. This is either the InputTestFile when a html file or
        /// it will be the generated test harness for .js files
        /// </summary>
        public string TestHarnessPath { get; set; }

        /// <summary>
        /// The directory of the test harness
        /// </summary>
        public string TestHarnessDirectory { get; set; }

        /// <summary>
        /// Is the harness on a remote server
        /// </summary>
        public bool IsRemoteHarness { get; set; }

        /// <summary>
        /// The list of referenced JavaScript files
        /// </summary>
        public ICollection<ReferencedFile> ReferencedFiles { get; set; }

        /// <summary>
        /// A list of temporary files that should be cleaned up after the test run is finished
        /// </summary>
        public ICollection<string> TemporaryFiles { get; set; }

        /// <summary>
        /// The chutzpah test settings found when building the context for this test
        /// </summary>
        public ChutzpahTestSettingsFile TestFileSettings { get; set; }

        /// <summary>
        /// Instance of the framework definition for this test context
        /// </summary>
        public IFrameworkDefinition FrameworkDefinition { get; set; }

        /// <summary>
        /// Instance of the code coverage engine for the context
        /// </summary>
        public ICoverageEngine CoverageEngine { get; set; }

        public string InputTestFilesString { get; set; }
        public bool TestHarnessCreationFailed { get; set; }
        public IChutzpahWebServerHost WebServerHost { get; set; }
    }
}