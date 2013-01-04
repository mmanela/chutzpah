using System;
using System.Collections.Concurrent;
using System.IO;
using Chutzpah.Wrappers;

namespace Chutzpah.Models
{
    public enum TestHarnessLocationMode
    {
        TestFileAdjacent,
        SettingsFileAdjacent,
        Custom
    }

    /// <summary>
    /// Represents the Chutzpah Test Settings file (chutzpah.json)
    /// Applies to all test files in its directory and below.
    /// </summary>
    public class ChutzpahTestSettingsFile
    {
        /// <summary>
        /// If not null tells Chutzpah which framework to use instead of detecting automatically
        /// </summary>
        public string Framework { get; set; }

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

            var settings = new ChutzpahTestSettingsFile();
            if(!ChutzpahSettingsFileCache.ContainsKey(directory))
            {
                var testSettingsFilePath = fileProbe.FindTestSettingsFile(directory);
                if(string.IsNullOrEmpty(testSettingsFilePath))
                {
                    // TODO: Log inability to find test file
                    return settings;
                }

                if (!ChutzpahSettingsFileCache.TryGetValue(testSettingsFilePath, out settings))
                {
                    settings = serializer.DeserializeFromFile<ChutzpahTestSettingsFile>(testSettingsFilePath);
                    settings.SettingsFileDirectory = Path.GetDirectoryName(testSettingsFilePath);

                    ValidateTestHarnessLocationMode(settings, fileProbe);

                    // Add a mapping in the cache for the directory that contains the test settings file
                    ChutzpahSettingsFileCache.TryAdd(testSettingsFilePath, settings);
                }

                // Add mapping in the cahce for the original directory tried to skip needing to traverse the tree again
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
                    // TODO: log failure to find custom test harness directory
                }
            }
        }
    }
}