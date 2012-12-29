using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Chutzpah.Coverage;
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
        private readonly ICoverageEngine mainCoverageEngine;

        public TestContextBuilder(IFileSystemWrapper fileSystem,
                                  IFileProbe fileProbe,
                                  IHasher hasher,
                                  ICoverageEngine coverageEngine,
                                  IEnumerable<IFrameworkDefinition> frameworkDefinitions,
                                  IEnumerable<IFileGenerator> fileGenerators)
        {
            this.fileSystem = fileSystem;
            this.fileProbe = fileProbe;
            this.hasher = hasher;
            this.frameworkDefinitions = frameworkDefinitions;
            this.fileGenerators = fileGenerators;
            mainCoverageEngine = coverageEngine;
        }

        public TestContext BuildContext(string file, TestOptions options)
        {
            if (String.IsNullOrWhiteSpace(file))
            {
                throw new ArgumentNullException("file");
            }

            PathInfo pathInfo = fileProbe.GetPathInfo(file);

            return BuildContext(pathInfo, options);
        }

        public TestContext BuildContext(PathInfo file, TestOptions options)
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

                var referencedFiles = new List<ReferencedFile>();
                var temporaryFiles = new List<string>();

                var fileUnderTest = GetFileUnderTest(testFilePath);
                referencedFiles.Add(fileUnderTest);
                definition.Process(fileUnderTest);

                GetReferencedFiles(referencedFiles, definition, testFileText, testFilePath);
                ProcessForFilesGeneration(referencedFiles, temporaryFiles);

                ICoverageEngine coverageEngine = GetConfiguredCoverageEngine(options);
                IEnumerable<string> deps = definition.FileDependencies;
                if (coverageEngine != null)
                {
                    deps = deps.Concat(coverageEngine.GetFileDependencies(definition));
                }

                foreach (string item in deps)
                {
                    string sourcePath = fileProbe.GetPathInfo(Path.Combine(TestFileFolder, item)).FullPath;
                    referencedFiles.Add(new ReferencedFile { IsLocal = true, IsTestFrameworkDependency = true, Path = sourcePath });
                }

                string testHtmlFilePath = CreateTestHarness(definition,
                                                            testFilePath,
                                                            referencedFiles,
                                                            coverageEngine,
                                                            temporaryFiles);

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

        public bool TryBuildContext(string file, TestOptions options, out TestContext context)
        {
            context = BuildContext(file, options);
            return context != null;
        }

        public bool TryBuildContext(PathInfo file, TestOptions options, out TestContext context)
        {
            context = BuildContext(file, options);
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
        /// Iterates over filegenerators letting the generators decide if they handle any files
        /// </summary>
        private void ProcessForFilesGeneration(List<ReferencedFile> referencedFiles, List<string> temporaryFiles)
        {
            foreach (var fileGenerator in fileGenerators)
            {
                fileGenerator.Generate(referencedFiles, temporaryFiles);
            }
        }

        private ICoverageEngine GetConfiguredCoverageEngine(TestOptions options)
        {
            if (options == null || !options.CoverageOptions.Enabled) return null;
            mainCoverageEngine.IncludePattern = options.CoverageOptions.IncludePattern;
            mainCoverageEngine.ExcludePattern = options.CoverageOptions.ExcludePattern;
            return mainCoverageEngine;
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
                                         string inputTestFilePath,
                                         IEnumerable<ReferencedFile> referencedFiles,
                                         ICoverageEngine coverageEngine,
                                         List<string> temporaryFiles)
        {
            // Use the directory of the test file to create the temporary html file
            string inputTestFileDir = Path.GetDirectoryName(inputTestFilePath);
            string testFilePathHash = hasher.Hash(inputTestFilePath);
            string testHtmlFilePath = Path.Combine(inputTestFileDir, string.Format(Constants.ChutzpahTemporaryFileFormat, testFilePathHash, "test.html"));
            temporaryFiles.Add(testHtmlFilePath);

            string templatePath = fileProbe.GetPathInfo(Path.Combine(TestFileFolder, definition.TestHarness)).FullPath;
            string testHtmlTemplate = fileSystem.GetText(templatePath);

            TestHarness harness = new TestHarness(inputTestFilePath, referencedFiles);
            CleanupTestHarness(harness);
            if (coverageEngine != null)
            {
                coverageEngine.PrepareTestHarnessForCoverage(harness, definition);
            }

            string testHtmlText = harness.CreateHtmlText(testHtmlTemplate);
            fileSystem.Save(testHtmlFilePath, testHtmlText);
            return testHtmlFilePath;
        }

        private void CleanupTestHarness(TestHarness harness)
        {
            // Remove additional references to QUnit.
            // (Iterate over a copy to avoid concurrent modification of the list!)
            foreach (HtmlTag reference in harness.ReferencedScripts.Where(r => r.ReferencedFile != null).ToList())
            {
                if (reference.ReferencedFile.IsFileUnderTest) continue;

                var lastSlash = reference.ReferencedFile.Path.LastIndexOfAny(new[] {'/', '\\'});
                string fileName = reference.ReferencedFile.Path.Substring(lastSlash + 1);
                if (Regex.IsMatch(fileName, "^qunit(-[0-9]+\\.[0-9]+\\.[0-9]+)?\\.js$", RegexOptions.IgnoreCase))
                {
                    harness.ReferencedScripts.Remove(reference);
                }
            }
        }

        /// <summary>
        /// Scans the test file extracting all referenced files from it.
        /// </summary>
        /// <param name="referencedFiles">The list of referenced files</param>
        /// <param name="definition">Test framework defintition</param>
        /// <param name="textToParse">The content of the file to parse and extract from</param>
        /// <param name="currentFilePath">Path to the file under test</param>
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

        private void VisitReferencedFile(string absoluteFilePath,
                                         IFrameworkDefinition definition,
                                         HashSet<string> discoveredPaths,
                                         ICollection<ReferencedFile> referencedFiles)
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
    }
}