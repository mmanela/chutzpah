using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public string FindFilePath(string fileName)
        {
            return FindPath(fileName, fileSystem.FileExists);
        }

        public string FindFolderPath(string fileName)
        {
            return FindPath(fileName, fileSystem.FolderExists);
        }

        private string FindPath(string path, Predicate<string> pathExists)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

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

        public bool IsTemporaryChutzpahFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            var fileName = Path.GetFileName(path);
            return !string.IsNullOrEmpty(fileName) && fileName.StartsWith(Constants.ChutzpahTemporaryFilePrefix);
        }

        public IEnumerable<PathInfo> FindScriptFiles(string path, TestingMode testingMode)
        {
            if (string.IsNullOrEmpty(path)) return Enumerable.Empty<PathInfo>();

            return FindScriptFiles(new[] {path}, testingMode);
        }

        public IEnumerable<PathInfo> FindScriptFiles(IEnumerable<string> testPaths, TestingMode testingMode)
        {
            if (testPaths == null) yield break;

            foreach (var path in testPaths)
            {
                var pathInfo = GetPathInfo(path);

                switch (pathInfo.Type)
                {
                    case PathType.Html:
                    case PathType.JavaScript:
                    case PathType.CoffeeScript:
                    case PathType.TypeScript:
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

        private static PathType GetFilePathType(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            PathType pathType;
            if(ext != null && ExtensionToPathTypeMap.TryGetValue(ext, out pathType))
            {
                return pathType;
            }

            return PathType.Other;
        }
    }
}