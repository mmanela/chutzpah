using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Chutzpah.Extensions;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah
{
    public class FileProbe : IFileProbe
    {
        private readonly IEnvironmentWrapper environment;
        private readonly IFileSystemWrapper fileSystem;

        private static readonly Dictionary<string, PathType> ExtensionToPathTypeMap =
            new Dictionary<string, PathType>
            {
                {Constants.TypeScriptExtension,PathType.TypeScript},
                {Constants.TypeScriptDefExtension,PathType.TypeScriptDef},
                {Constants.CoffeeScriptExtension,PathType.CoffeeScript},
                {Constants.JavaScriptExtension,PathType.JavaScript},
                {Constants.HtmlScriptExtension,PathType.Html},
                {Constants.HtmScriptExtension,PathType.Html}
            };

        public FileProbe(IEnvironmentWrapper environment, IFileSystemWrapper fileSystem)
        {
            this.environment = environment;
            this.fileSystem = fileSystem;
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

        public IEnumerable<PathInfo> FindScriptFiles(string path, TestingMode testingMode)
        {
            if (string.IsNullOrEmpty(path)) return Enumerable.Empty<PathInfo>();

            return FindScriptFiles(new[] { path }, testingMode);
        }

        public IEnumerable<PathInfo> FindScriptFiles(IEnumerable<string> testPaths, TestingMode testingMode)
        {
            if (testPaths == null) yield break;

            foreach (var path in testPaths)
            {
                var pathInfo = GetPathInfo(path);

                switch (pathInfo.Type)
                {
                    case PathType.Url:
                        if (testingMode == TestingMode.HTML || testingMode == TestingMode.All)
                        {
                            yield return pathInfo;
                        }
                        break;
                    case PathType.Html:
                    case PathType.JavaScript:
                    case PathType.CoffeeScript:
                    case PathType.TypeScript:
                    case PathType.TypeScriptDef:
                        if (!testingMode.FileBelongsToTestingMode(path)) break;
                        yield return pathInfo;
                        break;
                    case PathType.Folder:
                        var query = from file in fileSystem.GetFiles(pathInfo.FullPath, "*.*", SearchOption.AllDirectories)
                                    where !IsTemporaryChutzpahFile(file) && testingMode.FileBelongsToTestingMode(file)
                                    select file;
                        foreach (var item in query)
                        {
                            yield return GetPathInfo(item);
                        }

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

        public static string EncodeFilePath(string path)
        {
            return HttpUtility.UrlEncode(path)
                .Replace("+", "%20")
                .Replace("%3a", ":")
                .Replace("%5c", "/")
                .Replace("%2f", "/");
        }
    }
}