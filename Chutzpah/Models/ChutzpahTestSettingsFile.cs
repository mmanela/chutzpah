using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Chutzpah.Compilers.TypeScript;
namespace Chutzpah.Models
{
    public enum TestHarnessReferenceMode
    {
        Normal,
        AMD
    }

    public enum TestHarnessLocationMode
    {
        TestFileAdjacent,
        SettingsFileAdjacent,
        Custom
    }

    public enum RootReferencePathMode
    {
        DriveRoot,
        SettingsFileDirectory
    }


    /// <summary>
    /// Represents the Chutzpah Test Settings file (chutzpah.json)
    /// Applies to all test files in its directory and below.
    /// </summary>
    public class ChutzpahTestSettingsFile
    {
        public static ChutzpahTestSettingsFile Default = new ChutzpahTestSettingsFile();
        private Regex testPatternRegex;

        public ChutzpahTestSettingsFile()
        {
            CodeCoverageIncludes = new List<string>();
            CodeCoverageExcludes = new List<string>();
            References = new List<SettingsFileReference>();
            Tests = new List<SettingsFileTestPath>();
            CoffeeScriptBareMode = true;
            CodeCoverageSuccessPercentage = Constants.DefaultCodeCoverageSuccessPercentage;
        }


        /// <summary>
        /// The time to wait for the tests to compelte in milliseconds
        /// </summary>
        public int? TestFileTimeout { get; set; }

        /// <summary>
        /// If not null tells Chutzpah which framework to use instead of detecting automatically
        /// </summary>
        public string Framework { get; set; }


        /// <summary>
        /// Indicates which version of a framework Chutzah should use. 
        /// This is only useful when Chutzpah supports more than one version which is usually a temporary situation.
        /// If this is null chutzpah will use its default version of a framework.
        /// If an unknown version is set Chutzpah will use its default version
        /// </summary>
        public string FrameworkVersion { get; set; }

        /// <summary>
        /// The name of the Mocha interface the tests use
        /// This will override the detection mechanism for tests
        /// </summary>
        public string MochaInterface { get; set; }

        /// <summary>
        /// Determines how Chutzpah handles referenced files in the test harness
        /// Normal - Sets the test harness for normal test running
        /// AMD - Sets the test harness to running tests using AMD
        /// </summary>
        public TestHarnessReferenceMode TestHarnessReferenceMode { get; set; }


        /// <summary>
        /// Tells Chutzpah where it should place the generated test harness html.
        /// TestFileAdjacent - Places the harness next to the file under test. This is the default.
        /// SettingsFileAdjacent - Places the harness next to the first chutzpah.json file found up the directory tree from the file under test
        /// Custom - Lets you specify the TestHarnessDirectory property to give a custom folder to place the test harness. If folder is not found it will revert to the default.
        /// </summary>
        public TestHarnessLocationMode TestHarnessLocationMode { get; set; }

        /// <summary>
        /// If TestHarnessLocationMode is set to Custom then this will be the path to the folder to place the generated test harness file
        /// </summary>
        public string TestHarnessDirectory { get; set; }

        /// <summary>
        /// A Regualr Expression which tells Chutpah where to find the names of your tests in the test file. 
        /// The regex must contain a capture group named TestName like (?<TestName>) that contains the test name (inside of the quotes)
        /// </summary>
        public string TestPattern { get; set; }


        /// <summary>
        /// The path to your own test harness for Chutzpah to use. 
        /// This is an *advanced* scenario since Chutzpah has specific requirements on the test harness
        /// If you deploy your own then you must copy from Chutzpah's and if you upgrade Chutzpah
        /// you must keep parity
        /// There are no guarantees about anything working once you deploy your own.
        /// </summary>
        public string CustomTestHarnessPath { get; set; }

        /// <summary>
        /// This is the base url for use in resolving what path Chutzpah should use when it invokes your AMD test file.
        /// You only need this if you configure the baseUrl in your Require.js config.
        /// </summary>
        public string AMDBasePath { get; set; }

