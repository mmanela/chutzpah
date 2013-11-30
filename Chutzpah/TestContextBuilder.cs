using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chutzpah.Coverage;
using Chutzpah.FileGenerator;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;

namespace Chutzpah
{
    public class TestContextBuilder : ITestContextBuilder
    {
        private readonly IFileProbe fileProbe;
        private readonly IReferenceProcessor referenceProcessor;
        private readonly IHttpWrapper httpClient;
        private readonly IFileSystemWrapper fileSystem;
        private readonly IEnumerable<IFrameworkDefinition> frameworkDefinitions;
        private readonly IEnumerable<IFileGenerator> fileGenerators;
        private readonly IHasher hasher;
        private readonly ICoverageEngine mainCoverageEngine;
        private readonly IJsonSerializer serializer;

        public TestContextBuilder(
            IReferenceProcessor referenceProcessor,
            IFileSystemWrapper fileSystem,
            IHttpWrapper httpWrapper,
            IFileProbe fileProbe,
            IHasher hasher,
            ICoverageEngine coverageEngine,
            IJsonSerializer serializer,
            IEnumerable<IFrameworkDefinition> frameworkDefinitions,
            IEnumerable<IFileGenerator> fileGenerators)
        {
            this.referenceProcessor = referenceProcessor;
            this.httpClient = httpWrapper;
            this.fileSystem = fileSystem;
            this.fileProbe = fileProbe;
            this.hasher = hasher;
            this.serializer = serializer;
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


                string inputTestFileDir = Path.GetDirectoryName(testFilePath);
                var testHarnessDirectory = GetTestHarnessDirectory(chutzpahTestSettings, inputTestFileDir);
                var fileUnderTest = GetFileUnderTest(testFilePath, chutzpahTestSettings);
                referencedFiles.Add(fileUnderTest);
                definition.Process(fileUnderTest);


                referenceProcessor.GetReferencedFiles(referencedFiles, definition, testFileText, testFilePath,chutzpahTestSettings);

                ProcessForFilesGeneration(referencedFiles, temporaryFiles, chutzpahTestSettings);

                SetupAmdPathsIfNeeded(chutzpahTestSettings, referencedFiles, testHarnessDirectory);

                IEnumerable<string> deps = definition.FileDependencies;

                var coverageEngine = SetupCodeCoverageEngine(options, chutzpahTestSettings, definition, referencedFiles);

                AddTestFrameworkDependencies(deps, referencedFiles);

                string testHtmlFilePath = CreateTestHarness(
                    definition,
                    chutzpahTestSettings,
                    options,
                    testFilePath,
                    testHarnessDirectory,
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

        private void SetupAmdPathsIfNeeded(ChutzpahTestSettingsFile chutzpahTestSettings, List<ReferencedFile> referencedFiles, string testHarnessDirectory)
        {
            if (chutzpahTestSettings.TestHarnessReferenceMode == TestHarnessReferenceMode.AMD)
            {
                referenceProcessor.SetupAmdFilePaths(referencedFiles, testHarnessDirectory);
            }
        }

        private void AddTestFrameworkDependencies(IEnumerable<string> deps, List<ReferencedFile> referencedFiles)
        {
            foreach (string item in deps.Reverse())
            {
                string sourcePath = fileProbe.GetPathInfo(Path.Combine(Constants.TestFileFolder, item)).FullPath;
                ChutzpahTracer.TraceInformation("Added framework dependency '{0}' to referenced files", sourcePath);
                referencedFiles.Insert(0, new ReferencedFile {IsLocal = true, IsTestFrameworkDependency = true, Path = sourcePath, IncludeInTestHarness = true});
            }
        }

        private ICoverageEngine SetupCodeCoverageEngine(
            TestOptions options,
            ChutzpahTestSettingsFile chutzpahTestSettings,
            IFrameworkDefinition definition,
            List<ReferencedFile> referencedFiles)
        {
            ICoverageEngine coverageEngine = GetConfiguredCoverageEngine(options, chutzpahTestSettings);
            if (coverageEngine != null)
            {
                var deps = coverageEngine.GetFileDependencies(definition);

                foreach (string item in deps)
                {
                    string sourcePath = fileProbe.GetPathInfo(Path.Combine(Constants.TestFileFolder, item)).FullPath;
                    ChutzpahTracer.TraceInformation(
                        "Added code coverage dependency '{0}' to referenced files",
                        sourcePath);
                    referencedFiles.Add(
                        new ReferencedFile
                        {
                            IsLocal = true,
                            IsCodeCoverageDependency = true,
                            Path = sourcePath,
                            IncludeInTestHarness = true
                        });
                }
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
            if (!options.CoverageOptions.Enabled && !chutzpahTestSettings.EnableCodeCoverage) return null;

            ChutzpahTracer.TraceInformation("Setting up code coverage in test context");
            mainCoverageEngine.IncludePatterns = chutzpahTestSettings.CodeCoverageIncludes.Concat(options.CoverageOptions.IncludePatterns).ToList();
            mainCoverageEngine.ExcludePatterns = chutzpahTestSettings.CodeCoverageExcludes.Concat(options.CoverageOptions.ExcludePatterns).ToList();
            return mainCoverageEngine;
        }

        private ReferencedFile GetFileUnderTest(string testFilePath, ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            return new ReferencedFile
            {
                Path = testFilePath,
                IsLocal = true,
                IsFileUnderTest = true,
                IncludeInTestHarness = chutzpahTestSettings.TestHarnessReferenceMode == TestHarnessReferenceMode.Normal
            };
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

        private string CreateTestHarness(
            IFrameworkDefinition definition,
            ChutzpahTestSettingsFile chutzpahTestSettings,
            TestOptions options,
            string inputTestFilePath,
            string testHarnessDirectory,
            IEnumerable<ReferencedFile> referencedFiles,
            ICoverageEngine coverageEngine,
            IList<string> temporaryFiles)
        {
            string testFilePathHash = hasher.Hash(inputTestFilePath);

            string testHtmlFilePath = Path.Combine(testHarnessDirectory, string.Format(Constants.ChutzpahTemporaryFileFormat, testFilePathHash, "test.html"));
            temporaryFiles.Add(testHtmlFilePath);

            var templatePath = GetTestHarnessTemplatePath(definition, chutzpahTestSettings);

            string testHtmlTemplate = fileSystem.GetText(templatePath);

            var harness = new TestHarness(chutzpahTestSettings, options, referencedFiles, fileSystem);

            if (coverageEngine != null)
            {
                coverageEngine.PrepareTestHarnessForCoverage(harness, definition);
            }

            string testFileContents = fileSystem.GetText(inputTestFilePath);
            var frameworkReplacements = definition.GetFrameworkReplacements(chutzpahTestSettings, inputTestFilePath, testFileContents)
                                        ?? new Dictionary<string, string>();

            string testHtmlText = harness.CreateHtmlText(testHtmlTemplate, frameworkReplacements);
            fileSystem.Save(testHtmlFilePath, testHtmlText);
            return testHtmlFilePath;
        }

        private string GetTestHarnessTemplatePath(IFrameworkDefinition definition, ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            string templatePath = null;

            if (!string.IsNullOrEmpty(chutzpahTestSettings.CustomTestHarnessPath))
            {
                // If CustomTestHarnessPath is absolute path then Path.Combine just returns it
                var harnessPath = Path.Combine(chutzpahTestSettings.SettingsFileDirectory, chutzpahTestSettings.CustomTestHarnessPath);
                var fullPath = fileProbe.FindFilePath(harnessPath);
                if (fullPath != null)
                {
                    ChutzpahTracer.TraceInformation("Using Custom Test Harness from {0}", fullPath);
                    templatePath = fullPath;
                }
                else
                {
                    ChutzpahTracer.TraceError("Cannot find Custom Test Harness at {0}", chutzpahTestSettings.CustomTestHarnessPath);
                }
            }

            if (templatePath == null)
            {
                templatePath = fileProbe.GetPathInfo(Path.Combine(Constants.TestFileFolder, definition.TestHarness)).FullPath;

                ChutzpahTracer.TraceInformation("Using builtin Test Harness from {0}", templatePath);
            }
            return templatePath;
        }

        private static string GetTestHarnessDirectory(ChutzpahTestSettingsFile chutzpahTestSettings, string inputTestFileDir)
        {
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
            return testHarnessDirectory;
        }
    }
}