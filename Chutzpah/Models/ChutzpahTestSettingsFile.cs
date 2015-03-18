using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

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
        public static ChutzpahTestSettingsFile Default = new ChutzpahTestSettingsFile(true);
        private Regex testPatternRegex;

        public ChutzpahTestSettingsFile()
        {
            CodeCoverageIncludes = new List<string>();
            CodeCoverageExcludes = new List<string>();
            References = new List<SettingsFileReference>();
            Tests = new List<SettingsFileTestPath>();
            Transforms = new List<TransformConfig>();
        }

        private ChutzpahTestSettingsFile(bool isDefaultSetings) : this()
        {
            IsDefaultSettings = true;

            CodeCoverageSuccessPercentage = Constants.DefaultCodeCoverageSuccessPercentage;
            TestHarnessReferenceMode = Chutzpah.Models.TestHarnessReferenceMode.Normal;
            TestHarnessLocationMode = Chutzpah.Models.TestHarnessLocationMode.TestFileAdjacent;
            RootReferencePathMode = Chutzpah.Models.RootReferencePathMode.DriveRoot;
            EnableTestFileBatching = false;
            IgnoreResourceLoadingErrors = false;
        }

        public bool IsDefaultSettings { get; set; }

        /// <summary>
        /// Determines if this settings file should inherit and merge with the settings of its
        /// parent settings file.
        /// </summary>
        public bool InheritFromParent { get; set; }

        /// <summary>
        /// Determines if this settings file should inherit and merge with the settings of a given file
        /// </summary>
        public string InheritFromPath { get; set; }

        /// <summary>
        /// Suppress errors that are reporting when a script request to load a url (e.g. xhr/script/image)
        /// and that url fails to load
        /// </summary>
        public bool? IgnoreResourceLoadingErrors { get; set; }

        /// <summary>
        /// Determines if Chutzpah should try to batch all test files for this chutzpah.json file in one test harness
        /// </summary>
        public bool? EnableTestFileBatching { get; set; }

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
        public TestHarnessReferenceMode? TestHarnessReferenceMode { get; set; }


        /// <summary>
        /// Tells Chutzpah where it should place the generated test harness html.
        /// TestFileAdjacent - Places the harness next to the file under test. This is the default.
        /// SettingsFileAdjacent - Places the harness next to the first chutzpah.json file found up the directory tree from the file under test
        /// Custom - Lets you specify the TestHarnessDirectory property to give a custom folder to place the test harness. If folder is not found it will revert to the default.
        /// </summary>
        public TestHarnessLocationMode? TestHarnessLocationMode { get; set; }

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
        /// ADVANCED PROPERTY: This property allow you to tell Chutzpah to set the AMD - BaseUrl without having it try to do the file path
        /// remapping that it does when you use the AMDBasePath property. This is helpful only in special situations where you need this control
        /// </summary>
        public string AMDBaseUrlOverride { get; set; }

        /// <summary>
        /// Determines what a reference path that starts with / or \  (.e.g <reference path="/" />) is relative to
        /// DriveRoot - Make it relative to the root of the drive (e.g. C:\). This is default.
        /// SettingsFileDirectory - Makes root path relative to the directory of the settings file
        /// </summary>
        public RootReferencePathMode? RootReferencePathMode { get; set; }

        /// <summary>
        /// If True, forces code coverage to run always
        /// If Null or not not set, allows code coverage to run if invoked using test adapter, command line or context menu options (default)
        /// If False, forces code coverage to never run. 
        /// </summary>
        public bool? EnableCodeCoverage { get; set; }

        /// <summary>
        /// The percentage of lines should be covered to show the coverage output as success or failure. By default, this is 60.
        /// </summary>
        public double? CodeCoverageSuccessPercentage { get; set; }

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
        /// 
        /// This settings will not get inherited from parent settings file
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
        /// The compile settings which tell Chutzpah how to perform the batch compile
        /// </summary>
        public BatchCompileConfiguration Compile { get; set; }

        /// <summary>
        /// The user agent to tell PhantomJS to use when making web requests
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Maps the names of transforms to run after testing with their corresponding output paths.
        /// </summary>
        public ICollection<TransformConfig> Transforms { get; set; }


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

        public ChutzpahTestSettingsFile InheritFromDefault()
        {
            return this.InheritFrom(Default);
        }

        /// <summary>
        /// Merge a selection of settings from a parent file into the current one.
        /// This merge will work as follows
        /// 1. For basic properties the child's property wins if it is different than the default
        /// 2. For complex objects the child's property wins if it is not null
        /// 3. For lists the childs items get added to the parents
        /// </summary>
        /// <param name="parent"></param>
        public ChutzpahTestSettingsFile InheritFrom(ChutzpahTestSettingsFile parent)
        {
            if (parent == null || this.IsDefaultSettings)
            {
                return this;
            }


            this.References = parent.References.Concat(this.References).ToList();
            this.CodeCoverageIncludes = parent.CodeCoverageIncludes.Concat(this.CodeCoverageIncludes).ToList();
            this.CodeCoverageExcludes = parent.CodeCoverageExcludes.Concat(this.CodeCoverageExcludes).ToList();
            this.Transforms = parent.Transforms.Concat(this.Transforms).ToList();

            if (this.Compile == null)
            {
                this.Compile = parent.Compile;
            }

            
            this.AMDBasePath = this.AMDBasePath == null ? parent.AMDBasePath : this.AMDBasePath;
            this.AMDBaseUrlOverride = this.AMDBaseUrlOverride == null ? parent.AMDBaseUrlOverride : this.AMDBaseUrlOverride;
            this.CodeCoverageSuccessPercentage = this.CodeCoverageSuccessPercentage == null ? parent.CodeCoverageSuccessPercentage : this.CodeCoverageSuccessPercentage;
            this.CustomTestHarnessPath = this.CustomTestHarnessPath == null ? parent.CustomTestHarnessPath : this.CustomTestHarnessPath;
            this.EnableCodeCoverage = this.EnableCodeCoverage == null ? parent.EnableCodeCoverage : this.EnableCodeCoverage;
            this.Framework = this.Framework == null ? parent.Framework : this.Framework;
            this.FrameworkVersion = this.FrameworkVersion == null ? parent.FrameworkVersion : this.FrameworkVersion;
            this.MochaInterface = this.MochaInterface == null ? parent.MochaInterface : this.MochaInterface;
            this.RootReferencePathMode = this.RootReferencePathMode == null ? parent.RootReferencePathMode : this.RootReferencePathMode;
            this.TestFileTimeout = this.TestFileTimeout == null ? parent.TestFileTimeout : this.TestFileTimeout;
            this.TestHarnessReferenceMode = this.TestHarnessReferenceMode == null ? parent.TestHarnessReferenceMode : this.TestHarnessReferenceMode;
            this.TestPattern = this.TestPattern == null ? parent.TestPattern : this.TestPattern;
            this.UserAgent = this.UserAgent == null ? parent.UserAgent : this.UserAgent;
            this.EnableTestFileBatching = this.EnableTestFileBatching == null ? parent.EnableTestFileBatching : this.EnableTestFileBatching;
            this.IgnoreResourceLoadingErrors = this.IgnoreResourceLoadingErrors == null ? parent.IgnoreResourceLoadingErrors : this.IgnoreResourceLoadingErrors;


            // We need to handle an inherited test harness location mode specially
            // If the parent set their mode to SettingsFileAdjacent and the current file has it set to null 
            // Then we make the curent file have a Custom mode with the parent files settings directory
            if (this.TestHarnessLocationMode == null)
            {
                if (parent.TestHarnessLocationMode == Chutzpah.Models.TestHarnessLocationMode.SettingsFileAdjacent && !parent.IsDefaultSettings)
                {
                    this.TestHarnessLocationMode = Chutzpah.Models.TestHarnessLocationMode.Custom;
                    this.TestHarnessDirectory = parent.SettingsFileDirectory;
                }
                else
                {
                    this.TestHarnessDirectory = parent.TestHarnessDirectory;
                    this.TestHarnessLocationMode = parent.TestHarnessLocationMode;
                }
            }
            


            return this;
        }
    }
}