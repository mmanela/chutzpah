using System;
using System.Collections.Generic;
using System.Globalization;
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


        public bool IsChutzpahSettingsFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            var fileName = Path.GetFileName(path);
            return !string.IsNullOrEmpty(fileName) && fileName.Equals(Constants.SettingsFileName, StringComparison.OrdinalIgnoreCase);
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
                var includePattern = NormalizeFilePath(pathSettings.Include);
                var excludePattern = NormalizeFilePath(pathSettings.Exclude);


                // The path we assume default to the chuzpah.json directory if the Path property is not set
                var testPath = string.IsNullOrEmpty(pathSettings.Path) ? chutzpahTestSettings.SettingsFileDirectory : pathSettings.Path;
                testPath = NormalizeFilePath(testPath);
                testPath = testPath != null ? Path.Combine(chutzpahTestSettings.SettingsFileDirectory, testPath) : null;

                // If a file path is given just return that file
                var filePath = FindFilePath(testPath);
                if (filePath != null)
                {
                    ChutzpahTracer.TraceInformation("Found file  {0} from chutzpah.json", filePath);
                    yield return GetPathInfo(filePath);
                }

                // If a folder path is given enumerate that folder (recursively) with the optional include/exclude paths
                var folderPath = NormalizeFilePath(FindFolderPath(testPath));
                if (folderPath != null)
                {

                    var childFiles = fileSystem.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
                    var validFiles = from file in childFiles
                                     let normlizedFile = NormalizeFilePath(file)
                                     where !IsTemporaryChutzpahFile(normlizedFile)
                                             && (includePattern == null || NativeImports.PathMatchSpec(normlizedFile, includePattern))
                                             && (excludePattern == null || !NativeImports.PathMatchSpec(normlizedFile, excludePattern))
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

        private static string EncodeFilePath(string path)
        {
            return HttpUtility.UrlEncode(path)
                .Replace("+", "%20")
                .Replace("%3a", ":")
                .Replace("%5c", "/")
                .Replace("%2f", "/");
        }

        /// <summary>
        /// This generates a file url based on an absolute file path
        /// </summary>
        public static string GenerateFileUrl(string absolutePath)
        {
            var encodedReferencePath = EncodeFilePath(absolutePath);
            var fileUrlFormat = encodedReferencePath.StartsWith("//") ? "file://{0}" : "file:///{0}";
            return string.Format(fileUrlFormat, encodedReferencePath);
        }

        public static string NormalizeFilePath(string path)
        {
            if (path == null) return null;

            return path.ToLowerInvariant().Replace(@"/", @"\");
        }

        /// <summary>
        /// This get a relative path from one path to another. 
        /// </summary>
        /// <param name="pathToStartFrom"></param>
        /// <param name="pathToGetTo"></param>
        /// <returns></returns>
        public static string GetRelativePath(string pathToStartFrom, string pathToGetTo)
        {
            var pathToGetToUri = new Uri(pathToGetTo);
            
            // Folders must end in a slash
            if (!pathToStartFrom.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)))
            {
                pathToStartFrom += Path.DirectorySeparatorChar;
            }
            
            var pathToStartFromUri = new Uri(pathToStartFrom);
            return Uri.UnescapeDataString(pathToStartFromUri.MakeRelativeUri(pathToGetToUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }
    }
}