        /// <summary>
        /// Determines what a reference path that starts with / or \  (.e.g <reference path="/" />) is relative to
        /// DriveRoot - Make it relative to the root of the drive (e.g. C:\). This is default.
        /// SettingsFileDirectory - Makes root path relative to the directory of the settings file
        /// </summary>
        public RootReferencePathMode RootReferencePathMode { get; set; }

        /// <summary>
        /// The type of code for the TypeScript compiler to generate
        /// ES3 - Generate ECMAScript 3 Compatible code
        /// ES5 - Generate ECMAScript 5 Compatible code
        /// </summary>
        public TypeScriptCodeGenTarget TypeScriptCodeGenTarget { get; set; }

        /// <summary>
        /// The type of module code TypeScript should generate
        /// CommonJS - CommonJS Style
        /// AMD - AMD Style
        /// </summary>
        public TypeScriptModuleKind TypeScriptModuleKind { get; set; }

        /// <summary>
        /// If True, forces code coverage to run always
        /// If Null or not not set, allows code coverage to run if invoked using test adapter, command line or context menu options (default)
        /// If False, forces code coverage to never run. 
        /// </summary>
        public bool? EnableCodeCoverage { get; set; }

        /// <summary>
        /// The percentage of lines should be covered to show the coverage output as success or failure. By default, this is 60.
        /// </summary>
        public double CodeCoverageSuccessPercentage { get; set; }

        /// <summary>
        /// The collection code coverage file patterns to include in coverage. These are in glob format. If you specify none all files are included.
        /// </summary>
        public ICollection<string> CodeCoverageIncludes { get; set; }

        /// <summary>
        /// The collection code coverage file patterns to exclude in coverage. These are in glob format. If you specify none no files are excluded.
        /// </summary>
        public ICollection<string> CodeCoverageExcludes { get; set; }

        /// <summary>
        /// The collection of test files. These can list individual tests or folders scanned recursively. This setting can work in two ways:
        /// 1. If you run tests normally by specifying folders/files then this settings will filter the sets of those files.
        /// 2. If you run tests by running a specific chutzpah.json file then this settings will select the test files you choose.
        /// </summary>
        public ICollection<SettingsFileTestPath> Tests { get; set; }

        /// <summary>
        /// The collection of reference settings. These can list individual reference files or folders scanned recursively.
        /// </summary>
        public ICollection<SettingsFileReference> References { get; set; }

        /// <summary>
        /// The path to the settings file
        /// </summary>
        public string SettingsFileDirectory { get; set; }

        /// <summary>
        /// Determines if CoffeeScript should run in bare mode or not. Default is true.
        /// </summary>
        public bool CoffeeScriptBareMode { get; set; }

        /// <summary>
        /// The compile settings which tell Chutzpah how to perform the batch compile
        /// </summary>
        public BatchCompileConfiguration Compile { get; set; }


        public string SettingsFileName
        {
            get
            {
                return Path.Combine(SettingsFileDirectory, Constants.SettingsFileName);
            }
        }


        public Regex TestPatternRegex
        {
            get
            {
                if (TestPattern == null)
                {
                    return null;
                }

                return testPatternRegex ?? (testPatternRegex = new Regex(TestPattern));
            }
        }

        public override int GetHashCode()
        {
            if (SettingsFileDirectory == null)
            {
                return "".GetHashCode();
            }

            return SettingsFileDirectory.ToLowerInvariant().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var settings = obj as ChutzpahTestSettingsFile;
            if (settings == null)
            {
                return false;
            }

            if (SettingsFileDirectory == null && settings.SettingsFileDirectory == null)
            {
                return true;
            }

            return SettingsFileDirectory != null && SettingsFileDirectory.Equals(settings.SettingsFileDirectory, StringComparison.OrdinalIgnoreCase);

        }
    }
}