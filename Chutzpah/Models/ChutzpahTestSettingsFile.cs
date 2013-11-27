using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Chutzpah.Compilers.TypeScript;
using Chutzpah.Wrappers;

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
        public ChutzpahTestSettingsFile()
        {
            CodeCoverageIncludes = new List<string>();
            CodeCoverageExcludes = new List<string>();
            References = new List<SettingsFileReference>();
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
        /// The path to your own test harness for Chutzpah to use. 
        /// This is an *advanced* scenario since Chutzpah has specific requirements on the test harness
        /// If you deploy your own then you must copy from Chutzpah's and if you upgrade Chutzpah
        /// you must keep parity
        /// There are no guarantees about anything working once you deploy your own.
        /// </summary>
        public string CustomTestHarnessPath { get; set; }

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
        /// The collection code coverage file patterns to include in coverage. These are in glob format. If you specify none all files are included.
        /// </summary>
        public ICollection<string> CodeCoverageIncludes { get; set; }

        /// <summary>
        /// The collection code coverage file patterns to exclude in coverage. These are in glob format. If you specify none no files are excluded.
        /// </summary>
        public ICollection<string> CodeCoverageExcludes { get; set; }

        /// <summary>
        /// The collection code coverage file patterns to exclude in coverage. These are in glob format. If you specify none no files are excluded.
        /// </summary>
        public ICollection<SettingsFileReference> References { get; set; }

        /// <summary>
        /// The path to the settings file
        /// </summary>
        public string SettingsFileDirectory { get; set; }

        /// <summary>
        /// Cache settings file
        /// </summary>
        private static readonly ConcurrentDictionary<string, ChutzpahTestSettingsFile> ChutzpahSettingsFileCache =
            new ConcurrentDictionary<string, ChutzpahTestSettingsFile>(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Find and reads a chutzpah test settings file given a direcotry. If none is found a default settings object is created
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="fileProbe"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static ChutzpahTestSettingsFile Read(string directory, IFileProbe fileProbe, IJsonSerializer serializer)
        {
            if (string.IsNullOrEmpty(directory)) return new ChutzpahTestSettingsFile();

            directory = directory.TrimEnd('/', '\\');

            ChutzpahTestSettingsFile settings;
            if (!ChutzpahSettingsFileCache.TryGetValue(directory, out settings))
            {
                var testSettingsFilePath = fileProbe.FindTestSettingsFile(directory);
                if(string.IsNullOrEmpty(testSettingsFilePath))
                {
                    settings = new ChutzpahTestSettingsFile();
                }
                else if (!ChutzpahSettingsFileCache.TryGetValue(testSettingsFilePath, out settings))
                {
                    settings = serializer.DeserializeFromFile<ChutzpahTestSettingsFile>(testSettingsFilePath);
                    settings.SettingsFileDirectory = Path.GetDirectoryName(testSettingsFilePath);

                    ValidateTestHarnessLocationMode(settings, fileProbe);

                    // Add a mapping in the cache for the directory that contains the test settings file
                    ChutzpahSettingsFileCache.TryAdd(settings.SettingsFileDirectory, settings);
                }

                // Add mapping in the cache for the original directory tried to skip needing to traverse the tree again
                ChutzpahSettingsFileCache.TryAdd(directory, settings);
            }

            return settings;
        }

        private static void ValidateTestHarnessLocationMode(ChutzpahTestSettingsFile settings, IFileProbe fileProbe)
        {
            if (settings.TestHarnessLocationMode == TestHarnessLocationMode.Custom)
            {
                if (settings.TestHarnessDirectory != null)
                {
                    string relativeLocationPath = Path.Combine(settings.SettingsFileDirectory, settings.TestHarnessDirectory);
                    string absoluteFilePath = fileProbe.FindFolderPath(relativeLocationPath);
                    settings.TestHarnessDirectory = absoluteFilePath;
                }

                if (settings.TestHarnessDirectory == null)
                {
                    settings.TestHarnessLocationMode = TestHarnessLocationMode.TestFileAdjacent;
                    ChutzpahTracer.TraceWarning("Unable to find custom test harness directory at {0}", settings.TestHarnessDirectory);
                }
            }
        }
    }

}