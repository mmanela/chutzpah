using System;
using System.Collections.Concurrent;
using System.IO;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah
{
    public interface IChutzpahTestSettingsService
    {
        /// <summary>
        /// Find and reads a chutzpah test settings file given a direcotry. If none is found a default settings object is created
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        ChutzpahTestSettingsFile FindSettingsFile(string directory);

        void ClearCache();
    }

    public class ChutzpahTestSettingsService : IChutzpahTestSettingsService
    {
        /// <summary>
        /// Cache settings file
        /// </summary>
        private static readonly ConcurrentDictionary<string, ChutzpahTestSettingsFile> ChutzpahSettingsFileCache =
            new ConcurrentDictionary<string, ChutzpahTestSettingsFile>(StringComparer.OrdinalIgnoreCase);
        

        private readonly IFileProbe fileProbe;
        private readonly IJsonSerializer serializer;

        public ChutzpahTestSettingsService(IFileProbe fileProbe, IJsonSerializer serializer)
        {
            this.fileProbe = fileProbe;
            this.serializer = serializer;
        }

        /// <summary>
        /// Find and reads a chutzpah test settings file given a direcotry. If none is found a default settings object is created
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public ChutzpahTestSettingsFile FindSettingsFile(string directory)
        {
            if (string.IsNullOrEmpty(directory)) return ChutzpahTestSettingsFile.Default;

            directory = directory.TrimEnd('/', '\\');

            ChutzpahTestSettingsFile settings;
            if (!ChutzpahSettingsFileCache.TryGetValue(directory, out settings))
            {
                var testSettingsFilePath = fileProbe.FindTestSettingsFile(directory);
                if (string.IsNullOrEmpty(testSettingsFilePath))
                {
                    ChutzpahTracer.TraceInformation("Chutzpah.json file not found given starting directoy {0}", directory);
                    settings = ChutzpahTestSettingsFile.Default;
                }
                else if (!ChutzpahSettingsFileCache.TryGetValue(testSettingsFilePath, out settings))
                {
                    ChutzpahTracer.TraceInformation("Chutzpah.json file found at {0} given starting directoy {1}", testSettingsFilePath, directory);
                    settings = serializer.DeserializeFromFile<ChutzpahTestSettingsFile>(testSettingsFilePath);

                    if (settings == null)
                    {
                        settings = ChutzpahTestSettingsFile.Default;
                    }

                    settings.SettingsFileDirectory = Path.GetDirectoryName(testSettingsFilePath);

                    ValidateTestHarnessLocationMode(settings);

                    // Add a mapping in the cache for the directory that contains the test settings file
                    ChutzpahSettingsFileCache.TryAdd(settings.SettingsFileDirectory, settings);
                }

                // Add mapping in the cache for the original directory tried to skip needing to traverse the tree again
                ChutzpahSettingsFileCache.TryAdd(directory, settings);
            }

            return settings;
        }


        public void ClearCache()
        {
            ChutzpahTracer.TraceInformation("Chutzpah.json file cache cleared");
            ChutzpahSettingsFileCache.Clear();
        }

        private void ValidateTestHarnessLocationMode(ChutzpahTestSettingsFile settings)
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