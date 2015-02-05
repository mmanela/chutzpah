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
        private readonly IChutzpahTestSettingsService settingsService;
        private readonly ICoverageEngine mainCoverageEngine;

        public TestContextBuilder(
            IFileSystemWrapper fileSystem,
            IReferenceProcessor referenceProcessor,
            IHttpWrapper httpWrapper,
            IFileProbe fileProbe,
            ICoverageEngine coverageEngine,
            IEnumerable<IFrameworkDefinition> frameworkDefinitions,
            IEnumerable<IFileGenerator> fileGenerators,
            IChutzpahTestSettingsService settingsService)
        {
            this.referenceProcessor = referenceProcessor;
            this.httpClient = httpWrapper;
            this.fileSystem = fileSystem;
            this.fileProbe = fileProbe;
            this.frameworkDefinitions = frameworkDefinitions;
            this.fileGenerators = fileGenerators;
            this.settingsService = settingsService;
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
            return BuildContext(new List<PathInfo> { file }, options);
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

        public TestContext BuildContext(IEnumerable<PathInfo> files, TestOptions options)
        {
            if (files == null)
            {
                throw new ArgumentNullException("testFilePathInfo");
            }

            if (!files.Any())
            {
                ChutzpahTracer.TraceInformation("No files given to build test context for");
                return null;
            }

            var fileCount = files.Count();

            var allFilePathString = string.Join(",", files.Select(x => x.FullPath));
            ChutzpahTracer.TraceInformation("Building test context for '{0}'", allFilePathString);

            // Make sure all test paths have been resolved to real files
            var missingPaths = files.Where(x => x.FullPath == null).ToList();
            if (missingPaths.Any())
            {
                throw new FileNotFoundException("Unable to find files: " + string.Join(",",missingPaths.Select(x => x.Path)));
            }

            // Make sure all test paths have a valid file type
            if (!files.Select(x => x.Type).All(IsValidTestPathType))
            {
                throw new ArgumentException("Expecting a .js, .ts, .coffee or .html file or a url");
            }

            if(fileCount > 1 && files.Any(x => x.Type == PathType.Url || x.Type == PathType.Html))
            {
                throw new InvalidOperationException("Cannot build a batch context for Url or Html test files");
            }

            // We use the first file's directory to find the chutzpah.json file and to test framework 
            // since we assume all files in the batch must have the same values for those
            PathType firstFileKind = files.First().Type;
            string firstFilePath = files.First().FullPath;

            var firstTestFileDirectory = Path.GetDirectoryName(firstFilePath);
            var chutzpahTestSettings = settingsService.FindSettingsFile(firstTestFileDirectory);

            // Exclude any files that are not included based on the settings file
            var testedPaths = files.Select(f => new { File = f, IsIncluded = IsTestPathIncluded(f.FullPath, chutzpahTestSettings) }).ToList();
            if (testedPaths.Any(x => !x.IsIncluded))
            {
                var pathString = string.Join(",",testedPaths.Where(x => !x.IsIncluded).Select(x => x.File.FullPath));
                ChutzpahTracer.TraceInformation("Excluding test files {0} given chutzpah.json settings", pathString);
                files = testedPaths.Where(x => x.IsIncluded).Select(x => x.File).ToList();
            }

            string firstTestFileText;
            if (firstFileKind == PathType.Url)
            {
                firstTestFileText = httpClient.GetContent(firstFilePath);
            }
            else
            {
                firstTestFileText = fileSystem.GetText(firstFilePath);
            }

            IFrameworkDefinition definition;

            if (TryDetectFramework(firstTestFileText, firstFileKind, chutzpahTestSettings, out definition))
            {
                // For HTML test files we don't need to create a test harness to just return this file
                if (firstFileKind == PathType.Html || firstFileKind == PathType.Url)
                {
                    ChutzpahTracer.TraceInformation("Test kind is {0} so we are trusting the supplied test harness and not building our own", firstFileKind);

                    return new TestContext
                    {
                        InputTestFiles = new []{ firstFilePath },
                        InputTestFilesDisplayString = firstFilePath,
                        TestHarnessPath = firstFilePath,
                        IsRemoteHarness = firstFileKind == PathType.Url,
                        TestRunner = definition.GetTestRunner(chutzpahTestSettings),
                    };
                }

                var temporaryFiles = new List<string>();

                string firstInputTestFileDir = Path.GetDirectoryName(firstFilePath);
                var testHarnessDirectory = GetTestHarnessDirectory(chutzpahTestSettings, firstInputTestFileDir);

                var referencedFiles = GetFilesUnderTest(files, chutzpahTestSettings).ToList();

                referenceProcessor.GetReferencedFiles(referencedFiles, definition, chutzpahTestSettings);

                // This is the legacy way Chutzpah compiled files that are TypeScript or CoffeeScript
                // Remaining but will eventually be removed
                ProcessForFilesGeneration(referencedFiles, temporaryFiles, chutzpahTestSettings);

                IEnumerable<string> deps = definition.GetFileDependencies(chutzpahTestSettings);

                var coverageEngine = SetupCodeCoverageEngine(options, chutzpahTestSettings, definition, referencedFiles);

                AddTestFrameworkDependencies(deps, referencedFiles);

                var testFiles = referencedFiles.Where(x => x.IsFileUnderTest).Select(x => x.Path).ToList();
                return new TestContext
                {
                    FrameworkDefinition = definition,
                    CoverageEngine = coverageEngine,
                    InputTestFiles = testFiles,
                    InputTestFilesDisplayString = testFiles.FirstOrDefault(),
                    TestHarnessDirectory = testHarnessDirectory,
                    ReferencedFiles = referencedFiles,
                    TestRunner = definition.GetTestRunner(chutzpahTestSettings),
                    TemporaryFiles = temporaryFiles,
                    TestFileSettings = chutzpahTestSettings
                };
            }
            else
            {
                ChutzpahTracer.TraceWarning("Failed to detect test framework for '{0}'", firstFilePath);
            }

            return null;
        }

        public TestContext BuildContext(IEnumerable<string> files, TestOptions options)
        {
            if (files == null)
            {
                throw new ArgumentNullException("files");
            }

            return BuildContext(files.Select(fileProbe.GetPathInfo), options);
        }

        public bool TryBuildContext(IEnumerable<PathInfo> files, TestOptions options, out TestContext context)
        {
            context = BuildContext(files, options);
            return context != null;
        }

        public bool TryBuildContext(IEnumerable<string> files, TestOptions options, out TestContext context)
        {
            context = BuildContext(files, options);
            return context != null;
        }

        public bool IsTestFile(string file)
        {
            ChutzpahTracer.TraceInformation("Determining if '{0}' might be a test file", file);
            if (string.IsNullOrWhiteSpace(file))
            {
                return false;
            }

            PathInfo pathInfo = fileProbe.GetPathInfo(file);
            PathType testFileKind = pathInfo.Type;
            string testFilePath = pathInfo.FullPath;

            if (testFilePath == null || !IsValidTestPathType(testFileKind))
            {
                ChutzpahTracer.TraceInformation("Rejecting '{0}' since either it doesnt exist or does not have test extension", file);
                return false;
            }

            var testFileDirectory = Path.GetDirectoryName(testFilePath);
            var chutzpahTestSettings = settingsService.FindSettingsFile(testFileDirectory);

            if (!IsTestPathIncluded(testFilePath, chutzpahTestSettings))
            {
                ChutzpahTracer.TraceInformation("Excluded test file '{0}' given chutzpah.json settings", testFilePath);
                return false;
            }

            string testFileText = fileSystem.GetText(testFilePath);

            IFrameworkDefinition definition;
            var frameworkDetetected = TryDetectFramework(testFileText, testFileKind, chutzpahTestSettings, out definition);

            if (frameworkDetetected)
            {
                ChutzpahTracer.TraceInformation("Assuming '{0}' is a test file", file);
            }

            return frameworkDetetected;
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

        /// <summary>
        /// Matches the current test path against the Tests settings. The first setting to accept a file wins.
        /// </summary>
        /// <param name="testFilePath"></param>
        /// <param name="chutzpahTestSettings"></param>
        /// <returns></returns>
        private bool IsTestPathIncluded(string testFilePath, ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            // If those test filters are given then accept the test path
            if (!chutzpahTestSettings.Tests.Any())
            {
                return true;
            }

            testFilePath = FileProbe.NormalizeFilePath(testFilePath);

            foreach (var pathSettings in chutzpahTestSettings.Tests.Where(x => x != null))
            {
                var includePattern = FileProbe.NormalizeFilePath(pathSettings.Include);
                var excludePattern = FileProbe.NormalizeFilePath(pathSettings.Exclude);

                // The path we assume default to the chuzpah.json directory if the Path property is not set
                var testPath = string.IsNullOrEmpty(pathSettings.Path) ? pathSettings.SettingsFileDirectory : pathSettings.Path;
                testPath = FileProbe.NormalizeFilePath(testPath);
                testPath = testPath != null ? Path.Combine(pathSettings.SettingsFileDirectory, testPath) : null;

                // If a file path is given just match the test file against it to see if we should urn
                var filePath = fileProbe.FindFilePath(testPath);
                if (filePath != null)
                {
                    if (filePath.Equals(testFilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        ChutzpahTracer.TraceInformation("Test file {0} matched test file path from settings file", testFilePath);
                        return true;
                    }
                }

                // If a folder path is given then match the test file path that is in that folder with the optional include/exclude paths
                var folderPath = FileProbe.NormalizeFilePath(fileProbe.FindFolderPath(testPath));
                if (folderPath != null)
                {
                    if (testFilePath.Contains(folderPath))
                    {
                        var shouldIncludeFile = (includePattern == null || NativeImports.PathMatchSpec(testFilePath, includePattern))
                                                && (excludePattern == null || !NativeImports.PathMatchSpec(testFilePath, excludePattern));

                        if (shouldIncludeFile)
                        {
                            ChutzpahTracer.TraceInformation(
                                "Test file {0} matched folder {1} with include {2} and exclude {3} patterns from settings file",
                                testFilePath,
                                folderPath,
                                includePattern,
                                excludePattern);
                            return true;
                        }
                        else
                        {
                            ChutzpahTracer.TraceInformation(
                                "Test file {0} did not match folder {1} with include {2} and exclude {3} patterns from settings file",
                                testFilePath,
                                folderPath,
                                includePattern,
                                excludePattern);
                        }
                    }
                }
            }

            return false;
        }

        private void AddTestFrameworkDependencies(IEnumerable<string> deps, List<ReferencedFile> referencedFiles)
        {
            foreach (string item in deps.Reverse())
            {
                string sourcePath = fileProbe.GetPathInfo(Path.Combine(Constants.TestFileFolder, item)).FullPath;
                ChutzpahTracer.TraceInformation("Added framework dependency '{0}' to referenced files", sourcePath);
                referencedFiles.Insert(0, new ReferencedFile { IsLocal = true, IsTestFrameworkFile = true, Path = sourcePath, IncludeInTestHarness = true });
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
                var deps = coverageEngine.GetFileDependencies(definition, chutzpahTestSettings);

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


        private static bool IsValidTestPathType(PathType testFileKind)
        {
            return testFileKind == PathType.JavaScript
                   || testFileKind == PathType.TypeScript
                   || testFileKind == PathType.CoffeeScript
                   || testFileKind == PathType.Url
                   || testFileKind == PathType.Html;
        }

        /// <summary>
        /// Iterates over filegenerators letting the generators decide if they handle any files
        /// </summary>
        private void ProcessForFilesGeneration(List<ReferencedFile> referencedFiles, List<string> temporaryFiles, ChutzpahTestSettingsFile chutzpahTestSettings)
        {

            if (chutzpahTestSettings.Compile != null)
            {
                ChutzpahTracer.TraceInformation("Ignoring old style file compilation since we detected the new compile setting");
                return;
            }
            ChutzpahTracer.TraceInformation("Starting legacy file compilation/generation");

            foreach (var fileGenerator in fileGenerators)
            {
                fileGenerator.Generate(referencedFiles, temporaryFiles, chutzpahTestSettings);
            }

            ChutzpahTracer.TraceInformation("Finished legacy file compilation/generation");
        }

        private ICoverageEngine GetConfiguredCoverageEngine(TestOptions options, ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            // Don't run code coverage if in discovery mode
            if (options.TestExecutionMode == TestExecutionMode.Discovery) return null;

            var codeCoverageEnabled = (!chutzpahTestSettings.EnableCodeCoverage.HasValue && options.CoverageOptions.Enabled)
                                      || (chutzpahTestSettings.EnableCodeCoverage.HasValue && chutzpahTestSettings.EnableCodeCoverage.Value);
            if (!codeCoverageEnabled) return null;

            ChutzpahTracer.TraceInformation("Setting up code coverage in test context");
            mainCoverageEngine.ClearPatterns();
            mainCoverageEngine.AddIncludePatterns(chutzpahTestSettings.CodeCoverageIncludes.Concat(options.CoverageOptions.IncludePatterns));
            mainCoverageEngine.AddExcludePatterns(chutzpahTestSettings.CodeCoverageExcludes.Concat(options.CoverageOptions.ExcludePatterns));
            return mainCoverageEngine;
        }

        private IEnumerable<ReferencedFile> GetFilesUnderTest(IEnumerable<PathInfo> testFiles, ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            return testFiles.Select(f => new ReferencedFile
            {
                Path = f.FullPath,
                IsLocal = true,
                IsFileUnderTest = true,
                IncludeInTestHarness = chutzpahTestSettings.TestHarnessReferenceMode == TestHarnessReferenceMode.Normal
            });
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