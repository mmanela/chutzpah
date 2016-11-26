using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Chutzpah
{
    public class FileProbe : IFileProbe
    {
        private readonly IEnvironmentWrapper environment;
        private readonly IFileSystemWrapper fileSystem;
        private readonly IHasher hasher;
        private string builtInDependencyDirectory;

        private static readonly Dictionary<string, PathType> ExtensionToPathTypeMap =
            new Dictionary<string, PathType>
            {
                {Constants.TypeScriptExtension,PathType.JavaScript},
                {Constants.TypeScriptReactExtension,PathType.JavaScript},
                {Constants.TypeScriptDefExtension,PathType.JavaScript},
                {Constants.CoffeeScriptExtension,PathType.JavaScript},
                {Constants.JavaScriptExtension,PathType.JavaScript},
                {Constants.JavaScriptReactExtension,PathType.JavaScript},
                {Constants.HtmlScriptExtension,PathType.Html},
                {Constants.HtmScriptExtension,PathType.Html},
                {Constants.CSHtmlScriptExtension,PathType.Html},
            };

        public FileProbe(IEnvironmentWrapper environment, IFileSystemWrapper fileSystem, IHasher hasher)
        {
            this.environment = environment;
            this.fileSystem = fileSystem;
            this.hasher = hasher;
        }

        /// <summary>
        /// Finds a Chutzpah test settings file given a directory. Will recursively scan current direcotry 
        /// and all directories above until it finds the file 
        /// </summary>
        /// <param name="currentDirectory">the directory to start searching from</param>
        /// <returns>Eithe the found setting file path or null</returns>
        public string FindTestSettingsFile(string currentDirectory)
        {
            string settingsFilePath = null;

            while (!string.IsNullOrEmpty(currentDirectory))
            {
                settingsFilePath = Path.Combine(currentDirectory, Constants.SettingsFileName);
                if (fileSystem.FileExists(settingsFilePath))
                {
                    break;
                }
                else
                {
                    settingsFilePath = null;
                    currentDirectory = Path.GetDirectoryName(currentDirectory);
                }

            }

            return settingsFilePath;
        }

        public string FindFilePath(string path)
        {
            if (path != null && RegexPatterns.SchemePrefixRegex.IsMatch(path))
            {
                // Assume a web url exists
                return path;
            }

            return FindPath(path, fileSystem.FileExists);
        }

        public string FindFolderPath(string path)
        {
            return FindPath(path, fileSystem.FolderExists);
        }

        public bool IsTemporaryChutzpahFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            var fileName = Path.GetFileName(path);
            return !string.IsNullOrEmpty(fileName) && fileName.StartsWith(Constants.ChutzpahTemporaryFilePrefix, StringComparison.OrdinalIgnoreCase);
        }


        public bool IsChutzpahSettingsFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            var fileName = Path.GetFileName(path);
            return !string.IsNullOrEmpty(fileName) && fileName.Equals(Constants.SettingsFileName, StringComparison.OrdinalIgnoreCase);
        }

        public IEnumerable<PathInfo> FindScriptFiles(string path)
        {
            if (string.IsNullOrEmpty(path)) return Enumerable.Empty<PathInfo>();

            return FindScriptFiles(new[] { path });
        }

        public IEnumerable<PathInfo> FindScriptFiles(IEnumerable<string> testPaths)
        {
            if (testPaths == null) yield break;

            foreach (var path in testPaths)
            {
                var pathInfo = GetPathInfo(path);

                switch (pathInfo.Type)
                {
                    case PathType.Url:
                            yield return pathInfo;
                        break;
                    case PathType.Html:
                    case PathType.JavaScript:
                        yield return pathInfo;
                        break;
                    case PathType.Folder:
                        var query = from file in fileSystem.GetFiles(pathInfo.FullPath, "*.*", SearchOption.AllDirectories)
                                    where file.Length < 260 && !IsTemporaryChutzpahFile(file)
                                    let info = GetPathInfo(file)
                                    where info.Type != PathType.Other
                                    select info;

                        foreach (var item in query)
                        {
                            yield return item;
                        }

                        break;

                    default:
                        ChutzpahTracer.TraceWarning("Ignoring unsupported test path '{0}'", path);
                        break;

                }
            }
        }

        public PathInfo GetPathInfo(string path)
        {
            var fullPath = FindFolderPath(path);
            if (fullPath != null) return new PathInfo { Path = path, FullPath = fullPath, Type = PathType.Folder };

            fullPath = FindFilePath(path);
            var pathType = GetFilePathType(path);
            return new PathInfo { Path = path, FullPath = fullPath, Type = pathType };
        }

        public IEnumerable<PathInfo> FindScriptFiles(ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            if (chutzpahTestSettings == null) yield break;

            foreach (var pathSettings in chutzpahTestSettings.Tests.Where(x => x != null))
            {
                var includePatterns = pathSettings.Includes.Select(x => UrlBuilder.NormalizeFilePath(x)).ToList();
                var excludePatterns = pathSettings.Excludes.Select(x => UrlBuilder.NormalizeFilePath(x)).ToList();


                // The path we assume default to the chuzpah.json directory if the Path property is not set
                var testPath = string.IsNullOrEmpty(pathSettings.Path) ? pathSettings.SettingsFileDirectory : pathSettings.Path;
                testPath = UrlBuilder.NormalizeFilePath(testPath);
                testPath = testPath != null ? Path.Combine(pathSettings.SettingsFileDirectory, testPath) : null;

                // If a file path is given just return that file
                var filePath = FindFilePath(testPath);
                if (filePath != null)
                {
                    ChutzpahTracer.TraceInformation("Found file  {0} from chutzpah.json", filePath);
                    yield return GetPathInfo(filePath);
                }

                // If a folder path is given enumerate that folder (recursively) with the optional include/exclude paths
                var folderPath = UrlBuilder.NormalizeFilePath(FindFolderPath(testPath));
                if (folderPath != null)
                {

                    var childFiles = fileSystem.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
                    var validFiles = from file in childFiles
                                     let normalizedFile = UrlBuilder.NormalizeFilePath(file)
                                     where !IsTemporaryChutzpahFile(normalizedFile)
                                             && (!includePatterns.Any() || includePatterns.Any(pat => NativeImports.PathMatchSpec(normalizedFile, pat)))
                                             && (!excludePatterns.Any() || !excludePatterns.Any(pat => NativeImports.PathMatchSpec(normalizedFile, pat)))
                                     select file;


                    foreach (var item in validFiles)
                    {
                        yield return GetPathInfo(item);
                    }
                }
            }
        }

        public static PathType GetFilePathType(string fileName)
        {
            // Detect web urls
            if (RegexPatterns.SchemePrefixRegex.IsMatch(fileName))
            {
                return PathType.Url;
            }

            string ext = Path.GetExtension(fileName);
            PathType pathType;
            if (ext != null && ExtensionToPathTypeMap.TryGetValue(ext, out pathType))
            {
                return pathType;
            }

            return PathType.Other;
        }

        public string BuiltInDependencyDirectory
        {
            get
            {
                if(builtInDependencyDirectory == null)
                {
                    builtInDependencyDirectory = FindFolderPath(Constants.TestFileFolder);
                }

                return builtInDependencyDirectory;
            }
        }

        private string FindPath(string path, Predicate<string> pathExists)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (RegexPatterns.SchemePrefixRegex.IsMatch(path))
            {
                // Web url can't be a folder
                return null;
            }

            var currentDirFilePath = fileSystem.GetFullPath(path);
            if (pathExists(currentDirFilePath))
                return currentDirFilePath;

            var executingPath = environment.GetExeuctingAssemblyPath();
            var executingDir = fileSystem.GetDirectoryName(executingPath);
            var filePath = fileSystem.GetFullPath(Path.Combine(executingDir, path));
            if (pathExists(filePath))
                return filePath;

            return null;
        }

        public string GetReferencedFileContent(ReferencedFile file, ChutzpahTestSettingsFile settings)
        {
            return GetReferenceFileContentAndSetHash(file, settings);
        }

        public void SetReferencedFileHash(ReferencedFile file, ChutzpahTestSettingsFile settings)
        {
            GetReferenceFileContentAndSetHash(file, settings);
        }

        private string GetReferenceFileContentAndSetHash(ReferencedFile file, ChutzpahTestSettingsFile settings)
        {
            if(!file.IsLocal)
            {
                return null;
            }

            var text = fileSystem.GetText(file.Path);

            if (string.IsNullOrEmpty(file.Hash)
                && settings.Server != null
                && settings.Server.Enabled.GetValueOrDefault()
                && settings.Server.FileCachingEnabled.GetValueOrDefault())
            {
                file.Hash = hasher.Hash(text);
            }

            return text;
        }
    }
}