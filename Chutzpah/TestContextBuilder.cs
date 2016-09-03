using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chutzpah.Coverage;
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
        private readonly IChutzpahTestSettingsService settingsService;
        private readonly ICoverageEngineFactory coverageEngineFactory;

        public TestContextBuilder(
            IFileSystemWrapper fileSystem,
            IReferenceProcessor referenceProcessor,
            IHttpWrapper httpWrapper,
            IFileProbe fileProbe,
            ICoverageEngineFactory coverageEngineFactory,
            IEnumerable<IFrameworkDefinition> frameworkDefinitions,
            IChutzpahTestSettingsService settingsService)
        {
            this.referenceProcessor = referenceProcessor;
            this.httpClient = httpWrapper;
            this.fileSystem = fileSystem;
            this.fileProbe = fileProbe;
            this.frameworkDefinitions = frameworkDefinitions;
            this.settingsService = settingsService;
            this.coverageEngineFactory = coverageEngineFactory;
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
            ChutzpahTracer.TraceInformation("Building test context for {0}", allFilePathString);

            // Make sure all test paths have been resolved to real files
            var missingPaths = files.Where(x => x.FullPath == null).ToList();
            if (missingPaths.Any())
            {
                throw new FileNotFoundException("Unable to find files: " + string.Join(",", missingPaths.Select(x => x.Path)));
            }

            // Make sure all test paths have a valid file type
            if (!files.Select(x => x.Type).All(IsValidTestPathType))
            {
                throw new ArgumentException("Expecting valid file or a url");
            }

            if (fileCount > 1 && files.Any(x => x.Type == PathType.Url || x.Type == PathType.Html))
            {
                throw new InvalidOperationException("Cannot build a batch context for Url or Html test files");
            }

            // We use the first file's directory to find the chutzpah.json file and to test framework 
            // since we assume all files in the batch must have the same values for those
            PathType firstFileKind = files.First().Type;
            string firstFilePath = files.First().FullPath;

            var chutzpahTestSettings = settingsService.FindSettingsFile(firstFilePath, options.ChutzpahSettingsFileEnvironments);

            // Exclude any files that are not included based on the settings file
            SettingsFileTestPath matchingTestSettingsPath;
            var testedPaths = files.Select(f => new PathWithTestSetting { 
                                                    File = f, 
                                                    IsIncluded = IsTestPathIncluded(f.FullPath, chutzpahTestSettings, out matchingTestSettingsPath), 
                                                    MatchingTestSetting = matchingTestSettingsPath }).ToList();
            if (testedPaths.Any(x => !x.IsIncluded))
            {
                var pathString = string.Join(",", testedPaths.Where(x => !x.IsIncluded).Select(x => x.File.FullPath));
                ChutzpahTracer.TraceInformation("Excluding test files {0} given chutzpah.json settings", pathString);
                testedPaths = testedPaths.Where(x => x.IsIncluded).ToList();

                if (!testedPaths.Any())
                {
                    return null;
                }
            }

            IFrameworkDefinition definition;

            if (TryDetectFramework(testedPaths.First().File, chutzpahTestSettings, out definition))
            {
                // For HTML test files we don't need to create a test harness to just return this file
                if (firstFileKind == PathType.Html || firstFileKind == PathType.Url)
                {
                    ChutzpahTracer.TraceInformation("Test kind is {0} so we are trusting the supplied test harness and not building our own", firstFileKind);

                    return new TestContext
                    {
                        ReferencedFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = firstFilePath } },
                        InputTestFiles = new[] { firstFilePath },
                        FirstInputTestFile = firstFilePath,
                        InputTestFilesString = firstFilePath,
                        TestHarnessPath = firstFilePath,
                        IsRemoteHarness = firstFileKind == PathType.Url,
                        TestRunner = definition.GetTestRunner(chutzpahTestSettings),
                    };
                }

                var temporaryFiles = new List<string>();

                string firstInputTestFileDir = Path.GetDirectoryName(firstFilePath);
                var testHarnessDirectory = GetTestHarnessDirectory(chutzpahTestSettings, firstInputTestFileDir);

                var referencedFiles = GetFilesUnderTest(testedPaths, chutzpahTestSettings).ToList();

                referenceProcessor.GetReferencedFiles(referencedFiles, definition, chutzpahTestSettings);

                IEnumerable<string> deps = definition.GetFileDependencies(chutzpahTestSettings);

                var coverageEngine = SetupCodeCoverageEngine(options, chutzpahTestSettings, definition, referencedFiles);

                AddTestFrameworkDependencies(deps, referencedFiles);

                var testFiles = referencedFiles.Where(x => x.IsFileUnderTest).Select(x => x.Path).ToList();
                return new TestContext
                {
                    FrameworkDefinition = definition,
                    CoverageEngine = coverageEngine,
                    InputTestFiles = testFiles,
                    FirstInputTestFile = testFiles.FirstOrDefault(),
                    InputTestFilesString = string.Join(",", testFiles),
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

        public bool IsTestFile(string file, ChutzpahSettingsFileEnvironments environments = null)
        {
            ChutzpahTracer.TraceInformation("Determining if {0} might be a test file", file);
            if (string.IsNullOrWhiteSpace(file))
            {
                return false;
            }

            var testFilePath = fileProbe.FindFilePath(file);

            if (testFilePath == null)
            {
                ChutzpahTracer.TraceInformation("Rejecting '{0}' since either it doesnt exist", file);
                return false;
            }

            var chutzpahTestSettings = settingsService.FindSettingsFile(testFilePath, environments);

            if (!IsTestPathIncluded(testFilePath, chutzpahTestSettings))
            {
                ChutzpahTracer.TraceInformation("Excluded test file '{0}' given chutzpah.json settings", testFilePath);
                return false;
            }

            // If the framework or tests filters are set in the settings file then no need to check for 
            // test framework
            if (!string.IsNullOrEmpty(chutzpahTestSettings.Framework) || chutzpahTestSettings.Tests.Any())
            {
                return true;
            }
            else
            {
                string testFileText = fileSystem.GetText(testFilePath);

                IFrameworkDefinition definition;
                var info = new PathInfo { Path = file, FullPath = testFilePath, Type = FileProbe.GetFilePathType(testFilePath) };
                var frameworkDetetected = TryDetectFramework(info, chutzpahTestSettings, out definition);

                if (frameworkDetetected)
                {
                    ChutzpahTracer.TraceInformation("Assuming '{0}' is a test file", file);
                }

                return frameworkDetetected;
            }

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

        private bool IsTestPathIncluded(string testFilePath, ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            SettingsFileTestPath ignored;
            return IsTestPathIncluded(testFilePath, chutzpahTestSettings, out ignored);
        }

        /// <summary>
        /// Matches the current test path against the Tests settings. The first setting to accept a file wins.
        /// </summary>
        private bool IsTestPathIncluded(string testFilePath, ChutzpahTestSettingsFile chutzpahTestSettings, out SettingsFileTestPath matchingTestPath)
        {
            matchingTestPath = null;

            // If those test filters are given then accept the test path
            if (!chutzpahTestSettings.Tests.Any())
            {
                return true;
            }

            testFilePath = UrlBuilder.NormalizeFilePath(testFilePath);

            foreach (var pathSettings in chutzpahTestSettings.Tests.Where(x => x != null))
            {
                var includePatterns = pathSettings.Includes.Select(x => UrlBuilder.NormalizeFilePath(x)).ToList();
                var excludePatterns = pathSettings.Excludes.Select(x => UrlBuilder.NormalizeFilePath(x)).ToList();

                // The path we assume default to the chuzpah.json directory if the Path property is not set
                var testPath = string.IsNullOrEmpty(pathSettings.Path) ? pathSettings.SettingsFileDirectory : pathSettings.Path;
                testPath = UrlBuilder.NormalizeFilePath(testPath);
                testPath = testPath != null ? Path.Combine(pathSettings.SettingsFileDirectory, testPath) : null;

                // If a file path is given just match the test file against it to see if we should urn
                var filePath = fileProbe.FindFilePath(testPath);
                if (filePath != null)
                {
                    if (filePath.Equals(testFilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingTestPath = pathSettings;
                        ChutzpahTracer.TraceInformation("Test file {0} matched test file path from settings file", testFilePath);
                        return true;
                    }
                }

                // If a folder path is given then match the test file path that is in that folder with the optional include/exclude paths
                var folderPath = UrlBuilder.NormalizeFilePath(fileProbe.FindFolderPath(testPath));
                if (folderPath != null)
                {
                    if (testFilePath.Contains(folderPath))
                    {
                        var shouldIncludeFile = (!includePatterns.Any() || includePatterns.Any(pat => NativeImports.PathMatchSpec(testFilePath, pat)))
                                             && (!excludePatterns.Any() || !excludePatterns.Any(pat => NativeImports.PathMatchSpec(testFilePath, pat)));

                        if (shouldIncludeFile)
                        {
                            ChutzpahTracer.TraceInformation(
                                "Test file {0} matched folder {1} with includes {2} and excludes {3} patterns from settings file",
                                testFilePath,
                                folderPath,
                                string.Join(",", includePatterns),
                                string.Join(",", excludePatterns));


                            matchingTestPath = pathSettings;
                            return true;
                        }
                        else
                        {
                            ChutzpahTracer.TraceInformation(
                                "Test file {0} did not match folder {1} with includes {2} and excludes {3} patterns from settings file",
                                testFilePath,
                                folderPath,
                                string.Join(",", includePatterns),
                                string.Join(",", excludePatterns));
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
                string sourcePath = Path.Combine(fileProbe.BuiltInDependencyDirectory, item);
                ChutzpahTracer.TraceInformation("Added framework dependency '{0}' to referenced files", sourcePath);
                referencedFiles.Insert(0, new ReferencedFile { IsLocal = true, IsTestFrameworkFile = true, Path = sourcePath, IncludeInTestHarness = true, IsBuiltInDependency = true });
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
                    string sourcePath = Path.Combine(fileProbe.BuiltInDependencyDirectory, item);
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
                   || testFileKind == PathType.Url
                   || testFileKind == PathType.Html;
        }

        private ICoverageEngine GetConfiguredCoverageEngine(TestOptions options, ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            // Don't run code coverage if in discovery mode
            if (options.TestExecutionMode == TestExecutionMode.Discovery) return null;

            var codeCoverageEnabled = options.CoverageOptions.ShouldRunCoverage(chutzpahTestSettings.CodeCoverageExecutionMode);
            if (!codeCoverageEnabled) return null;

            ChutzpahTracer.TraceInformation("Setting up code coverage in test context");
            var coverageEngine = coverageEngineFactory.CreateCoverageEngine();
            coverageEngine.ClearPatterns();
            coverageEngine.AddIncludePatterns(chutzpahTestSettings.CodeCoverageIncludes.Concat(options.CoverageOptions.IncludePatterns));
            coverageEngine.AddExcludePatterns(chutzpahTestSettings.CodeCoverageExcludes.Concat(options.CoverageOptions.ExcludePatterns));
            coverageEngine.AddIgnorePatterns(chutzpahTestSettings.CodeCoverageIgnores.Concat(options.CoverageOptions.IgnorePatterns));
            return coverageEngine;
        }

        private IEnumerable<ReferencedFile> GetFilesUnderTest(IEnumerable<PathWithTestSetting> testFiles, ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            return testFiles.Select(f => new ReferencedFile
            {
                Path = f.File.FullPath,
                IsLocal = true,
                IsFileUnderTest = true,
                // Expand reference comments if we either do not have a matching test path setting or the user explictly asked to do it
                ExpandReferenceComments = f.MatchingTestSetting == null || f.MatchingTestSetting.ExpandReferenceComments,
                IncludeInTestHarness = chutzpahTestSettings.TestHarnessReferenceMode == TestHarnessReferenceMode.Normal
            });
        }

        private bool TryDetectFramework(PathInfo path, ChutzpahTestSettingsFile chutzpahTestSettings, out IFrameworkDefinition definition)
        {
            // TODO: Deprecate the fallback approach
            Lazy<string> fileText = new Lazy<string>(() =>
            {
                string firstTestFileText;
                if (path.Type == PathType.Url)
                {
                    firstTestFileText = httpClient.GetContent(path.FullPath);
                }
                else
                {
                    firstTestFileText = fileSystem.GetText(path.FullPath);
                }

                return firstTestFileText;
            });


            var strategies = new Func<IFrameworkDefinition>[]
            {
                // Check chutzpah settings
                () => frameworkDefinitions.FirstOrDefault(x => x.FrameworkKey.Equals(chutzpahTestSettings.Framework, StringComparison.OrdinalIgnoreCase)),

                // Check if we see an explicit reference to a framework file (e.g. <reference path="qunit.js" />)
                () => frameworkDefinitions.FirstOrDefault(x => x.FileUsesFramework(fileText.Value, false, path.Type)),

                // Check using basic heuristic like looking for test( or module( for QUnit
                () => frameworkDefinitions.FirstOrDefault(x => x.FileUsesFramework(fileText.Value, true, path.Type))
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

        private class PathWithTestSetting
        {
            public PathInfo File { get; set; }
            public SettingsFilePath MatchingTestSetting { get; set; }
            public bool IsIncluded { get; set; }
        }
    }
}