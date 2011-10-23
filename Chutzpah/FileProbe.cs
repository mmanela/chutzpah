using System;
using System.Collections.Generic;
using System.IO;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah
{
    public class FileProbe : IFileProbe
    {
        private readonly IEnvironmentWrapper environment;
        private readonly IFileSystemWrapper fileSystem;
        //private const int TestableFileSearchLimit = 100;

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

            var executingPath = environment.GetExeuctingAssemblyPath();
            var executingDir = fileSystem.GetDirectoryName(executingPath);
            var filePath = Path.Combine(executingDir, path);
            if (pathExists(filePath))
                return filePath;

            var currentDirFilePath = fileSystem.GetFullPath(path);
            if (pathExists(currentDirFilePath))
                return currentDirFilePath;

            return null;
        }

        public IEnumerable<string> FindScriptFiles(IEnumerable<string> testPaths)
        {
            if (testPaths == null) yield break;

            foreach (var path in testPaths)
            {
                var pathType = GetPathType(path);

                switch (pathType)
                {
                    case PathType.Html:
                    case PathType.JavaScript:
                        yield return path;
                        break;
                    case PathType.Folder:
                        //var testableFiles = from file in fileSystem.GetFiles(path, "*.js", SearchOption.AllDirectories)
                        //                    where testableFileDetector.IsTestableFile(file)
                        //                    select file;
                        //testableFiles = fileSystem
                        //    .GetFiles(path, "*.js", SearchOption.AllDirectories)
                        //    .Where(x => testableFileDetector.IsTestableFile(x));

                        //foreach (var name in fileSystem.GetFiles(path, "*.js", SearchOption.AllDirectories))
                        //{
                        //    var content = fileSystem.GetText(name);
                        //    var framework = Framework.Unknown;

                        //    // Attempt difinitive framework test on file
                        //    foreach (var key in FrameworkManager.Instance.Keys)
                        //    {
                        //        if (FrameworkManager.Instance[key].FileUsesFramework(content, false))
                        //        {
                        //            framework = key;
                        //            break;
                        //        }
                        //    }

                        //    if (framework == Framework.Unknown)
                        //    {
                        //        // Attempt best guess framework test on file
                        //        foreach (var key in FrameworkManager.Instance.Keys)
                        //        {
                        //            if (FrameworkManager.Instance[key].FileUsesFramework(content, true))
                        //            {
                        //                framework = key;
                        //                break;
                        //            }
                        //        }
                        //    }

                        //    if (framework != Framework.Unknown)
                        //    {
                        //        var testFile = new TestFile(name, content, framework);
                        //    }
                        //}

                        //foreach (var file in testableFiles.Take(TestableFileSearchLimit))
                        //{
                        //    yield return file;
                        //}

                        foreach (var item in fileSystem.GetFiles(path, "*.js", SearchOption.AllDirectories))
                        {
                            yield return item;
                        }

                        break;
                    default:
                        break;
                }
            }
        }

        public PathType GetPathType(string path)
        {
            if (IsFolder(path)) return PathType.Folder;
            if (IsHtmlFile(path)) return PathType.Html;
            if (IsJavaScriptFile(path)) return PathType.JavaScript;
            return PathType.Other;
        }

        private bool IsFolder(string path)
        {
            path = FindFolderPath(path);
            return path != null;
        }

        private bool IsHtmlFile(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            return ext != null &&
                   (ext.Equals(".html", StringComparison.OrdinalIgnoreCase) || ext.Equals(".htm", StringComparison.OrdinalIgnoreCase));
        }

        private bool IsJavaScriptFile(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            return ext != null && ext.Equals(".js", StringComparison.OrdinalIgnoreCase);
        }
    }
}