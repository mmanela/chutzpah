using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Chutzpah.Extensions;
using Chutzpah.FileGenerator;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;

namespace Chutzpah
{
    public class TestContextBuilder : ITestContextBuilder
    {
        private const string TestFileFolder = "TestFiles";

        private readonly Regex JsReferencePathRegex =
            new Regex(@"^\s*(///|##)\s*<\s*reference\s+path\s*=\s*[""""'](?<Path>[^""""<>|]+)[""""']\s*/>",
                      RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IFileProbe fileProbe;
        private readonly IFileSystemWrapper fileSystem;
        private readonly IEnumerable<IFrameworkDefinition> frameworkDefinitions;
        private readonly IEnumerable<IFileGenerator> fileGenerators;
        private readonly IHasher hasher;

        public TestContextBuilder(IFileSystemWrapper fileSystem,
                                  IFileProbe fileProbe,
                                  IHasher hasher,
                                  IEnumerable<IFrameworkDefinition> frameworkDefinitions,
                                  IEnumerable<IFileGenerator> fileGenerators)
        {
            this.fileSystem = fileSystem;
            this.fileProbe = fileProbe;
            this.hasher = hasher;
            this.frameworkDefinitions = frameworkDefinitions;
            this.fileGenerators = fileGenerators;
        }

        public TestContext BuildContext(string file)
        {
            if (String.IsNullOrWhiteSpace(file))
            {
                throw new ArgumentNullException("file");
            }

            PathInfo pathInfo = fileProbe.GetPathInfo(file);

            return BuildContext(pathInfo);
        }

        public TestContext BuildContext(PathInfo file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("testFilePathInfo");
            }

            PathType testFileKind = file.Type;
            string testFilePath = file.FullPath;

            if (IsValidTestPathType(testFileKind))
            {
                throw new ArgumentException("Expecting a .js, .ts, .coffee or .html file");
            }

            if (testFilePath == null)
            {
                throw new FileNotFoundException("Unable to find file: " + file.Path);
            }

            string testFileText = fileSystem.GetText(testFilePath);

            IFrameworkDefinition definition;

            if (TryDetectFramework(testFileText, testFileKind, out definition))
            {
                // For HTML test files we don't need to create a test harness to just return this file
                if (testFileKind == PathType.Html)
                {
                    return new TestContext
                        {
                            InputTestFile = testFilePath,
                            TestHarnessPath = testFilePath,
                            TestRunner = definition.TestRunner
                        };
                }

                string stagingFolder = fileSystem.GetTemporaryFolder(hasher.Hash(testFilePath));
                if (!fileSystem.FolderExists(stagingFolder))
                {
                    fileSystem.CreateDirectory(stagingFolder);
                }

                var referencedFiles = new List<ReferencedFile>();
                var temporaryFiles = new List<string>();

                var fileUnderTest = GetFileUnderTest(testFilePath);
                referencedFiles.Add(fileUnderTest);
                definition.Process(fileUnderTest);

                GetReferencedFiles(referencedFiles, definition, testFileText, testFilePath);
                ProcessForFilesGeneration(referencedFiles, temporaryFiles);

                foreach (string item in definition.FileDependencies)
                {
                    string sourcePath = fileProbe.GetPathInfo(Path.Combine(TestFileFolder, item)).FullPath;
                    string destinationPath = Path.Combine(stagingFolder, Path.GetFileName(item));
                    CreateIfDoesNotExist(sourcePath, destinationPath);
                }

                string testHtmlFilePath = CreateTestHarness(definition,
                                                            stagingFolder,
                                                            testFilePath,
                                                            testFileKind,
                                                            referencedFiles);

                return new TestContext
                    {
                        InputTestFile = testFilePath,
                        TestHarnessPath = testHtmlFilePath,
                        ReferencedJavaScriptFiles = referencedFiles,
                        TestRunner = definition.TestRunner,
                        TemporaryFiles = temporaryFiles
                    };
            }

            return null;
        }

        public bool TryBuildContext(string file, out TestContext context)
        {
            context = BuildContext(file);
            return context != null;
        }

        public bool TryBuildContext(PathInfo file, out TestContext context)
        {
            context = BuildContext(file);
            return context != null;
        }

        public bool IsTestFile(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                return false;
            }

            PathInfo pathInfo = fileProbe.GetPathInfo(file);
            PathType testFileKind = pathInfo.Type;
            string testFilePath = pathInfo.FullPath;

            if (testFilePath == null || (IsValidTestPathType(testFileKind)))
            {
                return false;
            }

            string testFileText = fileSystem.GetText(testFilePath);

            IFrameworkDefinition definition;
            return TryDetectFramework(testFileText, testFileKind, out definition);
        }

        public void CleanupContext(TestContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            foreach (var file in context.TemporaryFiles)
            {
                try
                {
                    fileSystem.DeleteFile(file);
                }
                catch (IOException)
                {
                    // Supress exception
                }
            }
        }

        private static bool IsValidTestPathType(PathType testFileKind)
        {
            return testFileKind != PathType.JavaScript
                   && testFileKind != PathType.TypeScript
                   && testFileKind != PathType.CoffeeScript
                   && testFileKind != PathType.Html;
        }

        /// <summary>
        /// Iterates over referenced files and over filegenerators letting the generators decide if they handle any files
        /// </summary>
        private void ProcessForFilesGeneration(List<ReferencedFile> referencedFiles, List<string> temporaryFiles)
        {
            foreach (var fileGenerator in fileGenerators)
            {
                foreach (var referencedFile in referencedFiles)
                {
                    fileGenerator.Generate(referencedFile, temporaryFiles);
                }
            }
        }

        private ReferencedFile GetFileUnderTest(string testFilePath)
        {
            return new ReferencedFile { Path = testFilePath, IsLocal = true, IsFileUnderTest = true };
        }

        private bool TryDetectFramework(string content, PathType pathType, out IFrameworkDefinition definition)
        {
            definition = frameworkDefinitions.FirstOrDefault(x => x.FileUsesFramework(content, false, pathType));

            if (definition == null)
            {
                definition = frameworkDefinitions.FirstOrDefault(x => x.FileUsesFramework(content, true, pathType));
            }

            return definition != null;
        }

        private string CreateTestHarness(IFrameworkDefinition definition,
                                         string stagingFolder,
                                         string inputTestFilePath,
                                         PathType testFileKind,
                                         IEnumerable<ReferencedFile> referencedFiles)
        {
            string testHtmlFilePath = Path.Combine(stagingFolder, "test.html");
            string templatePath = fileProbe.GetPathInfo(Path.Combine(TestFileFolder, definition.TestHarness)).FullPath;
            string testHtmlTemplate = fileSystem.GetText(templatePath);
            string inputTestFileDir = Path.GetDirectoryName(inputTestFilePath).Replace("\\", "/");
            string testHtmlText = FillTestHtmlTemplate(testHtmlTemplate, inputTestFileDir, testFileKind, referencedFiles);
            fileSystem.Save(testHtmlFilePath, testHtmlText);
            return testHtmlFilePath;
        }

        private void CreateIfDoesNotExist(string sourcePath, string destinationPath)
        {
            if (!fileSystem.FileExists(destinationPath)
                || fileSystem.GetLastWriteTime(sourcePath) > fileSystem.GetLastWriteTime(destinationPath))
            {
                fileSystem.CopyFile(sourcePath, destinationPath);
            }
        }

        /// <summary>
        /// Scans the test file extracting all referenced files from it. These will later be copied to the staging directory
        /// </summary>
        /// <param name="referencedFiles">The list of referenced files</param>
        /// <param name="definition">Test framework defintition</param>
        /// <param name="textToParse">The content of the file to parse and extract from</param>
        /// <param name="currentFilePath">Path to the file under test</param>
        /// <param name="stagingFolder">Folder where files are staged for testing</param>
        /// <returns></returns>
        private void GetReferencedFiles(List<ReferencedFile> referencedFiles,
                                        IFrameworkDefinition definition,
                                        string textToParse,
                                        string currentFilePath)
        {
            IList<ReferencedFile> result = GetReferencedFiles(new HashSet<string>(referencedFiles.Select(x => x.Path)),
                                                              definition,
                                                              textToParse,
                                                              currentFilePath);
            IEnumerable<ReferencedFile> flattenedReferenceTree = from root in result
                                                                 from flattened in FlattenReferenceGraph(root)
                                                                 select flattened;
            referencedFiles.AddRange(flattenedReferenceTree);
        }

        private IList<ReferencedFile> GetReferencedFiles(HashSet<string> discoveredPaths,
                                                         IFrameworkDefinition definition,
                                                         string textToParse,
                                                         string currentFilePath)
        {
            var referencedFiles = new List<ReferencedFile>();
            Regex regex = JsReferencePathRegex;
            foreach (Match match in regex.Matches(textToParse))
            {
                if (match.Success)
                {
                    string referencePath = match.Groups["Path"].Value;
                    var referenceUri = new Uri(referencePath, UriKind.RelativeOrAbsolute);
                    string referenceFileName = Path.GetFileName(referencePath);

                    // Don't copy over test runner, since we use our own.
                    if (definition.ReferenceIsDependency(referenceFileName))
                    {
                        continue;
                    }

                    if (!referenceUri.IsAbsoluteUri || referenceUri.IsFile)
                    {
                        string relativeReferencePath = Path.Combine(Path.GetDirectoryName(currentFilePath),
                                                                    referencePath);

                        // Find the full file path
                        string absoluteFilePath = fileProbe.FindFilePath(relativeReferencePath);

                        if (absoluteFilePath != null)
                        {
                            VisitReferencedFile(absoluteFilePath, definition, discoveredPaths, referencedFiles);
                        }
                        else // If path is not a file then check if it is a folder
                        {
                            string absoluteFolderPath = fileProbe.FindFolderPath(relativeReferencePath);
                            if (absoluteFolderPath != null)
                            {
                                // Find all files in this folder including sub-folders. This can be ALOT of files.
                                // Only a subset of these files Chutzpah might understand so many of these will be ignored.
                                var childFiles = fileSystem.GetFiles(absoluteFolderPath, "*.*", SearchOption.AllDirectories);
                                var validFiles = from file in childFiles
                                                 where !fileProbe.IsTemporaryChutzpahFile(file)
                                                 select file;
                                validFiles.ForEach(file => VisitReferencedFile(file, definition, discoveredPaths, referencedFiles));

                            }
                        }

                    }
                    else if (referenceUri.IsAbsoluteUri)
                    {
                        referencedFiles.Add(new ReferencedFile { Path = referencePath, IsLocal = false });
                    }
                }
            }

            return referencedFiles;
        }

        private void VisitReferencedFile(string absoluteFilePath, IFrameworkDefinition definition, HashSet<string> discoveredPaths, ICollection<ReferencedFile> referencedFiles)
        {
            // If the file doesn't exit exist or we have seen it already then return
            if (discoveredPaths.Any(x => x.Equals(absoluteFilePath, StringComparison.OrdinalIgnoreCase))) return;

            var referencedFile = new ReferencedFile { Path = absoluteFilePath, IsLocal = true };
            referencedFiles.Add(referencedFile);
            discoveredPaths.Add(referencedFile.Path); // Remmember this path to detect reference loops
            referencedFile.ReferencedFiles = ExpandNestedReferences(discoveredPaths, definition, absoluteFilePath);
        }

        private IList<ReferencedFile> ExpandNestedReferences(HashSet<string> discoveredPaths,
                                                             IFrameworkDefinition definition,
                                                             string currentFilePath)
        {
            try
            {
                string textToParse = fileSystem.GetText(currentFilePath);
                return GetReferencedFiles(discoveredPaths, definition, textToParse, currentFilePath);
            }
            catch (IOException)
            {
                // Unable to get file text
            }

            return new List<ReferencedFile>();
        }

        private static IEnumerable<ReferencedFile> FlattenReferenceGraph(ReferencedFile rootFile)
        {
            var flattenedFileList = new List<ReferencedFile>();
            foreach (ReferencedFile childFile in rootFile.ReferencedFiles)
            {
                flattenedFileList.AddRange(FlattenReferenceGraph(childFile));
            }
            flattenedFileList.Add(rootFile);

            return flattenedFileList;
        }

        private static string FillTestHtmlTemplate(string testHtmlTemplate,
                                                   string inputTestFileDir,
                                                   PathType testFileKind,
                                                   IEnumerable<ReferencedFile> referencedFiles)
        {
            var testJsReplacement = new StringBuilder();
            var referenceJsReplacement = new StringBuilder();
            var referenceCssReplacement = new StringBuilder();
            IEnumerable<ReferencedFile> referencedFilePaths =
                referencedFiles.OrderBy(x => x.IsFileUnderTest).Select(x => x);
            BuildReferenceHtml(referencedFilePaths, referenceCssReplacement, testJsReplacement, referenceJsReplacement);

            testHtmlTemplate = testHtmlTemplate.Replace("@@TestJSFile@@", testJsReplacement.ToString());
            testHtmlTemplate = testHtmlTemplate.Replace("@@TestJSFileDir@@", inputTestFileDir);
            testHtmlTemplate = testHtmlTemplate.Replace("@@ReferencedJSFiles@@", referenceJsReplacement.ToString());
            testHtmlTemplate = testHtmlTemplate.Replace("@@ReferencedCSSFiles@@", referenceCssReplacement.ToString());

            return testHtmlTemplate;
        }

        private static void BuildReferenceHtml(IEnumerable<ReferencedFile> referencedFilePaths,
                                               StringBuilder referenceCssReplacement,
                                               StringBuilder testJsReplacement,
                                               StringBuilder referenceJsReplacement,
                                               StringBuilder referenceIconReplacement = null)
        {
            foreach (ReferencedFile referencedFile in referencedFilePaths)
            {
                string referencePath = string.IsNullOrEmpty(referencedFile.GeneratedFilePath)
                                        ? referencedFile.Path
                                        : referencedFile.GeneratedFilePath;

                if (referencePath.EndsWith(Constants.CssExtension, StringComparison.OrdinalIgnoreCase) &&
                    referenceCssReplacement != null)
                {
                    referenceCssReplacement.AppendLine(GetStyleStatement(referencePath));
                }
                else if (referencedFile.IsFileUnderTest &&
                         referencePath.EndsWith(Constants.JavaScriptExtension, StringComparison.OrdinalIgnoreCase) && testJsReplacement != null)
                {
                    testJsReplacement.AppendLine(GetScriptStatement(referencePath));
                }
                else if (referencePath.EndsWith(Constants.JavaScriptExtension, StringComparison.OrdinalIgnoreCase) &&
                         referenceJsReplacement != null)
                {
                    referenceJsReplacement.AppendLine(GetScriptStatement(referencePath));
                }
                else if (referencePath.EndsWith(Constants.PngExtension, StringComparison.OrdinalIgnoreCase) &&
                         referenceIconReplacement != null)
                {
                    referenceIconReplacement.AppendLine(GetIconStatement(referencePath));
                }
            }
        }

        public static string GetScriptStatement(string path, bool absolute = true)
        {
            const string format = @"<script type=""text/javascript"" src=""{0}""></script>";
            return string.Format(format, absolute ? GetAbsoluteFileUrl(path) : path);
        }

        public static string GetCoffeeScriptStatement(string path)
        {
            const string format = @"<script type=""text/coffeescript"" src=""{0}""></script>";
            return string.Format(format, GetAbsoluteFileUrl(path));
        }

        public static string GetStyleStatement(string path)
        {
            const string format = @"<link rel=""stylesheet"" href=""{0}"" type=""text/css""/>";
            return string.Format(format, GetAbsoluteFileUrl(path));
        }

        public static string GetIconStatement(string path)
        {
            const string format = @"<link rel=""shortcut icon"" type=""image/png"" href=""{0}"">";
            return string.Format(format, GetAbsoluteFileUrl(path));
        }

        public static string GetAbsoluteFileUrl(string path)
        {
            if (!RegexPatterns.SchemePrefixRegex.IsMatch(path))
            {
                return "file:///" + path.Replace('\\', '/');
            }

            return path;
        }
    }
}