using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            new Regex(@"^\s*(///|##)\s*<\s*(?:chutzpah_)?reference\s+path\s*=\s*[""'](~?)(?<Path>[^""<>|]+)[""'](\s+chutzpah-exclude\s*=\s*[""'](?<Exclude>[^""<>|]+)[""'])?\s*/>",
                      RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex JsTemplatePathRegex =
            new Regex(@"^\s*(///|##)\s*<\s*template\s+path\s*=\s*[""'](~?)(?<Path>[^""<>|]+)[""']\s*/>",
                      RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IFileProbe fileProbe;
        private readonly IHttpWrapper httpClient;
        private readonly IFileSystemWrapper fileSystem;
        private readonly IEnumerable<IFrameworkDefinition> frameworkDefinitions;
        private readonly IEnumerable<IFileGenerator> fileGenerators;
        private readonly IHasher hasher;
        private readonly ICoverageEngine mainCoverageEngine;
        private readonly IJsonSerializer serializer;
        private readonly Dictionary<string, string> includedLibraries; 

        public TestContextBuilder(IFileSystemWrapper fileSystem,
                                  IHttpWrapper httpWrapper,
                                  IFileProbe fileProbe,
                                  IHasher hasher,
                                  ICoverageEngine coverageEngine,
                                  IJsonSerializer serializer,
                                  IEnumerable<IFrameworkDefinition> frameworkDefinitions,
                                  IEnumerable<IFileGenerator> fileGenerators)
        {
            this.httpClient = httpWrapper;
            this.fileSystem = fileSystem;
            this.fileProbe = fileProbe;
            this.hasher = hasher;
            this.serializer = serializer;
            this.frameworkDefinitions = frameworkDefinitions;
            this.fileGenerators = fileGenerators;
            mainCoverageEngine = coverageEngine;

            includedLibraries = new Dictionary<string, string>
            {
                {"chai", @"IncludedLibraries\chai.js"},
                {"expect", @"IncludedLibraries\expect.js"}
            };
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
                throw new ArgumentException("Expecting a .js, .ts, .coffee or .html file or a url");
            }

            if (testFilePath == null)
            {
                throw new FileNotFoundException("Unable to find file: " + file.Path);
            }

            var testFileDirectory = Path.GetDirectoryName(testFilePath);
            var chutzpahTestSettings = ChutzpahTestSettingsFile.Read(testFileDirectory, fileProbe, serializer);

            string testFileText;
            if (testFileKind == PathType.Url)
            {
                testFileText = httpClient.GetContent(testFilePath);
            }
            else
            {
                testFileText = fileSystem.GetText(testFilePath);
            }

            IFrameworkDefinition definition;

            if (TryDetectFramework(testFileText, testFileKind, chutzpahTestSettings, out definition))
            {
                // For HTML test files we don't need to create a test harness to just return this file
                if (testFileKind == PathType.Html || testFileKind == PathType.Url)
                {
                    ChutzpahTracer.TraceInformation("Test kind is {0} so we are trusting the supplied test harness and not building our own", testFileKind);
                    
                    return new TestContext
                        {
                            InputTestFile = testFilePath,
                            TestHarnessPath = testFilePath,
                            IsRemoteHarness = testFileKind == PathType.Url,
                            TestRunner = definition.TestRunner,
                        };
                }

                var referencedFiles = new List<ReferencedFile>();
                var temporaryFiles = new List<string>();

                var fileUnderTest = GetFileUnderTest(testFilePath);
                referencedFiles.Add(fileUnderTest);
                definition.Process(fileUnderTest);

                GetReferencedFiles(referencedFiles, definition, testFileText, testFilePath, chutzpahTestSettings);
                ProcessForFilesGeneration(referencedFiles, temporaryFiles, chutzpahTestSettings);

                IEnumerable<string> deps = definition.FileDependencies;

                var coverageEngine = SetupCodeCoverageEngine(options, chutzpahTestSettings, definition, ref deps);

                AddTestFrameworkDependencies(deps, referencedFiles);

                AddConditionalReferences(testFilePath, testFileText, chutzpahTestSettings, referencedFiles);

                string testHtmlFilePath = CreateTestHarness(definition,
                                                            chutzpahTestSettings,
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
                        TemporaryFiles = temporaryFiles,
                        TestFileSettings = chutzpahTestSettings
                    };
            }
            else
            {
                ChutzpahTracer.TraceWarning("Failed to detect test framework for '{0}'", testFilePath);
            }

            return null;
        }

        private void AddConditionalReferences(string testFilePath, string testFileText, ChutzpahTestSettingsFile chutzpahTestSettings, List<ReferencedFile> referencedFiles)
        {
            foreach (var settingsReference in chutzpahTestSettings.References)
            {
                var include = settingsReference.Include;

                if (!string.IsNullOrEmpty(settingsReference.PathPattern) && 
                    !Regex.IsMatch(testFilePath, settingsReference.PathPattern))
                {
                    ChutzpahTracer.TraceInformation("Skipping conditional reference '{0}' for path mismatch", include);
                    continue;   
                }

                if (!string.IsNullOrEmpty(settingsReference.ContentPattern) && 
                    !Regex.IsMatch(testFileText, settingsReference.ContentPattern))
                {
                    ChutzpahTracer.TraceInformation("Skipping conditional reference '{0}' for contents mismatch", include);
                    continue;   
                }

                var referencedFile = new ReferencedFile
                {
                    IsTestFrameworkDependency = false
                };

                string internalReference;
                if (includedLibraries.TryGetValue(include.ToLower(), out internalReference))
                {
                    referencedFile.Path = fileProbe.GetPathInfo(Path.Combine(TestFileFolder, internalReference)).FullPath;
                    referencedFile.IsLocal = false;
                }
                else
                {
                    referencedFile.Path = fileProbe.GetPathInfo(Path.Combine(chutzpahTestSettings.SettingsFileDirectory, include)).FullPath;
                    referencedFile.IsLocal = true;
                }

                ChutzpahTracer.TraceInformation("Added conditional reference '{0}' to referenced files", referencedFile.Path);
                referencedFiles.Add(referencedFile);
            }
        }

        private void AddTestFrameworkDependencies(IEnumerable<string> deps, List<ReferencedFile> referencedFiles)
        {
            foreach (string item in deps)
            {
                string sourcePath = fileProbe.GetPathInfo(Path.Combine(TestFileFolder, item)).FullPath;
                ChutzpahTracer.TraceInformation("Added framework dependency '{0}' to referenced files", sourcePath);
                referencedFiles.Add(new ReferencedFile {IsLocal = true, IsTestFrameworkDependency = true, Path = sourcePath});
            }
        }

        private ICoverageEngine SetupCodeCoverageEngine(TestOptions options,
                                                        ChutzpahTestSettingsFile chutzpahTestSettings,
                                                        IFrameworkDefinition definition,
                                                        ref IEnumerable<string> deps)
        {
            ICoverageEngine coverageEngine = GetConfiguredCoverageEngine(options, chutzpahTestSettings);
            if (coverageEngine != null)
            {
                deps = deps.Concat(coverageEngine.GetFileDependencies(definition));
            }
            return coverageEngine;
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

            var testFileDirectory = Path.GetDirectoryName(testFilePath);
            var chutzpahTestSettings = ChutzpahTestSettingsFile.Read(testFileDirectory, fileProbe, serializer);
            string testFileText = fileSystem.GetText(testFilePath);

            IFrameworkDefinition definition;
            return TryDetectFramework(testFileText, testFileKind, chutzpahTestSettings, out definition);
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
                   && testFileKind != PathType.Url
                   && testFileKind != PathType.Html;
        }

        /// <summary>
        /// Iterates over filegenerators letting the generators decide if they handle any files
        /// </summary>
        private void ProcessForFilesGeneration(List<ReferencedFile> referencedFiles, List<string> temporaryFiles, ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            foreach (var fileGenerator in fileGenerators)
            {
                fileGenerator.Generate(referencedFiles, temporaryFiles, chutzpahTestSettings);
            }
        }

        private ICoverageEngine GetConfiguredCoverageEngine(TestOptions options, ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            if (options == null || !options.CoverageOptions.Enabled) return null;
            ChutzpahTracer.TraceInformation("Setting up code coverage in test context");
            mainCoverageEngine.IncludePatterns = chutzpahTestSettings.CodeCoverageIncludes.Concat(options.CoverageOptions.IncludePatterns).ToList();
            mainCoverageEngine.ExcludePatterns = chutzpahTestSettings.CodeCoverageExcludes.Concat(options.CoverageOptions.ExcludePatterns).ToList();
            return mainCoverageEngine;
        }

        private ReferencedFile GetFileUnderTest(string testFilePath)
        {
            return new ReferencedFile { Path = testFilePath, IsLocal = true, IsFileUnderTest = true };
        }

        private bool TryDetectFramework(string content, PathType pathType, ChutzpahTestSettingsFile chutzpahTestSettings, out IFrameworkDefinition definition)
        {

            var strategies = new Func<IFrameworkDefinition>[]
                {
                    // Check chutzpah settings
                    () => frameworkDefinitions.FirstOrDefault(x => x.FrameworkKey.Equals(chutzpahTestSettings.Framework, StringComparison.OrdinalIgnoreCase)),

                    // Check if we see an explicit reference to a framework file (e.g. <reference path="qunit.js" />)
                    () => frameworkDefinitions.FirstOrDefault(x => x.FileUsesFramework(content, false, pathType)),

                    // Check using basic heuristic like looking for test( or module( for QUnit
                    () => frameworkDefinitions.FirstOrDefault(x => x.FileUsesFramework(content, true, pathType))
                };

            definition = strategies.Select(x => x()).FirstOrDefault(x => x != null);
            return definition != null;
        }

        private string CreateTestHarness(IFrameworkDefinition definition,
                                         ChutzpahTestSettingsFile chutzpahTestSettings,
                                         string inputTestFilePath,
                                         IEnumerable<ReferencedFile> referencedFiles,
                                         ICoverageEngine coverageEngine,
                                         IList<string> temporaryFiles)
        {
            string inputTestFileDir = Path.GetDirectoryName(inputTestFilePath);
            string testFilePathHash = hasher.Hash(inputTestFilePath);

            string testHarnessDirectory;
            switch (chutzpahTestSettings.TestHarnessLocationMode)
            {
                case TestHarnessLocationMode.TestFileAdjacent:
                    testHarnessDirectory = inputTestFileDir;
                    break;
                case TestHarnessLocationMode.SettingsFileAdjacent:
                    testHarnessDirectory = chutzpahTestSettings.SettingsFileDirectory;
                    break;
                case TestHarnessLocationMode.Custom:
                    testHarnessDirectory = chutzpahTestSettings.TestHarnessDirectory;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("chutzpahTestSettings");
            }

            string testHtmlFilePath = Path.Combine(testHarnessDirectory, string.Format(Constants.ChutzpahTemporaryFileFormat, testFilePathHash, "test.html"));
            temporaryFiles.Add(testHtmlFilePath);

            string templatePath = fileProbe.GetPathInfo(Path.Combine(TestFileFolder, definition.TestHarness)).FullPath;
            string testHtmlTemplate = fileSystem.GetText(templatePath);
            var harness = new TestHarness(referencedFiles, fileSystem);

            if (coverageEngine != null)
            {
                coverageEngine.PrepareTestHarnessForCoverage(harness, definition);
            }

            string testFileContents = fileSystem.GetText(inputTestFilePath);
            var frameworkReplacements = definition.GetFrameworkReplacements(inputTestFilePath, testFileContents)
                ?? new Dictionary<string, string>();

            string testHtmlText = harness.CreateHtmlText(testHtmlTemplate, frameworkReplacements);
            fileSystem.Save(testHtmlFilePath, testHtmlText);
            return testHtmlFilePath;
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
                                        string currentFilePath,
                                        ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            IList<ReferencedFile> result = GetReferencedFiles(new HashSet<string>(referencedFiles.Select(x => x.Path)),
                                                              definition,
                                                              textToParse,
                                                              currentFilePath,
                                                              chutzpahTestSettings);

            IEnumerable<ReferencedFile> flattenedReferenceTree = from root in result
                                                                 from flattened in FlattenReferenceGraph(root)
                                                                 select flattened;
            referencedFiles.AddRange(flattenedReferenceTree);
        }

        private IList<ReferencedFile> GetReferencedFiles(HashSet<string> discoveredPaths,
                                                         IFrameworkDefinition definition,
                                                         string textToParse,
                                                         string currentFilePath,
                                                         ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            var referencedFiles = new List<ReferencedFile>();

            Regex regex = JsReferencePathRegex;
            foreach (Match match in regex.Matches(textToParse))
            {
                if (ShouldIncludeReference(match))
                {
                    string referencePath = match.Groups["Path"].Value;

                    // Check test settings and adjust the path if it is rooted (e.g. /some/path)
                    referencePath = AdjustPathIfRooted(chutzpahTestSettings, referencePath);

                    var referenceUri = new Uri(referencePath, UriKind.RelativeOrAbsolute);
                    string referenceFileName = Path.GetFileName(referencePath);

                    //  Ignore test runner, since we use our own.
                    if (definition.ReferenceIsDependency(referenceFileName))
                    {
                        ChutzpahTracer.TraceInformation("Ignoring reference file '{0}' as a duplicate reference to {1}", referenceFileName, definition.FrameworkKey);
                        continue;
                    }

                    // If this either a relative uri or a file uri 
                    if (!referenceUri.IsAbsoluteUri || referenceUri.IsFile)
                    {
                        string relativeReferencePath = Path.Combine(Path.GetDirectoryName(currentFilePath), referencePath);

                        // Find the full file path
                        string absoluteFilePath = fileProbe.FindFilePath(relativeReferencePath);

                        if (absoluteFilePath != null)
                        {
                            VisitReferencedFile(absoluteFilePath, definition, discoveredPaths, referencedFiles, chutzpahTestSettings);
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
                                validFiles.ForEach(file => VisitReferencedFile(file, definition, discoveredPaths, referencedFiles, chutzpahTestSettings));
                            }
                        }
                    }
                    else if (referenceUri.IsAbsoluteUri)
                    {
                        ChutzpahTracer.TraceInformation("Added url '{0}' to referenced files", referencePath);
                        referencedFiles.Add(new ReferencedFile { Path = referencePath, IsLocal = false });
                    }
                }
            }

            foreach (Match match in JsTemplatePathRegex.Matches(textToParse))
            {
                string referencePath = match.Groups["Path"].Value;

                referencePath = AdjustPathIfRooted(chutzpahTestSettings, referencePath);
                string relativeReferencePath = Path.Combine(Path.GetDirectoryName(currentFilePath), referencePath);
                string absoluteFilePath = fileProbe.FindFilePath(relativeReferencePath);
                if (referencedFiles.All(r => r.Path != absoluteFilePath))
                {

                    ChutzpahTracer.TraceInformation("Added html template '{0}' to referenced files", absoluteFilePath);
                    referencedFiles.Add(new ReferencedFile { Path = absoluteFilePath, IsLocal = false });
                }
            }

            return referencedFiles;
        }

        /// <summary>
        /// If the reference path is rooted (e.g. /some/path) and the user chose to adjust it then change it
        /// </summary>
        /// <returns></returns>
        private static string AdjustPathIfRooted(ChutzpahTestSettingsFile chutzpahTestSettings, string referencePath)
        {
            if (chutzpahTestSettings.RootReferencePathMode == RootReferencePathMode.SettingsFileDirectory &&
                (referencePath.StartsWith("/") || referencePath.StartsWith("\\")))
            {
                ChutzpahTracer.TraceInformation("Changing reference '{0}' to be rooted from settings directory '{1}'",
                                                referencePath,
                                                chutzpahTestSettings.SettingsFileDirectory);

                referencePath = chutzpahTestSettings.SettingsFileDirectory + referencePath;
            }

            return referencePath;
        }

        private void VisitReferencedFile(string absoluteFilePath,
                                         IFrameworkDefinition definition,
                                         HashSet<string> discoveredPaths,
                                         ICollection<ReferencedFile> referencedFiles,
                                         ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            // If the file doesn't exit exist or we have seen it already then return
            if (discoveredPaths.Any(x => x.Equals(absoluteFilePath, StringComparison.OrdinalIgnoreCase))) return;


            ChutzpahTracer.TraceInformation("Added local file '{0}' to referenced files", absoluteFilePath);
            var referencedFile = new ReferencedFile { Path = absoluteFilePath, IsLocal = true };
            referencedFiles.Add(referencedFile);
            discoveredPaths.Add(referencedFile.Path); // Remmember this path to detect reference loops
            referencedFile.ReferencedFiles = ExpandNestedReferences(discoveredPaths, definition, absoluteFilePath, chutzpahTestSettings);
        }

        private IList<ReferencedFile> ExpandNestedReferences(HashSet<string> discoveredPaths,
                                                             IFrameworkDefinition definition,
                                                             string currentFilePath,
                                                             ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            try
            {
                string textToParse = fileSystem.GetText(currentFilePath);
                return GetReferencedFiles(discoveredPaths, definition, textToParse, currentFilePath, chutzpahTestSettings);
            }
            catch (IOException e)
            {
                // Unable to get file text
                ChutzpahTracer.TraceError(e, "Unable to get file text from test reference with path {0}", currentFilePath);
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

        /// <summary>
        /// Decides whether a reference match should be included.
        /// </summary>
        /// <param name="match">The reference match.</param>
        /// <returns>
        /// <c>true</c> if the reference should be included, otherwise <c>false</c>.
        /// </returns>
        private static bool ShouldIncludeReference(Match match)
        {
            if (match.Success)
            {
                var exclude = match.Groups["Exclude"].Value;

                if (string.IsNullOrWhiteSpace(exclude)
                    || exclude.Equals("false", StringComparison.OrdinalIgnoreCase)
                    || exclude.Equals("no", StringComparison.OrdinalIgnoreCase))
                {
                    // The exclude flag is empty or negative
                    return true;
                }
            }

            ChutzpahTracer.TraceInformation("Excluding reference file because it contains a postitive chutzpah-exclude attribute");

            return false;
        }
    }
}