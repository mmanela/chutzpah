using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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


        private readonly IDictionary<string,string> chutzpahCompileVariables = new  Dictionary<string, string>();
           

        private readonly IFileProbe fileProbe;
        private readonly IJsonSerializer serializer;

        public ChutzpahTestSettingsService(IFileProbe fileProbe, IJsonSerializer serializer)
        {
            this.fileProbe = fileProbe;
            this.serializer = serializer;

            var clrDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            var msbuildExe = Path.Combine(clrDir, "msbuild.exe");
            var powershellExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"windowspowershell\v1.0\powershell.exe");

            chutzpahCompileVariables["%clrdir%"] = clrDir;
            chutzpahCompileVariables["%msbuildexe%"] = msbuildExe;
            chutzpahCompileVariables["%powershellexe%"] = powershellExe;

            // This is not needed but it is a nice alias
            chutzpahCompileVariables["%cmdexe%"] = Environment.ExpandEnvironmentVariables("%comspec%");
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

                    ResolveTestHarnessDirectory(settings);

                    ResolveAMDBaseUrl(settings);

                    ResolveBatchCompileConfiguration(settings);

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

        private void ResolveTestHarnessDirectory(ChutzpahTestSettingsFile settings)
        {
            if (settings.TestHarnessLocationMode == TestHarnessLocationMode.Custom)
            {
                if (settings.TestHarnessDirectory != null)
                {
                    string absoluteFilePath = ResolveFolderPath(settings, settings.TestHarnessDirectory);
                    settings.TestHarnessDirectory = absoluteFilePath;
                }

                if (settings.TestHarnessDirectory == null)
                {
                    settings.TestHarnessLocationMode = TestHarnessLocationMode.TestFileAdjacent;
                    ChutzpahTracer.TraceWarning("Unable to find custom test harness directory at {0}", settings.TestHarnessDirectory);
                }
            }
        }

        private void ResolveAMDBaseUrl(ChutzpahTestSettingsFile settings)
        {
            if (!string.IsNullOrEmpty(settings.AMDBasePath))
            {

                string absoluteFilePath = ResolveFolderPath(settings, settings.AMDBasePath);
                settings.AMDBasePath = absoluteFilePath;

                if (string.IsNullOrEmpty(settings.AMDBasePath))
                {
                    ChutzpahTracer.TraceWarning("Unable to find AMDBasePath at {0}", settings.AMDBasePath);
                }
            }
        }

        private void ResolveBatchCompileConfiguration(ChutzpahTestSettingsFile settings)
        {
            if (settings.Compile != null)
            {
                settings.Compile.Extensions = settings.Compile.Extensions ?? new List<string>();

                if (string.IsNullOrEmpty(settings.Compile.Executable))
                {
                    throw new ArgumentException("Executable path must be passed for compile setting");
                }
                
                settings.Compile.Executable = ResolveFilePath(settings, ExpandVariable(settings.Compile.Executable ?? ""));
                settings.Compile.Arguments = ExpandVariable(settings.Compile.Arguments ?? "");
                settings.Compile.WorkingDirectory = ResolveFolderPath(settings, settings.Compile.WorkingDirectory);
                settings.Compile.SourceDirectory = ResolveFolderPath(settings, settings.Compile.SourceDirectory);
                settings.Compile.OutDirectory = ResolveFolderPath(settings, ExpandVariable(settings.Compile.OutDirectory));

                // Default timeout to 5 minutes if missing
                settings.Compile.Timeout = settings.Compile.Timeout.HasValue ? settings.Compile.Timeout.Value : 1000 * 60 * 5;
            }
        }


        /// <summary>
        /// Resolved a path relative to the settings file if it is not absolute
        /// </summary>
        private string ResolveFolderPath(ChutzpahTestSettingsFile settings, string path)
        {
            string relativeLocationPath = Path.Combine(settings.SettingsFileDirectory, path ?? "");
            string absoluteFilePath = fileProbe.FindFolderPath(relativeLocationPath);
            return absoluteFilePath;
        }

        private string ResolveFilePath(ChutzpahTestSettingsFile settings, string path)
        {
            string relativeLocationPath = Path.Combine(settings.SettingsFileDirectory, path ?? "");
            string absoluteFilePath = fileProbe.FindFilePath(relativeLocationPath);
            return absoluteFilePath;
        }

        private string ExpandVariable(string str)
        {
            return ExpandChutzpahVariables(Environment.ExpandEnvironmentVariables(str));
        }

        private string ExpandChutzpahVariables(string str)
        {
            if (str == null)
            {
                return null;
            }

            return chutzpahCompileVariables.Aggregate(str, (current, pair) => current.Replace(pair.Key, pair.Value));
        }
    }
}