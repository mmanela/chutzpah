using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Chutzpah.FileConverter;
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
        private readonly ICoffeeScriptFileConverter coffeeScriptFileConverter;
        private readonly IHasher hasher;

        public TestContextBuilder(IFileSystemWrapper fileSystem,
                                  IFileProbe fileProbe,
                                  IHasher hasher,
                                  IEnumerable<IFrameworkDefinition> frameworkDefinitions,
                                  ICoffeeScriptFileConverter coffeeScriptFileConverter)
        {
            this.fileSystem = fileSystem;
            this.fileProbe = fileProbe;
            this.hasher = hasher;
            this.frameworkDefinitions = frameworkDefinitions;
            this.coffeeScriptFileConverter = coffeeScriptFileConverter;
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

            if (testFileKind != PathType.JavaScript && testFileKind != PathType.CoffeeScript &&
                testFileKind != PathType.Html)
            {
                throw new ArgumentException("Expecting a .js, .coffee or .html file");
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
                ProcessCoffeeScriptFiles(referencedFiles, temporaryFiles);

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

            if (testFilePath == null || (testFileKind != PathType.JavaScript && testFileKind != PathType.CoffeeScript && testFileKind != PathType.Html))
            {
                return false;
            }

            string testFileText = fileSystem.GetText(testFilePath);

            IFrameworkDefinition definition;
            return TryDetectFramework(testFileText, testFileKind, out definition);
        }

        public void CleanupContext(TestContext context)
        {
            if(context == null) throw new ArgumentNullException("context");
            foreach(var file in context.TemporaryFiles)
            {
                try
                {
                    fileSystem.DeleteFile(file);
                }
                catch (IOException)
                {
                    // Supress exception
                    // TODO: Log this
                }
            }
        }

        /// <summary>
        /// Iterates over referenced files and process any which are coffeescript files
        /// </summary>
        /// <param name="referencedFiles"></param>
        /// <param name="temporaryFiles"> </param>
        private void ProcessCoffeeScriptFiles(List<ReferencedFile> referencedFiles, List<string> temporaryFiles)
        {
            referencedFiles.ForEach(referencedFile => coffeeScriptFileConverter.Convert(referencedFile, temporaryFiles));
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
                        string absolutePath = fileProbe.FindFilePath(relativeReferencePath);
                        if (absolutePath != null &&
                            !discoveredPaths.Any(x => x.Equals(absolutePath, StringComparison.OrdinalIgnoreCase)))
                        {
                            var referencedFile = new ReferencedFile {Path = absolutePath, IsLocal = true};
                            referencedFiles.Add(referencedFile);
                            discoveredPaths.Add(referencedFile.Path); // Remmember this path to detect reference loops
                            referencedFile.ReferencedFiles = ExpandNestedReferences(discoveredPaths,
                                                                                    definition,
                                                                                    absolutePath);
                        }
                    }
                    else if (referenceUri.IsAbsoluteUri)
                    {
                        referencedFiles.Add(new ReferencedFile {Path = referencePath, IsLocal = false});
                    }
                }
            }

            return referencedFiles;
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
                string referencePath = referencedFile.Path;

                if (referencePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase) &&
                    referenceCssReplacement != null)
                {
                    referenceCssReplacement.AppendLine(GetStyleStatement(referencePath));
                }
                else if (referencedFile.IsFileUnderTest &&
                         referencePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase) && testJsReplacement != null)
                {
                    testJsReplacement.AppendLine(GetScriptStatement(referencePath));
                }
                else if (referencePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase) &&
                         referenceJsReplacement != null)
                {
                    referenceJsReplacement.AppendLine(GetScriptStatement(referencePath));
                }
                else if (referencePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
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