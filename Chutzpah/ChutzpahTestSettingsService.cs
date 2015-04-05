using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
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
        ChutzpahTestSettingsFile FindSettingsFile(string directory, ChutzpahSettingsFileEnvironments environments = null);

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
        private readonly IFileSystemWrapper fileSystem;

        public ChutzpahTestSettingsService(IFileProbe fileProbe, IJsonSerializer serializer, IFileSystemWrapper fileSystem)
        {
            this.fileProbe = fileProbe;
            this.serializer = serializer;
            this.fileSystem = fileSystem;


        }

        /// <summary>
        /// Find and reads a chutzpah test settings file given a direcotry. If none is found a default settings object is created
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        /// 
        public ChutzpahTestSettingsFile FindSettingsFile(string directory, ChutzpahSettingsFileEnvironments environments = null)
        {
            if (string.IsNullOrEmpty(directory)) return ChutzpahTestSettingsFile.Default;

            directory = directory.TrimEnd('/', '\\');

            ChutzpahTestSettingsFile settings;
            if (!ChutzpahSettingsFileCache.TryGetValue(directory, out settings))
            {
                ChutzpahSettingsFileEnvironment environment = null;
                if (environments != null)
                {
                    environment = environments.GetSettingsFileEnvironment(directory);
                }

                return FindSettingsFile(directory, environment).InheritFromDefault();
            }
            else
            {
                return settings;
            }
        }

        private ChutzpahTestSettingsFile FindSettingsFile(string directory, ChutzpahSettingsFileEnvironment environment)
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
                    else
                    {
                        settings.IsDefaultSettings = false;
                    }

                    settings.SettingsFileDirectory = Path.GetDirectoryName(testSettingsFilePath);

                    var chutzpahVariables = BuildChutzpahReplacementVariables(testSettingsFilePath, environment, settings);

                    ResolveTestHarnessDirectory(settings, chutzpahVariables);

                    ResolveAMDBaseUrl(settings, chutzpahVariables);

                    ResolveBatchCompileConfiguration(settings, chutzpahVariables);

                    ProcessPathSettings(settings, chutzpahVariables);

                    ProcessInheritance(environment, settings);

                    // Add a mapping in the cache for the directory that contains the test settings file
                    ChutzpahSettingsFileCache.TryAdd(settings.SettingsFileDirectory, settings);
                }

                // Add mapping in the cache for the original directory tried to skip needing to traverse the tree again
                ChutzpahSettingsFileCache.TryAdd(directory, settings);
            }



            return settings;
        }

        private void ProcessInheritance(ChutzpahSettingsFileEnvironment environment, ChutzpahTestSettingsFile settings)
        {
            if (settings.InheritFromParent || !string.IsNullOrEmpty(settings.InheritFromPath))
            {

                if (string.IsNullOrEmpty(settings.InheritFromPath))
                {
                    ChutzpahTracer.TraceInformation("Searching for parent Chutzpah.json to inherit from");
                    settings.InheritFromPath = Path.GetDirectoryName(settings.SettingsFileDirectory);
                }
                else
                {
                    ChutzpahTracer.TraceInformation("Searching for Chutzpah.json to inherit from at {0}", settings.InheritFromPath);

                    string settingsToInherit = settings.InheritFromPath;
                    if (settingsToInherit.EndsWith(Constants.SettingsFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        settingsToInherit = Path.GetDirectoryName(settingsToInherit);
                    }

                    settingsToInherit = ResolveFolderPath(settings, settingsToInherit);

                    settings.InheritFromPath = settingsToInherit;
                }

                var parentSettingsFile = FindSettingsFile(settings.InheritFromPath, environment);

                if (!parentSettingsFile.IsDefaultSettings)
                {

                    ChutzpahTracer.TraceInformation("Found parent Chutzpah.json in directory {0}", parentSettingsFile.SettingsFileDirectory);
                    settings.InheritFrom(parentSettingsFile);
                }
                else
                {
                    ChutzpahTracer.TraceInformation("Could not find a parent Chutzpah.json");
                }

            }
        }


        public void ClearCache()
        {
            ChutzpahTracer.TraceInformation("Chutzpah.json file cache cleared");
            ChutzpahSettingsFileCache.Clear();
        }

        private void ResolveTestHarnessDirectory(ChutzpahTestSettingsFile settings, IDictionary<string, string> chutzpahVariables)
        {
            if (settings.TestHarnessLocationMode == TestHarnessLocationMode.Custom)
            {
                if (settings.TestHarnessDirectory != null)
                {
                    string absoluteFilePath = ResolveFolderPath(settings, ExpandVariable(chutzpahVariables, settings.TestHarnessDirectory));
                    settings.TestHarnessDirectory = absoluteFilePath;
                }

                if (settings.TestHarnessDirectory == null)
                {
                    settings.TestHarnessLocationMode = TestHarnessLocationMode.TestFileAdjacent;
                    ChutzpahTracer.TraceWarning("Unable to find custom test harness directory at {0}", settings.TestHarnessDirectory);
                }
            }
        }

        private void ResolveAMDBaseUrl(ChutzpahTestSettingsFile settings, IDictionary<string, string> chutzpahVariables)
        {
            if (!string.IsNullOrEmpty(settings.AMDBasePath))
            {

                string absoluteFilePath = ResolveFolderPath(settings, ExpandVariable(chutzpahVariables, settings.AMDBasePath));
                settings.AMDBasePath = absoluteFilePath;

                if (string.IsNullOrEmpty(settings.AMDBasePath))
                {
                    ChutzpahTracer.TraceWarning("Unable to find AMDBasePath at {0}", settings.AMDBasePath);
                }
            }


            if (!string.IsNullOrEmpty(settings.AMDBaseUrlOverride))
            {
                string absoluteFilePath = ResolveFolderPath(settings, ExpandVariable(chutzpahVariables, settings.AMDBaseUrlOverride));
                settings.AMDBaseUrlOverride = absoluteFilePath;

                if (string.IsNullOrEmpty(settings.AMDBaseUrlOverride))
                {
                    ChutzpahTracer.TraceWarning("Unable to find AMDBaseUrlOverride at {0}", settings.AMDBaseUrlOverride);
                }
            }
            
        }

        private void ProcessPathSettings(ChutzpahTestSettingsFile settings, IDictionary<string, string> chutzpahVariables)
        {
            var i = 0;
            foreach (var test in settings.Tests)
            {
                test.SettingsFileDirectory = settings.SettingsFileDirectory;
                test.Path = ExpandVariable(chutzpahVariables, test.Path);


                for (i = 0; i < test.Includes.Count; i++)
                {
                    test.Includes[i] = ExpandVariable(chutzpahVariables, test.Includes[i]);
                }

                for (i = 0; i < test.Excludes.Count; i++)
                {
                    test.Excludes[i] = ExpandVariable(chutzpahVariables, test.Excludes[i]);
                }
            }

            foreach (var reference in settings.References)
            {
                reference.SettingsFileDirectory = settings.SettingsFileDirectory;
                reference.Path = ExpandVariable(chutzpahVariables, reference.Path);

                for (i = 0; i < reference.Includes.Count; i++)
                {
                    reference.Includes[i] = ExpandVariable(chutzpahVariables, reference.Includes[i]);
                }

                for (i = 0; i < reference.Excludes.Count; i++)
                {
                    reference.Excludes[i] = ExpandVariable(chutzpahVariables, reference.Excludes[i]);
                }
            }

            foreach (var transform in settings.Transforms)
            {
                transform.SettingsFileDirectory = settings.SettingsFileDirectory;
                transform.Path = ExpandVariable(chutzpahVariables, transform.Path);
            }
        }

        private void ResolveBatchCompileConfiguration(ChutzpahTestSettingsFile settings, IDictionary<string, string> chutzpahVariables)
        {
            if (settings.Compile != null)
            {
                settings.Compile.SettingsFileDirectory = settings.SettingsFileDirectory;
                settings.Compile.Extensions = settings.Compile.Extensions ?? new List<string>();


                // If the mode is executable then set its properties
                if (settings.Compile.Mode == BatchCompileMode.Executable)
                {
                    if (string.IsNullOrEmpty(settings.Compile.Executable))
                    {
                        throw new ArgumentException("Executable path must be passed for compile setting");
                    }
                    settings.Compile.Executable = ResolveFilePath(settings, ExpandVariable(chutzpahVariables, settings.Compile.Executable));
                    settings.Compile.Arguments = ExpandVariable(chutzpahVariables, settings.Compile.Arguments);
                    settings.Compile.WorkingDirectory = ResolveFolderPath(settings, settings.Compile.WorkingDirectory);

                    // Default timeout to 5 minutes if missing
                    settings.Compile.Timeout = settings.Compile.Timeout.HasValue ? settings.Compile.Timeout.Value : 1000 * 60 * 5;
                }

                // These settings might be needed in either External
                settings.Compile.SourceDirectory = ResolveFolderPath(settings, settings.Compile.SourceDirectory);
                settings.Compile.OutDirectory = ResolveFolderPath(settings, ExpandVariable(chutzpahVariables, settings.Compile.OutDirectory), true);
            }
        }


        /// <summary>
        /// Resolved a path relative to the settings file if it is not absolute
        /// </summary>
        private string ResolveFolderPath(ChutzpahTestSettingsFile settings, string path, bool createIfNeeded = false)
        {
            string relativeLocationPath = Path.Combine(settings.SettingsFileDirectory, path ?? "");
            string absoluteFilePath = fileProbe.FindFolderPath(relativeLocationPath);
            if (createIfNeeded && absoluteFilePath == null)
            {
                fileSystem.CreateDirectory(relativeLocationPath);
                absoluteFilePath = fileProbe.FindFolderPath(relativeLocationPath);
            }

            return absoluteFilePath;
        }

        private string ResolveFilePath(ChutzpahTestSettingsFile settings, string path)
        {
            string relativeLocationPath = Path.Combine(settings.SettingsFileDirectory, path ?? "");
            string absoluteFilePath = fileProbe.FindFilePath(relativeLocationPath);
            return absoluteFilePath;
        }

        private string ExpandVariable(IDictionary<string, string> chutzpahCompileVariables, string str)
        {
            return ExpandChutzpahVariables(chutzpahCompileVariables, Environment.ExpandEnvironmentVariables(str ?? ""));
        }

        private string ExpandChutzpahVariables(IDictionary<string, string> chutzpahCompileVariables, string str)
        {
            if (str == null)
            {
                return null;
            }

            return chutzpahCompileVariables.Aggregate(str, (current, pair) => current.Replace(pair.Key, pair.Value));
        }

        private IDictionary<string, string> BuildChutzpahReplacementVariables(string settingsFilePath, ChutzpahSettingsFileEnvironment environment, ChutzpahTestSettingsFile settings)
        {
            IDictionary<string, string> chutzpahVariables = new Dictionary<string, string>();

            var clrDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            var msbuildExe = Path.Combine(clrDir, "msbuild.exe");
            var powershellExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"windowspowershell\v1.0\powershell.exe");


            AddChutzpahVariable(chutzpahVariables, "chutzpahsettingsdir", settings.SettingsFileDirectory);

            AddChutzpahVariable(chutzpahVariables, "clrdir", clrDir);
            AddChutzpahVariable(chutzpahVariables, "msbuildexe", msbuildExe);
            AddChutzpahVariable(chutzpahVariables, "powershellexe", powershellExe);

            // This is not needed but it is a nice alias
            AddChutzpahVariable(chutzpahVariables, "cmdexe", Environment.ExpandEnvironmentVariables("%comspec%"));

            if (environment != null)
            {
                // See if we have a settingsfileenvironment set and if so add its properties as chutzpah settings file variables
                var props = environment.Properties;
                foreach (var prop in props)
                {
                    AddChutzpahVariable(chutzpahVariables, prop.Name, prop.Value);
                }
            }

            return chutzpahVariables;
        }

        private void AddChutzpahVariable(IDictionary<string, string> chutzpahCompileVariables, string name, string value)
        {
            name = string.Format("%{0}%", name);
            chutzpahCompileVariables[name] = value;
        }
    }
}