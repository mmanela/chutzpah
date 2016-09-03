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
        /// Find and reads a chutzpah test settings file given a directory. If none is found a default settings object is created
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        ChutzpahTestSettingsFile FindSettingsFileFromDirectory(string directory, ChutzpahSettingsFileEnvironments environments = null);


        /// <summary>
        /// Find and reads a chutzpah test settings file given a path to a the file. If none is found a default settings object is created
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        ChutzpahTestSettingsFile FindSettingsFile(string filePath, ChutzpahSettingsFileEnvironments environments = null);

        void ClearCache();
    }

    public class ChutzpahTestSettingsService : IChutzpahTestSettingsService
    {
        /// <summary>
        /// Cache settings file
        /// </summary>
        private readonly ConcurrentDictionary<string, ChutzpahTestSettingsFile> ChutzpahSettingsFileCache =
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


        public ChutzpahTestSettingsFile FindSettingsFile(string filePath, ChutzpahSettingsFileEnvironments environments = null)
        {
            if (string.IsNullOrEmpty(filePath)) return ChutzpahTestSettingsFile.Default;

            var directory = Path.GetDirectoryName(filePath);
            return FindSettingsFileFromDirectory(directory, environments);
        }

        /// <summary>
        /// Find and reads a chutzpah test settings file given a directory. If none is found a default settings object is created
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        /// 
        public ChutzpahTestSettingsFile FindSettingsFileFromDirectory(string directory, ChutzpahSettingsFileEnvironments environments = null)
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

                return ProcessSettingsFile(directory, environment).InheritFromDefault();
            }
            else
            {
                return settings;
            }
        }

        private ChutzpahTestSettingsFile ProcessSettingsFile(string directory, ChutzpahSettingsFileEnvironment environment, bool forceFresh = false)
        {
            if (string.IsNullOrEmpty(directory)) return ChutzpahTestSettingsFile.Default;

            directory = directory.TrimEnd('/', '\\');

            ChutzpahTestSettingsFile settings;
            if (!ChutzpahSettingsFileCache.TryGetValue(directory, out settings) || forceFresh)
            {
                var testSettingsFilePath = fileProbe.FindTestSettingsFile(directory);
                if (string.IsNullOrEmpty(testSettingsFilePath))
                {
                    ChutzpahTracer.TraceInformation("Chutzpah.json file not found given starting directory {0}", directory);
                    settings = ChutzpahTestSettingsFile.Default;
                }
                else if (!ChutzpahSettingsFileCache.TryGetValue(Path.GetDirectoryName(testSettingsFilePath), out settings) || forceFresh)
                {
                    ChutzpahTracer.TraceInformation("Chutzpah.json file found at {0} given starting directory {1}", testSettingsFilePath, directory);
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

                    ProcessTraceFilePath(settings, chutzpahVariables);

                    ProcessServerSettings(settings, chutzpahVariables);

                    ProcessInheritance(environment, settings, chutzpahVariables);

                    if (!forceFresh)
                    {
                        // Add a mapping in the cache for the directory that contains the test settings file
                        ChutzpahSettingsFileCache.TryAdd(settings.SettingsFileDirectory, settings);
                    }
                }

                if (!forceFresh)
                {
                    // Add mapping in the cache for the original directory tried to skip needing to traverse the tree again
                    ChutzpahSettingsFileCache.TryAdd(directory, settings);
                }
            }

            return settings;
        }

        private void ProcessServerSettings(ChutzpahTestSettingsFile settings, IDictionary<string, string> chutzpahVariables)
        {
            if (settings.Server != null)
            { 

                settings.Server.DefaultPort = settings.Server.DefaultPort ?? Constants.DefaultWebServerPort;

                string rootPath = null;
                if (!string.IsNullOrEmpty(settings.Server.RootPath))
                {
                    rootPath = settings.Server.RootPath;
                }
                else
                {
                    ChutzpahTracer.TraceInformation("Defaulting the RootPath to the drive root of the chutzpah.json file");
                    rootPath = Path.GetPathRoot(settings.SettingsFileDirectory);
                }

                settings.Server.RootPath = ResolveFolderPath(settings, ExpandVariable(chutzpahVariables, rootPath));

                
            }
        }

        private void ProcessTraceFilePath(ChutzpahTestSettingsFile settings, IDictionary<string, string> chutzpahVariables)
        {
            if (!string.IsNullOrEmpty(settings.TraceFilePath))
            {
                var path = Path.Combine(settings.SettingsFileDirectory, ExpandVariable(chutzpahVariables, settings.TraceFilePath));
                settings.TraceFilePath = path;
            }
        }

        private void ProcessInheritance(ChutzpahSettingsFileEnvironment environment, ChutzpahTestSettingsFile settings, IDictionary<string, string> chutzpahVariables)
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

                    string settingsToInherit = ExpandVariable(chutzpahVariables, settings.InheritFromPath);
                    if (settingsToInherit.EndsWith(Constants.SettingsFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        settingsToInherit = Path.GetDirectoryName(settingsToInherit);
                    }

                    settingsToInherit = ResolveFolderPath(settings, settingsToInherit);

                    settings.InheritFromPath = settingsToInherit;
                }

                // If we have any environment properties do not use cached
                // parents and re-evaluate using current environment
                var forceFresh = environment != null && environment.Properties.Any();

                var parentSettingsFile = ProcessSettingsFile(settings.InheritFromPath, environment, forceFresh);

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
            if (!string.IsNullOrEmpty(settings.AMDBaseUrl))
            {
                string absoluteFilePath = ResolveFolderPath(settings, ExpandVariable(chutzpahVariables, settings.AMDBaseUrl));
                settings.AMDBaseUrl = absoluteFilePath;

                if (string.IsNullOrEmpty(settings.AMDBaseUrl))
                {
                    ChutzpahTracer.TraceWarning("Unable to find AMDBaseUrl at {0}", settings.AMDBaseUrl);
                }
            }

            if (!string.IsNullOrEmpty(settings.AMDAppDirectory))
            {
                string absoluteFilePath = ResolveFolderPath(settings, ExpandVariable(chutzpahVariables, settings.AMDAppDirectory));
                settings.AMDAppDirectory = absoluteFilePath;

                if (string.IsNullOrEmpty(settings.AMDAppDirectory))
                {
                    ChutzpahTracer.TraceWarning("Unable to find AMDAppDirectory at {0}", settings.AMDAppDirectory);
                }
            }

            // Legacy AMDBasePath property
            if (!string.IsNullOrEmpty(settings.AMDBasePath))
            {

                string absoluteFilePath = ResolveFolderPath(settings, ExpandVariable(chutzpahVariables, settings.AMDBasePath));
                settings.AMDBasePath = absoluteFilePath;

                if (string.IsNullOrEmpty(settings.AMDBasePath))
                {
                    ChutzpahTracer.TraceWarning("Unable to find AMDBasePath at {0}", settings.AMDBasePath);
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
                if (settings.Compile.Mode == BatchCompileMode.Executable || (settings.Compile.Mode == null && !string.IsNullOrEmpty(settings.Compile.Executable)))
                {
                    if (string.IsNullOrEmpty(settings.Compile.Executable))
                    {
                        throw new ArgumentException("Executable path must be passed for compile setting");
                    }

                    settings.Compile.Mode = BatchCompileMode.Executable;
                    settings.Compile.Executable = ResolveFilePath(settings, ExpandVariable(chutzpahVariables, settings.Compile.Executable));
                    settings.Compile.Arguments = ExpandVariable(chutzpahVariables, settings.Compile.Arguments);
                    settings.Compile.WorkingDirectory = ResolveFolderPath(settings, settings.Compile.WorkingDirectory);

                    // Default timeout to 5 minutes if missing
                    settings.Compile.Timeout = settings.Compile.Timeout.HasValue ? settings.Compile.Timeout.Value : 1000 * 60 * 5;
                }
                else
                {
                    settings.Compile.Mode = BatchCompileMode.External;
                }



                // If Paths are not given 
                if (!settings.Compile.Paths.Any())
                {
                    // If not Paths are given handle backcompat and look at sourcedirectory and outdirectory.
                    // This will also handle the empty case in general since it will set empty values which will get resolved to the Chutzpah Settings file directory
                    settings.Compile.Paths.Add(new CompilePathMap { SourcePath = settings.Compile.SourceDirectory, OutputPath = settings.Compile.OutDirectory });
                }

                foreach (var pathMap in settings.Compile.Paths)
                {
                    ResolveCompilePathMap(settings, chutzpahVariables, pathMap);
                }
            }
        }

        private void ResolveCompilePathMap(ChutzpahTestSettingsFile settings, IDictionary<string, string> chutzpahVariables, CompilePathMap pathMap)
        {
            var sourcePath = pathMap.SourcePath;
            bool? sourcePathIsFile = false;

            // If SourcePath is null then we will assume later on this is the current settings directory
            pathMap.SourcePath = ResolvePath(settings, sourcePath, out sourcePathIsFile);
            if (pathMap.SourcePath == null)
            {
                throw new FileNotFoundException("Unable to find file/directory specified by SourcePath of {0}", (sourcePath ?? ""));
            }


            pathMap.SourcePathIsFile = sourcePathIsFile.HasValue ? sourcePathIsFile.Value : false;

            // If OutputPath is null then we will assume later on this is the current settings directory
            // We do not use the resolvePath method here since the path may not exist yet

            pathMap.OutputPath = UrlBuilder.NormalizeFilePath(Path.Combine(settings.SettingsFileDirectory, ExpandVariable(chutzpahVariables, pathMap.OutputPath)));
            if (pathMap.OutputPath == null)
            {
                throw new FileNotFoundException("Unable to find file/directory specified by OutputPath of {0}", (pathMap.OutputPath ?? ""));
            }

            // Since the output path might not exist yet we need a more complicated way to 
            // determine if it is a file or folder
            // 1. If the user explicitly told us what it should be using OutputPathType use that
            // 2. Assume it is a file if it has a .js extension
            if (pathMap.OutputPathType.HasValue)
            {
                pathMap.OutputPathIsFile = pathMap.OutputPathType == CompilePathType.File;
            }
            else
            {
                pathMap.OutputPathIsFile = pathMap.OutputPath.EndsWith(".js", StringComparison.OrdinalIgnoreCase);
            }

        }


        private string ResolvePath(ChutzpahTestSettingsFile settings, string path, out bool? isFile)
        {
            isFile = null;

            var filePath = ResolveFilePath(settings, path);
            if (filePath == null)
            {
                filePath = ResolveFolderPath(settings, path);
                if (filePath != null)
                {
                    isFile = false;
                }
            }
            else
            {
                isFile = true;
            }

            return filePath;
        }

        /// <summary>
        /// Resolved a path relative to the settings file if it is not absolute
        /// </summary>
        private string ResolveFolderPath(ChutzpahTestSettingsFile settings, string path)
        {
            string relativeLocationPath = Path.Combine(settings.SettingsFileDirectory, path ?? "");
            string absoluteFilePath = fileProbe.FindFolderPath(relativeLocationPath);

            return absoluteFilePath ?? relativeLocationPath;
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