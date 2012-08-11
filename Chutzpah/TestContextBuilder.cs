using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;

namespace Chutzpah
{
    public class TestContextBuilder : ITestContextBuilder
    {
        private readonly IFileSystemWrapper fileSystem;
        private readonly IFileProbe fileProbe;
        private readonly IHasher hasher;
        private readonly IEnumerable<IFrameworkDefinition> frameworkDefinitions;

        private const string TestFileFolder = "TestFiles";

        private readonly Regex JsReferencePathRegex = new Regex(@"^\s*///\s*<\s*reference\s+path\s*=\s*[""""'](?<Path>[^""""<>|]+)[""""']\s*/>",
                                                              RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public TestContextBuilder(IFileSystemWrapper fileSystem, IFileProbe fileProbe, IHasher hasher, IEnumerable<IFrameworkDefinition> frameworkDefinitions)
        {
            this.fileSystem = fileSystem;
            this.fileProbe = fileProbe;
            this.hasher = hasher;
            this.frameworkDefinitions = frameworkDefinitions;
        }

        public TestContext BuildContext(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                throw new ArgumentNullException("file");
            }

            var pathInfo = fileProbe.GetPathInfo(file);
            var testFileKind = pathInfo.Type;
            var testFilePath = pathInfo.FullPath;

            if (testFileKind != PathType.JavaScript && testFileKind != PathType.Html)
            {
                throw new ArgumentException("Expecting a .js or .html file");
            }

            if (testFilePath == null)
            {
                throw new FileNotFoundException("Unable to find file: " + file);
            }

            var testFileText = fileSystem.GetText(testFilePath);

            IFrameworkDefinition definition;

            if (TryDetectFramework(testFileText, out definition))
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

                var stagingFolder = fileSystem.GetTemporaryFolder(hasher.Hash(testFilePath));
                if (!fileSystem.FolderExists(stagingFolder))
                {
                    fileSystem.CreateDirectory(stagingFolder);
                }

                var referencedFiles = new List<ReferencedFile>();

                var fileUnderTest = new ReferencedFile { Path = testFilePath, IsLocal = true, IsFileUnderTest = true };
                referencedFiles.Add(fileUnderTest);
                definition.Process(fileUnderTest);

                GetReferencedFiles(referencedFiles, definition, testFileKind, testFileText, testFilePath);

                foreach (var item in definition.FileDependencies)
                {
                    var sourcePath = fileProbe.GetPathInfo(Path.Combine(TestFileFolder, item)).FullPath;
                    var destinationPath = Path.Combine(stagingFolder, Path.GetFileName(item));
                    CreateIfDoesNotExist(sourcePath, destinationPath);
                }

                var testHtmlFilePath = CreateTestHarness(definition, stagingFolder, testFilePath, referencedFiles);

                return new TestContext
                    {
                        InputTestFile = testFilePath,
                        TestHarnessPath = testHtmlFilePath,
                        ReferencedJavaScriptFiles = referencedFiles,
                        TestRunner = definition.TestRunner
                    };
            }

            return null;
        }

        public bool TryBuildContext(string file, out TestContext context)
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

            var pathInfo = fileProbe.GetPathInfo(file);
            var testFileKind = pathInfo.Type;
            var testFilePath = pathInfo.FullPath;

            if (testFilePath == null || testFileKind != PathType.JavaScript && testFileKind != PathType.Html)
            {
                return false;
            }

            var testFileText = fileSystem.GetText(testFilePath);

            IFrameworkDefinition definition;
            return TryDetectFramework(testFileText, out definition);
        }

        private bool TryDetectFramework(string content, out IFrameworkDefinition definition)
        {
            definition = frameworkDefinitions.FirstOrDefault(x => x.FileUsesFramework(content, false));

            if (definition == null)
            {
                definition = frameworkDefinitions.FirstOrDefault(x => x.FileUsesFramework(content, true));
            }

            return definition != null;
        }

        private string CreateTestHarness(IFrameworkDefinition definition, string stagingFolder, string inputTestFilePath, IEnumerable<ReferencedFile> referencedFiles)
        {
            var testHtmlFilePath = Path.Combine(stagingFolder, "test.html");
            var templatePath = fileProbe.GetPathInfo(Path.Combine(TestFileFolder, definition.TestHarness)).FullPath;
            var testHtmlTemplate = fileSystem.GetText(templatePath);
            var inputTestFileDir = Path.GetDirectoryName(inputTestFilePath).Replace("\\", "/");
            string testHtmlText = FillTestHtmlTemplate(testHtmlTemplate, inputTestFileDir, referencedFiles);
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
        /// <param name="testFileType">The type of testing file (JS or HTML)</param>
        /// <param name="textToParse">The content of the file to parse and extract from</param>
        /// <param name="currentFilePath">Path to the file under test</param>
        /// <param name="stagingFolder">Folder where files are staged for testing</param>
        /// <returns></returns>
        private void GetReferencedFiles(List<ReferencedFile> referencedFiles,
                                        IFrameworkDefinition definition,
                                        PathType testFileType,
                                        string textToParse,
                                        string currentFilePath)
        {
            var result = GetReferencedFiles(new HashSet<string>(referencedFiles.Select(x => x.Path)), definition, testFileType, textToParse, currentFilePath);
            var flattenedReferenceTree = from root in result
                                         from flattened in FlattenReferenceGraph(root)
                                         select flattened;
            referencedFiles.AddRange(flattenedReferenceTree);
        }

        private IList<ReferencedFile> GetReferencedFiles(HashSet<string> discoveredPaths,
                                                         IFrameworkDefinition definition,
                                                         PathType testFileType,
                                                         string textToParse,
                                                         string currentFilePath)
        {
            var referencedFiles = new List<ReferencedFile>();
            var regex = JsReferencePathRegex;
            foreach (Match match in regex.Matches(textToParse))
            {
                if (match.Success)
                {
                    string referencePath = match.Groups["Path"].Value;
                    Uri referenceUri = new Uri(referencePath, UriKind.RelativeOrAbsolute);
                    var referenceFileName = Path.GetFileName(referencePath);

                    // Don't copy over test runner, since we use our own.
                    if (definition.ReferenceIsDependency(referenceFileName))
                    {
                        continue;
                    }

                    if (!referenceUri.IsAbsoluteUri || referenceUri.IsFile)
                    {
                        string relativeReferencePath = Path.Combine(Path.GetDirectoryName(currentFilePath), referencePath);
                        var absolutePath = fileProbe.FindFilePath(relativeReferencePath);
                        if (absolutePath != null && !discoveredPaths.Any(x => x.Equals(absolutePath, StringComparison.OrdinalIgnoreCase)))
                        {
                            var referencedFile = new ReferencedFile { Path = absolutePath, IsLocal = true };
                            referencedFiles.Add(referencedFile);
                            discoveredPaths.Add(referencedFile.Path); // Remmember this path to detect reference loops
                            referencedFile.ReferencedFiles = ExpandNestedReferences(discoveredPaths, definition, absolutePath);
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

        private IList<ReferencedFile> ExpandNestedReferences(HashSet<string> discoveredPaths, IFrameworkDefinition definition, string currentFilePath)
        {
            try
            {
                var textToParse = fileSystem.GetText(currentFilePath);
                return GetReferencedFiles(discoveredPaths, definition, PathType.JavaScript, textToParse, currentFilePath);
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
            foreach (var childFile in rootFile.ReferencedFiles)
            {
                flattenedFileList.AddRange(FlattenReferenceGraph(childFile));
            }
            flattenedFileList.Add(rootFile);

            return flattenedFileList;
        }

        private static string FillTestHtmlTemplate(string testHtmlTemplate, string inputTestFileDir, IEnumerable<ReferencedFile> referencedFiles)
        {
            var testJsReplacement = new StringBuilder();
            var referenceJsReplacement = new StringBuilder();
            var referenceCssReplacement = new StringBuilder();
            var referencedFilePaths = referencedFiles.OrderBy(x => x.IsFileUnderTest).Select(x => x);
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
            foreach (var referencedFile in referencedFilePaths)
            {
                var referencePath = referencedFile.Path;

                if (referencePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase) && referenceCssReplacement != null)
                {
                    referenceCssReplacement.AppendLine(GetStyleStatement(referencePath));
                }
                else if (referencedFile.IsFileUnderTest && referencePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase) && testJsReplacement != null)
                {
                    testJsReplacement.AppendLine(GetScriptStatement(referencePath));
                }
                else if (referencePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase) && referenceJsReplacement != null)
                {
                    referenceJsReplacement.AppendLine(GetScriptStatement(referencePath));
                }
                else if (referencePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && referenceIconReplacement != null)
                {
                    referenceIconReplacement.AppendLine(GetIconStatement(referencePath));
                }
            }
        }

        public static string GetScriptStatement(string path)
        {
            const string format = @"<script type=""text/javascript"" src=""{0}""></script>";
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