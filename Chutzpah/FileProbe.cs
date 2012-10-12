using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah
{
    public class FileProbe : IFileProbe
    {
        private readonly IEnvironmentWrapper environment;
        private readonly IFileSystemWrapper fileSystem;

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

        public IEnumerable<PathInfo> FindScriptFiles(IEnumerable<string> testPaths, TestingMode testingMode)
        {
            if (testPaths == null) yield break;

            foreach (var path in testPaths)
            {
                var pathInfo = GetPathInfo(path);

                switch (pathInfo.Type)
                {
                    case PathType.Html:
                        if (!testingMode.HasFlag(TestingMode.HTML)) break;
                        yield return pathInfo;
                        break;
                    case PathType.JavaScript:
                    case PathType.CoffeeScript:
                        if (!testingMode.HasFlag(TestingMode.JavaScript)) break;
                        yield return pathInfo;
                        break;
                    case PathType.Folder:

                        var query = fileSystem.GetFiles(pathInfo.FullPath, "*.*", SearchOption.AllDirectories)
                            .Where(file => file.EndsWith(".js", StringComparison.OrdinalIgnoreCase) 
                                        || file.EndsWith(".coffee", StringComparison.OrdinalIgnoreCase));
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
            if (IsHtmlFile(path)) return new PathInfo { Path = path, FullPath = fullPath, Type = PathType.Html };
            if (IsJavaScriptFile(path)) return new PathInfo { Path = path, FullPath = fullPath, Type = PathType.JavaScript };
            if (IsCoffeeScriptFile(path)) return new PathInfo { Path = path, FullPath = fullPath, Type = PathType.CoffeeScript };
            return new PathInfo { Path = path, FullPath = fullPath, Type = PathType.Other };
        }

        private static bool IsHtmlFile(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            return ext != null &&
                   (ext.Equals(".html", StringComparison.OrdinalIgnoreCase) || ext.Equals(".htm", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsJavaScriptFile(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            return ext != null && ext.Equals(".js", StringComparison.OrdinalIgnoreCase);
        }        
        
        private static bool IsCoffeeScriptFile(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            return ext != null && ext.Equals(".coffee", StringComparison.OrdinalIgnoreCase);
        }
    }
}